// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Transactions;
using System.Xml.Linq;

namespace System.Activities;
using Hosting;
using Internals;
using Runtime;
using Runtime.DurableInstancing;

public partial class WorkflowApplication
{

    private class InvokeAsyncResult : AsyncResult
    {
        private static Action<object, TimeoutException> waitCompleteCallback;
        private readonly WorkflowApplication _instance;
        private readonly AsyncWaitHandle _completionWaiter;
        private IDictionary<string, object> _outputs;
        private Exception _completionException;

        public InvokeAsyncResult(
            Activity activity,
            IDictionary<string, object> inputs,
            WorkflowInstanceExtensionManager extensions,
            TimeSpan timeout,
            SynchronizationContext syncContext,
            AsyncInvokeContext invokeContext,
            AsyncCallback callback,
            object state)
            : base(callback, state)
        {
            Fx.Assert(activity != null, "Need an activity");

            _completionWaiter = new AsyncWaitHandle();
            syncContext ??= SynchronousSynchronizationContext.Value;

            _instance = StartInvoke(activity, inputs, extensions, syncContext, new Action(OnInvokeComplete), invokeContext);

            if (_completionWaiter.WaitAsync(WaitCompleteCallback, this, timeout))
            {
                bool completeSelf = OnWorkflowCompletion();

                if (completeSelf)
                {
                    if (_completionException != null)
                    {
                        throw FxTrace.Exception.AsError(_completionException);
                    }
                    else
                    {
                        Complete(true);
                    }
                }
            }
        }

        private static Action<object, TimeoutException> WaitCompleteCallback
        {
            get
            {
                waitCompleteCallback ??= new Action<object, TimeoutException>(OnWaitComplete);
                return waitCompleteCallback;
            }
        }

        public static IDictionary<string, object> End(IAsyncResult result)
        {
            InvokeAsyncResult thisPtr = End<InvokeAsyncResult>(result);
            return thisPtr._outputs;
        }

        private void OnInvokeComplete() => _completionWaiter.Set();

        private static void OnWaitComplete(object state, TimeoutException asyncException)
        {
            InvokeAsyncResult thisPtr = (InvokeAsyncResult)state;

            if (asyncException != null)
            {
                thisPtr._instance.Abort(SR.AbortingDueToInstanceTimeout);
                thisPtr.Complete(false, asyncException);
                return;
            }

            bool completeSelf = true;

            try
            {
                completeSelf = thisPtr.OnWorkflowCompletion();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                thisPtr._completionException = e;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, thisPtr._completionException);
            }
        }

        private bool OnWorkflowCompletion()
        {
            if (_instance.Controller.State == WorkflowInstanceState.Aborted)
            {
                _completionException = new WorkflowApplicationAbortedException(SR.DefaultAbortReason, _instance.Controller.GetAbortReason());
            }
            else
            {
                Fx.Assert(_instance.Controller.State == WorkflowInstanceState.Complete, "We should only get here when we are completed.");

                _instance.Controller.GetCompletionState(out _outputs, out _completionException);
            }

            return true;
        }

    }

    private class ResumeBookmarkAsyncResult : AsyncResult
    {
        private static readonly AsyncCompletion resumedCallback = new(OnResumed);
        private static readonly Action<object, TimeoutException> waitCompleteCallback = new(OnWaitComplete);
        private static readonly AsyncCompletion trackingCompleteCallback = new(OnTrackingComplete);
        private readonly WorkflowApplication _instance;
        private readonly Bookmark _bookmark;
        private readonly object _value;
        private BookmarkResumptionResult _resumptionResult;
        private TimeoutHelper _timeoutHelper;
        private readonly bool _isFromExtension;
        private bool _pendedUnenqueued;
        private InstanceOperation _currentOperation;

        public ResumeBookmarkAsyncResult(WorkflowApplication instance, Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
            : this(instance, bookmark, value, false, timeout, callback, state) { }

        public ResumeBookmarkAsyncResult(WorkflowApplication instance, Bookmark bookmark, object value, bool isFromExtension, TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _instance = instance;
            _bookmark = bookmark;
            _value = value;
            _isFromExtension = isFromExtension;
            _timeoutHelper = new TimeoutHelper(timeout);

            bool completeSelf = false;
            bool success = false;

            OnCompleting = new Action<AsyncResult, Exception>(Finally);

            try
            {
                if (!_instance._hasCalledRun && !_isFromExtension)
                {
                    // Increment the pending unenqueued count so we don't raise idle in the time between
                    // when the Run completes and when we enqueue our InstanceOperation.
                    _pendedUnenqueued = true;
                    _instance.IncrementPendingUnenqueud();

                    IAsyncResult result = _instance.BeginInternalRun(_timeoutHelper.RemainingTime(), false, PrepareAsyncCompletion(resumedCallback), this);
                    if (result.CompletedSynchronously)
                    {
                        completeSelf = OnResumed(result);
                    }
                }
                else
                {
                    completeSelf = StartResumptionLoop();
                }

                success = true;
            }
            finally
            {
                // We only want to call this if we are throwing.  Otherwise OnCompleting will take care of it.
                if (!success)
                {
                    Finally(null, null);
                }
            }

            if (completeSelf)
            {
                Complete(true);
            }
        }

        public static BookmarkResumptionResult End(IAsyncResult result)
        {
            ResumeBookmarkAsyncResult thisPtr = End<ResumeBookmarkAsyncResult>(result);

            return thisPtr._resumptionResult;
        }

        private void ClearPendedUnenqueued()
        {
            if (_pendedUnenqueued)
            {
                _pendedUnenqueued = false;
                _instance.DecrementPendingUnenqueud();
            }
        }

        private void NotifyOperationComplete()
        {
            InstanceOperation lastOperation = _currentOperation;
            _currentOperation = null;
            _instance.NotifyOperationComplete(lastOperation);
        }

        private void Finally(AsyncResult result, Exception completionException)
        {
            ClearPendedUnenqueued();
            NotifyOperationComplete();
        }

        private static bool OnResumed(IAsyncResult result)
        {
            ResumeBookmarkAsyncResult thisPtr = (ResumeBookmarkAsyncResult)result.AsyncState;
            thisPtr._instance.EndRun(result);
            return thisPtr.StartResumptionLoop();
        }

        private bool StartResumptionLoop()
        {
            _currentOperation = new RequiresIdleOperation(_isFromExtension);
            return WaitOnCurrentOperation();
        }

        private bool WaitOnCurrentOperation()
        {
            bool stillSync = true;
            bool tryOneMore = true;

            while (tryOneMore)
            {
                tryOneMore = false;

                Fx.Assert(_currentOperation != null, "We should always have a current operation here.");

                if (_instance.WaitForTurnAsync(_currentOperation, _timeoutHelper.RemainingTime(), waitCompleteCallback, this))
                {
                    ClearPendedUnenqueued();

                    if (CheckIfBookmarksAreInvalid())
                    {
                        stillSync = true;
                    }
                    else
                    {
                        stillSync = ProcessResumption();

                        tryOneMore = _resumptionResult == BookmarkResumptionResult.NotReady;
                    }
                }
                else
                {
                    stillSync = false;
                }
            }

            return stillSync;
        }

        private static void OnWaitComplete(object state, TimeoutException asyncException)
        {
            ResumeBookmarkAsyncResult thisPtr = (ResumeBookmarkAsyncResult)state;

            if (asyncException != null)
            {
                thisPtr.Complete(false, asyncException);
                return;
            }

            Exception completionException = null;
            bool completeSelf;
            try
            {
                thisPtr.ClearPendedUnenqueued();

                if (thisPtr.CheckIfBookmarksAreInvalid())
                {
                    completeSelf = true;
                }
                else
                {
                    completeSelf = thisPtr.ProcessResumption();

                    if (thisPtr._resumptionResult == BookmarkResumptionResult.NotReady)
                    {
                        completeSelf = thisPtr.WaitOnCurrentOperation();
                    }
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                completeSelf = true;
                completionException = e;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        private bool CheckIfBookmarksAreInvalid() => _instance.AreBookmarksInvalid(out _resumptionResult);

        private bool ProcessResumption()
        {
            bool stillSync = true;

            _resumptionResult = _instance.ResumeBookmarkCore(_bookmark, _value);

            if (_resumptionResult == BookmarkResumptionResult.Success)
            {
                if (_instance.Controller.HasPendingTrackingRecords)
                {
                    IAsyncResult result = _instance.Controller.BeginFlushTrackingRecords(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(trackingCompleteCallback), this);

                    if (result.CompletedSynchronously)
                    {
                        stillSync = OnTrackingComplete(result);
                    }
                    else
                    {
                        stillSync = false;
                    }
                }
            }
            else if (_resumptionResult == BookmarkResumptionResult.NotReady)
            {
                NotifyOperationComplete();
                _currentOperation = new DeferredRequiresIdleOperation();
            }

            return stillSync;
        }

        private static bool OnTrackingComplete(IAsyncResult result)
        {
            ResumeBookmarkAsyncResult thisPtr = (ResumeBookmarkAsyncResult)result.AsyncState;

            thisPtr._instance.Controller.EndFlushTrackingRecords(result);

            return true;
        }
    }

    private class UnloadOrPersistAsyncResult : TransactedAsyncResult
    {
        private static readonly Action<object, TimeoutException> waitCompleteCallback = new(OnWaitComplete);
        private static readonly AsyncCompletion savedCallback = new(OnSaved);
        private static readonly AsyncCompletion persistedCallback = new(OnPersisted);
        private static readonly AsyncCompletion initializedCallback = new(OnProviderInitialized);
        private static readonly AsyncCompletion readynessEnsuredCallback = new(OnProviderReadynessEnsured);
        private static readonly AsyncCompletion trackingCompleteCallback = new(OnTrackingComplete);
        private static readonly AsyncCompletion deleteOwnerCompleteCallback = new(OnOwnerDeleted);
        private static readonly AsyncCompletion completeContextCallback = new(OnCompleteContext);
        private static readonly Action<AsyncResult, Exception> completeCallback = new(OnComplete);
        private readonly DependentTransaction _dependentTransaction;
        private readonly WorkflowApplication _instance;
        private readonly bool _isUnloaded;
        private TimeoutHelper _timeoutHelper;
        private PersistenceOperation _operation;
        private RequiresPersistenceOperation _instanceOperation;
        private WorkflowPersistenceContext _context;
        private IDictionary<XName, InstanceValue> _data;
        private PersistencePipeline _pipeline;
        private readonly bool _isInternalPersist;

        public UnloadOrPersistAsyncResult(WorkflowApplication instance, TimeSpan timeout, PersistenceOperation operation,
            bool isWorkflowThread, bool isInternalPersist, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _instance = instance;
            _timeoutHelper = new TimeoutHelper(timeout);
            _operation = operation;
            _isInternalPersist = isInternalPersist;
            _isUnloaded = (operation == PersistenceOperation.Unload || operation == PersistenceOperation.Complete);

            OnCompleting = completeCallback;

            bool completeSelf;
            bool success = false;

            // Save off the current transaction in case we have an async operation before we end up creating
            // the WorkflowPersistenceContext and create it on another thread. Do a blocking dependent clone that
            // we will complete when we are completed.
            //
            // This will throw TransactionAbortedException by design, if the transaction is already rolled back.
            Transaction currentTransaction = Transaction.Current;
            if (currentTransaction != null)
            {
                _dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
            }

            try
            {
                if (isWorkflowThread)
                {
                    Fx.Assert(_instance.Controller.IsPersistable, "The runtime won't schedule this work item unless we've passed the guard");

                    // We're an internal persistence on the workflow thread which means
                    // that we are passed the guard already, we have the lock, and we know
                    // we aren't detached.

                    completeSelf = InitializeProvider();
                    success = true;
                }
                else
                {
                    _instanceOperation = new RequiresPersistenceOperation();
                    try
                    {
                        if (_instance.WaitForTurnAsync(_instanceOperation, _timeoutHelper.RemainingTime(), waitCompleteCallback, this))
                        {
                            completeSelf = ValidateState();
                        }
                        else
                        {
                            completeSelf = false;
                        }
                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            NotifyOperationComplete();
                        }
                    }
                }
            }
            finally
            {
                // If we had an exception, we need to complete the dependent transaction.
                if (!success)
                {
                    _dependentTransaction?.Complete();
                }
            }

            if (completeSelf)
            {
                Complete(true);
            }
        }

        private bool ValidateState()
        {
            bool alreadyUnloaded = false;
            if (_operation == PersistenceOperation.Unload)
            {
                _instance.ValidateStateForUnload();
                alreadyUnloaded = _instance._state == WorkflowApplicationState.Unloaded;
            }
            else
            {
                _instance.ValidateStateForPersist();
            }

            return alreadyUnloaded || InitializeProvider();
        }

        private static void OnWaitComplete(object state, TimeoutException asyncException)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)state;
            if (asyncException != null)
            {
                thisPtr.Complete(false, asyncException);
                return;
            }

            bool completeSelf;
            Exception completionException = null;

            try
            {
                completeSelf = thisPtr.ValidateState();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                completionException = e;
                completeSelf = true;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        private bool InitializeProvider()
        {
            // We finally have the lock and are passed the guard.  Let's update our operation if this is an Unload.
            if (_operation == PersistenceOperation.Unload && _instance.Controller.State == WorkflowInstanceState.Complete)
            {
                _operation = PersistenceOperation.Complete;
            }

            if (_instance.HasPersistenceProvider && !_instance._persistenceManager.IsInitialized)
            {
                IAsyncResult result = _instance._persistenceManager.BeginInitialize(_instance.DefinitionIdentity, _timeoutHelper.RemainingTime(),
                    PrepareAsyncCompletion(initializedCallback), this);
                return SyncContinue(result);
            }
            else
            {
                return EnsureProviderReadyness();
            }
        }

        private static bool OnProviderInitialized(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            thisPtr._instance._persistenceManager.EndInitialize(result);
            return thisPtr.EnsureProviderReadyness();
        }

        private bool EnsureProviderReadyness()
        {
            if (_instance.HasPersistenceProvider && !_instance._persistenceManager.IsLocked && _dependentTransaction != null)
            {
                IAsyncResult result = _instance._persistenceManager.BeginEnsureReadyness(_timeoutHelper.RemainingTime(),
                    PrepareAsyncCompletion(readynessEnsuredCallback), this);
                return SyncContinue(result);
            }
            else
            {
                return Track();
            }
        }

        private static bool OnProviderReadynessEnsured(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            thisPtr._instance._persistenceManager.EndEnsureReadyness(result);
            return thisPtr.Track();
        }

        public static void End(IAsyncResult result) => End<UnloadOrPersistAsyncResult>(result);

        private void NotifyOperationComplete()
        {
            RequiresPersistenceOperation localInstanceOperation = _instanceOperation;
            _instanceOperation = null;
            _instance.NotifyOperationComplete(localInstanceOperation);
        }

        private bool Track()
        {
            // Do the tracking before preparing in case the tracking data is being pushed into
            // an extension and persisted transactionally with the instance state.

            if (_instance.HasPersistenceProvider)
            {
                // We only track the persistence operation if we actually
                // are persisting (and not just hitting PersistenceParticipants)
                _instance.TrackPersistence(_operation);
            }

            if (_instance.Controller.HasPendingTrackingRecords)
            {
                TimeSpan flushTrackingRecordsTimeout;

                if (_isInternalPersist)
                {
                    // If we're an internal persist we're using TimeSpan.MaxValue
                    // for our persistence and we want to use a smaller timeout
                    // for tracking
                    flushTrackingRecordsTimeout = ActivityDefaults.TrackingTimeout;
                }
                else
                {
                    flushTrackingRecordsTimeout = _timeoutHelper.RemainingTime();
                }

                IAsyncResult result = _instance.Controller.BeginFlushTrackingRecords(flushTrackingRecordsTimeout, PrepareAsyncCompletion(trackingCompleteCallback), this);
                return SyncContinue(result);
            }

            return CollectAndMap();
        }

        private static bool OnTrackingComplete(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            thisPtr._instance.Controller.EndFlushTrackingRecords(result);
            return thisPtr.CollectAndMap();
        }

        private bool CollectAndMap()
        {
            bool success = false;
            try
            {
                if (_instance.HasPersistenceModule)
                {
                    IEnumerable<IPersistencePipelineModule> modules = _instance.GetExtensions<IPersistencePipelineModule>();
                    _pipeline = new PersistencePipeline(modules, PersistenceManager.GenerateInitialData(_instance));
                    _pipeline.Collect();
                    _pipeline.Map();
                    _data = _pipeline.Values;
                }
                success = true;
            }
            finally
            {
                if (!success && _context != null)
                {
                    _context.Abort();
                }
            }

            return _instance.HasPersistenceProvider ? Persist() : Save();
        }

        private bool Persist()
        {
            IAsyncResult result = null;
            try
            {
                _data ??= PersistenceManager.GenerateInitialData(_instance);
                _context ??= new WorkflowPersistenceContext(_pipeline != null && _pipeline.IsSaveTransactionRequired,
                    _dependentTransaction, _timeoutHelper.OriginalTimeout);

                using (PrepareTransactionalCall(_context.PublicTransaction))
                {
                    result = _instance._persistenceManager.BeginSave(_data, _operation, _timeoutHelper.RemainingTime(), PrepareAsyncCompletion(persistedCallback), this);
                }
            }
            finally
            {
                if (result == null && _context != null)
                {
                    _context.Abort();
                }
            }
            return SyncContinue(result);
        }

        private static bool OnPersisted(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            bool success = false;
            try
            {
                thisPtr._instance._persistenceManager.EndSave(result);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    thisPtr._context.Abort();
                }
            }
            return thisPtr.Save();
        }

        private bool Save()
        {
            if (_pipeline == null)
            {
                return CompleteContext();
            }

            IAsyncResult result = null;
            try
            {
                _context ??= new WorkflowPersistenceContext(_pipeline.IsSaveTransactionRequired,
                    _dependentTransaction, _timeoutHelper.RemainingTime());
                _instance._persistencePipelineInUse = _pipeline;
                Thread.MemoryBarrier();
                if (_instance._state == WorkflowApplicationState.Aborted)
                {
                    throw FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                }

                using (PrepareTransactionalCall(_context.PublicTransaction))
                {
                    result = _pipeline.BeginSave(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(savedCallback), this);
                }
            }
            finally
            {
                if (result == null)
                {
                    _instance._persistencePipelineInUse = null;
                    _context?.Abort();
                }
            }
            return SyncContinue(result);
        }

        private static bool OnSaved(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;

            bool success = false;
            try
            {
                PersistencePipeline.EndSave(result);
                success = true;
            }
            finally
            {
                thisPtr._instance._persistencePipelineInUse = null;
                if (!success)
                {
                    thisPtr._context.Abort();
                }
            }

            return thisPtr.CompleteContext();
        }

        private bool CompleteContext()
        {
            bool wentAsync = false;
            IAsyncResult completeResult = null;

            if (_context != null)
            {
                wentAsync = _context.TryBeginComplete(PrepareAsyncCompletion(completeContextCallback), this, out completeResult);
            }

            if (wentAsync)
            {
                Fx.Assert(completeResult != null, "We shouldn't have null here because we would have rethrown or gotten false for went async.");
                return SyncContinue(completeResult);
            }
            else
            {
                // We completed synchronously if we didn't get an async result out of
                // TryBeginComplete
                return DeleteOwner();
            }
        }

        private static bool OnCompleteContext(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            thisPtr._context.EndComplete(result);

            return thisPtr.DeleteOwner();
        }

        private bool DeleteOwner()
        {
            if (_instance.HasPersistenceProvider && _instance._persistenceManager.OwnerWasCreated &&
                (_operation == PersistenceOperation.Unload || _operation == PersistenceOperation.Complete))
            {
                // This call uses the ambient transaction directly if there was one, to mimic the sync case.
                // TODO: suppress the transaction always.
                IAsyncResult deleteOwnerResult = null;
                using (PrepareTransactionalCall(_dependentTransaction))
                {
                    deleteOwnerResult = _instance._persistenceManager.BeginDeleteOwner(_timeoutHelper.RemainingTime(),
                        PrepareAsyncCompletion(deleteOwnerCompleteCallback), this);
                }
                return SyncContinue(deleteOwnerResult);
            }
            else
            {
                return CloseInstance();
            }
        }

        private static bool OnOwnerDeleted(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
            thisPtr._instance._persistenceManager.EndDeleteOwner(result);
            return thisPtr.CloseInstance();
        }

        private bool CloseInstance()
        {
            // NOTE: We need to make sure that any changes which occur
            // here are appropriately ported to WorkflowApplication's
            // CompletionHandler.OnStage1Complete method in the case
            // where we don't call BeginPersist.
            if (_operation != PersistenceOperation.Save)
            {
                // Stop execution if we've given up the instance lock
                _instance._state = WorkflowApplicationState.Paused;
            }

            if (_isUnloaded)
            {
                _instance.MarkUnloaded();
            }

            return true;
        }

        private static void OnComplete(AsyncResult result, Exception exception)
        {
            UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result;
            try
            {
                thisPtr.NotifyOperationComplete();
            }
            finally
            {
                thisPtr._dependentTransaction?.Complete();
            }
        }
    }

    private abstract class SimpleOperationAsyncResult : AsyncResult
    {
        private static readonly Action<object, TimeoutException> waitCompleteCallback = new(OnWaitComplete);
        private static readonly AsyncCallback trackingCompleteCallback = Fx.ThunkCallback(new AsyncCallback(OnTrackingComplete));
        private readonly WorkflowApplication _instance;
        private TimeoutHelper _timeoutHelper;

        protected SimpleOperationAsyncResult(WorkflowApplication instance, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _instance = instance;
        }

        protected WorkflowApplication Instance => _instance;

        protected void Run(TimeSpan timeout)
        {
            _timeoutHelper = new TimeoutHelper(timeout);

            InstanceOperation operation = new();

            bool completeSelf = true;

            try
            {
                completeSelf = _instance.WaitForTurnAsync(operation, _timeoutHelper.RemainingTime(), waitCompleteCallback, this);

                if (completeSelf)
                {
                    ValidateState();

                    completeSelf = PerformOperationAndTrack();
                }
            }
            finally
            {
                if (completeSelf)
                {
                    _instance.NotifyOperationComplete(operation);
                }
            }

            if (completeSelf)
            {
                Complete(true);
            }
        }

        private bool PerformOperationAndTrack()
        {
            PerformOperation();

            bool completedSync = true;

            if (_instance.Controller.HasPendingTrackingRecords)
            {
                IAsyncResult trackingResult = _instance.Controller.BeginFlushTrackingRecords(_timeoutHelper.RemainingTime(), trackingCompleteCallback, this);

                if (trackingResult.CompletedSynchronously)
                {
                    _instance.Controller.EndFlushTrackingRecords(trackingResult);
                }
                else
                {
                    completedSync = false;
                }
            }

            return completedSync;
        }

        private static void OnWaitComplete(object state, TimeoutException asyncException)
        {
            SimpleOperationAsyncResult thisPtr = (SimpleOperationAsyncResult)state;

            if (asyncException != null)
            {
                thisPtr.Complete(false, asyncException);
                return;
            }

            Exception completionException = null;
            bool completeSelf = true;

            try
            {
                thisPtr.ValidateState();

                completeSelf = thisPtr.PerformOperationAndTrack();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                completionException = e;
            }
            finally
            {
                if (completeSelf)
                {
                    thisPtr._instance.ForceNotifyOperationComplete();
                }
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }

        }

        private static void OnTrackingComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            SimpleOperationAsyncResult thisPtr = (SimpleOperationAsyncResult)result.AsyncState;

            Exception completionException = null;

            try
            {
                thisPtr._instance.Controller.EndFlushTrackingRecords(result);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                completionException = e;
            }
            finally
            {
                thisPtr._instance.ForceNotifyOperationComplete();
            }

            thisPtr.Complete(false, completionException);
        }

        protected abstract void ValidateState();
        protected abstract void PerformOperation();
    }

    private class TerminateAsyncResult : SimpleOperationAsyncResult
    {
        private readonly Exception _reason;

        private TerminateAsyncResult(WorkflowApplication instance, Exception reason, AsyncCallback callback, object state)
            : base(instance, callback, state)
        {
            _reason = reason;
        }

        public static TerminateAsyncResult Create(WorkflowApplication instance, Exception reason, TimeSpan timeout, AsyncCallback callback, object state)
        {
            TerminateAsyncResult result = new(instance, reason, callback, state);
            result.Run(timeout);
            return result;
        }

        public static void End(IAsyncResult result) => End<TerminateAsyncResult>(result);

        protected override void ValidateState() => Instance.ValidateStateForTerminate();

        protected override void PerformOperation() => Instance.TerminateCore(_reason);
    }

    private class CancelAsyncResult : SimpleOperationAsyncResult
    {
        private CancelAsyncResult(WorkflowApplication instance, AsyncCallback callback, object state)
            : base(instance, callback, state) { }

        public static CancelAsyncResult Create(WorkflowApplication instance, TimeSpan timeout, AsyncCallback callback, object state)
        {
            CancelAsyncResult result = new(instance, callback, state);
            result.Run(timeout);
            return result;
        }

        public static void End(IAsyncResult result) => End<CancelAsyncResult>(result);

        protected override void ValidateState() => Instance.ValidateStateForCancel();

        protected override void PerformOperation() => Instance.CancelCore();
    }

    private class RunAsyncResult : SimpleOperationAsyncResult
    {
        private readonly bool _isUserRun;

        private RunAsyncResult(WorkflowApplication instance, bool isUserRun, AsyncCallback callback, object state)
            : base(instance, callback, state)
        {
            _isUserRun = isUserRun;
        }

        public static RunAsyncResult Create(WorkflowApplication instance, bool isUserRun, TimeSpan timeout, AsyncCallback callback, object state)
        {
            RunAsyncResult result = new(instance, isUserRun, callback, state);
            result.Run(timeout);
            return result;
        }

        public static void End(IAsyncResult result) => End<RunAsyncResult>(result);

        protected override void ValidateState() => Instance.ValidateStateForRun();

        protected override void PerformOperation()
        {
            if (_isUserRun)
            {
                // We set this to true here so that idle will be raised
                // regardless of whether any work is performed.
                Instance._hasExecutionOccurredSinceLastIdle = true;
            }

            Instance.RunCore();
        }
    }

    private class UnlockInstanceAsyncResult : TransactedAsyncResult
    {
        private static readonly AsyncCompletion instanceUnlockedCallback = new(OnInstanceUnlocked);
        private static readonly AsyncCompletion ownerDeletedCallback = new(OnOwnerDeleted);
        private static readonly Action<AsyncResult, Exception> completeCallback = new(OnComplete);
        private readonly PersistenceManager _persistenceManager;
        private readonly TimeoutHelper _timeoutHelper;
        private readonly DependentTransaction _dependentTransaction;

        public UnlockInstanceAsyncResult(PersistenceManager persistenceManager, TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _persistenceManager = persistenceManager;
            _timeoutHelper = timeoutHelper;

            Transaction currentTransaction = Transaction.Current;
            if (currentTransaction != null)
            {
                _dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
            }

            OnCompleting = completeCallback;

            bool success = false;
            try
            {
                IAsyncResult result;
                using (PrepareTransactionalCall(_dependentTransaction))
                {
                    if (_persistenceManager.OwnerWasCreated)
                    {
                        // if the owner was created by this WorkflowApplication, delete it.
                        // This implicitly unlocks the instance.
                        result = _persistenceManager.BeginDeleteOwner(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(ownerDeletedCallback), this);
                    }
                    else
                    {
                        result = _persistenceManager.BeginUnlock(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(instanceUnlockedCallback), this);
                    }
                }

                if (SyncContinue(result))
                {
                    Complete(true);
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    _persistenceManager.Abort();
                }
            }
        }

        public static void End(IAsyncResult result)
        {
            End<UnlockInstanceAsyncResult>(result);
        }

        private static bool OnInstanceUnlocked(IAsyncResult result)
        {
            UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result.AsyncState;
            thisPtr._persistenceManager.EndUnlock(result);
            return true;
        }

        private static bool OnOwnerDeleted(IAsyncResult result)
        {
            UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result.AsyncState;
            thisPtr._persistenceManager.EndDeleteOwner(result);
            return true;
        }

        private static void OnComplete(AsyncResult result, Exception exception)
        {
            UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result;
            thisPtr._dependentTransaction?.Complete();
            thisPtr._persistenceManager.Abort();
        }
    }

    private class LoadAsyncResult : TransactedAsyncResult
    {
        private static readonly Action<object, TimeoutException> waitCompleteCallback = new(OnWaitComplete);
        private static readonly AsyncCompletion providerRegisteredCallback = new(OnProviderRegistered);
        private static readonly AsyncCompletion loadCompleteCallback = new(OnLoadComplete);
        private static readonly AsyncCompletion loadPipelineCallback = new(OnLoadPipeline);
        private static readonly AsyncCompletion completeContextCallback = new(OnCompleteContext);
        private static readonly Action<AsyncResult, Exception> completeCallback = new(OnComplete);
        private readonly WorkflowApplication _application;
        private readonly PersistenceManager _persistenceManager;
        private readonly TimeoutHelper _timeoutHelper;
        private readonly bool _loadAny;
        private object _deserializedRuntimeState;
        private PersistencePipeline _pipeline;
        private WorkflowPersistenceContext _context;
        private DependentTransaction _dependentTransaction;
        private IDictionary<XName, InstanceValue> _values;
        private InstanceOperation _instanceOperation;

#if DYNAMICUPDATE
        private DynamicUpdateMap updateMap; 
#endif

        public LoadAsyncResult(WorkflowApplication application, PersistenceManager persistenceManager,
            IDictionary<XName, InstanceValue> values,
#if DYNAMICUPDATE
                    DynamicUpdateMap updateMap,

#endif                
            TimeSpan timeout,
            AsyncCallback callback, object state)
            : base(callback, state)
        {
            _application = application;
            _persistenceManager = persistenceManager;
            _values = values;
            _timeoutHelper = new TimeoutHelper(timeout);
#if DYNAMICUPDATE
            this.updateMap = updateMap; 
#endif

            Initialize();
        }

        public LoadAsyncResult(WorkflowApplication application, PersistenceManager persistenceManager,
            bool loadAny, TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _application = application;
            _persistenceManager = persistenceManager;
            _loadAny = loadAny;
            _timeoutHelper = new TimeoutHelper(timeout);

            Initialize();
        }

        private void Initialize()
        {
            OnCompleting = completeCallback;

            // Save off the current transaction in case we have an async operation before we end up creating
            // the WorkflowPersistenceContext and create it on another thread. Do a simple clone here to prevent
            // the object referenced by Transaction.Current from disposing before we get around to referencing it
            // when we create the WorkflowPersistenceContext.
            //
            // This will throw TransactionAbortedException by design, if the transaction is already rolled back.
            Transaction currentTransaction = Transaction.Current;
            if (currentTransaction != null)
            {
                _dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
            }

            bool completeSelf;
            bool success = false;
            Exception updateException = null;
            try
            {
                if (_application == null)
                {
                    completeSelf = RegisterProvider();
                }
                else
                {
                    completeSelf = WaitForTurn();
                }
                success = true;
            }
#if DYNAMICUPDATE
            catch (InstanceUpdateException e)
            {
                updateException = e;
                throw;
            } 
#endif
            catch (VersionMismatchException e)
            {
                updateException = e;
                throw;
            }
            finally
            {
                if (!success)
                {
                    _dependentTransaction?.Complete();
                    Abort(this, updateException);
                }
            }

            if (completeSelf)
            {
                Complete(true);
            }
        }

        public static void End(IAsyncResult result) => End<LoadAsyncResult>(result);

        public static WorkflowApplicationInstance EndAndCreateInstance(IAsyncResult result)
        {
            LoadAsyncResult thisPtr = End<LoadAsyncResult>(result);
            Fx.AssertAndThrow(thisPtr._application == null, "Should not create a WorkflowApplicationInstance if we already have a WorkflowApplication");

            ActivityExecutor deserializedRuntimeState = ExtractRuntimeState(thisPtr._values, thisPtr._persistenceManager.InstanceId);
            return new WorkflowApplicationInstance(thisPtr._persistenceManager, thisPtr._values, deserializedRuntimeState.WorkflowIdentity);
        }

        private bool WaitForTurn()
        {
            bool completeSelf;
            bool success = false;
            _instanceOperation = new InstanceOperation { RequiresInitialized = false };
            try
            {
                if (_application.WaitForTurnAsync(_instanceOperation, _timeoutHelper.RemainingTime(), waitCompleteCallback, this))
                {
                    completeSelf = ValidateState();
                }
                else
                {
                    completeSelf = false;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    NotifyOperationComplete();
                }
            }

            return completeSelf;
        }

        private static void OnWaitComplete(object state, TimeoutException asyncException)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)state;
            if (asyncException != null)
            {
                thisPtr.Complete(false, asyncException);
                return;
            }

            bool completeSelf;
            Exception completionException = null;

            try
            {
                completeSelf = thisPtr.ValidateState();
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                completionException = e;
                completeSelf = true;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        private bool ValidateState()
        {
            _application.ValidateStateForLoad();

            _application.SetPersistenceManager(_persistenceManager);
            if (!_loadAny)
            {
                _application._instanceId = _persistenceManager.InstanceId;
                _application._instanceIdSet = true;
            }
            if (_application.InstanceStore == null)
            {
                _application.InstanceStore = _persistenceManager.InstanceStore;
            }

            return RegisterProvider();
        }

        private bool RegisterProvider()
        {
            if (!_persistenceManager.IsInitialized)
            {
                WorkflowIdentity definitionIdentity = _application != null ? _application.DefinitionIdentity : unknownIdentity;
                IAsyncResult result = _persistenceManager.BeginInitialize(definitionIdentity, _timeoutHelper.RemainingTime(), PrepareAsyncCompletion(providerRegisteredCallback), this);
                return SyncContinue(result);
            }
            else
            {
                return Load();
            }
        }

        private static bool OnProviderRegistered(IAsyncResult result)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
            thisPtr._persistenceManager.EndInitialize(result);
            return thisPtr.Load();
        }

        private bool Load()
        {
            bool success = false;
            IAsyncResult result = null;
            try
            {
                bool transactionRequired = _application != null && _application.IsLoadTransactionRequired();
                _context = new WorkflowPersistenceContext(transactionRequired,
                    _dependentTransaction, _timeoutHelper.OriginalTimeout);

                // Values is null if this is an initial load from the database.
                // It is non-null if we already loaded values into a WorkflowApplicationInstance,
                // and are now loading from that WAI.
                if (_values == null)
                {
                    using (PrepareTransactionalCall(_context.PublicTransaction))
                    {
                        if (_loadAny)
                        {
                            result = _persistenceManager.BeginTryLoad(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(loadCompleteCallback), this);
                        }
                        else
                        {
                            result = _persistenceManager.BeginLoad(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(loadCompleteCallback), this);
                        }
                    }
                }
                success = true;
            }
            finally
            {
                if (!success && _context != null)
                {
                    _context.Abort();
                }
            }

            return result == null ? LoadValues(null) : SyncContinue(result);
        }

        private static bool OnLoadComplete(IAsyncResult result)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
            return thisPtr.LoadValues(result);
        }

        private bool LoadValues(IAsyncResult result)
        {
            IAsyncResult loadResult = null;
            bool success = false;
            try
            {
                Fx.Assert(result == null != (_values == null), "We should either have values already retrieved, or an IAsyncResult to retrieve them");

                if (result != null)
                {
                    if (_loadAny)
                    {
                        if (!_persistenceManager.EndTryLoad(result, out _values))
                        {
                            throw FxTrace.Exception.AsError(new InstanceNotReadyException(SR.NoRunnableInstances));
                        }
                        if (_application != null)
                        {
                            if (_application._instanceIdSet)
                            {
                                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
                            }

                            _application._instanceId = _persistenceManager.InstanceId;
                            _application._instanceIdSet = true;
                        }
                    }
                    else
                    {
                        _values = _persistenceManager.EndLoad(result);
                    }
                }

                if (_application != null)
                {
                    _pipeline = _application.ProcessInstanceValues(_values, out _deserializedRuntimeState);

                    if (_pipeline != null)
                    {
                        _pipeline.SetLoadedValues(_values);

                        _application._persistencePipelineInUse = _pipeline;
                        Thread.MemoryBarrier();
                        if (_application._state == WorkflowApplicationState.Aborted)
                        {
                            throw FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                        }

                        using (PrepareTransactionalCall(_context.PublicTransaction))
                        {
                            loadResult = _pipeline.BeginLoad(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(loadPipelineCallback), this);
                        }
                    }
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    _context.Abort();
                }
            }

            return _pipeline != null ? SyncContinue(loadResult) : CompleteContext();
        }

        private static bool OnLoadPipeline(IAsyncResult result)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;

            bool success = false;
            try
            {
                thisPtr._pipeline.EndLoad(result);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    thisPtr._context.Abort();
                }
            }
            return thisPtr.CompleteContext();
        }

        private bool CompleteContext()
        {
            if (_application != null)
            {
#if DYNAMICUPDATE
                this.application.Initialize(this.deserializedRuntimeState, this.updateMap);
                if (this.updateMap != null)
                {
                    this.application.UpdateInstanceMetadata();
                } 
#else
                _application.Initialize(_deserializedRuntimeState);
#endif
            }

            if (_context.TryBeginComplete(PrepareAsyncCompletion(completeContextCallback), this, out IAsyncResult completeResult))
            {
                Fx.Assert(completeResult != null, "We shouldn't have null here.");
                return SyncContinue(completeResult);
            }
            else
            {
                return Finish();
            }
        }

        private static bool OnCompleteContext(IAsyncResult result)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
            thisPtr._context.EndComplete(result);
            return thisPtr.Finish();
        }

        private bool Finish()
        {
            _pipeline?.Publish();
            return true;
        }

        private void NotifyOperationComplete()
        {
            if (_application != null)
            {
                InstanceOperation localInstanceOperation = _instanceOperation;
                _instanceOperation = null;
                _application.NotifyOperationComplete(localInstanceOperation);
            }
        }

        private static void OnComplete(AsyncResult result, Exception exception)
        {
            LoadAsyncResult thisPtr = (LoadAsyncResult)result;
            try
            {
                thisPtr._dependentTransaction?.Complete();

                if (exception != null)
                {
                    Abort(thisPtr, exception);
                }
            }
            finally
            {
                thisPtr.NotifyOperationComplete();
            }
        }

        private static void Abort(LoadAsyncResult thisPtr, Exception exception)
        {
            if (thisPtr._application == null)
            {
                thisPtr._persistenceManager.Abort();
            }
            else
            {
                thisPtr._application.AbortDueToException(exception);
            }
        }
    }

    private class InstanceCommandWithTemporaryHandleAsyncResult : TransactedAsyncResult
    {
        private static readonly AsyncCompletion commandCompletedCallback = new(OnCommandCompleted);
        private static readonly Action<AsyncResult, Exception> completeCallback = new(OnComplete);
        private readonly DependentTransaction _dependentTransaction;
        private readonly InstanceStore _instanceStore;
        private readonly InstanceHandle _temporaryHandle;
        private InstanceView _commandResult;

        public InstanceCommandWithTemporaryHandleAsyncResult(InstanceStore instanceStore, InstancePersistenceCommand command,
            TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _instanceStore = instanceStore;
            _temporaryHandle = instanceStore.CreateInstanceHandle();

            Transaction currentTransaction = Transaction.Current;
            if (currentTransaction != null)
            {
                _dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
            }

            OnCompleting = completeCallback;

            IAsyncResult result;
            using (PrepareTransactionalCall(_dependentTransaction))
            {
                result = instanceStore.BeginExecute(_temporaryHandle, command, timeout, PrepareAsyncCompletion(commandCompletedCallback), this);
            }

            if (SyncContinue(result))
            {
                Complete(true);
            }
        }

        public static void End(IAsyncResult result, out InstanceStore instanceStore, out InstanceView commandResult)
        {
            InstanceCommandWithTemporaryHandleAsyncResult thisPtr = End<InstanceCommandWithTemporaryHandleAsyncResult>(result);
            instanceStore = thisPtr._instanceStore;
            commandResult = thisPtr._commandResult;
        }

        private static bool OnCommandCompleted(IAsyncResult result)
        {
            InstanceCommandWithTemporaryHandleAsyncResult thisPtr = (InstanceCommandWithTemporaryHandleAsyncResult)result.AsyncState;
            thisPtr._commandResult = thisPtr._instanceStore.EndExecute(result);
            return true;
        }

        private static void OnComplete(AsyncResult result, Exception exception)
        {
            InstanceCommandWithTemporaryHandleAsyncResult thisPtr = (InstanceCommandWithTemporaryHandleAsyncResult)result;
            if (thisPtr._dependentTransaction != null)
            {
                thisPtr._dependentTransaction.Complete();
            }
            thisPtr._temporaryHandle.Free();
        }
    }
}

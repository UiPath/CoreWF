// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.DurableInstancing;
using System.Activities.Tracking;
using System.Transactions;

namespace System.Activities.Runtime;

internal partial class ActivityExecutor
{
    [DataContract]
    internal class AbortActivityWorkItem : WorkItem
    {
        private Exception _reason;
        private ActivityInstanceReference _originalSource;

        public AbortActivityWorkItem(ActivityInstance activityInstance, Exception reason, ActivityInstanceReference originalSource)
            : base(activityInstance)
        {
            _reason = reason;
            _originalSource = originalSource;
            IsEmpty = true;
        }

        public override ActivityInstance OriginalExceptionSource => _originalSource.ActivityInstance;

        public override bool IsValid => ActivityInstance.State == ActivityInstanceState.Executing;

        public override ActivityInstance PropertyManagerOwner
        {
            get
            {
                Fx.Assert("This is never called.");
                return null;
            }
        }

        [DataMember(Name = "reason")]
        internal Exception SerializedReason
        {
            get => _reason;
            set => _reason = value;
        }

        [DataMember(Name = "originalSource")]
        internal ActivityInstanceReference SerializedOriginalSource
        {
            get => _originalSource;
            set => _originalSource = value;
        }

        public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

        public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

        public override void TraceStarting() => TraceRuntimeWorkItemStarting();

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            Fx.Assert("This is never called");
            return true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            executor.AbortActivityInstance(ActivityInstance, _reason);

            // We always repropagate the exception from here
            ExceptionToPropagate = _reason;

            // Tell the executor to decrement its NoPersistCount, if necessary.
            executor.ExitNoPersistForExceptionPropagation();
        }
    }

    [DataContract]
    internal class CompleteAsyncOperationWorkItem : BookmarkWorkItem
    {
        public CompleteAsyncOperationWorkItem(BookmarkCallbackWrapper wrapper, Bookmark bookmark, object value)
            : base(wrapper, bookmark, value)
        {
            ExitNoPersistRequired = true;
        }
    }

    [DataContract]
    internal class CancelActivityWorkItem : ActivityExecutionWorkItem
    {
        public CancelActivityWorkItem(ActivityInstance activityInstance)
            : base(activityInstance) { }

        public override void TraceCompleted()
        {
            if (TD.CompleteCancelActivityWorkItemIsEnabled())
            {
                TD.CompleteCancelActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceScheduled()
        {
            if (TD.ScheduleCancelActivityWorkItemIsEnabled())
            {
                TD.ScheduleCancelActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceStarting()
        {
            if (TD.StartCancelActivityWorkItemIsEnabled())
            {
                TD.StartCancelActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            try
            {
                ActivityInstance.Cancel(executor, bookmarkManager);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                ExceptionToPropagate = e;
            }

            return true;
        }
    }

    [DataContract]
    internal class ExecuteActivityWorkItem : ActivityExecutionWorkItem
    {
        private bool _requiresSymbolResolution;
        private IDictionary<string, object> _argumentValueOverrides;

        // Called by the pool.
        public ExecuteActivityWorkItem()
        {
            IsPooled = true;
        }

        // Called by non-pool subclasses.
        protected ExecuteActivityWorkItem(ActivityInstance activityInstance, bool requiresSymbolResolution, IDictionary<string, object> argumentValueOverrides)
            : base(activityInstance)
        {
            _requiresSymbolResolution = requiresSymbolResolution;
            _argumentValueOverrides = argumentValueOverrides;
        }

        [DataMember(EmitDefaultValue = false, Name = "requiresSymbolResolution")]
        internal bool SerializedRequiresSymbolResolution
        {
            get => _requiresSymbolResolution;
            set => _requiresSymbolResolution = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "argumentValueOverrides")]
        internal IDictionary<string, object> SerializedArgumentValueOverrides
        {
            get => _argumentValueOverrides;
            set => _argumentValueOverrides = value;
        }

        public void Initialize(ActivityInstance activityInstance, bool requiresSymbolResolution, IDictionary<string, object> argumentValueOverrides)
        {
            base.Reinitialize(activityInstance);
            _requiresSymbolResolution = requiresSymbolResolution;
            _argumentValueOverrides = argumentValueOverrides;
        }

        protected override void ReleaseToPool(ActivityExecutor executor)
        {
            base.ClearForReuse();
            _requiresSymbolResolution = false;
            _argumentValueOverrides = null;

            executor.ExecuteActivityWorkItemPool.Release(this);
        }

        public override void TraceScheduled()
        {
            if (TD.IsEnd2EndActivityTracingEnabled() && TD.ScheduleExecuteActivityWorkItemIsEnabled())
            {
                TD.ScheduleExecuteActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceStarting()
        {
            if (TD.IsEnd2EndActivityTracingEnabled() && TD.StartExecuteActivityWorkItemIsEnabled())
            {
                TD.StartExecuteActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceCompleted()
        {
            if (TD.IsEnd2EndActivityTracingEnabled() && TD.CompleteExecuteActivityWorkItemIsEnabled())
            {
                TD.CompleteExecuteActivityWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager) => ExecuteBody(executor, bookmarkManager, null);

        protected bool ExecuteBody(ActivityExecutor executor, BookmarkManager bookmarkManager, Location resultLocation)
        {
            try
            {
                if (_requiresSymbolResolution)
                {
                    if (!ActivityInstance.ResolveArguments(executor, _argumentValueOverrides, resultLocation))
                    {
                        return true;
                    }

                    if (!ActivityInstance.ResolveVariables(executor))
                    {
                        return true;
                    }
                }
                // We want to do this if there was no symbol resolution or if ResolveVariables completed
                // synchronously.
                ActivityInstance.SetInitializedSubstate(executor);

#if NET45
                if (executor.IsDebugged())
                {
                    executor.debugController.ActivityStarted(this.ActivityInstance);
                }
#endif

                ActivityInstance.Execute(executor, bookmarkManager);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                ExceptionToPropagate = e;
            }

            return true;
        }
    }

    [DataContract]
    internal class ExecuteRootWorkItem : ExecuteActivityWorkItem
    {
        public ExecuteRootWorkItem(ActivityInstance activityInstance, bool requiresSymbolResolution, IDictionary<string, object> argumentValueOverrides)
            : base(activityInstance, requiresSymbolResolution, argumentValueOverrides) { }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            if (executor.ShouldTrackActivityScheduledRecords)
            {
                executor.AddTrackingRecord(
                    new ActivityScheduledRecord(
                        executor.WorkflowInstanceId,
                        null,
                        ActivityInstance));
            }

            return ExecuteBody(executor, bookmarkManager, null);
        }
    }

    [DataContract]
    internal class ExecuteExpressionWorkItem : ExecuteActivityWorkItem
    {
        private Location _resultLocation;

        public ExecuteExpressionWorkItem(ActivityInstance activityInstance, bool requiresSymbolResolution, IDictionary<string, object> argumentValueOverrides, Location resultLocation)
            : base(activityInstance, requiresSymbolResolution, argumentValueOverrides)
        {
            Fx.Assert(resultLocation != null, "We should only use this work item when we are resolving arguments/variables and therefore have a result location.");
            _resultLocation = resultLocation;
        }

        [DataMember(Name = "resultLocation")]
        internal Location SerializedResultLocation
        {
            get => _resultLocation;
            set => _resultLocation = value;
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager) => ExecuteBody(executor, bookmarkManager, _resultLocation);
    }

    [DataContract]
    internal class PropagateExceptionWorkItem : ActivityExecutionWorkItem
    {
        private Exception _exception;

        public PropagateExceptionWorkItem(Exception exception, ActivityInstance activityInstance)
            : base(activityInstance)
        {
            Fx.Assert(exception != null, "We must not have a null exception.");

            _exception = exception;
            IsEmpty = true;
        }

        [DataMember(EmitDefaultValue = false, Name = "exception")]
        internal Exception SerializedException
        {
            get => _exception;
            set => _exception = value;
        }

        public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

        public override void TraceStarting() => TraceRuntimeWorkItemStarting();

        public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            Fx.Assert("This shouldn't be called because we are empty.");
            return false;
        }

        public override void PostProcess(ActivityExecutor executor) => ExceptionToPropagate = _exception;
    }

    [DataContract]
    internal class RethrowExceptionWorkItem : WorkItem
    {
        private Exception _exception;
        private ActivityInstanceReference _source;

        public RethrowExceptionWorkItem(ActivityInstance activityInstance, Exception exception, ActivityInstanceReference source)
            : base(activityInstance)
        {
            _exception = exception;
            _source = source;
            IsEmpty = true;
        }

        public override bool IsValid => ActivityInstance.State == ActivityInstanceState.Executing;

        public override ActivityInstance PropertyManagerOwner
        {
            get
            {
                Fx.Assert("This is never called.");
                return null;
            }
        }

        public override ActivityInstance OriginalExceptionSource => _source.ActivityInstance;

        [DataMember(Name = "exception")]
        internal Exception SerializedException
        {
            get => _exception;
            set => _exception = value;
        }

        [DataMember(Name = "source")]
        internal ActivityInstanceReference SerializedSource
        {
            get => _source;
            set => _source = value;
        }

        public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

        public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

        public override void TraceStarting() => TraceRuntimeWorkItemStarting();

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            Fx.Assert("This shouldn't be called because we are IsEmpty = true.");
            return true;
        }

        public override void PostProcess(ActivityExecutor executor)
        {
            executor.AbortActivityInstance(ActivityInstance, ExceptionToPropagate);
            ExceptionToPropagate = _exception;
        }
    }

    // This is not DataContract because this is always scheduled in a no-persist zone.
    // This work items exits the no persist zone when it is released.
    private class CompleteTransactionWorkItem : WorkItem
    {
        private static AsyncCallback persistCompleteCallback;
        private static AsyncCallback commitCompleteCallback;
        private static Action<object, TimeoutException> outcomeDeterminedCallback;
        private RuntimeTransactionData _runtimeTransaction;
        private ActivityExecutor _executor;

        public CompleteTransactionWorkItem(ActivityInstance instance)
            : base(instance)
        {
            ExitNoPersistRequired = true;
        }

        private static AsyncCallback PersistCompleteCallback
        {
            get
            {
                persistCompleteCallback ??= Fx.ThunkCallback(new AsyncCallback(OnPersistComplete));
                return persistCompleteCallback;
            }
        }

        private static AsyncCallback CommitCompleteCallback
        {
            get
            {
                commitCompleteCallback ??= Fx.ThunkCallback(new AsyncCallback(OnCommitComplete));
                return commitCompleteCallback;
            }
        }

        private static Action<object, TimeoutException> OutcomeDeterminedCallback
        {
            get
            {
                outcomeDeterminedCallback ??= new Action<object, TimeoutException>(OnOutcomeDetermined);
                return outcomeDeterminedCallback;
            }
        }

        public override bool IsValid => true;

        public override ActivityInstance PropertyManagerOwner => null;

        public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

        public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

        public override void TraceStarting() => TraceRuntimeWorkItemStarting();

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            _runtimeTransaction = executor._runtimeTransaction;
            _executor = executor;

            // We need to take care of any pending cancelation
            _executor.SchedulePendingCancelation();

            bool completeSelf;
            try
            {
                // If the transaction is already rolled back, skip the persistence.  This allows us to avoid aborting the instance.
                completeSelf = CheckTransactionAborted();
                if (!completeSelf)
                {
                    IAsyncResult result = new TransactionalPersistAsyncResult(_executor, PersistCompleteCallback, this);
                    if (result.CompletedSynchronously)
                    {
                        completeSelf = FinishPersist(result);
                    }
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                HandleException(e);
                completeSelf = true;
            }

            if (completeSelf)
            {
                _executor._runtimeTransaction = null;

                TraceTransactionOutcome();
                return true;
            }

            return false;
        }

        private void TraceTransactionOutcome()
        {
            if (TD.RuntimeTransactionCompleteIsEnabled())
            {
                TD.RuntimeTransactionComplete(_runtimeTransaction.TransactionStatus.ToString());
            }
        }

        private void HandleException(Exception exception)
        {
            try
            {
                _runtimeTransaction.OriginalTransaction.Rollback(exception);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                _workflowAbortException = e;
            }

            if (_runtimeTransaction.TransactionHandle.AbortInstanceOnTransactionFailure)
            {
                // We might be overwriting a more recent exception from above, but it is
                // more important that we tell the user why they failed originally.
                _workflowAbortException = exception;
            }
            else
            {
                ExceptionToPropagate = exception;
            }
        }

        private static void OnPersistComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            CompleteTransactionWorkItem thisPtr = (CompleteTransactionWorkItem)result.AsyncState;
            bool completeSelf;
            try
            {
                completeSelf = thisPtr.FinishPersist(result);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                thisPtr.HandleException(e);
                completeSelf = true;
            }

            if (completeSelf)
            {
                thisPtr._executor._runtimeTransaction = null;

                thisPtr.TraceTransactionOutcome();

                thisPtr._executor.FinishWorkItem(thisPtr);
            }
        }

        private bool FinishPersist(IAsyncResult result)
        {
            TransactionalPersistAsyncResult.End(result);

            return CompleteTransaction();
        }

        private bool CompleteTransaction()
        {
            PreparingEnlistment enlistment = null;

            lock (_runtimeTransaction)
            {
                if (_runtimeTransaction.PendingPreparingEnlistment != null)
                {
                    enlistment = _runtimeTransaction.PendingPreparingEnlistment;
                }

                _runtimeTransaction.HasPrepared = true;
            }

            enlistment?.Prepared();

            Transaction original = _runtimeTransaction.OriginalTransaction;

            DependentTransaction dependentTransaction = original as DependentTransaction;
            if (dependentTransaction != null)
            {
                dependentTransaction.Complete();
                return CheckOutcome();
            }
            else
            {
                CommittableTransaction committableTransaction = original as CommittableTransaction;
                if (committableTransaction != null)
                {
                    IAsyncResult result = committableTransaction.BeginCommit(CommitCompleteCallback, this);

                    return result.CompletedSynchronously && FinishCommit(result);
                }
                else
                {
                    return CheckOutcome();
                }
            }
        }

        private static void OnCommitComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            CompleteTransactionWorkItem thisPtr = (CompleteTransactionWorkItem)result.AsyncState;
            bool completeSelf;
            try
            {
                completeSelf = thisPtr.FinishCommit(result);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                thisPtr.HandleException(e);
                completeSelf = true;
            }

            if (completeSelf)
            {
                thisPtr._executor._runtimeTransaction = null;

                thisPtr.TraceTransactionOutcome();

                thisPtr._executor.FinishWorkItem(thisPtr);
            }
        }

        private bool FinishCommit(IAsyncResult result)
        {
            ((CommittableTransaction)_runtimeTransaction.OriginalTransaction).EndCommit(result);

            return CheckOutcome();
        }

        private bool CheckOutcome()
        {
            AsyncWaitHandle completionEvent = null;

            lock (_runtimeTransaction)
            {
                TransactionStatus status = _runtimeTransaction.TransactionStatus;

                if (status == TransactionStatus.Active)
                {
                    completionEvent = new AsyncWaitHandle();
                    _runtimeTransaction.CompletionEvent = completionEvent;
                }
            }

            if (completionEvent != null && !completionEvent.WaitAsync(OutcomeDeterminedCallback, this, ActivityDefaults.TransactionCompletionTimeout))
            {
                return false;
            }

            return FinishCheckOutcome();
        }

        private static void OnOutcomeDetermined(object state, TimeoutException asyncException)
        {
            CompleteTransactionWorkItem thisPtr = (CompleteTransactionWorkItem)state;
            bool completeSelf = true;

            if (asyncException != null)
            {
                thisPtr.HandleException(asyncException);
            }
            else
            {
                try
                {
                    completeSelf = thisPtr.FinishCheckOutcome();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    thisPtr.HandleException(e);
                    completeSelf = true;
                }
            }

            if (completeSelf)
            {
                thisPtr._executor._runtimeTransaction = null;

                thisPtr.TraceTransactionOutcome();

                thisPtr._executor.FinishWorkItem(thisPtr);
            }
        }

        private bool FinishCheckOutcome()
        {
            CheckTransactionAborted();
            return true;
        }

        private bool CheckTransactionAborted()
        {
            try
            {
                TransactionHelper.ThrowIfTransactionAbortedOrInDoubt(_runtimeTransaction.OriginalTransaction);
                return false;
            }
            catch (TransactionException exception)
            {
                if (_runtimeTransaction.TransactionHandle.AbortInstanceOnTransactionFailure)
                {
                    _workflowAbortException = exception;
                }
                else
                {
                    ExceptionToPropagate = exception;
                }
                return true;
            }
        }

        public override void PostProcess(ActivityExecutor executor) { }

        private class TransactionalPersistAsyncResult : TransactedAsyncResult
        {
            private readonly CompleteTransactionWorkItem _workItem;
            private static readonly AsyncCompletion onPersistComplete = new(OnPersistComplete);
            private readonly ActivityExecutor _executor;

            public TransactionalPersistAsyncResult(ActivityExecutor executor, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _executor = executor;
                _workItem = (CompleteTransactionWorkItem)state;
                IAsyncResult result = null;
                using (PrepareTransactionalCall(_executor.CurrentTransaction))
                {
                    try
                    {
                        result = _executor._host.OnBeginPersist(PrepareAsyncCompletion(onPersistComplete), this);
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }
                        _workItem._workflowAbortException = e;
                        throw;
                    }
                }
                if (SyncContinue(result))
                {
                    Complete(true);
                }
            }

            public static void End(IAsyncResult result) => End<TransactionalPersistAsyncResult>(result);

            private static bool OnPersistComplete(IAsyncResult result)
            {
                TransactionalPersistAsyncResult thisPtr = (TransactionalPersistAsyncResult)result.AsyncState;

                try
                {
                    thisPtr._executor._host.OnEndPersist(result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    thisPtr._workItem._workflowAbortException = e;
                    throw;
                }

                return true;
            }
        }
    }

    [DataContract]
    internal class TransactionContextWorkItem : ActivityExecutionWorkItem
    {
        private TransactionContextWaiter _waiter;

        public TransactionContextWorkItem(TransactionContextWaiter waiter)
            : base(waiter.WaitingInstance)
        {
            _waiter = waiter;

            if (_waiter.IsRequires)
            {
                ExitNoPersistRequired = true;
            }
        }

        [DataMember(Name = "waiter")]
        internal TransactionContextWaiter SerializedWaiter
        {
            get => _waiter;
            set => _waiter = value;
        }

        public override void TraceCompleted()
        {
            if (TD.CompleteTransactionContextWorkItemIsEnabled())
            {
                TD.CompleteTransactionContextWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceScheduled()
        {
            if (TD.ScheduleTransactionContextWorkItemIsEnabled())
            {
                TD.ScheduleTransactionContextWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override void TraceStarting()
        {
            if (TD.StartTransactionContextWorkItemIsEnabled())
            {
                TD.StartTransactionContextWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            NativeActivityTransactionContext transactionContext = null;

            try
            {
                transactionContext = new NativeActivityTransactionContext(ActivityInstance, executor, bookmarkManager, _waiter.Handle);
                _waiter.CallbackWrapper.Invoke(transactionContext, _waiter.State);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                ExceptionToPropagate = e;
            }
            finally
            {
                transactionContext?.Dispose();
            }

            return true;
        }
    }

    private class PoolOfEmptyWorkItems : Pool<EmptyWorkItem>
    {
        protected override EmptyWorkItem CreateNew() => new();
    }

    private class PoolOfExecuteActivityWorkItems : Pool<ExecuteActivityWorkItem>
    {
        protected override ExecuteActivityWorkItem CreateNew() => new();
    }

    private class PoolOfExecuteSynchronousExpressionWorkItems : Pool<ExecuteSynchronousExpressionWorkItem>
    {
        protected override ExecuteSynchronousExpressionWorkItem CreateNew() => new();
    }

    private class PoolOfCompletionWorkItems : Pool<CompletionCallbackWrapper.CompletionWorkItem>
    {
        protected override CompletionCallbackWrapper.CompletionWorkItem CreateNew() => new();
    }

    private class PoolOfResolveNextArgumentWorkItems : Pool<ResolveNextArgumentWorkItem>
    {
        protected override ResolveNextArgumentWorkItem CreateNew() => new();
    }
}

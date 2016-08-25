// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.DurableInstancing;
using Microsoft.CoreWf.Hosting;
using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.DurableInstancing;
using Microsoft.CoreWf.Tracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Xml.Linq;

namespace Microsoft.CoreWf
{
    // WorkflowApplication is free-threaded. It is responsible for the correct locking and usage of the ActivityExecutor.
    // Given that there are two simultaneous users of ActivityExecutor (WorkflowApplication and NativeActivityContext),
    // it is imperative that WorkflowApplication only calls into ActivityExecutor when there are no activities executing
    // (and thus no worries about colliding with AEC calls).

    // SYNCHRONIZATION SCHEME
    // WorkflowInstance is defined to not be thread safe and to disallow all operations while it is (potentially
    // asynchronously) running.  The WorkflowInstance is in the "running" state between a call to Run and the
    // subsequent call to either WorkflowInstance NotifyPaused or NotifyUnhandledException.
    // WorkflowApplication keeps track of a boolean "isBusy" and a list of pending operations.  WI is busy whenever
    // it is servicing an operation or the runtime is in the "running" state.
    // Enqueue - This enqueues an operation into the pending operation list.  If WI is not busy then the operation
    //    can be serviced immediately.  This is the only place where "isBusy" flips to true.
    // OnNotifiedUnhandledException - This method performs some processing and then calls OnNotifiedPaused.
    // OnNotifiedPaused - This method is only ever called when "isBusy" is true.  It first checks to see if there
    //    is other work to be done (prioritization: raise completed, handle an operation, resume execution, raise
    //    idle, stop).  This is the only place where "isBusy" flips to false and this only occurs when there is no
    //    other work to be done.
    // [Force]NotifyOperationComplete - These methods are called by individual operations when they are done
    //    processing.  If the operation was notified (IE - actually performed in the eyes of WI) then this is simply
    //    a call to OnNotifiedPaused.
    // Operation notification - The InstanceOperation class keeps tracks of whether a specified operation was
    //    dispatched by WI or not.  If it was dispatched (determined either in Enqueue, FindOperation, or Remove)
    //    then it MUST result in a call to OnNotifiedPaused when complete.
    [Fx.Tag.XamlVisible(false)]
    public sealed class WorkflowApplication : WorkflowInstance
    {
        private static AsyncCallback s_eventFrameCallback;
        private static IdleEventHandler s_idleHandler;
        private static CompletedEventHandler s_completedHandler;
        private static UnhandledExceptionEventHandler s_unhandledExceptionHandler;
        private static Action<object, TimeoutException> s_waitAsyncCompleteCallback;
        private static readonly WorkflowIdentity s_unknownIdentity = new WorkflowIdentity();

        private Action<WorkflowApplicationAbortedEventArgs> _onAborted;
        private Action<WorkflowApplicationEventArgs> _onUnloaded;
        private Action<WorkflowApplicationCompletedEventArgs> _onCompleted;
        private Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> _onUnhandledException;
        private Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> _onPersistableIdle;
        private Action<WorkflowApplicationIdleEventArgs> _onIdle;

        private WorkflowEventData _eventData;

        private WorkflowInstanceExtensionManager _extensions;
        private PersistencePipeline _persistencePipelineInUse;
        private InstanceStore _instanceStore;
        private PersistenceManager _persistenceManager;
        private WorkflowApplicationState _state;
        private int _handlerThreadId;
        private bool _isInHandler;
        private Action _invokeCompletedCallback;
        private Guid _instanceId;
        private bool _instanceIdSet;  // Checking for Guid.Empty is expensive.

        // Tracking for one-time actions per in-memory pulse
        private bool _hasCalledAbort;
        private bool _hasCalledRun;

        // Tracking for one-time actions per instance lifetime (these end up being persisted)
        private bool _hasRaisedCompleted;

        private Quack<InstanceOperation> _pendingOperations;
        private bool _isBusy;
        private bool _hasExecutionOccurredSinceLastIdle;

        // Count of operations that are about to be enqueued.
        // We use this when enqueueing multiple operations, to avoid raising
        // idle on dequeue of the first operation.
        private int _pendingUnenqueued;

        // We use this to keep track of the number of "interesting" things that have happened.
        // Notifying operations and calling Run on the runtime count as interesting things.
        // All operations are stamped with the actionCount at the time of being enqueued.
        private int _actionCount;

        // Initial creation data
        private IDictionary<string, object> _initialWorkflowArguments;
        private IList<Handle> _rootExecutionProperties;

        private IDictionary<XName, InstanceValue> _instanceMetadata;

        public WorkflowApplication(Activity workflowDefinition)
            : this(workflowDefinition, (WorkflowIdentity)null)
        {
        }

        public WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs)
            : this(workflowDefinition, inputs, (WorkflowIdentity)null)
        {
        }

        public WorkflowApplication(Activity workflowDefinition, WorkflowIdentity definitionIdentity)
            : base(workflowDefinition, definitionIdentity)
        {
            _pendingOperations = new Quack<InstanceOperation>();
            Fx.Assert(_state == WorkflowApplicationState.Paused, "We always start out paused (the default)");
        }

        public WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs, WorkflowIdentity definitionIdentity)
            : this(workflowDefinition, definitionIdentity)
        {
            if (inputs == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("inputs");
            }
            _initialWorkflowArguments = inputs;
        }

        private WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs, IList<Handle> executionProperties)
            : this(workflowDefinition)
        {
            _initialWorkflowArguments = inputs;
            _rootExecutionProperties = executionProperties;
        }

        public InstanceStore InstanceStore
        {
            get
            {
                return _instanceStore;
            }
            set
            {
                ThrowIfReadOnly();
                _instanceStore = value;
            }
        }

        public WorkflowInstanceExtensionManager Extensions
        {
            get
            {
                if (_extensions == null)
                {
                    _extensions = new WorkflowInstanceExtensionManager();
                    if (base.IsReadOnly)
                    {
                        _extensions.MakeReadOnly();
                    }
                }
                return _extensions;
            }
        }

        public Action<WorkflowApplicationAbortedEventArgs> Aborted
        {
            get
            {
                return _onAborted;
            }
            set
            {
                ThrowIfMulticast(value);
                _onAborted = value;
            }
        }

        public Action<WorkflowApplicationEventArgs> Unloaded
        {
            get
            {
                return _onUnloaded;
            }
            set
            {
                ThrowIfMulticast(value);
                _onUnloaded = value;
            }
        }

        public Action<WorkflowApplicationCompletedEventArgs> Completed
        {
            get
            {
                return _onCompleted;
            }
            set
            {
                ThrowIfMulticast(value);
                _onCompleted = value;
            }
        }

        public Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> OnUnhandledException
        {
            get
            {
                return _onUnhandledException;
            }
            set
            {
                ThrowIfMulticast(value);
                _onUnhandledException = value;
            }
        }

        public Action<WorkflowApplicationIdleEventArgs> Idle
        {
            get
            {
                return _onIdle;
            }
            set
            {
                ThrowIfMulticast(value);
                _onIdle = value;
            }
        }

        public Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> PersistableIdle
        {
            get
            {
                return _onPersistableIdle;
            }
            set
            {
                ThrowIfMulticast(value);
                _onPersistableIdle = value;
            }
        }

        public override Guid Id
        {
            get
            {
                if (!_instanceIdSet)
                {
                    lock (_pendingOperations)
                    {
                        if (!_instanceIdSet)
                        {
                            _instanceId = Guid.NewGuid();
                            _instanceIdSet = true;
                        }
                    }
                }
                return _instanceId;
            }
        }

        protected internal override bool SupportsInstanceKeys
        {
            get
            {
                return false;
            }
        }

        private static AsyncCallback EventFrameCallback
        {
            get
            {
                if (s_eventFrameCallback == null)
                {
                    s_eventFrameCallback = Fx.ThunkCallback(new AsyncCallback(EventFrame));
                }

                return s_eventFrameCallback;
            }
        }

        private WorkflowEventData EventData
        {
            get
            {
                if (_eventData == null)
                {
                    _eventData = new WorkflowEventData(this);
                }

                return _eventData;
            }
        }

        private bool HasPersistenceProvider
        {
            get
            {
                return _persistenceManager != null;
            }
        }

        private bool IsHandlerThread
        {
            get
            {
                //return this.isInHandler && this.handlerThreadId == Thread.CurrentThread.ManagedThreadId;
                return false;
            }
        }

        private bool IsInTerminalState
        {
            get
            {
                return _state == WorkflowApplicationState.Unloaded || _state == WorkflowApplicationState.Aborted;
            }
        }

        public void AddInitialInstanceValues(IDictionary<XName, object> writeOnlyValues)
        {
            ThrowIfReadOnly();

            if (writeOnlyValues != null)
            {
                if (_instanceMetadata == null)
                {
                    _instanceMetadata = new Dictionary<XName, InstanceValue>(writeOnlyValues.Count);
                }

                foreach (KeyValuePair<XName, object> pair in writeOnlyValues)
                {
                    // We use the indexer so that we can replace keys that already exist
                    _instanceMetadata[pair.Key] = new InstanceValue(pair.Value, InstanceValueOptions.Optional | InstanceValueOptions.WriteOnly);
                }
            }
        }

        // host-facing access to our cascading ExtensionManager resolution. Used by WorkflowApplicationEventArgs
        internal IEnumerable<T> InternalGetExtensions<T>() where T : class
        {
            return base.GetExtensions<T>();
        }


        private static void EventFrame(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            WorkflowEventData data = (WorkflowEventData)result.AsyncState;
            WorkflowApplication thisPtr = data.Instance;

            bool done = true;

            try
            {
                Exception abortException = null;

                try
                {
                    // The "false" is to notify that we are not still sync
                    done = data.NextCallback(result, thisPtr, false);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    abortException = e;
                }

                if (abortException != null)
                {
                    thisPtr.AbortInstance(abortException, true);
                }
            }
            finally
            {
                if (done)
                {
                    thisPtr.OnNotifyPaused();
                }
            }
        }

        private bool ShouldRaiseComplete(WorkflowInstanceState state)
        {
            return state == WorkflowInstanceState.Complete && !_hasRaisedCompleted;
        }

        private void Enqueue(InstanceOperation operation)
        {
            Enqueue(operation, false);
        }

        private void Enqueue(InstanceOperation operation, bool push)
        {
            lock (_pendingOperations)
            {
                operation.ActionId = _actionCount;

                if (_isBusy)
                {
                    // If base.IsReadOnly == false, we can't call the Controller yet because WorkflowInstance is not initialized.
                    // But that's okay; if the instance isn't initialized then the scheduler's not running yet, so no need to pause it.
                    if (operation.InterruptsScheduler && base.IsReadOnly)
                    {
                        this.Controller.RequestPause();
                    }

                    AddToPending(operation, push);
                }
                else
                {
                    // first make sure we're ready to run
                    if (operation.RequiresInitialized)
                    {
                        EnsureInitialized();
                    }

                    if (!operation.CanRun(this) && !this.IsInTerminalState)
                    {
                        AddToPending(operation, push);
                    }
                    else
                    {
                        // Action: Notifying an operation
                        _actionCount++;

                        // We've essentially just notified this
                        // operation that it is free to do its
                        // thing
                        try
                        {
                        }
                        finally
                        {
                            operation.Notified = true;
                            _isBusy = true;
                        }
                    }
                }
            }
        }

        private void IncrementPendingUnenqueud()
        {
            lock (_pendingOperations)
            {
                _pendingUnenqueued++;
            }
        }

        private void DecrementPendingUnenqueud()
        {
            lock (_pendingOperations)
            {
                _pendingUnenqueued--;
            }
        }

        private void AddToPending(InstanceOperation operation, bool push)
        {
            if (base.IsReadOnly)
            {
                // We're already initialized
                operation.RequiresInitialized = false;
            }

            if (push)
            {
                _pendingOperations.PushFront(operation);
            }
            else
            {
                _pendingOperations.Enqueue(operation);
            }

            operation.OnEnqueued();
        }

        private bool Remove(InstanceOperation operation)
        {
            lock (_pendingOperations)
            {
                return _pendingOperations.Remove(operation);
            }
        }

        private bool WaitForTurn(InstanceOperation operation, TimeSpan timeout)
        {
            Enqueue(operation);
            return this.WaitForTurnNoEnqueue(operation, timeout);
        }

        private bool WaitForTurnNoEnqueue(InstanceOperation operation, TimeSpan timeout)
        {
            if (!operation.WaitForTurn(timeout))
            {
                if (Remove(operation))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new TimeoutException(SR.TimeoutOnOperation(timeout)));
                }
            }
            return true;
        }

        private bool WaitForTurnAsync(InstanceOperation operation, TimeSpan timeout, Action<object, TimeoutException> callback, object state)
        {
            return WaitForTurnAsync(operation, false, timeout, callback, state);
        }

        private bool WaitForTurnAsync(InstanceOperation operation, bool push, TimeSpan timeout, Action<object, TimeoutException> callback, object state)
        {
            Enqueue(operation, push);

            return this.WaitForTurnNoEnqueueAsync(operation, timeout, callback, state);
        }

        private bool WaitForTurnNoEnqueueAsync(InstanceOperation operation, TimeSpan timeout, Action<object, TimeoutException> callback, object state)
        {
            if (s_waitAsyncCompleteCallback == null)
            {
                s_waitAsyncCompleteCallback = new Action<object, TimeoutException>(OnWaitAsyncComplete);
            }
            return operation.WaitForTurnAsync(timeout, s_waitAsyncCompleteCallback, new WaitForTurnData(callback, state, operation, this));
        }

        private static void OnWaitAsyncComplete(object state, TimeoutException exception)
        {
            WaitForTurnData data = (WaitForTurnData)state;

            if (!data.Instance.Remove(data.Operation))
            {
                exception = null;
            }

            data.Callback(data.State, exception);
        }

        // For when we know that the operation is non-null
        // and notified (like in async paths)
        private void ForceNotifyOperationComplete()
        {
            OnNotifyPaused();
        }

        private void NotifyOperationComplete(InstanceOperation operation)
        {
            if (operation != null && operation.Notified)
            {
                OnNotifyPaused();
            }
        }

        private InstanceOperation FindOperation()
        {
            if (_pendingOperations.Count > 0)
            {
                // Special case the first one
                InstanceOperation temp = _pendingOperations[0];

                if (temp.RequiresInitialized)
                {
                    EnsureInitialized();
                }

                // Even if we can't run this operation we want to notify
                // it if all the operations are invalid.  This will cause
                // the Validate* method to throw to the caller.
                if (temp.CanRun(this) || this.IsInTerminalState)
                {
                    // Action: Notifying an operation
                    _actionCount++;

                    temp.Notified = true;
                    _pendingOperations.Dequeue();
                    return temp;
                }
                else
                {
                    for (int i = 0; i < _pendingOperations.Count; i++)
                    {
                        temp = _pendingOperations[i];

                        if (temp.RequiresInitialized)
                        {
                            EnsureInitialized();
                        }

                        if (temp.CanRun(this))
                        {
                            // Action: Notifying an operation
                            _actionCount++;

                            temp.Notified = true;
                            _pendingOperations.Remove(i);
                            return temp;
                        }
                    }
                }
            }

            return null;
        }

        // assumes that we're called under the pendingOperations lock
        private void EnsureInitialized()
        {
            if (!base.IsReadOnly)
            {
                // For newly created workflows (e.g. not the Load() case), we need to initialize now
                base.RegisterExtensionManager(_extensions);
                base.Initialize(_initialWorkflowArguments, _rootExecutionProperties);

                // make sure we have a persistence manager if necessary
                if (_persistenceManager == null && _instanceStore != null)
                {
                    Fx.Assert(this.Id != Guid.Empty, "should have a valid Id at this point");
                    _persistenceManager = new PersistenceManager(_instanceStore, GetInstanceMetadata(), this.Id);
                }
            }
        }

        protected override void OnNotifyPaused()
        {
            Fx.Assert(_isBusy, "We're always busy when we get this notification.");

            WorkflowInstanceState? localInstanceState = null;
            if (base.IsReadOnly)
            {
                localInstanceState = this.Controller.State;
            }
            WorkflowApplicationState localApplicationState = _state;

            bool stillSync = true;

            while (stillSync)
            {
                if (localInstanceState.HasValue && ShouldRaiseComplete(localInstanceState.Value))
                {
                    Exception abortException = null;

                    try
                    {
                        // We're about to notify the world that this instance is completed
                        // so let's make it official.
                        _hasRaisedCompleted = true;

                        if (s_completedHandler == null)
                        {
                            s_completedHandler = new CompletedEventHandler();
                        }
                        stillSync = s_completedHandler.Run(this);
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        abortException = e;
                    }

                    if (abortException != null)
                    {
                        AbortInstance(abortException, true);
                    }
                }
                else
                {
                    InstanceOperation toRun = null;
                    bool shouldRunNow;
                    bool shouldRaiseIdleNow;

                    lock (_pendingOperations)
                    {
                        toRun = FindOperation();

                        // Cache the state in local variables to ensure that none 
                        // of the decision points in the ensuing "if" statement flip 
                        // when control gets out of the lock.
                        shouldRunNow = (localInstanceState.HasValue && localInstanceState == WorkflowInstanceState.Runnable && localApplicationState == WorkflowApplicationState.Runnable);
                        shouldRaiseIdleNow = _hasExecutionOccurredSinceLastIdle &&
                            localInstanceState.HasValue && localInstanceState == WorkflowInstanceState.Idle &&
                            !_hasRaisedCompleted && _pendingUnenqueued == 0;

                        if (toRun == null && !shouldRunNow && !shouldRaiseIdleNow)
                        {
                            _isBusy = false;
                            stillSync = false;
                        }
                    }

                    if (toRun != null)
                    {
                        toRun.NotifyTurn();
                        stillSync = false;
                    }
                    else if (shouldRaiseIdleNow)
                    {
                        _hasExecutionOccurredSinceLastIdle = false;

                        Fx.Assert(_isBusy, "we must be busy if we're raising idle");

                        Exception abortException = null;

                        try
                        {
                            if (s_idleHandler == null)
                            {
                                s_idleHandler = new IdleEventHandler();
                            }
                            stillSync = s_idleHandler.Run(this);
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            abortException = e;
                        }

                        if (abortException != null)
                        {
                            AbortInstance(abortException, true);
                        }
                    }
                    else if (shouldRunNow)
                    {
                        _hasExecutionOccurredSinceLastIdle = true;

                        // Action: Running the scheduler
                        _actionCount++;

                        this.Controller.Run();
                        stillSync = false;
                    }
                }
            }
        }

        // used by WorkflowInvoker in the InvokeAsync case
        internal void GetCompletionStatus(out Exception terminationException, out bool cancelled)
        {
            IDictionary<string, object> dummyOutputs;
            ActivityInstanceState completionState = this.Controller.GetCompletionState(out dummyOutputs, out terminationException);
            Fx.Assert(completionState != ActivityInstanceState.Executing, "Activity cannot be executing when this method is called");
            cancelled = (completionState == ActivityInstanceState.Canceled);
        }

        protected internal override void OnRequestAbort(Exception reason)
        {
            this.AbortInstance(reason, false);
        }

        public void Abort()
        {
            Abort(SR.DefaultAbortReason);
        }

        public void Abort(string reason)
        {
            this.Abort(reason, null);
        }

        private void Abort(string reason, Exception innerException)
        {
            // This is pretty loose check, but it is okay if we
            // go down the abort path multiple times
            if (_state != WorkflowApplicationState.Aborted)
            {
                AbortInstance(new WorkflowApplicationAbortedException(reason, innerException), false);
            }
        }

        private void AbortPersistence()
        {
            if (_persistenceManager != null)
            {
                _persistenceManager.Abort();
            }

            PersistencePipeline currentPersistencePipeline = _persistencePipelineInUse;
            if (currentPersistencePipeline != null)
            {
                currentPersistencePipeline.Abort();
            }
        }

        private void AbortInstance(Exception reason, bool isWorkflowThread)
        {
            _state = WorkflowApplicationState.Aborted;

            // Need to ensure that either components see the Aborted state, this method sees the components, or both.
            //Thread.MemoryBarrier();

            // We do this outside of the lock since persistence
            // might currently be blocking access to the lock.
            AbortPersistence();

            if (isWorkflowThread)
            {
                if (!_hasCalledAbort)
                {
                    _hasCalledAbort = true;
                    this.Controller.Abort(reason);

                    // We should get off this thread because we're unsure of its state
                    ScheduleTrackAndRaiseAborted(reason);
                }
            }
            else
            {
                bool completeSelf = true;
                InstanceOperation operation = null;

                try
                {
                    operation = new InstanceOperation();

                    completeSelf = WaitForTurnAsync(operation, true, ActivityDefaults.AcquireLockTimeout, new Action<object, TimeoutException>(OnAbortWaitComplete), reason);

                    if (completeSelf)
                    {
                        if (!_hasCalledAbort)
                        {
                            _hasCalledAbort = true;
                            this.Controller.Abort(reason);

                            // We need to get off this thread so we don't block the caller
                            // of abort
                            ScheduleTrackAndRaiseAborted(reason);
                        }
                    }
                }
                finally
                {
                    if (completeSelf)
                    {
                        NotifyOperationComplete(operation);
                    }
                }
            }
        }

        private void OnAbortWaitComplete(object state, TimeoutException exception)
        {
            if (exception != null)
            {
                // We eat this exception because we were simply doing our
                // best to get the lock.  Note that we won't proceed without
                // the lock because we may have already succeeded on another
                // thread.  Technically this abort call has failed.

                return;
            }

            bool shouldRaise = false;
            Exception reason = (Exception)state;

            try
            {
                if (!_hasCalledAbort)
                {
                    shouldRaise = true;
                    _hasCalledAbort = true;
                    this.Controller.Abort(reason);
                }
            }
            finally
            {
                ForceNotifyOperationComplete();
            }

            if (shouldRaise)
            {
                // We call this from this thread because we've already
                // had a thread switch
                TrackAndRaiseAborted(reason);
            }
        }

        private void ScheduleTrackAndRaiseAborted(Exception reason)
        {
            if (this.Controller.HasPendingTrackingRecords || this.Aborted != null)
            {
                ActionItem.Schedule(new Action<object>(TrackAndRaiseAborted), reason);
            }
        }

        // This is only ever called from an appropriate thread (not the thread
        // that called abort unless it was an internal abort).
        // This method is called without the lock.  We still provide single threaded
        // guarantees to the Controller because:
        //    * No other call can ever enter the executor again once the state has
        //      switched to Aborted
        //    * If this was an internal abort then the thread was fast pathing its
        //      way out of the runtime and won't conflict
        private void TrackAndRaiseAborted(object state)
        {
            Exception reason = (Exception)state;

            if (this.Controller.HasPendingTrackingRecords)
            {
                try
                {
                    IAsyncResult result = this.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, Fx.ThunkCallback(new AsyncCallback(OnAbortTrackingComplete)), reason);

                    if (result.CompletedSynchronously)
                    {
                        this.Controller.EndFlushTrackingRecords(result);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    // We eat any exception here because we are on the abort path
                    // and are doing a best effort to track this record.
                }
            }

            RaiseAborted(reason);
        }

        private void OnAbortTrackingComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            Exception reason = (Exception)result.AsyncState;

            try
            {
                this.Controller.EndFlushTrackingRecords(result);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                // We eat any exception here because we are on the abort path
                // and are doing a best effort to track this record.
            }

            RaiseAborted(reason);
        }

        private void RaiseAborted(Exception reason)
        {
            if (_invokeCompletedCallback == null)
            {
                Action<WorkflowApplicationAbortedEventArgs> abortedHandler = this.Aborted;

                if (abortedHandler != null)
                {
                    try
                    {
                        //this.handlerThreadId = Thread.CurrentThread.ManagedThreadId;
                        _isInHandler = true;

                        abortedHandler(new WorkflowApplicationAbortedEventArgs(this, reason));
                    }
                    finally
                    {
                        _isInHandler = false;
                    }
                }
            }
            else
            {
                _invokeCompletedCallback();
            }

            if (TD.WorkflowInstanceAbortedIsEnabled())
            {
                TD.WorkflowInstanceAborted(this.Id.ToString(), reason);
            }
        }

        public void Terminate(string reason)
        {
            Terminate(reason, ActivityDefaults.AcquireLockTimeout);
        }

        public void Terminate(Exception reason)
        {
            Terminate(reason, ActivityDefaults.AcquireLockTimeout);
        }

        public void Terminate(string reason, TimeSpan timeout)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("reason");
            }

            Terminate(new WorkflowApplicationTerminatedException(reason, this.Id), timeout);
        }

        public void Terminate(Exception reason, TimeSpan timeout)
        {
            if (reason == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("reason");
            }

            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            InstanceOperation operation = null;

            try
            {
                operation = new InstanceOperation();

                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForTerminate();

                TerminateCore(reason);

                this.Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        private void TerminateCore(Exception reason)
        {
            this.Controller.Terminate(reason);
        }

        public IAsyncResult BeginTerminate(string reason, AsyncCallback callback, object state)
        {
            return BeginTerminate(reason, ActivityDefaults.AcquireLockTimeout, callback, state);
        }

        public IAsyncResult BeginTerminate(Exception reason, AsyncCallback callback, object state)
        {
            return BeginTerminate(reason, ActivityDefaults.AcquireLockTimeout, callback, state);
        }

        public IAsyncResult BeginTerminate(string reason, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("reason");
            }

            return BeginTerminate(new WorkflowApplicationTerminatedException(reason, this.Id), timeout, callback, state);
        }

        public IAsyncResult BeginTerminate(Exception reason, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (reason == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("reason");
            }

            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return TerminateAsyncResult.Create(this, reason, timeout, callback, state);
        }

        public void EndTerminate(IAsyncResult result)
        {
            TerminateAsyncResult.End(result);
        }

        // called from the sync and async paths
        private void CancelCore()
        {
            // We only actually do any work if we haven't completed and we aren't
            // unloaded.
            if (!_hasRaisedCompleted && _state != WorkflowApplicationState.Unloaded)
            {
                this.Controller.ScheduleCancel();

                // This is a loose check, but worst case scenario we call
                // an extra, unnecessary Run
                if (!_hasCalledRun && !_hasRaisedCompleted)
                {
                    RunCore();
                }
            }
        }

        public void Cancel()
        {
            Cancel(ActivityDefaults.AcquireLockTimeout);
        }

        public void Cancel(TimeSpan timeout)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            InstanceOperation operation = null;

            try
            {
                operation = new InstanceOperation();

                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForCancel();

                CancelCore();

                this.Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        public IAsyncResult BeginCancel(AsyncCallback callback, object state)
        {
            return BeginCancel(ActivityDefaults.AcquireLockTimeout, callback, state);
        }

        public IAsyncResult BeginCancel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return CancelAsyncResult.Create(this, timeout, callback, state);
        }

        public void EndCancel(IAsyncResult result)
        {
            CancelAsyncResult.End(result);
        }

        // called on the Invoke path, this will go away when WorkflowInvoker implements WorkflowInstance directly
        private static WorkflowApplication CreateInstance(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, SynchronizationContext syncContext, Action invokeCompletedCallback)
        {
            // 1) Create the workflow instance
            //Transaction ambientTransaction = Transaction.Current;
            List<Handle> workflowExecutionProperties = null;

            //if (ambientTransaction != null)
            //{
            //    // no need for a NoPersistHandle since the ActivityExecutor performs a no-persist zone
            //    // as part of the RuntimeTransactionHandle processing
            //    workflowExecutionProperties = new List<Handle>(1)
            //    {
            //        new RuntimeTransactionHandle(ambientTransaction)
            //    };
            //}

            WorkflowApplication instance = new WorkflowApplication(activity, inputs, workflowExecutionProperties)
            {
                SynchronizationContext = syncContext
            };

            bool success = false;

            try
            {
                // 2) Take the executor lock before allowing extensions to be added
                instance._isBusy = true;

                // 3) Add extensions
                if (extensions != null)
                {
                    instance._extensions = extensions;
                }

                // 4) Setup miscellaneous state
                instance._invokeCompletedCallback = invokeCompletedCallback;

                success = true;
            }
            finally
            {
                if (!success)
                {
                    instance._isBusy = false;
                }
            }

            return instance;
        }

        private static void RunInstance(WorkflowApplication instance)
        {
            // We still have the lock because we took it in Create

            // first make sure we're ready to run
            instance.EnsureInitialized();

            // Shortcut path for resuming the instance
            instance.RunCore();

            instance._hasExecutionOccurredSinceLastIdle = true;
            instance.Controller.Run();
        }

        private static WorkflowApplication StartInvoke(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, SynchronizationContext syncContext, Action invokeCompletedCallback, AsyncInvokeContext invokeContext)
        {
            WorkflowApplication instance = CreateInstance(activity, inputs, extensions, syncContext, invokeCompletedCallback);
            if (invokeContext != null)
            {
                invokeContext.WorkflowApplication = instance;
            }
            RunInstance(instance);
            return instance;
        }

        internal static IDictionary<string, object> Invoke(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, TimeSpan timeout)
        {
            Fx.Assert(activity != null, "Activity must not be null.");

            // Create the invoke synchronization context
            PumpBasedSynchronizationContext syncContext = new PumpBasedSynchronizationContext(timeout);
            WorkflowApplication instance = CreateInstance(activity, inputs, extensions, syncContext, new Action(syncContext.OnInvokeCompleted));
            // Wait for completion
            try
            {
                RunInstance(instance);
                syncContext.DoPump();
            }
            catch (TimeoutException)
            {
                instance.Abort(SR.AbortingDueToInstanceTimeout);
                throw;
            }

            Exception completionException = null;
            IDictionary<string, object> outputs = null;

            if (instance.Controller.State == WorkflowInstanceState.Aborted)
            {
                completionException = new WorkflowApplicationAbortedException(SR.DefaultAbortReason, instance.Controller.GetAbortReason());
            }
            else
            {
                Fx.Assert(instance.Controller.State == WorkflowInstanceState.Complete, "We should only get here when we are completed.");

                instance.Controller.GetCompletionState(out outputs, out completionException);
            }

            if (completionException != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(completionException);
            }

            return outputs;
        }

        internal static IAsyncResult BeginInvoke(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, TimeSpan timeout, SynchronizationContext syncContext, AsyncInvokeContext invokeContext, AsyncCallback callback, object state)
        {
            Fx.Assert(activity != null, "The activity must not be null.");

            return new InvokeAsyncResult(activity, inputs, extensions, timeout, syncContext, invokeContext, callback, state);
        }

        internal static IDictionary<string, object> EndInvoke(IAsyncResult result)
        {
            return InvokeAsyncResult.End(result);
        }

        public void Run()
        {
            Run(ActivityDefaults.AcquireLockTimeout);
        }

        public void Run(TimeSpan timeout)
        {
            InternalRun(timeout, true);
        }

        private void InternalRun(TimeSpan timeout, bool isUserRun)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            InstanceOperation operation = null;

            try
            {
                operation = new InstanceOperation();

                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForRun();

                if (isUserRun)
                {
                    // We set this to true here so that idle is raised
                    // regardless of whether the call to Run resulted
                    // in execution.
                    _hasExecutionOccurredSinceLastIdle = true;
                }

                RunCore();

                this.Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        private void RunCore()
        {
            if (!_hasCalledRun)
            {
                _hasCalledRun = true;
            }

            _state = WorkflowApplicationState.Runnable;
        }

        public IAsyncResult BeginRun(AsyncCallback callback, object state)
        {
            return BeginRun(ActivityDefaults.AcquireLockTimeout, callback, state);
        }

        public IAsyncResult BeginRun(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginInternalRun(timeout, true, callback, state);
        }

        private IAsyncResult BeginInternalRun(TimeSpan timeout, bool isUserRun, AsyncCallback callback, object state)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return RunAsyncResult.Create(this, isUserRun, timeout, callback, state);
        }

        public void EndRun(IAsyncResult result)
        {
            RunAsyncResult.End(result);
        }

        //// shared by Load/BeginLoad
        //bool IsLoadTransactionRequired()
        //{
        //    return base.GetExtensions<IPersistencePipelineModule>().Any(module => module.IsLoadTransactionRequired);
        //}

        private void CreatePersistenceManager()
        {
            PersistenceManager newManager = new PersistenceManager(this.InstanceStore, GetInstanceMetadata(), _instanceId);
            SetPersistenceManager(newManager);
        }

        // shared by Load(WorkflowApplicationInstance)/BeginLoad*
        private void SetPersistenceManager(PersistenceManager newManager)
        {
            Fx.Assert(_persistenceManager == null, "SetPersistenceManager should only be called once");

            // first register our extensions since we'll need them to construct the pipeline
            base.RegisterExtensionManager(_extensions);
            _persistenceManager = newManager;
        }

        // shared by Load/BeginLoad
        private PersistencePipeline ProcessInstanceValues(IDictionary<XName, InstanceValue> values, out object deserializedRuntimeState)
        {
            PersistencePipeline result = null;
            deserializedRuntimeState = ExtractRuntimeState(values, _persistenceManager.InstanceId);

            if (HasPersistenceModule)
            {
                IEnumerable<IPersistencePipelineModule> modules = base.GetExtensions<IPersistencePipelineModule>();
                result = new PersistencePipeline(modules);
                result.SetLoadedValues(values);
            }

            return result;
        }

        private static ActivityExecutor ExtractRuntimeState(IDictionary<XName, InstanceValue> values, Guid instanceId)
        {
            InstanceValue value;
            if (values.TryGetValue(WorkflowNamespace.Workflow, out value))
            {
                ActivityExecutor result = value.Value as ActivityExecutor;
                if (result != null)
                {
                    return result;
                }
            }
            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InstancePersistenceException(SR.WorkflowInstanceNotFoundInStore(instanceId)));
        }

        public static void CreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
        {
            CreateDefaultInstanceOwner(instanceStore, definitionIdentity, identityFilter, ActivityDefaults.OpenTimeout);
        }

        public static void CreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter, TimeSpan timeout)
        {
            if (instanceStore == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
            }
            if (instanceStore.DefaultInstanceOwner != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("instanceStore", SR.InstanceStoreHasDefaultOwner);
            }

            CreateWorkflowOwnerWithIdentityCommand command = GetCreateOwnerCommand(definitionIdentity, identityFilter);
            InstanceView commandResult = ExecuteInstanceCommandWithTemporaryHandle(instanceStore, command, timeout);
            instanceStore.DefaultInstanceOwner = commandResult.InstanceOwner;
        }

        //public static IAsyncResult BeginCreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity,
        //    WorkflowIdentityFilter identityFilter, AsyncCallback callback, object state)
        //{
        //    return BeginCreateDefaultInstanceOwner(instanceStore, definitionIdentity, identityFilter, ActivityDefaults.OpenTimeout, callback, state);
        //}

        //public static IAsyncResult BeginCreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity,
        //    WorkflowIdentityFilter identityFilter, TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    if (instanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
        //    }
        //    if (instanceStore.DefaultInstanceOwner != null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("instanceStore", SR.InstanceStoreHasDefaultOwner);
        //    }

        //    CreateWorkflowOwnerWithIdentityCommand command = GetCreateOwnerCommand(definitionIdentity, identityFilter);
        //    return new InstanceCommandWithTemporaryHandleAsyncResult(instanceStore, command, timeout, callback, state);
        //}

        //public static void EndCreateDefaultInstanceOwner(IAsyncResult asyncResult)
        //{
        //    InstanceStore instanceStore;
        //    InstanceView commandResult;
        //    InstanceCommandWithTemporaryHandleAsyncResult.End(asyncResult, out instanceStore, out commandResult);
        //    instanceStore.DefaultInstanceOwner = commandResult.InstanceOwner;
        //}

        //public static void DeleteDefaultInstanceOwner(InstanceStore instanceStore)
        //{
        //    DeleteDefaultInstanceOwner(instanceStore, ActivityDefaults.CloseTimeout);
        //}

        //public static void DeleteDefaultInstanceOwner(InstanceStore instanceStore, TimeSpan timeout)
        //{
        //    if (instanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
        //    }
        //    if (instanceStore.DefaultInstanceOwner == null)
        //    {
        //        return;
        //    }

        //    DeleteWorkflowOwnerCommand command = new DeleteWorkflowOwnerCommand();
        //    ExecuteInstanceCommandWithTemporaryHandle(instanceStore, command, timeout);
        //    instanceStore.DefaultInstanceOwner = null;
        //}

        //public static IAsyncResult BeginDeleteDefaultInstanceOwner(InstanceStore instanceStore, AsyncCallback callback, object state)
        //{
        //    return BeginDeleteDefaultInstanceOwner(instanceStore, ActivityDefaults.CloseTimeout, callback, state);
        //}

        //public static IAsyncResult BeginDeleteDefaultInstanceOwner(InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    if (instanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
        //    }
        //    if (instanceStore.DefaultInstanceOwner == null)
        //    {
        //        return new CompletedAsyncResult(callback, state);
        //    }

        //    DeleteWorkflowOwnerCommand command = new DeleteWorkflowOwnerCommand();
        //    return new InstanceCommandWithTemporaryHandleAsyncResult(instanceStore, command, timeout, callback, state);
        //}

        //public static void EndDeleteDefaultInstanceOwner(IAsyncResult asyncResult)
        //{
        //    InstanceStore instanceStore;
        //    InstanceView commandResult;

        //    if (asyncResult is CompletedAsyncResult)
        //    {
        //        CompletedAsyncResult.End(asyncResult);
        //        return;
        //    }

        //    InstanceCommandWithTemporaryHandleAsyncResult.End(asyncResult, out instanceStore, out commandResult);            
        //    instanceStore.DefaultInstanceOwner = null;
        //}

        private static InstanceView ExecuteInstanceCommandWithTemporaryHandle(InstanceStore instanceStore, InstancePersistenceCommand command, TimeSpan timeout)
        {
            InstanceHandle temporaryHandle = null;
            try
            {
                temporaryHandle = instanceStore.CreateInstanceHandle();
                return instanceStore.Execute(temporaryHandle, command, timeout);
            }
            finally
            {
                if (temporaryHandle != null)
                {
                    temporaryHandle.Free();
                }
            }
        }

        private static CreateWorkflowOwnerWithIdentityCommand GetCreateOwnerCommand(WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
        {
            if (!identityFilter.IsValid())
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new ArgumentOutOfRangeException("identityFilter"));
            }
            if (definitionIdentity == null && identityFilter != WorkflowIdentityFilter.Any)
            {
                // This API isn't useful for null identity, because WFApp only adds a default WorkflowHostType
                // to instances with non-null identity.
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("definitionIdentity", SR.CannotCreateOwnerWithoutIdentity);
            }
            return new CreateWorkflowOwnerWithIdentityCommand
            {
                InstanceOwnerMetadata =
                {
                    { WorkflowNamespace.WorkflowHostType, new InstanceValue(Workflow45Namespace.WorkflowApplication) },
                    { Workflow45Namespace.DefinitionIdentities, new InstanceValue(new Collection<WorkflowIdentity> { definitionIdentity }) },
                    { Workflow45Namespace.DefinitionIdentityFilter, new InstanceValue(identityFilter) },
                }
            };
        }

        public static WorkflowApplicationInstance GetRunnableInstance(InstanceStore instanceStore)
        {
            return GetRunnableInstance(instanceStore, ActivityDefaults.LoadTimeout);
        }

        public static WorkflowApplicationInstance GetRunnableInstance(InstanceStore instanceStore, TimeSpan timeout)
        {
            if (instanceStore == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
            }
            TimeoutHelper.ThrowIfNegativeArgument(timeout);
            if (instanceStore.DefaultInstanceOwner == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.GetRunnableRequiresOwner));
            }

            PersistenceManager newManager = new PersistenceManager(instanceStore, null);
            return LoadCore(timeout, true, newManager);
        }

        //   public static IAsyncResult BeginGetRunnableInstance(InstanceStore instanceStore, AsyncCallback callback, object state)
        //   {
        //       return BeginGetRunnableInstance(instanceStore, ActivityDefaults.LoadTimeout, callback, state);
        //   }

        //   public static IAsyncResult BeginGetRunnableInstance(InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
        //   {
        //       if (instanceStore == null)
        //       {
        //           throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
        //       }
        //       TimeoutHelper.ThrowIfNegativeArgument(timeout);
        //       if (instanceStore.DefaultInstanceOwner == null)
        //       {
        //           throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.GetRunnableRequiresOwner));
        //       }

        //       PersistenceManager newManager = new PersistenceManager(instanceStore, null);
        //       return new LoadAsyncResult(null, newManager, true, timeout, callback, state);
        //}

        //   public static WorkflowApplicationInstance EndGetRunnableInstance(IAsyncResult asyncResult)
        //   {
        //       return LoadAsyncResult.EndAndCreateInstance(asyncResult);
        //   }

        public static WorkflowApplicationInstance GetInstance(Guid instanceId, InstanceStore instanceStore)
        {
            return GetInstance(instanceId, instanceStore, ActivityDefaults.LoadTimeout);
        }

        public static WorkflowApplicationInstance GetInstance(Guid instanceId, InstanceStore instanceStore, TimeSpan timeout)
        {
            if (instanceId == Guid.Empty)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("instanceId");
            }
            if (instanceStore == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
            }
            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            PersistenceManager newManager = new PersistenceManager(instanceStore, null, instanceId);
            return LoadCore(timeout, false, newManager);
        }

        //public static IAsyncResult BeginGetInstance(Guid instanceId, InstanceStore instanceStore, AsyncCallback callback, object state)
        //{
        //    return BeginGetInstance(instanceId, instanceStore, ActivityDefaults.LoadTimeout, callback, state);
        //}

        //public static IAsyncResult BeginGetInstance(Guid instanceId, InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    if (instanceId == Guid.Empty)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("instanceId");
        //    }
        //    if (instanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instanceStore");
        //    }
        //    TimeoutHelper.ThrowIfNegativeArgument(timeout);

        //    PersistenceManager newManager = new PersistenceManager(instanceStore, null, instanceId);
        //    return new LoadAsyncResult(null, newManager, false, timeout, callback, state);
        //}

        //public static WorkflowApplicationInstance EndGetInstance(IAsyncResult asyncResult)
        //{
        //    return LoadAsyncResult.EndAndCreateInstance(asyncResult);
        //}

        public void Load(WorkflowApplicationInstance instance)
        {
            Load(instance, ActivityDefaults.LoadTimeout);
        }

        //public void Load(WorkflowApplicationInstance instance, TimeSpan timeout)
        //{
        //    Load(instance, null, timeout);
        //}

        //public void Load(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap)
        //{
        //    Load(instance, updateMap, ActivityDefaults.LoadTimeout);
        //}

        public void Load(WorkflowApplicationInstance instance, /*DynamicUpdateMap updateMap,*/ TimeSpan timeout)
        {
            ThrowIfAborted();
            ThrowIfReadOnly(); // only allow a single Load() or Run()
            if (instance == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instance");
            }

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            if (_instanceIdSet)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
            }
            if (_initialWorkflowArguments != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
            }
            if (this.InstanceStore != null && this.InstanceStore != instance.InstanceStore)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("instance", SR.InstanceStoreDoesntMatchWorkflowApplication);
            }

            instance.MarkAsLoaded();

            InstanceOperation operation = new InstanceOperation { RequiresInitialized = false };

            try
            {
                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForLoad();

                _instanceId = instance.InstanceId;
                _instanceIdSet = true;
                if (_instanceStore == null)
                {
                    _instanceStore = instance.InstanceStore;
                }

                PersistenceManager newManager = (PersistenceManager)instance.PersistenceManager;
                newManager.SetInstanceMetadata(GetInstanceMetadata());
                SetPersistenceManager(newManager);

                LoadCore(/*updateMap,*/ timeoutHelper, false, instance.Values);
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        public void LoadRunnableInstance()
        {
            LoadRunnableInstance(ActivityDefaults.LoadTimeout);
        }

        public void LoadRunnableInstance(TimeSpan timeout)
        {
            ThrowIfReadOnly(); // only allow a single Load() or Run()

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            if (this.InstanceStore == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
            }
            if (_instanceIdSet)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
            }
            if (_initialWorkflowArguments != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
            }
            if (_persistenceManager != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
            }

            InstanceOperation operation = new InstanceOperation { RequiresInitialized = false };

            try
            {
                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForLoad();

                RegisterExtensionManager(_extensions);
                _persistenceManager = new PersistenceManager(InstanceStore, GetInstanceMetadata());

                if (!_persistenceManager.IsInitialized)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
                }

                LoadCore(/*null,*/ timeoutHelper, true);
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        public void Load(Guid instanceId)
        {
            Load(instanceId, ActivityDefaults.LoadTimeout);
        }

        public void Load(Guid instanceId, TimeSpan timeout)
        {
            ThrowIfAborted();
            ThrowIfReadOnly(); // only allow a single Load() or Run()
            if (instanceId == Guid.Empty)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("instanceId");
            }

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            if (this.InstanceStore == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
            }
            if (_instanceIdSet)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
            }
            if (_initialWorkflowArguments != null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
            }

            InstanceOperation operation = new InstanceOperation { RequiresInitialized = false };

            try
            {
                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForLoad();

                _instanceId = instanceId;
                _instanceIdSet = true;

                CreatePersistenceManager();
                LoadCore(/*null,*/ timeoutHelper, false);
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        private void LoadCore(/*DynamicUpdateMap updateMap,*/ TimeoutHelper timeoutHelper, bool loadAny, IDictionary<XName, InstanceValue> values = null)
        {
            if (values == null)
            {
                if (!_persistenceManager.IsInitialized)
                {
                    _persistenceManager.Initialize(this.DefinitionIdentity, timeoutHelper.RemainingTime());
                }
            }
            else
            {
                Fx.Assert(_persistenceManager.IsInitialized, "Caller should have initialized Persistence Manager");
                Fx.Assert(_instanceIdSet, "Caller should have set InstanceId");
            }

            PersistencePipeline pipeline = null;
            //WorkflowPersistenceContext context = null;
            //TransactionScope scope = null;
            bool success = false;
            Exception abortReasonInnerException = null;
            try
            {
                //InitializePersistenceContext(timeoutHelper, out context);

                if (values == null)
                {
                    values = LoadValues(_persistenceManager, timeoutHelper, loadAny);
                    if (loadAny)
                    {
                        if (_instanceIdSet)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
                        }

                        _instanceId = _persistenceManager.InstanceId;
                        _instanceIdSet = true;
                    }
                }
                object deserializedRuntimeState;
                pipeline = ProcessInstanceValues(values, out deserializedRuntimeState);

                if (pipeline != null)
                {
                    try
                    {
                        _persistencePipelineInUse = pipeline;

                        // Need to ensure that either we see the Aborted state, AbortInstance sees us, or both.
                        if (_state == WorkflowApplicationState.Aborted)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                        }

                        pipeline.EndLoad(pipeline.BeginLoad(timeoutHelper.RemainingTime(), null, null));
                    }
                    finally
                    {
                        _persistencePipelineInUse = null;
                    }
                }

                try
                {
                    base.Initialize(deserializedRuntimeState);
                    //if (updateMap != null)
                    //{
                    //    UpdateInstanceMetadata();
                    //}
                }
                //catch (InstanceUpdateException e)
                //{
                //    abortReasonInnerException = e;
                //    throw;
                //}
                catch (VersionMismatchException e)
                {
                    abortReasonInnerException = e;
                    throw;
                }

                success = true;
            }
            finally
            {
                //CompletePersistenceContext(context, success);
                if (!success)
                {
                    this.AbortDueToException(abortReasonInnerException);
                }
            }

            if (pipeline != null)
            {
                pipeline.Publish();
            }
        }

        private void AbortDueToException(Exception e)
        {
            //if (e is InstanceUpdateException)
            //{
            //    this.Abort(SR.AbortingDueToDynamicUpdateFailure, e);
            //}
            //else 
            if (e is VersionMismatchException)
            {
                this.Abort(SR.AbortingDueToVersionMismatch, e);
            }
            else
            {
                this.Abort(SR.AbortingDueToLoadFailure);
            }
        }

        private static WorkflowApplicationInstance LoadCore(TimeSpan timeout, bool loadAny, PersistenceManager persistenceManager)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            if (!persistenceManager.IsInitialized)
            {
                persistenceManager.Initialize(s_unknownIdentity, timeoutHelper.RemainingTime());
            }

            //WorkflowPersistenceContext context = null;
            //TransactionScope scope = null;
            WorkflowApplicationInstance result = null;
            try
            {
                //InitializePersistenceContext(timeoutHelper, out context);

                IDictionary<XName, InstanceValue> values = LoadValues(persistenceManager, timeoutHelper, loadAny);
                ActivityExecutor deserializedRuntimeState = ExtractRuntimeState(values, persistenceManager.InstanceId);
                result = new WorkflowApplicationInstance(persistenceManager, values, deserializedRuntimeState.WorkflowIdentity);
            }
            finally
            {
                bool success = (result != null);
                //CompletePersistenceContext(context, success);
                if (!success)
                {
                    persistenceManager.Abort();
                }
            }

            return result;
        }

        //static void InitializePersistenceContext(bool isTransactionRequired, TimeoutHelper timeoutHelper,
        //    out WorkflowPersistenceContext context, out TransactionScope scope)
        //{
        //    context = new WorkflowPersistenceContext(isTransactionRequired, timeoutHelper.OriginalTimeout);
        //    scope = TransactionHelper.CreateTransactionScope(context.PublicTransaction);
        //}

        //static void InitializePersistenceContext(TimeoutHelper timeoutHelper, out WorkflowPersistenceContext context)
        //{
        //    context = new WorkflowPersistenceContext(false, timeoutHelper.OriginalTimeout);
        //}

        //static void CompletePersistenceContext(WorkflowPersistenceContext context, TransactionScope scope, bool success)
        //{
        //    // Clean up the transaction scope regardless of failure
        //    TransactionHelper.CompleteTransactionScope(ref scope);

        //    if (context != null)
        //    {
        //        if (success)
        //        {
        //            context.Complete();
        //        }
        //        else
        //        {
        //            context.Abort();
        //        }
        //    }
        //}

        //static void CompletePersistenceContext(WorkflowPersistenceContext context, bool success)
        //{
        //    if (context != null)
        //    {
        //        if (success)
        //        {
        //            context.Complete();
        //        }
        //        else
        //        {
        //            context.Abort();
        //        }
        //    }
        //}

        private static IDictionary<XName, InstanceValue> LoadValues(PersistenceManager persistenceManager, TimeoutHelper timeoutHelper, bool loadAny)
        {
            IDictionary<XName, InstanceValue> values;
            if (loadAny)
            {
                if (!persistenceManager.TryLoad(timeoutHelper.RemainingTime(), out values))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InstanceNotReadyException(SR.NoRunnableInstances));
                }
            }
            else
            {
                values = persistenceManager.Load(timeoutHelper.RemainingTime());
            }

            return values;
        }

        internal static void DiscardInstance(PersistenceManagerBase persistanceManager, TimeSpan timeout)
        {
            PersistenceManager manager = (PersistenceManager)persistanceManager;
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            UnlockInstance(manager, timeoutHelper);
        }

        //internal static IAsyncResult BeginDiscardInstance(PersistenceManagerBase persistanceManager, TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    PersistenceManager manager = (PersistenceManager)persistanceManager;
        //    TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
        //    return new UnlockInstanceAsyncResult(manager, timeoutHelper, callback, state);
        //}

        //internal static void EndDiscardInstance(IAsyncResult asyncResult)
        //{
        //    UnlockInstanceAsyncResult.End(asyncResult);
        //}

        private static void UnlockInstance(PersistenceManager persistenceManager, TimeoutHelper timeoutHelper)
        {
            try
            {
                if (persistenceManager.OwnerWasCreated)
                {
                    // if the owner was created by this WorkflowApplication, delete it.
                    // This implicitly unlocks the instance.
                    persistenceManager.DeleteOwner(timeoutHelper.RemainingTime());
                }
                else
                {
                    persistenceManager.Unlock(timeoutHelper.RemainingTime());
                }
            }
            finally
            {
                persistenceManager.Abort();
            }
        }

        //internal static IList<ActivityBlockingUpdate> GetActivitiesBlockingUpdate(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap)
        //{
        //    object deserializedRuntimeState = ExtractRuntimeState(instance.Values, instance.InstanceId);
        //    return WorkflowInstance.GetActivitiesBlockingUpdate(deserializedRuntimeState, updateMap);
        //}

        //public IAsyncResult BeginLoadRunnableInstance(AsyncCallback callback, object state)
        //{
        //    return BeginLoadRunnableInstance(ActivityDefaults.LoadTimeout, callback, state);
        //}

        //public IAsyncResult BeginLoadRunnableInstance(TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    ThrowIfReadOnly(); // only allow a single Load() or Run()

        //    TimeoutHelper.ThrowIfNegativeArgument(timeout);

        //    if (this.InstanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        //    }
        //    if (this.instanceIdSet)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        //    }
        //    if (this.initialWorkflowArguments != null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        //    }
        //    if (this.persistenceManager != null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
        //    }

        //    PersistenceManager newManager = new PersistenceManager(InstanceStore, GetInstanceMetadata());
        //    if (!newManager.IsInitialized)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
        //    }

        //    return new LoadAsyncResult(this, newManager, true, timeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(Guid instanceId, AsyncCallback callback, object state)
        //{
        //    return BeginLoad(instanceId, ActivityDefaults.LoadTimeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(Guid instanceId, TimeSpan timeout, AsyncCallback callback, object state)
        //{
        //    ThrowIfAborted();
        //    ThrowIfReadOnly(); // only allow a single Load() or Run()
        //    if (instanceId == Guid.Empty)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("instanceId");
        //    }

        //    TimeoutHelper.ThrowIfNegativeArgument(timeout);

        //    if (this.InstanceStore == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        //    }
        //    if (this.instanceIdSet)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        //    }
        //    if (this.initialWorkflowArguments != null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        //    }

        //    PersistenceManager newManager = new PersistenceManager(this.InstanceStore, GetInstanceMetadata(), instanceId);

        //    return new LoadAsyncResult(this, newManager, false, timeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, AsyncCallback callback, object state)
        //{
        //    return BeginLoad(instance, /*null,*/ ActivityDefaults.LoadTimeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, TimeSpan timeout,
        //    AsyncCallback callback, object state)
        //{
        //    return BeginLoad(instance, null, timeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap,
        //    AsyncCallback callback, object state)
        //{
        //    return BeginLoad(instance, updateMap, ActivityDefaults.LoadTimeout, callback, state);
        //}

        //public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, /*DynamicUpdateMap updateMap,*/ TimeSpan timeout,
        //    AsyncCallback callback, object state)
        //{
        //    ThrowIfAborted();
        //    ThrowIfReadOnly(); // only allow a single Load() or Run()
        //    if (instance == null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("instance");
        //    }

        //    TimeoutHelper.ThrowIfNegativeArgument(timeout);

        //    if (this.instanceIdSet)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        //    }
        //    if (this.initialWorkflowArguments != null)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        //    }
        //    if (this.InstanceStore != null && this.InstanceStore != instance.InstanceStore)
        //    {
        //        throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("instance", SR.InstanceStoreDoesntMatchWorkflowApplication);
        //    }

        //    instance.MarkAsLoaded();
        //    PersistenceManager newManager = (PersistenceManager)instance.PersistenceManager;
        //    newManager.SetInstanceMetadata(GetInstanceMetadata());

        //    return new LoadAsyncResult(this, newManager, instance.Values, /*updateMap,*/ timeout, callback, state);
        //}

        //public void EndLoad(IAsyncResult result)
        //{
        //    LoadAsyncResult.End(result);
        //}

        //public void EndLoadRunnableInstance(IAsyncResult result)
        //{
        //    LoadAsyncResult.End(result);
        //}

        protected override void OnNotifyUnhandledException(Exception exception, Activity exceptionSource, string exceptionSourceInstanceId)
        {
            bool done = true;

            try
            {
                Exception abortException = null;

                try
                {
                    if (s_unhandledExceptionHandler == null)
                    {
                        s_unhandledExceptionHandler = new UnhandledExceptionEventHandler();
                    }

                    done = s_unhandledExceptionHandler.Run(this, exception, exceptionSource, exceptionSourceInstanceId);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    abortException = e;
                }

                if (abortException != null)
                {
                    AbortInstance(abortException, true);
                }
            }
            finally
            {
                if (done)
                {
                    OnNotifyPaused();
                }
            }
        }

        private IAsyncResult BeginInternalPersist(PersistenceOperation operation, TimeSpan timeout, bool isInternalPersist, AsyncCallback callback, object state)
        {
            return new UnloadOrPersistAsyncResult(this, timeout, operation, true, isInternalPersist, callback, state);
        }

        private void EndInternalPersist(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult.End(result);
        }

        private void TrackPersistence(PersistenceOperation operation)
        {
            if (this.Controller.TrackingEnabled)
            {
                if (operation == PersistenceOperation.Complete)
                {
                    this.Controller.Track(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Deleted, this.DefinitionIdentity));
                }
                else if (operation == PersistenceOperation.Unload)
                {
                    this.Controller.Track(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Unloaded, this.DefinitionIdentity));
                }
                else
                {
                    this.Controller.Track(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Persisted, this.DefinitionIdentity));
                }
            }
        }

        private void PersistCore(ref TimeoutHelper timeoutHelper, PersistenceOperation operation)
        {
            if (HasPersistenceProvider)
            {
                if (!_persistenceManager.IsInitialized)
                {
                    _persistenceManager.Initialize(this.DefinitionIdentity, timeoutHelper.RemainingTime());
                }
                //if (!this.persistenceManager.IsLocked && Transaction.Current != null)
                //{
                //    this.persistenceManager.EnsureReadyness(timeoutHelper.RemainingTime());
                //}

                // Do the tracking before preparing in case the tracking data is being pushed into
                // an extension and persisted transactionally with the instance state.
                TrackPersistence(operation);

                this.Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
            }

            bool success = false;
            //WorkflowPersistenceContext context = null;
            //TransactionScope scope = null;

            try
            {
                IDictionary<XName, InstanceValue> data = null;
                PersistencePipeline pipeline = null;
                if (HasPersistenceModule)
                {
                    IEnumerable<IPersistencePipelineModule> modules = base.GetExtensions<IPersistencePipelineModule>();
                    pipeline = new PersistencePipeline(modules, PersistenceManager.GenerateInitialData(this));
                    pipeline.Collect();
                    pipeline.Map();
                    data = pipeline.Values;
                }

                if (HasPersistenceProvider)
                {
                    if (data == null)
                    {
                        data = PersistenceManager.GenerateInitialData(this);
                    }

                    //if (context == null)
                    //{
                    //    //Fx.Assert(scope == null, "Should not have been able to set up a scope.");
                    //    InitializePersistenceContext(timeoutHelper, out context);
                    //}

                    _persistenceManager.Save(data, operation, timeoutHelper.RemainingTime());
                }

                if (pipeline != null)
                {
                    //if (context == null)
                    //{
                    //    //Fx.Assert(scope == null, "Should not have been able to set up a scope if we had no context.");
                    //    InitializePersistenceContext(timeoutHelper, out context);
                    //}

                    try
                    {
                        _persistencePipelineInUse = pipeline;

                        // Need to ensure that either we see the Aborted state, AbortInstance sees us, or both.
                        //Thread.MemoryBarrier();

                        if (_state == WorkflowApplicationState.Aborted)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                        }

                        pipeline.EndSave(pipeline.BeginSave(timeoutHelper.RemainingTime(), null, null));
                    }
                    finally
                    {
                        _persistencePipelineInUse = null;
                    }
                }

                success = true;
            }
            finally
            {
                //CompletePersistenceContext(context, success);

                if (success)
                {
                    if (operation != PersistenceOperation.Save)
                    {
                        // Stop execution if we've given up the instance lock
                        _state = WorkflowApplicationState.Paused;

                        if (TD.WorkflowApplicationUnloadedIsEnabled())
                        {
                            TD.WorkflowApplicationUnloaded(this.Id.ToString());
                        }
                    }
                    else
                    {
                        if (TD.WorkflowApplicationPersistedIsEnabled())
                        {
                            TD.WorkflowApplicationPersisted(this.Id.ToString());
                        }
                    }

                    if (operation == PersistenceOperation.Complete || operation == PersistenceOperation.Unload)
                    {
                        // We did a Delete or Unload, so if we have a persistence provider, tell it to delete the owner.
                        if (HasPersistenceProvider && _persistenceManager.OwnerWasCreated)
                        {
                            // This will happen to be under the caller's transaction, if there is one.
                            _persistenceManager.DeleteOwner(timeoutHelper.RemainingTime());
                        }

                        MarkUnloaded();
                    }
                }
            }
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public void Persist()
        {
            Persist(ActivityDefaults.SaveTimeout);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public void Persist(TimeSpan timeout)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            RequiresPersistenceOperation operation = new RequiresPersistenceOperation();

            try
            {
                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForPersist();

                PersistCore(ref timeoutHelper, PersistenceOperation.Save);
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public IAsyncResult BeginPersist(AsyncCallback callback, object state)
        {
            return BeginPersist(ActivityDefaults.SaveTimeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public IAsyncResult BeginPersist(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return new UnloadOrPersistAsyncResult(this, timeout, PersistenceOperation.Save, false, false, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public void EndPersist(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult.End(result);
        }

        // called from WorkflowApplicationIdleEventArgs
        internal ReadOnlyCollection<BookmarkInfo> GetBookmarksForIdle()
        {
            return this.Controller.GetBookmarks();
        }

        public ReadOnlyCollection<BookmarkInfo> GetBookmarks()
        {
            return GetBookmarks(ActivityDefaults.ResumeBookmarkTimeout);
        }

        public ReadOnlyCollection<BookmarkInfo> GetBookmarks(TimeSpan timeout)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            InstanceOperation operation = new InstanceOperation();

            try
            {
                WaitForTurn(operation, timeout);

                ValidateStateForGetAllBookmarks();

                return this.Controller.GetBookmarks();
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        protected internal override IAsyncResult OnBeginPersist(AsyncCallback callback, object state)
        {
            return this.BeginInternalPersist(PersistenceOperation.Save, ActivityDefaults.InternalSaveTimeout, true, callback, state);
        }

        protected internal override void OnEndPersist(IAsyncResult result)
        {
            this.EndInternalPersist(result);
        }

        protected internal override IAsyncResult OnBeginAssociateKeys(ICollection<InstanceKey> keys, AsyncCallback callback, object state)
        {
            throw Fx.AssertAndThrow("WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call OnBeginAssociateKeys.");
        }

        protected internal override void OnEndAssociateKeys(IAsyncResult result)
        {
            throw Fx.AssertAndThrow("WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call OnEndAssociateKeys.");
        }

        protected internal override void OnDisassociateKeys(ICollection<InstanceKey> keys)
        {
            throw Fx.AssertAndThrow("WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call OnDisassociateKeys.");
        }

        private bool AreBookmarksInvalid(out BookmarkResumptionResult result)
        {
            if (_hasRaisedCompleted)
            {
                result = BookmarkResumptionResult.NotFound;
                return true;
            }
            else if (_state == WorkflowApplicationState.Unloaded || _state == WorkflowApplicationState.Aborted)
            {
                result = BookmarkResumptionResult.NotReady;
                return true;
            }

            result = BookmarkResumptionResult.Success;
            return false;
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public BookmarkResumptionResult ResumeBookmark(string bookmarkName, object value)
        {
            if (string.IsNullOrEmpty(bookmarkName))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("bookmarkName");
            }

            return ResumeBookmark(new Bookmark(bookmarkName), value);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value)
        {
            return ResumeBookmark(bookmark, value, ActivityDefaults.ResumeBookmarkTimeout);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public BookmarkResumptionResult ResumeBookmark(string bookmarkName, object value, TimeSpan timeout)
        {
            if (string.IsNullOrEmpty(bookmarkName))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("bookmarkName");
            }

            return ResumeBookmark(new Bookmark(bookmarkName), value, timeout);
        }

        [Fx.Tag.InheritThrows(From = "BeginResumeBookmark", FromDeclaringType = typeof(WorkflowInstance))]
        public BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout)
        {
            TimeoutHelper.ThrowIfNegativeArgument(timeout);
            ThrowIfHandlerThread();
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            InstanceOperation operation = new RequiresIdleOperation();
            BookmarkResumptionResult result;
            bool pendedUnenqueued = false;

            try
            {
                // This is a loose check, but worst case scenario we call
                // an extra, unnecessary Run
                if (!_hasCalledRun)
                {
                    // Increment the pending unenqueued count so we don't raise idle in the time between
                    // when the Run completes and when we enqueue our InstanceOperation.
                    pendedUnenqueued = true;
                    IncrementPendingUnenqueud();

                    InternalRun(timeoutHelper.RemainingTime(), false);
                }

                do
                {
                    InstanceOperation nextOperation = null;

                    try
                    {
                        // Need to enqueue and wait for turn as two separate steps, so that
                        // OnQueued always gets called and we make sure to decrement the pendingUnenqueued counter
                        WaitForTurn(operation, timeoutHelper.RemainingTime());

                        if (pendedUnenqueued)
                        {
                            DecrementPendingUnenqueud();
                            pendedUnenqueued = false;
                        }

                        if (AreBookmarksInvalid(out result))
                        {
                            return result;
                        }

                        result = ResumeBookmarkCore(bookmark, value);

                        if (result == BookmarkResumptionResult.Success)
                        {
                            this.Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
                        }
                        else if (result == BookmarkResumptionResult.NotReady)
                        {
                            nextOperation = new DeferredRequiresIdleOperation();
                        }
                    }
                    finally
                    {
                        NotifyOperationComplete(operation);
                    }

                    operation = nextOperation;
                } while (operation != null);

                return result;
            }
            finally
            {
                if (pendedUnenqueued)
                {
                    DecrementPendingUnenqueud();
                }
            }
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public IAsyncResult BeginResumeBookmark(string bookmarkName, object value, AsyncCallback callback, object state)
        {
            if (string.IsNullOrEmpty(bookmarkName))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("bookmarkName");
            }

            return BeginResumeBookmark(new Bookmark(bookmarkName), value, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public IAsyncResult BeginResumeBookmark(string bookmarkName, object value, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (string.IsNullOrEmpty(bookmarkName))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("bookmarkName");
            }

            return BeginResumeBookmark(new Bookmark(bookmarkName), value, timeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        {
            return BeginResumeBookmark(bookmark, value, ActivityDefaults.ResumeBookmarkTimeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper.ThrowIfNegativeArgument(timeout);
            ThrowIfHandlerThread();

            return new ResumeBookmarkAsyncResult(this, bookmark, value, timeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
        public BookmarkResumptionResult EndResumeBookmark(IAsyncResult result)
        {
            return ResumeBookmarkAsyncResult.End(result);
        }

        protected internal override IAsyncResult OnBeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
        {
            ThrowIfHandlerThread();
            return new ResumeBookmarkAsyncResult(this, bookmark, value, true, timeout, callback, state);
        }

        protected internal override BookmarkResumptionResult OnEndResumeBookmark(IAsyncResult result)
        {
            return ResumeBookmarkAsyncResult.End(result);
        }

        private BookmarkResumptionResult ResumeBookmarkCore(Bookmark bookmark, object value)
        {
            BookmarkResumptionResult result = this.Controller.ScheduleBookmarkResumption(bookmark, value);

            if (result == BookmarkResumptionResult.Success)
            {
                RunCore();
            }

            return result;
        }

        // Returns true if successful, false otherwise
        private bool RaiseIdleEvent()
        {
            if (TD.WorkflowApplicationIdledIsEnabled())
            {
                TD.WorkflowApplicationIdled(this.Id.ToString());
            }

            Exception abortException = null;

            try
            {
                Action<WorkflowApplicationIdleEventArgs> idleHandler = this.Idle;

                if (idleHandler != null)
                {
                    //this.handlerThreadId = Thread.CurrentThread.ManagedThreadId;
                    _isInHandler = true;

                    idleHandler(new WorkflowApplicationIdleEventArgs(this));
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                abortException = e;
            }
            finally
            {
                _isInHandler = false;
            }

            if (abortException != null)
            {
                AbortInstance(abortException, true);
                return false;
            }

            return true;
        }

        private void MarkUnloaded()
        {
            _state = WorkflowApplicationState.Unloaded;

            // don't abort completed instances
            if (this.Controller.State != WorkflowInstanceState.Complete)
            {
                this.Controller.Abort();
            }
            else
            {
                base.DisposeExtensions();
            }

            Exception abortException = null;

            try
            {
                Action<WorkflowApplicationEventArgs> handler = this.Unloaded;

                if (handler != null)
                {
                    //this.handlerThreadId = Thread.CurrentThread.ManagedThreadId;
                    _isInHandler = true;

                    handler(new WorkflowApplicationEventArgs(this));
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                abortException = e;
            }
            finally
            {
                _isInHandler = false;
            }

            if (abortException != null)
            {
                AbortInstance(abortException, true);
            }
        }

        [Fx.Tag.Throws(typeof(WorkflowApplicationException), "The WorkflowApplication is in a state for which unloading is not valid.  The specific subclass denotes which state the instance is in.")]
        [Fx.Tag.Throws(typeof(InstancePersistenceException), "Something went wrong during persistence, but persistence can be retried.")]
        [Fx.Tag.Throws(typeof(TimeoutException), "The workflow could not be unloaded within the given timeout.")]
        public void Unload()
        {
            Unload(ActivityDefaults.SaveTimeout);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public void Unload(TimeSpan timeout)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            RequiresPersistenceOperation operation = new RequiresPersistenceOperation();

            try
            {
                WaitForTurn(operation, timeoutHelper.RemainingTime());

                ValidateStateForUnload();
                if (_state != WorkflowApplicationState.Unloaded) // Unload on unload is a no-op
                {
                    PersistenceOperation persistenceOperation;

                    if (this.Controller.State == WorkflowInstanceState.Complete)
                    {
                        persistenceOperation = PersistenceOperation.Complete;
                    }
                    else
                    {
                        persistenceOperation = PersistenceOperation.Unload;
                    }

                    PersistCore(ref timeoutHelper, persistenceOperation);
                }
            }
            finally
            {
                NotifyOperationComplete(operation);
            }
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public IAsyncResult BeginUnload(AsyncCallback callback, object state)
        {
            return BeginUnload(ActivityDefaults.SaveTimeout, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public IAsyncResult BeginUnload(TimeSpan timeout, AsyncCallback callback, object state)
        {
            ThrowIfHandlerThread();

            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return new UnloadOrPersistAsyncResult(this, timeout, PersistenceOperation.Unload, false, false, callback, state);
        }

        [Fx.Tag.InheritThrows(From = "Unload")]
        public void EndUnload(IAsyncResult result)
        {
            UnloadOrPersistAsyncResult.End(result);
        }

        private IDictionary<XName, InstanceValue> GetInstanceMetadata()
        {
            if (this.DefinitionIdentity != null)
            {
                if (_instanceMetadata == null)
                {
                    _instanceMetadata = new Dictionary<XName, InstanceValue>(2);
                }
                if (!_instanceMetadata.ContainsKey(WorkflowNamespace.WorkflowHostType))
                {
                    _instanceMetadata.Add(WorkflowNamespace.WorkflowHostType, new InstanceValue(Workflow45Namespace.WorkflowApplication));
                }
                _instanceMetadata[Workflow45Namespace.DefinitionIdentity] =
                    new InstanceValue(this.DefinitionIdentity, InstanceValueOptions.Optional);
            }
            return _instanceMetadata;
        }

        private void UpdateInstanceMetadata()
        {
            // Update the metadata to reflect the new identity after a Dynamic Update
            _persistenceManager.SetMutablemetadata(new Dictionary<XName, InstanceValue>
            {
                { Workflow45Namespace.DefinitionIdentity, new InstanceValue(this.DefinitionIdentity, InstanceValueOptions.Optional) }
            });
        }

        private void ThrowIfMulticast(Delegate value)
        {
            if (value != null && value.GetInvocationList().Length > 1)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("value", SR.OnlySingleCastDelegatesAllowed);
            }
        }

        private void ThrowIfAborted()
        {
            if (_state == WorkflowApplicationState.Aborted)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new WorkflowApplicationAbortedException(SR.WorkflowApplicationAborted(this.Id), this.Id));
            }
        }

        private void ThrowIfTerminatedOrCompleted()
        {
            if (_hasRaisedCompleted)
            {
                Exception completionException;
                this.Controller.GetCompletionState(out completionException);
                if (completionException != null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new WorkflowApplicationTerminatedException(SR.WorkflowApplicationTerminated(this.Id), this.Id, completionException));
                }
                else
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new WorkflowApplicationCompletedException(SR.WorkflowApplicationCompleted(this.Id), this.Id));
                }
            }
        }

        private void ThrowIfUnloaded()
        {
            if (_state == WorkflowApplicationState.Unloaded)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new WorkflowApplicationUnloadedException(SR.WorkflowApplicationUnloaded(this.Id), this.Id));
            }
        }

        private void ThrowIfNoInstanceStore()
        {
            if (!HasPersistenceProvider)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InstanceStoreRequiredToPersist));
            }
        }

        private void ThrowIfHandlerThread()
        {
            if (this.IsHandlerThread)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationFromHandlerThread));
            }
        }

        private void ValidateStateForRun()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();
            ThrowIfTerminatedOrCompleted();
            ThrowIfUnloaded();
        }

        private void ValidateStateForGetAllBookmarks()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();
            ThrowIfTerminatedOrCompleted();
            ThrowIfUnloaded();
        }

        private void ValidateStateForCancel()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();

            // We only validate that we aren't aborted and no-op otherwise.
            // This is because the scenario for calling cancel is for it to
            // be a best attempt from an unknown thread.  The less it throws
            // the easier it is to author a host.
        }

        private void ValidateStateForLoad()
        {
            ThrowIfAborted();
            ThrowIfReadOnly(); // only allow a single Load() or Run()
            if (_instanceIdSet)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
            }
        }

        private void ValidateStateForPersist()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();
            ThrowIfTerminatedOrCompleted();
            ThrowIfUnloaded();

            // Other validations
            ThrowIfNoInstanceStore();
        }

        private void ValidateStateForUnload()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();

            // Other validations
            if (this.Controller.State != WorkflowInstanceState.Complete)
            {
                ThrowIfNoInstanceStore();
            }
        }

        private void ValidateStateForTerminate()
        {
            // WorkflowInstanceException validations
            ThrowIfAborted();
            ThrowIfTerminatedOrCompleted();
            ThrowIfUnloaded();
        }

        private enum PersistenceOperation : byte
        {
            Complete,
            Save,
            Unload
        }

        private enum WorkflowApplicationState : byte
        {
            Paused,
            Runnable,
            Unloaded,
            Aborted
        }

        internal class SynchronousSynchronizationContext : SynchronizationContext
        {
            private static SynchronousSynchronizationContext s_value;

            private SynchronousSynchronizationContext()
            {
            }

            public static SynchronousSynchronizationContext Value
            {
                get
                {
                    if (s_value == null)
                    {
                        s_value = new SynchronousSynchronizationContext();
                    }
                    return s_value;
                }
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                d(state);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                d(state);
            }
        }

        private class InvokeAsyncResult : AsyncResult
        {
            private static Action<object, TimeoutException> s_waitCompleteCallback;
            private WorkflowApplication _instance;
            private AsyncWaitHandle _completionWaiter;
            private IDictionary<string, object> _outputs;
            private Exception _completionException;

            public InvokeAsyncResult(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, TimeSpan timeout, SynchronizationContext syncContext, AsyncInvokeContext invokeContext, AsyncCallback callback, object state)
                : base(callback, state)
            {
                Fx.Assert(activity != null, "Need an activity");

                _completionWaiter = new AsyncWaitHandle();
                syncContext = syncContext ?? SynchronousSynchronizationContext.Value;

                _instance = WorkflowApplication.StartInvoke(activity, inputs, extensions, syncContext, new Action(this.OnInvokeComplete), invokeContext);

                if (_completionWaiter.WaitAsync(WaitCompleteCallback, this, timeout))
                {
                    bool completeSelf = OnWorkflowCompletion();

                    if (completeSelf)
                    {
                        if (_completionException != null)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(_completionException);
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
                    if (s_waitCompleteCallback == null)
                    {
                        s_waitCompleteCallback = new Action<object, TimeoutException>(OnWaitComplete);
                    }

                    return s_waitCompleteCallback;
                }
            }

            public static IDictionary<string, object> End(IAsyncResult result)
            {
                InvokeAsyncResult thisPtr = AsyncResult.End<InvokeAsyncResult>(result);
                return thisPtr._outputs;
            }

            private void OnInvokeComplete()
            {
                _completionWaiter.Set();
            }

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
            private static AsyncCompletion s_resumedCallback = new AsyncCompletion(OnResumed);
            private static Action<object, TimeoutException> s_waitCompleteCallback = new Action<object, TimeoutException>(OnWaitComplete);
            private static AsyncCompletion s_trackingCompleteCallback = new AsyncCompletion(OnTrackingComplete);

            private WorkflowApplication _instance;
            private Bookmark _bookmark;
            private object _value;
            private BookmarkResumptionResult _resumptionResult;
            private TimeoutHelper _timeoutHelper;
            private bool _isFromExtension;
            private bool _pendedUnenqueued;

            private InstanceOperation _currentOperation;

            public ResumeBookmarkAsyncResult(WorkflowApplication instance, Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
                : this(instance, bookmark, value, false, timeout, callback, state)
            {
            }

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

                this.OnCompleting = new Action<AsyncResult, Exception>(Finally);

                try
                {
                    if (!_instance._hasCalledRun && !_isFromExtension)
                    {
                        // Increment the pending unenqueued count so we don't raise idle in the time between
                        // when the Run completes and when we enqueue our InstanceOperation.
                        _pendedUnenqueued = true;
                        _instance.IncrementPendingUnenqueud();

                        IAsyncResult result = _instance.BeginInternalRun(_timeoutHelper.RemainingTime(), false, PrepareAsyncCompletion(s_resumedCallback), this);
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
                ResumeBookmarkAsyncResult thisPtr = AsyncResult.End<ResumeBookmarkAsyncResult>(result);

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

                    if (_instance.WaitForTurnAsync(_currentOperation, _timeoutHelper.RemainingTime(), s_waitCompleteCallback, this))
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
                bool completeSelf = false;

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

            private bool CheckIfBookmarksAreInvalid()
            {
                if (_instance.AreBookmarksInvalid(out _resumptionResult))
                {
                    return true;
                }

                return false;
            }

            private bool ProcessResumption()
            {
                bool stillSync = true;

                _resumptionResult = _instance.ResumeBookmarkCore(_bookmark, _value);

                if (_resumptionResult == BookmarkResumptionResult.Success)
                {
                    if (_instance.Controller.HasPendingTrackingRecords)
                    {
                        IAsyncResult result = _instance.Controller.BeginFlushTrackingRecords(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(s_trackingCompleteCallback), this);

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

        private class UnloadOrPersistAsyncResult : AsyncResult
        {
            private static Action<object, TimeoutException> s_waitCompleteCallback = new Action<object, TimeoutException>(OnWaitComplete);
            private static AsyncCompletion s_savedCallback = new AsyncCompletion(OnSaved);
            private static AsyncCompletion s_persistedCallback = new AsyncCompletion(OnPersisted);
            private static AsyncCompletion s_initializedCallback = new AsyncCompletion(OnProviderInitialized);
            private static AsyncCompletion s_readynessEnsuredCallback = new AsyncCompletion(OnProviderReadynessEnsured);
            private static AsyncCompletion s_trackingCompleteCallback = new AsyncCompletion(OnTrackingComplete);
            private static AsyncCompletion s_deleteOwnerCompleteCallback = new AsyncCompletion(OnOwnerDeleted);
            private static AsyncCompletion s_completeContextCallback = new AsyncCompletion(OnCompleteContext);
            private static Action<AsyncResult, Exception> s_completeCallback = new Action<AsyncResult, Exception>(OnComplete);

            //DependentTransaction dependentTransaction;
            private WorkflowApplication _instance;
            private bool _isUnloaded;
            private TimeoutHelper _timeoutHelper;
            private PersistenceOperation _operation;
            private RequiresPersistenceOperation _instanceOperation;
            //WorkflowPersistenceContext context;
            private IDictionary<XName, InstanceValue> _data;
            private PersistencePipeline _pipeline;
            private bool _isInternalPersist;

            public UnloadOrPersistAsyncResult(WorkflowApplication instance, TimeSpan timeout, PersistenceOperation operation,
                bool isWorkflowThread, bool isInternalPersist, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _instance = instance;
                _timeoutHelper = new TimeoutHelper(timeout);
                _operation = operation;
                _isInternalPersist = isInternalPersist;
                _isUnloaded = (operation == PersistenceOperation.Unload || operation == PersistenceOperation.Complete);

                this.OnCompleting = UnloadOrPersistAsyncResult.s_completeCallback;

                bool completeSelf;
                bool success = false;

                // Save off the current transaction in case we have an async operation before we end up creating
                // the WorkflowPersistenceContext and create it on another thread. Do a blocking dependent clone that
                // we will complete when we are completed.
                //
                // This will throw TransactionAbortedException by design, if the transaction is already rolled back.
                //Transaction currentTransaction = Transaction.Current;
                //if (currentTransaction != null)
                //{
                //    this.dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
                //}

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
                            if (_instance.WaitForTurnAsync(_instanceOperation, _timeoutHelper.RemainingTime(), s_waitCompleteCallback, this))
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
                        //if (this.dependentTransaction != null)
                        //{
                        //    this.dependentTransaction.Complete();
                        //}
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

                if (alreadyUnloaded)
                {
                    return true;
                }
                else
                {
                    return InitializeProvider();
                }
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
                        PrepareAsyncCompletion(UnloadOrPersistAsyncResult.s_initializedCallback), this);
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
                //if (this.instance.HasPersistenceProvider && !this.instance.persistenceManager.IsLocked && this.dependentTransaction != null)
                //{
                //    IAsyncResult result = this.instance.persistenceManager.BeginEnsureReadyness(this.timeoutHelper.RemainingTime(),
                //        PrepareAsyncCompletion(UnloadOrPersistAsyncResult.readynessEnsuredCallback), this);
                //    return SyncContinue(result);
                //}
                //else
                //{
                return Track();
                //}
            }

            private static bool OnProviderReadynessEnsured(IAsyncResult result)
            {
                UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;
                thisPtr._instance._persistenceManager.EndEnsureReadyness(result);
                return thisPtr.Track();
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<UnloadOrPersistAsyncResult>(result);
            }

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

                    IAsyncResult result = _instance.Controller.BeginFlushTrackingRecords(flushTrackingRecordsTimeout, PrepareAsyncCompletion(s_trackingCompleteCallback), this);
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
                    //if (!success && this.context != null)
                    //{
                    //    this.context.Abort();
                    //}
                }

                if (_instance.HasPersistenceProvider)
                {
                    return Persist();
                }
                else
                {
                    return Save();
                }
            }

            private bool Persist()
            {
                IAsyncResult result = null;
                try
                {
                    if (_data == null)
                    {
                        _data = PersistenceManager.GenerateInitialData(_instance);
                    }

                    //if (this.context == null)
                    //{
                    //    //this.context = new WorkflowPersistenceContext(this.pipeline != null && this.pipeline.IsSaveTransactionRequired,
                    //    //    this.dependentTransaction, this.timeoutHelper.OriginalTimeout);
                    //    this.context = new WorkflowPersistenceContext(false, this.timeoutHelper.OriginalTimeout);
                    //}

                    //using (PrepareTransactionalCall(this.context.PublicTransaction))
                    //{
                    result = _instance._persistenceManager.BeginSave(_data, _operation, _timeoutHelper.RemainingTime(), PrepareAsyncCompletion(s_persistedCallback), this);
                    //}
                }
                finally
                {
                    //if (result == null && this.context != null)
                    //{
                    //    this.context.Abort();
                    //}
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
                    //if (!success)
                    //{
                    //    thisPtr.context.Abort();
                    //}
                }
                return thisPtr.Save();
            }

            private bool Save()
            {
                if (_pipeline != null)
                {
                    IAsyncResult result = null;
                    try
                    {
                        //if (this.context == null)
                        //{
                        //    //this.context = new WorkflowPersistenceContext(this.pipeline.IsSaveTransactionRequired,
                        //    //    this.dependentTransaction, this.timeoutHelper.RemainingTime());
                        //    this.context = new WorkflowPersistenceContext(false, this.timeoutHelper.RemainingTime());
                        //}

                        _instance._persistencePipelineInUse = _pipeline;
                        //Thread.MemoryBarrier();
                        if (_instance._state == WorkflowApplicationState.Aborted)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                        }

                        //using (PrepareTransactionalCall(this.context.PublicTransaction))
                        //{
                        result = _pipeline.BeginSave(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(s_savedCallback), this);
                        //}
                    }
                    finally
                    {
                        if (result == null)
                        {
                            _instance._persistencePipelineInUse = null;
                            //if (this.context != null)
                            //{
                            //    this.context.Abort();
                            //}
                        }
                    }
                    return SyncContinue(result);
                }
                else
                {
                    return CompleteContext();
                }
            }

            private static bool OnSaved(IAsyncResult result)
            {
                UnloadOrPersistAsyncResult thisPtr = (UnloadOrPersistAsyncResult)result.AsyncState;

                bool success = false;
                try
                {
                    thisPtr._pipeline.EndSave(result);
                    success = true;
                }
                finally
                {
                    thisPtr._instance._persistencePipelineInUse = null;
                    if (!success)
                    {
                        //thisPtr.context.Abort();
                    }
                }

                return thisPtr.CompleteContext();
            }

            private bool CompleteContext()
            {
                bool wentAsync = false;
                IAsyncResult completeResult = null;

                //if (this.context != null)
                //{
                //    wentAsync = this.context.TryBeginComplete(this.PrepareAsyncCompletion(completeContextCallback), this, out completeResult);
                //}

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
                //thisPtr.context.EndComplete(result);

                return thisPtr.DeleteOwner();
            }

            private bool DeleteOwner()
            {
                if (_instance.HasPersistenceProvider && _instance._persistenceManager.OwnerWasCreated &&
                    (_operation == PersistenceOperation.Unload || _operation == PersistenceOperation.Complete))
                {
                    // This call uses the ambient transaction directly if there was one, to mimic the sync case.
                    IAsyncResult deleteOwnerResult = null;
                    //using (PrepareTransactionalCall(this.dependentTransaction))
                    //{
                    deleteOwnerResult = _instance._persistenceManager.BeginDeleteOwner(_timeoutHelper.RemainingTime(),
                        this.PrepareAsyncCompletion(UnloadOrPersistAsyncResult.s_deleteOwnerCompleteCallback), this);
                    //}
                    return this.SyncContinue(deleteOwnerResult);
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
                    //if (thisPtr.dependentTransaction != null)
                    //{
                    //    thisPtr.dependentTransaction.Complete();
                    //}
                }
            }
        }

        private abstract class SimpleOperationAsyncResult : AsyncResult
        {
            private static Action<object, TimeoutException> s_waitCompleteCallback = new Action<object, TimeoutException>(OnWaitComplete);
            private static AsyncCallback s_trackingCompleteCallback = Fx.ThunkCallback(new AsyncCallback(OnTrackingComplete));

            private WorkflowApplication _instance;
            private TimeoutHelper _timeoutHelper;

            protected SimpleOperationAsyncResult(WorkflowApplication instance, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _instance = instance;
            }

            protected WorkflowApplication Instance
            {
                get
                {
                    return _instance;
                }
            }

            protected void Run(TimeSpan timeout)
            {
                _timeoutHelper = new TimeoutHelper(timeout);

                InstanceOperation operation = new InstanceOperation();

                bool completeSelf = true;

                try
                {
                    completeSelf = _instance.WaitForTurnAsync(operation, _timeoutHelper.RemainingTime(), s_waitCompleteCallback, this);

                    if (completeSelf)
                    {
                        this.ValidateState();

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
                    IAsyncResult trackingResult = _instance.Controller.BeginFlushTrackingRecords(_timeoutHelper.RemainingTime(), s_trackingCompleteCallback, this);

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
                }
                else
                {
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
            private Exception _reason;

            private TerminateAsyncResult(WorkflowApplication instance, Exception reason, AsyncCallback callback, object state)
                : base(instance, callback, state)
            {
                _reason = reason;
            }

            public static TerminateAsyncResult Create(WorkflowApplication instance, Exception reason, TimeSpan timeout, AsyncCallback callback, object state)
            {
                TerminateAsyncResult result = new TerminateAsyncResult(instance, reason, callback, state);
                result.Run(timeout);
                return result;
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<TerminateAsyncResult>(result);
            }

            protected override void ValidateState()
            {
                this.Instance.ValidateStateForTerminate();
            }

            protected override void PerformOperation()
            {
                this.Instance.TerminateCore(_reason);
            }
        }

        private class CancelAsyncResult : SimpleOperationAsyncResult
        {
            private CancelAsyncResult(WorkflowApplication instance, AsyncCallback callback, object state)
                : base(instance, callback, state)
            {
            }

            public static CancelAsyncResult Create(WorkflowApplication instance, TimeSpan timeout, AsyncCallback callback, object state)
            {
                CancelAsyncResult result = new CancelAsyncResult(instance, callback, state);
                result.Run(timeout);
                return result;
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<CancelAsyncResult>(result);
            }

            protected override void ValidateState()
            {
                this.Instance.ValidateStateForCancel();
            }

            protected override void PerformOperation()
            {
                this.Instance.CancelCore();
            }
        }

        private class RunAsyncResult : SimpleOperationAsyncResult
        {
            private bool _isUserRun;

            private RunAsyncResult(WorkflowApplication instance, bool isUserRun, AsyncCallback callback, object state)
                : base(instance, callback, state)
            {
                _isUserRun = isUserRun;
            }

            public static RunAsyncResult Create(WorkflowApplication instance, bool isUserRun, TimeSpan timeout, AsyncCallback callback, object state)
            {
                RunAsyncResult result = new RunAsyncResult(instance, isUserRun, callback, state);
                result.Run(timeout);
                return result;
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<RunAsyncResult>(result);
            }

            protected override void ValidateState()
            {
                this.Instance.ValidateStateForRun();
            }

            protected override void PerformOperation()
            {
                if (_isUserRun)
                {
                    // We set this to true here so that idle will be raised
                    // regardless of whether any work is performed.
                    this.Instance._hasExecutionOccurredSinceLastIdle = true;
                }

                this.Instance.RunCore();
            }
        }

        //class UnlockInstanceAsyncResult : TransactedAsyncResult
        //{
        //    static AsyncCompletion instanceUnlockedCallback = new AsyncCompletion(OnInstanceUnlocked);
        //    static AsyncCompletion ownerDeletedCallback = new AsyncCompletion(OnOwnerDeleted);
        //    static Action<AsyncResult, Exception> completeCallback = new Action<AsyncResult, Exception>(OnComplete);

        //    readonly PersistenceManager persistenceManager;
        //    readonly TimeoutHelper timeoutHelper;

        //    DependentTransaction dependentTransaction;

        //    public UnlockInstanceAsyncResult(PersistenceManager persistenceManager, TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
        //        : base(callback, state)
        //    {
        //        this.persistenceManager = persistenceManager;
        //        this.timeoutHelper = timeoutHelper;

        //        Transaction currentTransaction = Transaction.Current;
        //        if (currentTransaction != null)
        //        {
        //            this.dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
        //        }

        //        OnCompleting = UnlockInstanceAsyncResult.completeCallback;

        //        bool success = false;
        //        try
        //        {
        //            IAsyncResult result;
        //            using (this.PrepareTransactionalCall(this.dependentTransaction))
        //            {
        //                if (this.persistenceManager.OwnerWasCreated)
        //                {
        //                    // if the owner was created by this WorkflowApplication, delete it.
        //                    // This implicitly unlocks the instance.
        //                    result = this.persistenceManager.BeginDeleteOwner(this.timeoutHelper.RemainingTime(), this.PrepareAsyncCompletion(ownerDeletedCallback), this);
        //                }
        //                else
        //                {
        //                    result = this.persistenceManager.BeginUnlock(this.timeoutHelper.RemainingTime(), this.PrepareAsyncCompletion(instanceUnlockedCallback), this);
        //                }
        //            }

        //            if (SyncContinue(result))
        //            {
        //                Complete(true);
        //            }

        //            success = true;
        //        }
        //        finally
        //        {
        //            if (!success)
        //            {
        //                this.persistenceManager.Abort();
        //            }
        //        }
        //    }

        //    public static void End(IAsyncResult result)
        //    {
        //        AsyncResult.End<UnlockInstanceAsyncResult>(result);
        //    }

        //    static bool OnInstanceUnlocked(IAsyncResult result)
        //    {
        //        UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result.AsyncState;
        //        thisPtr.persistenceManager.EndUnlock(result);
        //        return true;
        //    }

        //    static bool OnOwnerDeleted(IAsyncResult result)
        //    {
        //        UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result.AsyncState;
        //        thisPtr.persistenceManager.EndDeleteOwner(result);
        //        return true;
        //    }

        //    static void OnComplete(AsyncResult result, Exception exception)
        //    {
        //        UnlockInstanceAsyncResult thisPtr = (UnlockInstanceAsyncResult)result;
        //        if (thisPtr.dependentTransaction != null)
        //        {
        //            thisPtr.dependentTransaction.Complete();
        //        }
        //        thisPtr.persistenceManager.Abort();
        //    }
        //}

        //class LoadAsyncResult : TransactedAsyncResult
        //{
        //    static Action<object, TimeoutException> waitCompleteCallback = new Action<object, TimeoutException>(OnWaitComplete);
        //    static AsyncCompletion providerRegisteredCallback = new AsyncCompletion(OnProviderRegistered);
        //    static AsyncCompletion loadCompleteCallback = new AsyncCompletion(OnLoadComplete);
        //    static AsyncCompletion loadPipelineCallback = new AsyncCompletion(OnLoadPipeline);
        //    static AsyncCompletion completeContextCallback = new AsyncCompletion(OnCompleteContext);
        //    static Action<AsyncResult, Exception> completeCallback = new Action<AsyncResult, Exception>(OnComplete);

        //    readonly WorkflowApplication application;
        //    readonly PersistenceManager persistenceManager;
        //    readonly TimeoutHelper timeoutHelper;
        //    readonly bool loadAny;

        //    object deserializedRuntimeState;
        //    PersistencePipeline pipeline;
        //    WorkflowPersistenceContext context;
        //    DependentTransaction dependentTransaction;
        //    IDictionary<XName, InstanceValue> values;
        //    DynamicUpdateMap updateMap;
        //    InstanceOperation instanceOperation;

        //    public LoadAsyncResult(WorkflowApplication application, PersistenceManager persistenceManager,
        //        IDictionary<XName, InstanceValue> values, DynamicUpdateMap updateMap, TimeSpan timeout, 
        //        AsyncCallback callback, object state)
        //        : base(callback, state)
        //    {
        //        this.application = application;
        //        this.persistenceManager = persistenceManager;
        //        this.values = values;
        //        this.timeoutHelper = new TimeoutHelper(timeout);
        //        this.updateMap = updateMap;

        //        Initialize();
        //    }

        //    public LoadAsyncResult(WorkflowApplication application, PersistenceManager persistenceManager,
        //        bool loadAny, TimeSpan timeout, AsyncCallback callback, object state)
        //        : base(callback, state)
        //    {
        //        this.application = application;
        //        this.persistenceManager = persistenceManager;
        //        this.loadAny = loadAny;
        //        this.timeoutHelper = new TimeoutHelper(timeout);

        //        Initialize();
        //    }

        //    void Initialize()
        //    {
        //        OnCompleting = LoadAsyncResult.completeCallback;

        //        // Save off the current transaction in case we have an async operation before we end up creating
        //        // the WorkflowPersistenceContext and create it on another thread. Do a simple clone here to prevent
        //        // the object referenced by Transaction.Current from disposing before we get around to referencing it
        //        // when we create the WorkflowPersistenceContext.
        //        //
        //        // This will throw TransactionAbortedException by design, if the transaction is already rolled back.
        //        Transaction currentTransaction = Transaction.Current;
        //        if (currentTransaction != null)
        //        {
        //            this.dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
        //        }

        //        bool completeSelf;
        //        bool success = false;
        //        Exception updateException = null;
        //        try
        //        {
        //            if (this.application == null)
        //            {
        //                completeSelf = RegisterProvider();
        //            }
        //            else
        //            {
        //                completeSelf = WaitForTurn();
        //            }
        //            success = true;
        //        }
        //        catch (InstanceUpdateException e)
        //        {
        //            updateException = e;
        //            throw;
        //        }
        //        catch (VersionMismatchException e)
        //        {
        //            updateException = e;
        //            throw;
        //        }
        //        finally
        //        {
        //            if (!success)
        //            {
        //                if (this.dependentTransaction != null)
        //                {
        //                    this.dependentTransaction.Complete();
        //                }
        //                Abort(this, updateException);
        //            }
        //        }

        //        if (completeSelf)
        //        {
        //            Complete(true);
        //        }
        //    }

        //    public static void End(IAsyncResult result)
        //    {
        //        AsyncResult.End<LoadAsyncResult>(result);
        //    }

        //    public static WorkflowApplicationInstance EndAndCreateInstance(IAsyncResult result)
        //    {
        //        LoadAsyncResult thisPtr = AsyncResult.End<LoadAsyncResult>(result);
        //        Fx.AssertAndThrow(thisPtr.application == null, "Should not create a WorkflowApplicationInstance if we already have a WorkflowApplication");

        //        ActivityExecutor deserializedRuntimeState = WorkflowApplication.ExtractRuntimeState(thisPtr.values, thisPtr.persistenceManager.InstanceId);
        //        return new WorkflowApplicationInstance(thisPtr.persistenceManager, thisPtr.values, deserializedRuntimeState.WorkflowIdentity);
        //    }

        //    bool WaitForTurn()
        //    {
        //        bool completeSelf;
        //        bool success = false;
        //        this.instanceOperation = new InstanceOperation { RequiresInitialized = false };
        //        try
        //        {
        //            if (this.application.WaitForTurnAsync(this.instanceOperation, this.timeoutHelper.RemainingTime(), waitCompleteCallback, this))
        //            {
        //                completeSelf = ValidateState();
        //            }
        //            else
        //            {
        //                completeSelf = false;
        //            }
        //            success = true;
        //        }
        //        finally
        //        {
        //            if (!success)
        //            {
        //                NotifyOperationComplete();
        //            }
        //        }

        //        return completeSelf;
        //    }

        //    static void OnWaitComplete(object state, TimeoutException asyncException)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)state;
        //        if (asyncException != null)
        //        {
        //            thisPtr.Complete(false, asyncException);
        //            return;
        //        }

        //        bool completeSelf;
        //        Exception completionException = null;

        //        try
        //        {
        //            completeSelf = thisPtr.ValidateState();
        //        }
        //        catch (Exception e)
        //        {
        //            if (Fx.IsFatal(e))
        //            {
        //                throw;
        //            }

        //            completionException = e;
        //            completeSelf = true;
        //        }

        //        if (completeSelf)
        //        {
        //            thisPtr.Complete(false, completionException);
        //        }
        //    }

        //    bool ValidateState()
        //    {
        //        this.application.ValidateStateForLoad();

        //        this.application.SetPersistenceManager(this.persistenceManager);
        //        if (!this.loadAny)
        //        {
        //            this.application.instanceId = this.persistenceManager.InstanceId;
        //            this.application.instanceIdSet = true;
        //        }
        //        if (this.application.InstanceStore == null)
        //        {
        //            this.application.InstanceStore = this.persistenceManager.InstanceStore;
        //        }

        //        return RegisterProvider();
        //    }

        //    bool RegisterProvider()
        //    {
        //        if (!this.persistenceManager.IsInitialized)
        //        {
        //            WorkflowIdentity definitionIdentity = this.application != null ? this.application.DefinitionIdentity : WorkflowApplication.unknownIdentity;
        //            IAsyncResult result = this.persistenceManager.BeginInitialize(definitionIdentity, this.timeoutHelper.RemainingTime(), PrepareAsyncCompletion(providerRegisteredCallback), this);
        //            return SyncContinue(result);
        //        }
        //        else
        //        {
        //            return Load();
        //        }
        //    }

        //    static bool OnProviderRegistered(IAsyncResult result)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
        //        thisPtr.persistenceManager.EndInitialize(result);
        //        return thisPtr.Load();
        //    }

        //    bool Load()
        //    {
        //        bool success = false;
        //        IAsyncResult result = null;
        //        try
        //        {
        //            bool transactionRequired = this.application != null ? this.application.IsLoadTransactionRequired() : false;
        //            this.context = new WorkflowPersistenceContext(transactionRequired,
        //                this.dependentTransaction, this.timeoutHelper.OriginalTimeout);

        //            // Values is null if this is an initial load from the database.
        //            // It is non-null if we already loaded values into a WorkflowApplicationInstance,
        //            // and are now loading from that WAI.
        //            if (this.values == null)
        //            {
        //                using (PrepareTransactionalCall(this.context.PublicTransaction))
        //                {
        //                    if (this.loadAny)
        //                    {
        //                        result = this.persistenceManager.BeginTryLoad(this.timeoutHelper.RemainingTime(), PrepareAsyncCompletion(loadCompleteCallback), this);
        //                    }
        //                    else
        //                    {
        //                        result = this.persistenceManager.BeginLoad(this.timeoutHelper.RemainingTime(), PrepareAsyncCompletion(loadCompleteCallback), this);
        //                    }
        //                }
        //            }
        //            success = true;
        //        }
        //        finally
        //        {
        //            if (!success && this.context != null)
        //            {
        //                this.context.Abort();
        //            }
        //        }

        //        if (result == null)
        //        {
        //            return LoadValues(null);
        //        }
        //        else
        //        {
        //            return SyncContinue(result);
        //        }
        //    }

        //    static bool OnLoadComplete(IAsyncResult result)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
        //        return thisPtr.LoadValues(result);
        //    }

        //    bool LoadValues(IAsyncResult result)
        //    {
        //        IAsyncResult loadResult = null;
        //        bool success = false;
        //        try
        //        {
        //            Fx.Assert((result == null) != (this.values == null), "We should either have values already retrieved, or an IAsyncResult to retrieve them");

        //            if (result != null)
        //            {
        //                if (this.loadAny)
        //                {
        //                    if (!this.persistenceManager.EndTryLoad(result, out this.values))
        //                    {
        //                        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InstanceNotReadyException(SR.NoRunnableInstances));
        //                    }
        //                    if (this.application != null)
        //                    {
        //                        if (this.application.instanceIdSet)
        //                        {
        //                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        //                        }

        //                        this.application.instanceId = this.persistenceManager.InstanceId;
        //                        this.application.instanceIdSet = true;
        //                    }
        //                }
        //                else
        //                {
        //                    this.values = this.persistenceManager.EndLoad(result);
        //                }
        //            }

        //            if (this.application != null)
        //            {
        //                this.pipeline = this.application.ProcessInstanceValues(this.values, out this.deserializedRuntimeState);

        //                if (this.pipeline != null)
        //                {
        //                    this.pipeline.SetLoadedValues(this.values);

        //                    this.application.persistencePipelineInUse = this.pipeline;
        //                    Thread.MemoryBarrier();
        //                    if (this.application.state == WorkflowApplicationState.Aborted)
        //                    {
        //                        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
        //                    }

        //                    using (this.PrepareTransactionalCall(this.context.PublicTransaction))
        //                    {
        //                        loadResult = this.pipeline.BeginLoad(this.timeoutHelper.RemainingTime(), this.PrepareAsyncCompletion(loadPipelineCallback), this);
        //                    }
        //                }
        //            }

        //            success = true;
        //        }
        //        finally
        //        {
        //            if (!success)
        //            {
        //                this.context.Abort();
        //            }
        //        }

        //        if (this.pipeline != null)
        //        {
        //            return this.SyncContinue(loadResult);
        //        }
        //        else
        //        {
        //            return this.CompleteContext();
        //        }
        //    }

        //    static bool OnLoadPipeline(IAsyncResult result)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;

        //        bool success = false;
        //        try
        //        {
        //            thisPtr.pipeline.EndLoad(result);
        //            success = true;
        //        }
        //        finally
        //        {
        //            if (!success)
        //            {
        //                thisPtr.context.Abort();
        //            }
        //        }
        //        return thisPtr.CompleteContext();
        //    }

        //    bool CompleteContext()
        //    {
        //        if (this.application != null)
        //        {
        //            this.application.Initialize(this.deserializedRuntimeState, this.updateMap);
        //            if (this.updateMap != null)
        //            {
        //                this.application.UpdateInstanceMetadata();
        //            }
        //        }

        //        IAsyncResult completeResult;
        //        if (this.context.TryBeginComplete(PrepareAsyncCompletion(completeContextCallback), this, out completeResult))
        //        {
        //            Fx.Assert(completeResult != null, "We shouldn't have null here.");
        //            return SyncContinue(completeResult);
        //        }
        //        else
        //        {
        //            return Finish();
        //        }
        //    }

        //    static bool OnCompleteContext(IAsyncResult result)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)result.AsyncState;
        //        thisPtr.context.EndComplete(result);
        //        return thisPtr.Finish();
        //    }

        //    bool Finish()
        //    {
        //        if (this.pipeline != null)
        //        {
        //            this.pipeline.Publish();
        //        }
        //        return true;
        //    }

        //    void NotifyOperationComplete()
        //    {
        //        if (this.application != null)
        //        {
        //            InstanceOperation localInstanceOperation = this.instanceOperation;
        //            this.instanceOperation = null;
        //            this.application.NotifyOperationComplete(localInstanceOperation);
        //        }
        //    }

        //    static void OnComplete(AsyncResult result, Exception exception)
        //    {
        //        LoadAsyncResult thisPtr = (LoadAsyncResult)result;
        //        try
        //        {
        //            if (thisPtr.dependentTransaction != null)
        //            {
        //                thisPtr.dependentTransaction.Complete();
        //            }

        //            if (exception != null)
        //            {
        //                Abort(thisPtr, exception);
        //            }
        //        }
        //        finally
        //        {
        //            thisPtr.NotifyOperationComplete();
        //        }
        //    }

        //    static void Abort(LoadAsyncResult thisPtr, Exception exception)
        //    {
        //        if (thisPtr.application == null)
        //        {
        //            thisPtr.persistenceManager.Abort();
        //        }
        //        else
        //        {
        //            thisPtr.application.AbortDueToException(exception);
        //        }
        //    }
        //}

        // this class is not a general purpose SyncContext and is only meant to work for workflow scenarios, where the scheduler ensures 
        // at most one work item pending. The scheduler ensures that Invoke must run before Post is called on a different thread.
        private class PumpBasedSynchronizationContext : SynchronizationContext
        {
            // The waitObject is cached per thread so that we can avoid the cost of creating
            // events for multiple synchronous invokes.
            [ThreadStatic]
            private static AutoResetEvent t_waitObject;
            private AutoResetEvent _queueWaiter;
            private WorkItem _currentWorkItem;
            private object _thisLock;
            private TimeoutHelper _timeoutHelper;

            public PumpBasedSynchronizationContext(TimeSpan timeout)
            {
                _timeoutHelper = new TimeoutHelper(timeout);
                _thisLock = new object();
            }

            private bool IsInvokeCompleted
            {
                get;
                set;
            }

            public void DoPump()
            {
                Fx.Assert(_currentWorkItem != null, "the work item cannot be null");
                WorkItem workItem;

                lock (_thisLock)
                {
                    if (PumpBasedSynchronizationContext.t_waitObject == null)
                    {
                        PumpBasedSynchronizationContext.t_waitObject = new AutoResetEvent(false);
                    }
                    _queueWaiter = PumpBasedSynchronizationContext.t_waitObject;

                    workItem = _currentWorkItem;
                    _currentWorkItem = null;
                    workItem.Invoke();
                }

                Fx.Assert(_queueWaiter != null, "queue waiter cannot be null");

                while (this.WaitForNextItem())
                {
                    Fx.Assert(_currentWorkItem != null, "the work item cannot be null");
                    workItem = _currentWorkItem;
                    _currentWorkItem = null;
                    workItem.Invoke();
                }
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                ScheduleWorkItem(new WorkItem(d, state));
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new NotSupportedException(SR.SendNotSupported));
            }

            // Since tracking can go async this may or may not be called directly
            // under a call to workItem.Invoke.  Also, the scheduler may call
            // OnNotifyPaused or OnNotifyUnhandledException from any random thread
            // if runtime goes async (post-work item tracking, AsyncCodeActivity).
            public void OnInvokeCompleted()
            {
                Fx.AssertAndFailFast(_currentWorkItem == null, "There can be no pending work items when complete");

                this.IsInvokeCompleted = true;

                lock (_thisLock)
                {
                    if (_queueWaiter != null)
                    {
                        // Since we don't know which thread this is being called
                        // from we just set the waiter directly rather than
                        // doing our SetWaiter cleanup.
                        _queueWaiter.Set();
                    }
                }
            }

            private void ScheduleWorkItem(WorkItem item)
            {
                lock (_thisLock)
                {
                    Fx.AssertAndFailFast(_currentWorkItem == null, "There cannot be more than 1 work item at a given time");
                    _currentWorkItem = item;
                    if (_queueWaiter != null)
                    {
                        // Since we don't know which thread this is being called
                        // from we just set the waiter directly rather than
                        // doing our SetWaiter cleanup.
                        _queueWaiter.Set();
                    }
                }
            }

            private bool WaitOne(AutoResetEvent waiter, TimeSpan timeout)
            {
                bool success = false;
                try
                {
                    bool result = TimeoutHelper.WaitOne(waiter, timeout);
                    // if the wait timed out, reset the thread static
                    success = result;
                    return result;
                }
                finally
                {
                    if (!success)
                    {
                        PumpBasedSynchronizationContext.t_waitObject = null;
                    }
                }
            }

            private bool WaitForNextItem()
            {
                if (!WaitOne(_queueWaiter, _timeoutHelper.RemainingTime()))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new TimeoutException(SR.TimeoutOnOperation(_timeoutHelper.OriginalTimeout)));
                }

                // We need to check this after the wait as well in 
                // case the notification came in asynchronously
                if (this.IsInvokeCompleted)
                {
                    return false;
                }

                return true;
            }

            private class WorkItem
            {
                private SendOrPostCallback _callback;
                private object _state;

                public WorkItem(SendOrPostCallback callback, object state)
                {
                    _callback = callback;
                    _state = state;
                }

                public void Invoke()
                {
                    _callback(_state);
                }
            }
        }

        private class WorkflowEventData
        {
            public WorkflowEventData(WorkflowApplication instance)
            {
                this.Instance = instance;
            }

            public WorkflowApplication Instance
            {
                get;
                private set;
            }

            public Func<IAsyncResult, WorkflowApplication, bool, bool> NextCallback
            {
                get;
                set;
            }

            public Exception UnhandledException
            {
                get;
                set;
            }

            public Activity UnhandledExceptionSource
            {
                get;
                set;
            }

            public string UnhandledExceptionSourceInstance
            {
                get;
                set;
            }
        }

        private class IdleEventHandler
        {
            private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;
            private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage2Callback;

            public IdleEventHandler()
            {
            }

            private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
            {
                get
                {
                    if (_stage1Callback == null)
                    {
                        _stage1Callback = new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                    }

                    return _stage1Callback;
                }
            }

            private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage2Callback
            {
                get
                {
                    if (_stage2Callback == null)
                    {
                        _stage2Callback = new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage2Complete);
                    }

                    return _stage2Callback;
                }
            }

            public bool Run(WorkflowApplication instance)
            {
                IAsyncResult result = null;

                if (instance.Controller.TrackingEnabled)
                {
                    instance.Controller.Track(new WorkflowInstanceRecord(instance.Id, instance.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Idle, instance.DefinitionIdentity));

                    instance.EventData.NextCallback = this.Stage1Callback;
                    result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                return OnStage1Complete(result, instance, true);
            }

            private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication application, bool isStillSync)
            {
                if (lastResult != null)
                {
                    application.Controller.EndFlushTrackingRecords(lastResult);
                }

                IAsyncResult result = null;

                if (application.RaiseIdleEvent())
                {
                    if (application.Controller.IsPersistable && application._persistenceManager != null)
                    {
                        Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> persistableIdleHandler = application.PersistableIdle;

                        if (persistableIdleHandler != null)
                        {
                            PersistableIdleAction action = PersistableIdleAction.None;

                            //application.handlerThreadId = Thread.CurrentThread.ManagedThreadId;

                            try
                            {
                                application._isInHandler = true;
                                action = persistableIdleHandler(new WorkflowApplicationIdleEventArgs(application));
                            }
                            finally
                            {
                                application._isInHandler = false;
                            }

                            if (TD.WorkflowApplicationPersistableIdleIsEnabled())
                            {
                                TD.WorkflowApplicationPersistableIdle(application.Id.ToString(), action.ToString());
                            }

                            if (action != PersistableIdleAction.None)
                            {
                                PersistenceOperation operation = PersistenceOperation.Unload;

                                if (action == PersistableIdleAction.Persist)
                                {
                                    operation = PersistenceOperation.Save;
                                }
                                else if (action != PersistableIdleAction.Unload)
                                {
                                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidIdleAction));
                                }

                                application.EventData.NextCallback = this.Stage2Callback;
                                result = application.BeginInternalPersist(operation, ActivityDefaults.InternalSaveTimeout, true, EventFrameCallback, application.EventData);

                                if (!result.CompletedSynchronously)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            // Trace the default action
                            if (TD.WorkflowApplicationPersistableIdleIsEnabled())
                            {
                                TD.WorkflowApplicationPersistableIdle(application.Id.ToString(), PersistableIdleAction.None.ToString());
                            }
                        }
                    }
                }

                return OnStage2Complete(result, application, isStillSync);
            }

            private bool OnStage2Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
            {
                if (lastResult != null)
                {
                    instance.EndInternalPersist(lastResult);
                }

                return true;
            }
        }

        private class CompletedEventHandler
        {
            private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;
            private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage2Callback;

            public CompletedEventHandler()
            {
            }

            private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
            {
                get
                {
                    if (_stage1Callback == null)
                    {
                        _stage1Callback = new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                    }

                    return _stage1Callback;
                }
            }

            private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage2Callback
            {
                get
                {
                    if (_stage2Callback == null)
                    {
                        _stage2Callback = new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage2Complete);
                    }

                    return _stage2Callback;
                }
            }

            public bool Run(WorkflowApplication instance)
            {
                IAsyncResult result = null;
                if (instance.Controller.HasPendingTrackingRecords)
                {
                    instance.EventData.NextCallback = this.Stage1Callback;
                    result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                return OnStage1Complete(result, instance, true);
            }

            private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
            {
                if (lastResult != null)
                {
                    instance.Controller.EndFlushTrackingRecords(lastResult);
                }

                IDictionary<string, object> outputs;
                Exception completionException;
                ActivityInstanceState completionState = instance.Controller.GetCompletionState(out outputs, out completionException);

                if (instance._invokeCompletedCallback == null)
                {
                    Action<WorkflowApplicationCompletedEventArgs> handler = instance.Completed;

                    if (handler != null)
                    {
                        //instance.handlerThreadId = Thread.CurrentThread.ManagedThreadId;

                        try
                        {
                            instance._isInHandler = true;
                            handler(new WorkflowApplicationCompletedEventArgs(instance, completionException, completionState, outputs));
                        }
                        finally
                        {
                            instance._isInHandler = false;
                        }
                    }
                }

                switch (completionState)
                {
                    case ActivityInstanceState.Closed:
                        if (TD.WorkflowApplicationCompletedIsEnabled())
                        {
                            TD.WorkflowApplicationCompleted(instance.Id.ToString());
                        }
                        break;
                    case ActivityInstanceState.Canceled:
                        if (TD.WorkflowInstanceCanceledIsEnabled())
                        {
                            TD.WorkflowInstanceCanceled(instance.Id.ToString());
                        }
                        break;
                    case ActivityInstanceState.Faulted:
                        if (TD.WorkflowApplicationTerminatedIsEnabled())
                        {
                            TD.WorkflowApplicationTerminated(instance.Id.ToString(), completionException);
                        }
                        break;
                }

                IAsyncResult result = null;
                Fx.Assert(instance.Controller.IsPersistable, "Should not be in a No Persist Zone once the instance is complete.");
                if (instance._persistenceManager != null || instance.HasPersistenceModule)
                {
                    instance.EventData.NextCallback = this.Stage2Callback;
                    result = instance.BeginInternalPersist(PersistenceOperation.Unload, ActivityDefaults.InternalSaveTimeout, true, EventFrameCallback, instance.EventData);

                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }
                else
                {
                    instance.MarkUnloaded();
                }

                return OnStage2Complete(result, instance, isStillSync);
            }

            private bool OnStage2Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
            {
                if (lastResult != null)
                {
                    instance.EndInternalPersist(lastResult);
                }

                if (instance._invokeCompletedCallback != null)
                {
                    instance._invokeCompletedCallback();
                }

                return true;
            }
        }

        private class UnhandledExceptionEventHandler
        {
            private Func<IAsyncResult, WorkflowApplication, bool, bool> _stage1Callback;

            public UnhandledExceptionEventHandler()
            {
            }

            private Func<IAsyncResult, WorkflowApplication, bool, bool> Stage1Callback
            {
                get
                {
                    if (_stage1Callback == null)
                    {
                        _stage1Callback = new Func<IAsyncResult, WorkflowApplication, bool, bool>(OnStage1Complete);
                    }

                    return _stage1Callback;
                }
            }

            public bool Run(WorkflowApplication instance, Exception exception, Activity exceptionSource, string exceptionSourceInstanceId)
            {
                IAsyncResult result = null;

                if (instance.Controller.HasPendingTrackingRecords)
                {
                    instance.EventData.NextCallback = this.Stage1Callback;
                    instance.EventData.UnhandledException = exception;
                    instance.EventData.UnhandledExceptionSource = exceptionSource;
                    instance.EventData.UnhandledExceptionSourceInstance = exceptionSourceInstanceId;
                    result = instance.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, EventFrameCallback, instance.EventData);

                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                return OnStage1Complete(result, instance, exception, exceptionSource, exceptionSourceInstanceId);
            }

            private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, bool isStillSync)
            {
                return OnStage1Complete(lastResult, instance, instance.EventData.UnhandledException, instance.EventData.UnhandledExceptionSource, instance.EventData.UnhandledExceptionSourceInstance);
            }

            private bool OnStage1Complete(IAsyncResult lastResult, WorkflowApplication instance, Exception exception, Activity source, string sourceInstanceId)
            {
                if (lastResult != null)
                {
                    instance.Controller.EndFlushTrackingRecords(lastResult);
                }

                Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> handler = instance.OnUnhandledException;

                UnhandledExceptionAction action = UnhandledExceptionAction.Terminate;

                if (handler != null)
                {
                    try
                    {
                        instance._isInHandler = true;
                        //instance.handlerThreadId = Thread.CurrentThread.ManagedThreadId;

                        action = handler(new WorkflowApplicationUnhandledExceptionEventArgs(instance, exception, source, sourceInstanceId));
                    }
                    finally
                    {
                        instance._isInHandler = false;
                    }
                }

                if (instance._invokeCompletedCallback != null)
                {
                    action = UnhandledExceptionAction.Terminate;
                }

                if (TD.WorkflowApplicationUnhandledExceptionIsEnabled())
                {
                    TD.WorkflowApplicationUnhandledException(instance.Id.ToString(), source.GetType().ToString(), source.DisplayName, action.ToString(), exception);
                }

                switch (action)
                {
                    case UnhandledExceptionAction.Abort:
                        instance.AbortInstance(exception, true);
                        break;
                    case UnhandledExceptionAction.Cancel:
                        instance.Controller.ScheduleCancel();
                        break;
                    case UnhandledExceptionAction.Terminate:
                        instance.TerminateCore(exception);
                        break;
                    default:
                        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidUnhandledExceptionAction));
                }

                return true;
            }
        }

        //class InstanceCommandWithTemporaryHandleAsyncResult : TransactedAsyncResult
        //{
        //    static AsyncCompletion commandCompletedCallback = new AsyncCompletion(OnCommandCompleted);
        //    static Action<AsyncResult, Exception> completeCallback = new Action<AsyncResult, Exception>(OnComplete);

        //    InstancePersistenceCommand command;
        //    DependentTransaction dependentTransaction;
        //    InstanceStore instanceStore;
        //    InstanceHandle temporaryHandle;
        //    InstanceView commandResult;

        //    public InstanceCommandWithTemporaryHandleAsyncResult(InstanceStore instanceStore, InstancePersistenceCommand command,
        //        TimeSpan timeout, AsyncCallback callback, object state)
        //        : base(callback, state)
        //    {
        //        this.instanceStore = instanceStore;
        //        this.command = command;
        //        this.temporaryHandle = instanceStore.CreateInstanceHandle();

        //        Transaction currentTransaction = Transaction.Current;
        //        if (currentTransaction != null)
        //        {
        //            this.dependentTransaction = currentTransaction.DependentClone(DependentCloneOption.BlockCommitUntilComplete);
        //        }

        //        OnCompleting = completeCallback;

        //        IAsyncResult result;
        //        using (this.PrepareTransactionalCall(this.dependentTransaction))
        //        {
        //            result = instanceStore.BeginExecute(this.temporaryHandle, command, timeout, PrepareAsyncCompletion(commandCompletedCallback), this);
        //        }

        //        if (SyncContinue(result))
        //        {
        //            Complete(true);
        //        }
        //    }

        //    public static void End(IAsyncResult result, out InstanceStore instanceStore, out InstanceView commandResult)
        //    {
        //        InstanceCommandWithTemporaryHandleAsyncResult thisPtr = AsyncResult.End<InstanceCommandWithTemporaryHandleAsyncResult>(result);
        //        instanceStore = thisPtr.instanceStore;
        //        commandResult = thisPtr.commandResult;
        //    }

        //    static bool OnCommandCompleted(IAsyncResult result)
        //    {
        //        InstanceCommandWithTemporaryHandleAsyncResult thisPtr = (InstanceCommandWithTemporaryHandleAsyncResult)result.AsyncState;
        //        thisPtr.commandResult = thisPtr.instanceStore.EndExecute(result);
        //        return true;
        //    }

        //    static void OnComplete(AsyncResult result, Exception exception)
        //    {
        //        InstanceCommandWithTemporaryHandleAsyncResult thisPtr = (InstanceCommandWithTemporaryHandleAsyncResult)result;
        //        if (thisPtr.dependentTransaction != null)
        //        {
        //            thisPtr.dependentTransaction.Complete();
        //        }
        //        thisPtr.temporaryHandle.Free();
        //    }
        //}

        private class InstanceOperation
        {
            private AsyncWaitHandle _waitHandle;

            public InstanceOperation()
            {
                this.InterruptsScheduler = true;
                this.RequiresInitialized = true;
            }

            public bool Notified
            {
                get;
                set;
            }

            public int ActionId
            {
                get;
                set;
            }

            public bool InterruptsScheduler
            {
                get;
                protected set;
            }

            public bool RequiresInitialized
            {
                get;
                set;
            }

            public void OnEnqueued()
            {
                _waitHandle = new AsyncWaitHandle();
            }

            public virtual bool CanRun(WorkflowApplication instance)
            {
                return true;
            }

            public void NotifyTurn()
            {
                Fx.Assert(_waitHandle != null, "We must have a wait handle.");

                _waitHandle.Set();
            }

            public bool WaitForTurn(TimeSpan timeout)
            {
                if (_waitHandle != null)
                {
                    return _waitHandle.Wait(timeout);
                }

                return true;
            }

            public bool WaitForTurnAsync(TimeSpan timeout, Action<object, TimeoutException> callback, object state)
            {
                if (_waitHandle != null)
                {
                    return _waitHandle.WaitAsync(callback, state, timeout);
                }

                return true;
            }
        }

        private class RequiresIdleOperation : InstanceOperation
        {
            private bool _requiresRunnableInstance;

            public RequiresIdleOperation()
                : this(false)
            {
            }

            public RequiresIdleOperation(bool requiresRunnableInstance)
            {
                this.InterruptsScheduler = false;
                _requiresRunnableInstance = requiresRunnableInstance;
            }

            public override bool CanRun(WorkflowApplication instance)
            {
                if (_requiresRunnableInstance && instance._state != WorkflowApplicationState.Runnable)
                {
                    return false;
                }

                return instance.Controller.State == WorkflowInstanceState.Idle || instance.Controller.State == WorkflowInstanceState.Complete;
            }
        }

        private class DeferredRequiresIdleOperation : InstanceOperation
        {
            public DeferredRequiresIdleOperation()
            {
                this.InterruptsScheduler = false;
            }

            public override bool CanRun(WorkflowApplication instance)
            {
                return (this.ActionId != instance._actionCount && instance.Controller.State == WorkflowInstanceState.Idle) || instance.Controller.State == WorkflowInstanceState.Complete;
            }
        }

        private class RequiresPersistenceOperation : InstanceOperation
        {
            public override bool CanRun(WorkflowApplication instance)
            {
                if (!instance.Controller.IsPersistable && instance.Controller.State != WorkflowInstanceState.Complete)
                {
                    instance.Controller.PauseWhenPersistable();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private class WaitForTurnData
        {
            public WaitForTurnData(Action<object, TimeoutException> callback, object state, InstanceOperation operation, WorkflowApplication instance)
            {
                this.Callback = callback;
                this.State = state;
                this.Operation = operation;
                this.Instance = instance;
            }

            public Action<object, TimeoutException> Callback
            {
                get;
                private set;
            }

            public object State
            {
                get;
                private set;
            }

            public InstanceOperation Operation
            {
                get;
                private set;
            }

            public WorkflowApplication Instance
            {
                get;
                private set;
            }
        }

        // This is a thin shell of PersistenceManager functionality so that WorkflowApplicationInstance
        // can hold onto a PM without exposing the entire persistence functionality
        internal abstract class PersistenceManagerBase
        {
            public abstract InstanceStore InstanceStore { get; }
            public abstract Guid InstanceId { get; }
        }

        private class PersistenceManager : PersistenceManagerBase
        {
            private InstanceHandle _handle;
            private InstanceHandle _temporaryHandle;
            private InstanceOwner _owner;
            private bool _ownerWasCreated;
            private bool _isLocked;
            private bool _aborted;
            private bool _isTryLoad;
            private Guid _instanceId;
            private InstanceStore _store;
            // Initializing metadata, used when instance is created
            private IDictionary<XName, InstanceValue> _instanceMetadata;
            // Updateable metadata, used when instance is saved
            private IDictionary<XName, InstanceValue> _mutableMetadata;

            public PersistenceManager(InstanceStore store, IDictionary<XName, InstanceValue> instanceMetadata, Guid instanceId)
            {
                Fx.Assert(store != null, "We should never gets here without a store.");

                _instanceId = instanceId;
                _instanceMetadata = instanceMetadata;

                InitializeInstanceMetadata();

                _owner = store.DefaultInstanceOwner;
                if (_owner != null)
                {
                    _handle = store.CreateInstanceHandle(_owner, instanceId);
                }

                _store = store;
            }

            public PersistenceManager(InstanceStore store, IDictionary<XName, InstanceValue> instanceMetadata)
            {
                Fx.Assert(store != null, "We should never get here without a store.");

                _isTryLoad = true;
                _instanceMetadata = instanceMetadata;

                InitializeInstanceMetadata();

                _owner = store.DefaultInstanceOwner;
                if (_owner != null)
                {
                    _handle = store.CreateInstanceHandle(_owner);
                }

                _store = store;
            }

            public sealed override Guid InstanceId
            {
                get
                {
                    return _instanceId;
                }
            }

            public sealed override InstanceStore InstanceStore
            {
                get
                {
                    return _store;
                }
            }

            public bool IsInitialized
            {
                get
                {
                    return (_handle != null);
                }
            }

            public bool IsLocked
            {
                get
                {
                    return _isLocked;
                }
            }

            public bool OwnerWasCreated
            {
                get
                {
                    return _ownerWasCreated;
                }
            }

            private void InitializeInstanceMetadata()
            {
                if (_instanceMetadata == null)
                {
                    _instanceMetadata = new Dictionary<XName, InstanceValue>(1);
                }

                // We always set this key explicitly so that users can't override
                // this metadata value
                _instanceMetadata[PersistenceMetadataNamespace.InstanceType] = new InstanceValue(WorkflowNamespace.WorkflowHostType, InstanceValueOptions.WriteOnly);
            }

            public void SetInstanceMetadata(IDictionary<XName, InstanceValue> metadata)
            {
                Fx.Assert(_instanceMetadata.Count == 1, "We should only have the default metadata from InitializeInstanceMetadata");
                if (metadata != null)
                {
                    _instanceMetadata = metadata;
                    InitializeInstanceMetadata();
                }
            }

            public void SetMutablemetadata(IDictionary<XName, InstanceValue> metadata)
            {
                _mutableMetadata = metadata;
            }

            public void Initialize(WorkflowIdentity definitionIdentity, TimeSpan timeout)
            {
                Fx.Assert(_handle == null, "We are already initialized by now");

                //using (new TransactionScope(TransactionScopeOption.Suppress))
                //{
                try
                {
                    CreateTemporaryHandle(null);
                    _owner = _store.Execute(_temporaryHandle, GetCreateOwnerCommand(definitionIdentity), timeout).InstanceOwner;
                    _ownerWasCreated = true;
                }
                finally
                {
                    FreeTemporaryHandle();
                }

                _handle = _isTryLoad ? _store.CreateInstanceHandle(_owner) : _store.CreateInstanceHandle(_owner, InstanceId);

                //Thread.MemoryBarrier();
                if (_aborted)
                {
                    _handle.Free();
                }
                //}
            }

            private void CreateTemporaryHandle(InstanceOwner owner)
            {
                _temporaryHandle = _store.CreateInstanceHandle(owner);

                //Thread.MemoryBarrier();

                if (_aborted)
                {
                    FreeTemporaryHandle();
                }
            }

            private void FreeTemporaryHandle()
            {
                InstanceHandle handle = _temporaryHandle;

                if (handle != null)
                {
                    handle.Free();
                }
            }

            public IAsyncResult BeginInitialize(WorkflowIdentity definitionIdentity, TimeSpan timeout, AsyncCallback callback, object state)
            {
                Fx.Assert(_handle == null, "We are already initialized by now");

                //using (new TransactionScope(TransactionScopeOption.Suppress))
                //{
                IAsyncResult result = null;

                try
                {
                    CreateTemporaryHandle(null);
                    result = _store.BeginExecute(_temporaryHandle, GetCreateOwnerCommand(definitionIdentity), timeout, callback, state);
                }
                finally
                {
                    // We've encountered an exception
                    if (result == null)
                    {
                        FreeTemporaryHandle();
                    }
                }
                return result;
                //}
            }

            public void EndInitialize(IAsyncResult result)
            {
                try
                {
                    _owner = _store.EndExecute(result).InstanceOwner;
                    _ownerWasCreated = true;
                }
                finally
                {
                    FreeTemporaryHandle();
                }

                _handle = _isTryLoad ? _store.CreateInstanceHandle(_owner) : _store.CreateInstanceHandle(_owner, InstanceId);
                //Thread.MemoryBarrier();
                if (_aborted)
                {
                    _handle.Free();
                }
            }

            public void DeleteOwner(TimeSpan timeout)
            {
                try
                {
                    CreateTemporaryHandle(_owner);
                    _store.Execute(_temporaryHandle, new DeleteWorkflowOwnerCommand(), timeout);
                }
                // Ignore some exceptions because DeleteWorkflowOwner is best effort.
                catch (InstancePersistenceCommandException) { }
                catch (InstanceOwnerException) { }
                catch (OperationCanceledException) { }
                finally
                {
                    FreeTemporaryHandle();
                }
            }

            public IAsyncResult BeginDeleteOwner(TimeSpan timeout, AsyncCallback callback, object state)
            {
                IAsyncResult result = null;
                try
                {
                    CreateTemporaryHandle(_owner);
                    result = _store.BeginExecute(_temporaryHandle, new DeleteWorkflowOwnerCommand(), timeout, callback, state);
                }
                // Ignore some exceptions because DeleteWorkflowOwner is best effort.
                catch (InstancePersistenceCommandException) { }
                catch (InstanceOwnerException) { }
                catch (OperationCanceledException) { }
                finally
                {
                    if (result == null)
                    {
                        FreeTemporaryHandle();
                    }
                }
                return result;
            }

            public void EndDeleteOwner(IAsyncResult result)
            {
                try
                {
                    _store.EndExecute(result);
                }
                // Ignore some exceptions because DeleteWorkflowOwner is best effort.
                catch (InstancePersistenceCommandException) { }
                catch (InstanceOwnerException) { }
                catch (OperationCanceledException) { }
                finally
                {
                    FreeTemporaryHandle();
                }
            }

            public void EnsureReadyness(TimeSpan timeout)
            {
                Fx.Assert(_handle != null, "We should already be initialized by now");
                Fx.Assert(!IsLocked, "We are already ready for persistence; why are we being called?");
                Fx.Assert(!_isTryLoad, "Should not be on an initial save path if we tried load.");

                //using (new TransactionScope(TransactionScopeOption.Suppress))
                //{
                _store.Execute(_handle, CreateSaveCommand(null, _instanceMetadata, PersistenceOperation.Save), timeout);
                _isLocked = true;
                //}
            }

            public IAsyncResult BeginEnsureReadyness(TimeSpan timeout, AsyncCallback callback, object state)
            {
                Fx.Assert(_handle != null, "We should already be initialized by now");
                Fx.Assert(!IsLocked, "We are already ready for persistence; why are we being called?");
                Fx.Assert(!_isTryLoad, "Should not be on an initial save path if we tried load.");

                //using (new TransactionScope(TransactionScopeOption.Suppress))
                //{
                return _store.BeginExecute(_handle, CreateSaveCommand(null, _instanceMetadata, PersistenceOperation.Save), timeout, callback, state);
                //}
            }

            public void EndEnsureReadyness(IAsyncResult result)
            {
                _store.EndExecute(result);
                _isLocked = true;
            }

            public static Dictionary<XName, InstanceValue> GenerateInitialData(WorkflowApplication instance)
            {
                Dictionary<XName, InstanceValue> data = new Dictionary<XName, InstanceValue>(10);
                data[WorkflowNamespace.Bookmarks] = new InstanceValue(instance.Controller.GetBookmarks(), InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                data[WorkflowNamespace.LastUpdate] = new InstanceValue(DateTime.UtcNow, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);

                foreach (KeyValuePair<string, LocationInfo> mappedVariable in instance.Controller.GetMappedVariables())
                {
                    data[WorkflowNamespace.VariablesPath.GetName(mappedVariable.Key)] = new InstanceValue(mappedVariable.Value, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                }

                Fx.AssertAndThrow(instance.Controller.State != WorkflowInstanceState.Aborted, "Cannot generate data for an aborted instance.");
                if (instance.Controller.State != WorkflowInstanceState.Complete)
                {
                    data[WorkflowNamespace.Workflow] = new InstanceValue(instance.Controller.PrepareForSerialization());
                    data[WorkflowNamespace.Status] = new InstanceValue(instance.Controller.State == WorkflowInstanceState.Idle ? "Idle" : "Executing", InstanceValueOptions.WriteOnly);
                }
                else
                {
                    data[WorkflowNamespace.Workflow] = new InstanceValue(instance.Controller.PrepareForSerialization(), InstanceValueOptions.Optional);

                    Exception completionException;
                    IDictionary<string, object> outputs;
                    ActivityInstanceState completionState = instance.Controller.GetCompletionState(out outputs, out completionException);

                    if (completionState == ActivityInstanceState.Faulted)
                    {
                        data[WorkflowNamespace.Status] = new InstanceValue("Faulted", InstanceValueOptions.WriteOnly);
                        data[WorkflowNamespace.Exception] = new InstanceValue(completionException, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                    }
                    else if (completionState == ActivityInstanceState.Closed)
                    {
                        data[WorkflowNamespace.Status] = new InstanceValue("Closed", InstanceValueOptions.WriteOnly);
                        if (outputs != null)
                        {
                            foreach (KeyValuePair<string, object> output in outputs)
                            {
                                data[WorkflowNamespace.OutputPath.GetName(output.Key)] = new InstanceValue(output.Value, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                            }
                        }
                    }
                    else
                    {
                        Fx.AssertAndThrow(completionState == ActivityInstanceState.Canceled, "Cannot be executing when WorkflowState was completed.");
                        data[WorkflowNamespace.Status] = new InstanceValue("Canceled", InstanceValueOptions.WriteOnly);
                    }
                }
                return data;
            }

            private static InstancePersistenceCommand GetCreateOwnerCommand(WorkflowIdentity definitionIdentity)
            {
                // Technically, we only need to pass the owner identity when doing LoadRunnable.
                // However, if we create an instance with identity on a store that doesn't recognize it,
                // the identity metadata might be stored in a way which makes it unqueryable if the store
                // is later upgraded to support identity (e.g. SWIS 4.0 -> 4.5 upgrade). So to be on the
                // safe side, if we're using identity, we require the store to explicitly support it.
                if (definitionIdentity != null)
                {
                    CreateWorkflowOwnerWithIdentityCommand result = new CreateWorkflowOwnerWithIdentityCommand();
                    if (!object.ReferenceEquals(definitionIdentity, WorkflowApplication.s_unknownIdentity))
                    {
                        result.InstanceOwnerMetadata.Add(Workflow45Namespace.DefinitionIdentities,
                            new InstanceValue(new Collection<WorkflowIdentity> { definitionIdentity }));
                    }
                    return result;
                }
                else
                {
                    return new CreateWorkflowOwnerCommand();
                }
            }

            private static SaveWorkflowCommand CreateSaveCommand(IDictionary<XName, InstanceValue> instance, IDictionary<XName, InstanceValue> instanceMetadata, PersistenceOperation operation)
            {
                SaveWorkflowCommand saveCommand = new SaveWorkflowCommand()
                {
                    CompleteInstance = operation == PersistenceOperation.Complete,
                    UnlockInstance = operation != PersistenceOperation.Save,
                };

                if (instance != null)
                {
                    foreach (KeyValuePair<XName, InstanceValue> value in instance)
                    {
                        saveCommand.InstanceData.Add(value);
                    }
                }

                if (instanceMetadata != null)
                {
                    foreach (KeyValuePair<XName, InstanceValue> value in instanceMetadata)
                    {
                        saveCommand.InstanceMetadataChanges.Add(value);
                    }
                }

                return saveCommand;
            }

            private bool TryLoadHelper(InstanceView view, out IDictionary<XName, InstanceValue> data)
            {
                if (!view.IsBoundToLock)
                {
                    data = null;
                    return false;
                }
                _instanceId = view.InstanceId;
                _isLocked = true;

                if (!_handle.IsValid)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
                }

                data = view.InstanceData;
                return true;
            }

            public void Save(IDictionary<XName, InstanceValue> instance, PersistenceOperation operation, TimeSpan timeout)
            {
                _store.Execute(_handle, CreateSaveCommand(instance, (_isLocked ? _mutableMetadata : _instanceMetadata), operation), timeout);
                _isLocked = true;
            }

            public IDictionary<XName, InstanceValue> Load(TimeSpan timeout)
            {
                InstanceView view = _store.Execute(_handle, new LoadWorkflowCommand(), timeout);
                _isLocked = true;

                if (!_handle.IsValid)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
                }

                return view.InstanceData;
            }

            public bool TryLoad(TimeSpan timeout, out IDictionary<XName, InstanceValue> data)
            {
                InstanceView view = _store.Execute(_handle, new TryLoadRunnableWorkflowCommand(), timeout);
                return TryLoadHelper(view, out data);
            }

            public IAsyncResult BeginSave(IDictionary<XName, InstanceValue> instance, PersistenceOperation operation, TimeSpan timeout, AsyncCallback callback, object state)
            {
                return _store.BeginExecute(_handle, CreateSaveCommand(instance, (_isLocked ? _mutableMetadata : _instanceMetadata), operation), timeout, callback, state);
            }

            public void EndSave(IAsyncResult result)
            {
                _store.EndExecute(result);
                _isLocked = true;
            }

            public IAsyncResult BeginLoad(TimeSpan timeout, AsyncCallback callback, object state)
            {
                return _store.BeginExecute(_handle, new LoadWorkflowCommand(), timeout, callback, state);
            }

            public IDictionary<XName, InstanceValue> EndLoad(IAsyncResult result)
            {
                InstanceView view = _store.EndExecute(result);
                _isLocked = true;

                if (!_handle.IsValid)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
                }

                return view.InstanceData;
            }

            public IAsyncResult BeginTryLoad(TimeSpan timeout, AsyncCallback callback, object state)
            {
                return _store.BeginExecute(_handle, new TryLoadRunnableWorkflowCommand(), timeout, callback, state);
            }

            public bool EndTryLoad(IAsyncResult result, out IDictionary<XName, InstanceValue> data)
            {
                InstanceView view = _store.EndExecute(result);
                return TryLoadHelper(view, out data);
            }

            public void Abort()
            {
                _aborted = true;

                // Make sure the setter of handle sees aborted, or v.v., or both.
                //Thread.MemoryBarrier();

                InstanceHandle handle = _handle;
                if (handle != null)
                {
                    handle.Free();
                }

                FreeTemporaryHandle();
            }

            public void Unlock(TimeSpan timeout)
            {
                SaveWorkflowCommand saveCmd = new SaveWorkflowCommand()
                {
                    UnlockInstance = true,
                };

                _store.Execute(_handle, saveCmd, timeout);
            }

            public IAsyncResult BeginUnlock(TimeSpan timeout, AsyncCallback callback, object state)
            {
                SaveWorkflowCommand saveCmd = new SaveWorkflowCommand()
                {
                    UnlockInstance = true,
                };

                return _store.BeginExecute(_handle, saveCmd, timeout, callback, state);
            }

            public void EndUnlock(IAsyncResult result)
            {
                _store.EndExecute(result);
            }
        }
    }
}

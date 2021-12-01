// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Transactions;
using System.Xml.Linq;

namespace System.Activities;
using DurableInstancing;
using Hosting;
using Internals;
using Runtime;
using Runtime.DurableInstancing;
using Tracking;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

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
public sealed partial class WorkflowApplication : WorkflowInstance
{
    private static AsyncCallback eventFrameCallback;
    private static IdleEventHandler idleHandler;
    private static CompletedEventHandler completedHandler;
    private static UnhandledExceptionEventHandler unhandledExceptionHandler;
    private static Action<object, TimeoutException> waitAsyncCompleteCallback;
    private static readonly WorkflowIdentity unknownIdentity = new();
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
    private readonly Quack<InstanceOperation> _pendingOperations;
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
    private readonly IDictionary<string, object> _initialWorkflowArguments;
    private readonly IList<Handle> _rootExecutionProperties;
    private IDictionary<XName, InstanceValue> _instanceMetadata;

    public WorkflowApplication(Activity workflowDefinition)
        : this(workflowDefinition, (WorkflowIdentity)null) { }

    public WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs)
        : this(workflowDefinition, inputs, (WorkflowIdentity)null) { }

    public WorkflowApplication(Activity workflowDefinition, WorkflowIdentity definitionIdentity)
        : base(workflowDefinition, definitionIdentity)
    {
        _pendingOperations = new Quack<InstanceOperation>();
        Fx.Assert(_state == WorkflowApplicationState.Paused, "We always start out paused (the default)");
    }

    public WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs, WorkflowIdentity definitionIdentity)
        : this(workflowDefinition, definitionIdentity)
    {
        _initialWorkflowArguments = inputs ?? throw FxTrace.Exception.ArgumentNull(nameof(inputs));
    }

    private WorkflowApplication(Activity workflowDefinition, IDictionary<string, object> inputs, IList<Handle> executionProperties)
        : this(workflowDefinition)
    {
        _initialWorkflowArguments = inputs;
        _rootExecutionProperties = executionProperties;
    }

    public InstanceStore InstanceStore
    {
        get => _instanceStore;
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
                if (IsReadOnly)
                {
                    _extensions.MakeReadOnly();
                }
            }
            return _extensions;
        }
    }

    public Action<WorkflowApplicationAbortedEventArgs> Aborted
    {
        get => _onAborted;
        set
        {
            ThrowIfMulticast(value);
            _onAborted = value;
        }
    }

    public Action<WorkflowApplicationEventArgs> Unloaded
    {
        get => _onUnloaded;
        set
        {
            ThrowIfMulticast(value);
            _onUnloaded = value;
        }
    }

    public Action<WorkflowApplicationCompletedEventArgs> Completed
    {
        get => _onCompleted;
        set
        {
            ThrowIfMulticast(value);
            _onCompleted = value;
        }
    }

    public Func<WorkflowApplicationUnhandledExceptionEventArgs, UnhandledExceptionAction> OnUnhandledException
    {
        get => _onUnhandledException;
        set
        {
            ThrowIfMulticast(value);
            _onUnhandledException = value;
        }
    }

    public Action<WorkflowApplicationIdleEventArgs> Idle
    {
        get => _onIdle;
        set
        {
            ThrowIfMulticast(value);
            _onIdle = value;
        }
    }

    public Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> PersistableIdle
    {
        get => _onPersistableIdle;
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

    protected internal override bool SupportsInstanceKeys => false;

    private static AsyncCallback EventFrameCallback
    {
        get
        {
            eventFrameCallback ??= Fx.ThunkCallback(new AsyncCallback(EventFrame));
            return eventFrameCallback;
        }
    }

    private WorkflowEventData EventData
    {
        get
        {
            _eventData ??= new WorkflowEventData(this);
            return _eventData;
        }
    }

    private bool HasPersistenceProvider => _persistenceManager != null;

    private bool IsHandlerThread => _isInHandler && _handlerThreadId == Environment.CurrentManagedThreadId;

    private bool IsInTerminalState => _state == WorkflowApplicationState.Unloaded || _state == WorkflowApplicationState.Aborted;

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
    internal IEnumerable<T> InternalGetExtensions<T>() where T : class => GetExtensions<T>();

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

    private bool ShouldRaiseComplete(WorkflowInstanceState state) => state == WorkflowInstanceState.Complete && !_hasRaisedCompleted;

    private void Enqueue(InstanceOperation operation) => Enqueue(operation, false);

    private void Enqueue(InstanceOperation operation, bool push)
    {
        lock (_pendingOperations)
        {
            operation.ActionId = _actionCount;

            if (_isBusy)
            {
                // If base.IsReadOnly == false, we can't call the Controller yet because WorkflowInstance is not initialized.
                // But that's okay; if the instance isn't initialized then the scheduler's not running yet, so no need to pause it.
                if (operation.InterruptsScheduler && IsReadOnly)
                {
                    Controller.RequestPause();
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

                if (!operation.CanRun(this) && !IsInTerminalState)
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
        if (IsReadOnly)
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
        return WaitForTurnNoEnqueue(operation, timeout);
    }

    private bool WaitForTurnNoEnqueue(InstanceOperation operation, TimeSpan timeout)
    {
        if (!operation.WaitForTurn(timeout))
        {
            if (Remove(operation))
            {
                throw FxTrace.Exception.AsError(new TimeoutException(SR.TimeoutOnOperation(timeout)));
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

        return WaitForTurnNoEnqueueAsync(operation, timeout, callback, state);
    }

    private bool WaitForTurnNoEnqueueAsync(InstanceOperation operation, TimeSpan timeout, Action<object, TimeoutException> callback, object state)
    {
        if (waitAsyncCompleteCallback == null)
        {
            waitAsyncCompleteCallback = new Action<object, TimeoutException>(OnWaitAsyncComplete);
        }
        return operation.WaitForTurnAsync(timeout, waitAsyncCompleteCallback, new WaitForTurnData(callback, state, operation, this));
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
    private void ForceNotifyOperationComplete() => OnNotifyPaused();

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
            if (temp.CanRun(this) || IsInTerminalState)
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
        if (!IsReadOnly)
        {
            // For newly created workflows (e.g. not the Load() case), we need to initialize now
            RegisterExtensionManager(_extensions);
            Initialize(_initialWorkflowArguments, _rootExecutionProperties);

            // make sure we have a persistence manager if necessary
            if (_persistenceManager == null && _instanceStore != null)
            {
                Fx.Assert(Id != Guid.Empty, "should have a valid Id at this point");
                _persistenceManager = new PersistenceManager(_instanceStore, GetInstanceMetadata(), Id);
            }
        }
    }

    protected override void OnNotifyPaused()
    {
        Fx.Assert(_isBusy, "We're always busy when we get this notification.");

        WorkflowInstanceState? localInstanceState = null;
        if (IsReadOnly)
        {
            localInstanceState = Controller.State;
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

                    if (completedHandler == null)
                    {
                        completedHandler = new CompletedEventHandler();
                    }
                    stillSync = completedHandler.Run(this);
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
                        if (idleHandler == null)
                        {
                            idleHandler = new IdleEventHandler();
                        }
                        stillSync = idleHandler.Run(this);
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

                    Controller.Run();
                    stillSync = false;
                }
            }
        }
    }

    // used by WorkflowInvoker in the InvokeAsync case
    internal void GetCompletionStatus(out Exception terminationException, out bool cancelled)
    {
        ActivityInstanceState completionState = Controller.GetCompletionState(out _, out terminationException);
        Fx.Assert(completionState != ActivityInstanceState.Executing, "Activity cannot be executing when this method is called");
        cancelled = (completionState == ActivityInstanceState.Canceled);
    }

    protected internal override void OnRequestAbort(Exception reason) => AbortInstance(reason, false);

    public void Abort() => Abort(SR.DefaultAbortReason);

    public void Abort(string reason) => Abort(reason, null);

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
        _persistenceManager?.Abort();

        PersistencePipeline currentPersistencePipeline = _persistencePipelineInUse;
        currentPersistencePipeline?.Abort();
    }

    private void AbortInstance(Exception reason, bool isWorkflowThread)
    {
        _state = WorkflowApplicationState.Aborted;

        // Need to ensure that either components see the Aborted state, this method sees the components, or both.
        Thread.MemoryBarrier();

        // We do this outside of the lock since persistence
        // might currently be blocking access to the lock.
        AbortPersistence();

        if (isWorkflowThread)
        {
            if (!_hasCalledAbort)
            {
                _hasCalledAbort = true;
                Controller.Abort(reason);

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
                        Controller.Abort(reason);

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
            // We swallow this exception because we were simply doing our
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
                Controller.Abort(reason);
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
        if (Controller.HasPendingTrackingRecords || Aborted != null)
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

        if (Controller.HasPendingTrackingRecords)
        {
            try
            {
                IAsyncResult result = Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, Fx.ThunkCallback(new AsyncCallback(OnAbortTrackingComplete)), reason);

                if (result.CompletedSynchronously)
                {
                    Controller.EndFlushTrackingRecords(result);
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

                // We swallow any exception here because we are on the abort path
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
            Controller.EndFlushTrackingRecords(result);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            // We swallow any exception here because we are on the abort path
            // and are doing a best effort to track this record.
        }

        RaiseAborted(reason);
    }

    private void RaiseAborted(Exception reason)
    {
        if (_invokeCompletedCallback == null)
        {
            Action<WorkflowApplicationAbortedEventArgs> abortedHandler = Aborted;

            if (abortedHandler != null)
            {
                try
                {
                    _handlerThreadId = Environment.CurrentManagedThreadId;
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
            TD.WorkflowInstanceAborted(Id.ToString(), reason);
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
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(reason));
        }

        Terminate(new WorkflowApplicationTerminatedException(reason, Id), timeout);
    }

    public void Terminate(Exception reason, TimeSpan timeout)
    {
        if (reason == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(reason));
        }

        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        TimeoutHelper timeoutHelper = new(timeout);
        InstanceOperation operation = null;

        try
        {
            operation = new InstanceOperation();

            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForTerminate();

            TerminateCore(reason);

            Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }

    private void TerminateCore(Exception reason) => Controller.Terminate(reason);

    public IAsyncResult BeginTerminate(string reason, AsyncCallback callback, object state) => BeginTerminate(reason, ActivityDefaults.AcquireLockTimeout, callback, state);

    public IAsyncResult BeginTerminate(Exception reason, AsyncCallback callback, object state) => BeginTerminate(reason, ActivityDefaults.AcquireLockTimeout, callback, state);

    public IAsyncResult BeginTerminate(string reason, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (string.IsNullOrEmpty(reason))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(reason));
        }

        return BeginTerminate(new WorkflowApplicationTerminatedException(reason, Id), timeout, callback, state);
    }

    public IAsyncResult BeginTerminate(Exception reason, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (reason == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(reason));
        }

        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return TerminateAsyncResult.Create(this, reason, timeout, callback, state);
    }

#pragma warning disable CA1822 // Mark members as static
    public void EndTerminate(IAsyncResult result) => TerminateAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    // called from the sync and async paths
    private void CancelCore()
    {
        // We only actually do any work if we haven't completed and we aren't
        // unloaded.
        if (!_hasRaisedCompleted && _state != WorkflowApplicationState.Unloaded)
        {
            Controller.ScheduleCancel();

            // This is a loose check, but worst case scenario we call
            // an extra, unnecessary Run
            if (!_hasCalledRun && !_hasRaisedCompleted)
            {
                RunCore();
            }
        }
    }

    public void Cancel() => Cancel(ActivityDefaults.AcquireLockTimeout);

    public void Cancel(TimeSpan timeout)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        TimeoutHelper timeoutHelper = new(timeout);

        InstanceOperation operation = null;

        try
        {
            operation = new InstanceOperation();

            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForCancel();

            CancelCore();

            Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }

    public IAsyncResult BeginCancel(AsyncCallback callback, object state) => BeginCancel(ActivityDefaults.AcquireLockTimeout, callback, state);

    public IAsyncResult BeginCancel(TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return CancelAsyncResult.Create(this, timeout, callback, state);
    }

#pragma warning disable CA1822 // Mark members as static
    public void EndCancel(IAsyncResult result) => CancelAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    // called on the Invoke path, this will go away when WorkflowInvoker implements WorkflowInstance directly
    private static WorkflowApplication CreateInstance(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, SynchronizationContext syncContext, Action invokeCompletedCallback)
    {
        // 1) Create the workflow instance
        Transaction ambientTransaction = Transaction.Current;
        List<Handle> workflowExecutionProperties = null;

        if (ambientTransaction != null)
        {
            // no need for a NoPersistHandle since the ActivityExecutor performs a no-persist zone
            // as part of the RuntimeTransactionHandle processing
            workflowExecutionProperties = new List<Handle>(1)
            {
                new RuntimeTransactionHandle(ambientTransaction)
            };
        }

        WorkflowApplication instance = new(activity, inputs, workflowExecutionProperties)
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
        PumpBasedSynchronizationContext syncContext = new(timeout);
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
            throw FxTrace.Exception.AsError(completionException);
        }

        return outputs;
    }

    internal static IAsyncResult BeginInvoke(Activity activity, IDictionary<string, object> inputs, WorkflowInstanceExtensionManager extensions, TimeSpan timeout, SynchronizationContext syncContext, AsyncInvokeContext invokeContext, AsyncCallback callback, object state)
    {
        Fx.Assert(activity != null, "The activity must not be null.");

        return new InvokeAsyncResult(activity, inputs, extensions, timeout, syncContext, invokeContext, callback, state);
    }

    internal static IDictionary<string, object> EndInvoke(IAsyncResult result) => InvokeAsyncResult.End(result);

    public void Run() => Run(ActivityDefaults.AcquireLockTimeout);

    public void Run(TimeSpan timeout) => InternalRun(timeout, true);

    private void InternalRun(TimeSpan timeout, bool isUserRun)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        TimeoutHelper timeoutHelper = new(timeout);
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

            Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
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

    public IAsyncResult BeginRun(AsyncCallback callback, object state) => BeginRun(ActivityDefaults.AcquireLockTimeout, callback, state);

    public IAsyncResult BeginRun(TimeSpan timeout, AsyncCallback callback, object state) => BeginInternalRun(timeout, true, callback, state);

    private IAsyncResult BeginInternalRun(TimeSpan timeout, bool isUserRun, AsyncCallback callback, object state)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return RunAsyncResult.Create(this, isUserRun, timeout, callback, state);
    }

#pragma warning disable CA1822 // Mark members as static
    public void EndRun(IAsyncResult result) => RunAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    // shared by Load/BeginLoad
    private bool IsLoadTransactionRequired() => GetExtensions<IPersistencePipelineModule>().Any(module => module.IsLoadTransactionRequired);

    private void CreatePersistenceManager()
    {
        PersistenceManager newManager = new(InstanceStore, GetInstanceMetadata(), _instanceId);
        SetPersistenceManager(newManager);
    }

    // shared by Load(WorkflowApplicationInstance)/BeginLoad*
    private void SetPersistenceManager(PersistenceManager newManager)
    {
        Fx.Assert(_persistenceManager == null, "SetPersistenceManager should only be called once");

        // first register our extensions since we'll need them to construct the pipeline
        RegisterExtensionManager(_extensions);
        _persistenceManager = newManager;
    }

    // shared by Load/BeginLoad
    private PersistencePipeline ProcessInstanceValues(IDictionary<XName, InstanceValue> values, out object deserializedRuntimeState)
    {
        PersistencePipeline result = null;
        deserializedRuntimeState = ExtractRuntimeState(values, _persistenceManager.InstanceId);

        if (HasPersistenceModule)
        {
            IEnumerable<IPersistencePipelineModule> modules = GetExtensions<IPersistencePipelineModule>();
            result = new PersistencePipeline(modules);
            result.SetLoadedValues(values);
        }

        return result;
    }

    private static ActivityExecutor ExtractRuntimeState(IDictionary<XName, InstanceValue> values, Guid instanceId)
    {
        if (values.TryGetValue(WorkflowNamespace.Workflow, out InstanceValue value))
        {
            if (value.Value is ActivityExecutor result)
            {
                return result;
            }
        }
        throw FxTrace.Exception.AsError(new InstancePersistenceException(SR.WorkflowInstanceNotFoundInStore(instanceId)));
    }

    public static void CreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
        => CreateDefaultInstanceOwner(instanceStore, definitionIdentity, identityFilter, ActivityDefaults.OpenTimeout);

    public static void CreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter, TimeSpan timeout)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        if (instanceStore.DefaultInstanceOwner != null)
        {
            throw FxTrace.Exception.Argument(nameof(instanceStore), SR.InstanceStoreHasDefaultOwner);
        }

        CreateWorkflowOwnerWithIdentityCommand command = GetCreateOwnerCommand(definitionIdentity, identityFilter);
        InstanceView commandResult = ExecuteInstanceCommandWithTemporaryHandle(instanceStore, command, timeout);
        instanceStore.DefaultInstanceOwner = commandResult.InstanceOwner;
    }

    public static IAsyncResult BeginCreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity,
        WorkflowIdentityFilter identityFilter, AsyncCallback callback, object state)
        => BeginCreateDefaultInstanceOwner(instanceStore, definitionIdentity, identityFilter, ActivityDefaults.OpenTimeout, callback, state);

    public static IAsyncResult BeginCreateDefaultInstanceOwner(InstanceStore instanceStore, WorkflowIdentity definitionIdentity,
        WorkflowIdentityFilter identityFilter, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        if (instanceStore.DefaultInstanceOwner != null)
        {
            throw FxTrace.Exception.Argument(nameof(instanceStore), SR.InstanceStoreHasDefaultOwner);
        }

        CreateWorkflowOwnerWithIdentityCommand command = GetCreateOwnerCommand(definitionIdentity, identityFilter);
        return new InstanceCommandWithTemporaryHandleAsyncResult(instanceStore, command, timeout, callback, state);
    }

    public static void EndCreateDefaultInstanceOwner(IAsyncResult asyncResult)
    {
        InstanceCommandWithTemporaryHandleAsyncResult.End(asyncResult, out InstanceStore instanceStore, out InstanceView commandResult);
        instanceStore.DefaultInstanceOwner = commandResult.InstanceOwner;
    }

    public static void DeleteDefaultInstanceOwner(InstanceStore instanceStore) => DeleteDefaultInstanceOwner(instanceStore, ActivityDefaults.CloseTimeout);

    public static void DeleteDefaultInstanceOwner(InstanceStore instanceStore, TimeSpan timeout)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        if (instanceStore.DefaultInstanceOwner == null)
        {
            return;
        }

        DeleteWorkflowOwnerCommand command = new();
        ExecuteInstanceCommandWithTemporaryHandle(instanceStore, command, timeout);
        instanceStore.DefaultInstanceOwner = null;
    }

    public static IAsyncResult BeginDeleteDefaultInstanceOwner(InstanceStore instanceStore, AsyncCallback callback, object state)
        => BeginDeleteDefaultInstanceOwner(instanceStore, ActivityDefaults.CloseTimeout, callback, state);

    public static IAsyncResult BeginDeleteDefaultInstanceOwner(InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        if (instanceStore.DefaultInstanceOwner == null)
        {
            return new CompletedAsyncResult(callback, state);
        }

        DeleteWorkflowOwnerCommand command = new();
        return new InstanceCommandWithTemporaryHandleAsyncResult(instanceStore, command, timeout, callback, state);
    }

    public static void EndDeleteDefaultInstanceOwner(IAsyncResult asyncResult)
    {
        if (asyncResult is CompletedAsyncResult)
        {
            CompletedAsyncResult.End(asyncResult);
            return;
        }

        InstanceCommandWithTemporaryHandleAsyncResult.End(asyncResult, out InstanceStore instanceStore, out _);            
        instanceStore.DefaultInstanceOwner = null;
    }

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
            temporaryHandle?.Free();
        }
    }

    private static CreateWorkflowOwnerWithIdentityCommand GetCreateOwnerCommand(WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
    {
        if (!identityFilter.IsValid())
        {
            throw FxTrace.Exception.AsError(new ArgumentOutOfRangeException(nameof(identityFilter)));
        }
        if (definitionIdentity == null && identityFilter != WorkflowIdentityFilter.Any)
        {
            // This API isn't useful for null identity, because WFApp only adds a default WorkflowHostType
            // to instances with non-null identity.
            throw FxTrace.Exception.Argument(nameof(definitionIdentity), SR.CannotCreateOwnerWithoutIdentity);
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

    public static WorkflowApplicationInstance GetRunnableInstance(InstanceStore instanceStore) => GetRunnableInstance(instanceStore, ActivityDefaults.LoadTimeout);

    public static WorkflowApplicationInstance GetRunnableInstance(InstanceStore instanceStore, TimeSpan timeout)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        TimeoutHelper.ThrowIfNegativeArgument(timeout);
        if (instanceStore.DefaultInstanceOwner == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.GetRunnableRequiresOwner));
        }

        PersistenceManager newManager = new(instanceStore, null);
        return LoadCore(timeout, true, newManager);
    }

    public static IAsyncResult BeginGetRunnableInstance(InstanceStore instanceStore, AsyncCallback callback, object state)
        => BeginGetRunnableInstance(instanceStore, ActivityDefaults.LoadTimeout, callback, state);

    public static IAsyncResult BeginGetRunnableInstance(InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        TimeoutHelper.ThrowIfNegativeArgument(timeout);
        if (instanceStore.DefaultInstanceOwner == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.GetRunnableRequiresOwner));
        }

        PersistenceManager newManager = new(instanceStore, null);
        return new LoadAsyncResult(null, newManager, true, timeout, callback, state);
    }

    public static WorkflowApplicationInstance EndGetRunnableInstance(IAsyncResult asyncResult) => LoadAsyncResult.EndAndCreateInstance(asyncResult);

    public static WorkflowApplicationInstance GetInstance(Guid instanceId, InstanceStore instanceStore) => GetInstance(instanceId, instanceStore, ActivityDefaults.LoadTimeout);

    public static WorkflowApplicationInstance GetInstance(Guid instanceId, InstanceStore instanceStore, TimeSpan timeout)
    {
        if (instanceId == Guid.Empty)
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(instanceId));
        }
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        PersistenceManager newManager = new(instanceStore, null, instanceId);
        return LoadCore(timeout, false, newManager);
    }

    public static IAsyncResult BeginGetInstance(Guid instanceId, InstanceStore instanceStore, AsyncCallback callback, object state)
        => BeginGetInstance(instanceId, instanceStore, ActivityDefaults.LoadTimeout, callback, state);

    public static IAsyncResult BeginGetInstance(Guid instanceId, InstanceStore instanceStore, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (instanceId == Guid.Empty)
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(instanceId));
        }
        if (instanceStore == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instanceStore));
        }
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        PersistenceManager newManager = new(instanceStore, null, instanceId);
        return new LoadAsyncResult(null, newManager, false, timeout, callback, state);
    }

    public static WorkflowApplicationInstance EndGetInstance(IAsyncResult asyncResult) => LoadAsyncResult.EndAndCreateInstance(asyncResult);

#if DYNAMICUPDATE
    public void Load(WorkflowApplicationInstance instance)
    {
        Load(instance, ActivityDefaults.LoadTimeout);
    }

    public void Load(WorkflowApplicationInstance instance, TimeSpan timeout)
    {
        Load(instance, null, timeout);
    }

    public void Load(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap)
    {
        Load(instance, updateMap, ActivityDefaults.LoadTimeout);
    }

    public void Load(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap, TimeSpan timeout)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instance == null)
        {
            throw FxTrace.Exception.ArgumentNull("instance");
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (this.instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (this.initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (this.InstanceStore != null && this.InstanceStore != instance.InstanceStore)
        {
            throw FxTrace.Exception.Argument("instance", SR.InstanceStoreDoesntMatchWorkflowApplication);
        }

        instance.MarkAsLoaded();

        InstanceOperation operation = new InstanceOperation { RequiresInitialized = false };

        try
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForLoad();

            this.instanceId = instance.InstanceId;
            this.instanceIdSet = true;
            if (this.instanceStore == null)
            {
                this.instanceStore = instance.InstanceStore;
            }

            PersistenceManager newManager = (PersistenceManager)instance.PersistenceManager;
            newManager.SetInstanceMetadata(GetInstanceMetadata());
            SetPersistenceManager(newManager);

            LoadCore(updateMap, timeoutHelper, false, instance.Values);
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    } 
#else

    public void Load(WorkflowApplicationInstance instance) => Load(instance, ActivityDefaults.LoadTimeout);

    public void Load(WorkflowApplicationInstance instance, TimeSpan timeout)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instance == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instance));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (InstanceStore != null && InstanceStore != instance.InstanceStore)
        {
            throw FxTrace.Exception.Argument(nameof(instance), SR.InstanceStoreDoesntMatchWorkflowApplication);
        }

        instance.MarkAsLoaded();

        InstanceOperation operation = new() { RequiresInitialized = false };

        try
        {
            TimeoutHelper timeoutHelper = new(timeout);
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
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }
#endif

    public void LoadRunnableInstance() => LoadRunnableInstance(ActivityDefaults.LoadTimeout);

    public void LoadRunnableInstance(TimeSpan timeout)
    {
        ThrowIfReadOnly(); // only allow a single Load() or Run()

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (InstanceStore == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        }
        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (_persistenceManager != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
        }

        InstanceOperation operation = new() { RequiresInitialized = false };

        try
        {
            TimeoutHelper timeoutHelper = new(timeout);
            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForLoad();

            RegisterExtensionManager(_extensions);
            _persistenceManager = new PersistenceManager(InstanceStore, GetInstanceMetadata());

            if (!_persistenceManager.IsInitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
            }

#if DYNAMICUPDATE
            LoadCore(null, timeoutHelper, true); 
#else
            LoadCore(timeoutHelper, true);
#endif
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }

    public void Load(Guid instanceId) => Load(instanceId, ActivityDefaults.LoadTimeout);

    public void Load(Guid instanceId, TimeSpan timeout)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instanceId == Guid.Empty)
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(instanceId));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (InstanceStore == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        }
        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }

        InstanceOperation operation = new() { RequiresInitialized = false };

        try
        {
            TimeoutHelper timeoutHelper = new(timeout);
            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForLoad();

            _instanceId = instanceId;
            _instanceIdSet = true;

            CreatePersistenceManager();
#if DYNAMICUPDATE
            LoadCore(null, timeoutHelper, false); 
#else
            LoadCore(timeoutHelper, false);
#endif
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }

#if DYNAMICUPDATE
    void LoadCore(DynamicUpdateMap updateMap, TimeoutHelper timeoutHelper, bool loadAny, IDictionary<XName, InstanceValue> values = null) 
#else
    private void LoadCore(TimeoutHelper timeoutHelper, bool loadAny, IDictionary<XName, InstanceValue> values = null)
#endif
    {
        if (values == null)
        {
            if (!_persistenceManager.IsInitialized)
            {
                _persistenceManager.Initialize(DefinitionIdentity, timeoutHelper.RemainingTime());
            }
        }
        else
        {
            Fx.Assert(_persistenceManager.IsInitialized, "Caller should have initialized Persistence Manager");
            Fx.Assert(_instanceIdSet, "Caller should have set InstanceId");
        }

        PersistencePipeline pipeline = null;
        WorkflowPersistenceContext context = null;
        TransactionScope scope = null;
        bool success = false;
        Exception abortReasonInnerException = null;
        try
        {
            InitializePersistenceContext(IsLoadTransactionRequired(), timeoutHelper, out context, out scope);

            if (values == null)
            {
                values = LoadValues(_persistenceManager, timeoutHelper, loadAny);
                if (loadAny)
                {
                    if (_instanceIdSet)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
                    }

                    _instanceId = _persistenceManager.InstanceId;
                    _instanceIdSet = true;
                }
            }
            pipeline = ProcessInstanceValues(values, out object deserializedRuntimeState);

            if (pipeline != null)
            {
                try
                {
                    _persistencePipelineInUse = pipeline;

                    // Need to ensure that either we see the Aborted state, AbortInstance sees us, or both.
                    Thread.MemoryBarrier();

                    if (_state == WorkflowApplicationState.Aborted)
                    {
                        throw FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
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
#if DYNAMICUPDATE
                base.Initialize(deserializedRuntimeState, updateMap);
                if (updateMap != null)
                {
                    UpdateInstanceMetadata();
                } 
#else
                Initialize(deserializedRuntimeState);
#endif
            }
#if DYNAMICUPDATE
            catch (InstanceUpdateException e)
            {
                abortReasonInnerException = e;
                throw;
            } 
#endif
            catch (VersionMismatchException e)
            {
                abortReasonInnerException = e;
                throw;
            }

            success = true;
        }
        finally
        {
            CompletePersistenceContext(context, scope, success);
            if (!success)
            {
                AbortDueToException(abortReasonInnerException);
            }
        }

        pipeline?.Publish();
    }

    private void AbortDueToException(Exception e)
    {
#if DYNAMICUPDATE
        if (e is InstanceUpdateException)
        {
            this.Abort(SR.AbortingDueToDynamicUpdateFailure, e);
        }
        else  
#endif
        if (e is VersionMismatchException)
        {
            Abort(SR.AbortingDueToVersionMismatch, e);
        }
        else
        {
            Abort(SR.AbortingDueToLoadFailure);
        }
    }

    private static WorkflowApplicationInstance LoadCore(TimeSpan timeout, bool loadAny, PersistenceManager persistenceManager)
    {
        TimeoutHelper timeoutHelper = new(timeout);

        if (!persistenceManager.IsInitialized)
        {
            persistenceManager.Initialize(unknownIdentity, timeoutHelper.RemainingTime());
        }

        WorkflowPersistenceContext context = null;
        TransactionScope scope = null;
        WorkflowApplicationInstance result = null;
        try
        {
            InitializePersistenceContext(false, timeoutHelper, out context, out scope);

            IDictionary<XName, InstanceValue> values = LoadValues(persistenceManager, timeoutHelper, loadAny);
            ActivityExecutor deserializedRuntimeState = ExtractRuntimeState(values, persistenceManager.InstanceId);
            result = new WorkflowApplicationInstance(persistenceManager, values, deserializedRuntimeState.WorkflowIdentity);
        }
        finally
        {
            bool success = result != null;
            CompletePersistenceContext(context, scope, success);
            if (!success)
            {
                persistenceManager.Abort();
            }
        }

        return result;
    }

    private static void InitializePersistenceContext(bool isTransactionRequired, TimeoutHelper timeoutHelper,
        out WorkflowPersistenceContext context, out TransactionScope scope)
    {
        context = new WorkflowPersistenceContext(isTransactionRequired, timeoutHelper.OriginalTimeout);
        scope = TransactionHelper.CreateTransactionScope(context.PublicTransaction);
    }

    private static void CompletePersistenceContext(WorkflowPersistenceContext context, TransactionScope scope, bool success)
    {
        // Clean up the transaction scope regardless of failure
        TransactionHelper.CompleteTransactionScope(ref scope);

        if (context != null)
        {
            if (success)
            {
                context.Complete();
            }
            else
            {
                context.Abort();
            }
        }
    }

    private static IDictionary<XName, InstanceValue> LoadValues(PersistenceManager persistenceManager, TimeoutHelper timeoutHelper, bool loadAny)
    {
        IDictionary<XName, InstanceValue> values;
        if (loadAny)
        {
            if (!persistenceManager.TryLoad(timeoutHelper.RemainingTime(), out values))
            {
                throw FxTrace.Exception.AsError(new InstanceNotReadyException(SR.NoRunnableInstances));
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
        TimeoutHelper timeoutHelper = new(timeout);
        UnlockInstance(manager, timeoutHelper);
    }

    internal static IAsyncResult BeginDiscardInstance(PersistenceManagerBase persistanceManager, TimeSpan timeout, AsyncCallback callback, object state)
    {
        PersistenceManager manager = (PersistenceManager)persistanceManager;
        TimeoutHelper timeoutHelper = new(timeout);
        return new UnlockInstanceAsyncResult(manager, timeoutHelper, callback, state);
    }

    internal static void EndDiscardInstance(IAsyncResult asyncResult) => UnlockInstanceAsyncResult.End(asyncResult);

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

#if DYNAMICUPDATE
    internal static IList<ActivityBlockingUpdate> GetActivitiesBlockingUpdate(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap)
    {
        object deserializedRuntimeState = ExtractRuntimeState(instance.Values, instance.InstanceId);
        return WorkflowInstance.GetActivitiesBlockingUpdate(deserializedRuntimeState, updateMap);
    } 
#endif

    public IAsyncResult BeginLoadRunnableInstance(AsyncCallback callback, object state) => BeginLoadRunnableInstance(ActivityDefaults.LoadTimeout, callback, state);

    public IAsyncResult BeginLoadRunnableInstance(TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfReadOnly(); // only allow a single Load() or Run()

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (InstanceStore == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        }
        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (_persistenceManager != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
        }

        PersistenceManager newManager = new(InstanceStore, GetInstanceMetadata());
        if (!newManager.IsInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.TryLoadRequiresOwner));
        }

        return new LoadAsyncResult(this, newManager, true, timeout, callback, state);
    }

    public IAsyncResult BeginLoad(Guid instanceId, AsyncCallback callback, object state) => BeginLoad(instanceId, ActivityDefaults.LoadTimeout, callback, state);

    public IAsyncResult BeginLoad(Guid instanceId, TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instanceId == Guid.Empty)
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(instanceId));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (InstanceStore == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadingWorkflowApplicationRequiresInstanceStore));
        }
        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }

        PersistenceManager newManager = new(InstanceStore, GetInstanceMetadata(), instanceId);

        return new LoadAsyncResult(this, newManager, false, timeout, callback, state);
    }

#if DYNAMICUPDATE
    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, AsyncCallback callback, object state)
    {
        return BeginLoad(instance, null, ActivityDefaults.LoadTimeout, callback, state);
    }

    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, TimeSpan timeout,
        AsyncCallback callback, object state)
    {
        return BeginLoad(instance, null, timeout, callback, state);
    }

    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap,
        AsyncCallback callback, object state)
    {
        return BeginLoad(instance, updateMap, ActivityDefaults.LoadTimeout, callback, state);
    }

    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, DynamicUpdateMap updateMap, TimeSpan timeout,
        AsyncCallback callback, object state)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instance == null)
        {
            throw FxTrace.Exception.ArgumentNull("instance");
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (this.instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (this.initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (this.InstanceStore != null && this.InstanceStore != instance.InstanceStore)
        {
            throw FxTrace.Exception.Argument("instance", SR.InstanceStoreDoesntMatchWorkflowApplication);
        }

        instance.MarkAsLoaded();
        PersistenceManager newManager = (PersistenceManager)instance.PersistenceManager;
        newManager.SetInstanceMetadata(GetInstanceMetadata());

        return new LoadAsyncResult(this, newManager, instance.Values, updateMap, timeout, callback, state);
    } 
#else

    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, AsyncCallback callback, object state) => BeginLoad(instance, ActivityDefaults.LoadTimeout, callback, state);

    public IAsyncResult BeginLoad(WorkflowApplicationInstance instance, TimeSpan timeout,
        AsyncCallback callback, object state)
    {
        ThrowIfAborted();
        ThrowIfReadOnly(); // only allow a single Load() or Run()
        if (instance == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(instance));
        }

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        if (_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
        }
        if (_initialWorkflowArguments != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotUseInputsWithLoad));
        }
        if (InstanceStore != null && InstanceStore != instance.InstanceStore)
        {
            throw FxTrace.Exception.Argument(nameof(instance), SR.InstanceStoreDoesntMatchWorkflowApplication);
        }

        instance.MarkAsLoaded();
        PersistenceManager newManager = (PersistenceManager)instance.PersistenceManager;
        newManager.SetInstanceMetadata(GetInstanceMetadata());

        return new LoadAsyncResult(this, newManager, instance.Values, timeout, callback, state); 
    }
#endif

#pragma warning disable CA1822 // Mark members as static
    public void EndLoad(IAsyncResult result) => LoadAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

#pragma warning disable CA1822 // Mark members as static
    public void EndLoadRunnableInstance(IAsyncResult result) => LoadAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    protected override void OnNotifyUnhandledException(Exception exception, Activity exceptionSource, string exceptionSourceInstanceId)
    {
        bool done = true;

        try
        {
            Exception abortException = null;

            try
            {
                unhandledExceptionHandler ??= new UnhandledExceptionEventHandler();
                done = unhandledExceptionHandler.Run(this, exception, exceptionSource, exceptionSourceInstanceId);
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
        => new UnloadOrPersistAsyncResult(this, timeout, operation, true, isInternalPersist, callback, state);

#pragma warning disable CA1822 // Mark members as static
    private void EndInternalPersist(IAsyncResult result) => UnloadOrPersistAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    private void TrackPersistence(PersistenceOperation operation)
    {
        if (Controller.TrackingEnabled)
        {
            if (operation == PersistenceOperation.Complete)
            {
                Controller.Track(new WorkflowInstanceRecord(Id, WorkflowDefinition.DisplayName, WorkflowInstanceStates.Deleted, DefinitionIdentity));
            }
            else if (operation == PersistenceOperation.Unload)
            {
                Controller.Track(new WorkflowInstanceRecord(Id, WorkflowDefinition.DisplayName, WorkflowInstanceStates.Unloaded, DefinitionIdentity));
            }
            else
            {
                Controller.Track(new WorkflowInstanceRecord(Id, WorkflowDefinition.DisplayName, WorkflowInstanceStates.Persisted, DefinitionIdentity));
            }
        }
    }

    private void PersistCore(ref TimeoutHelper timeoutHelper, PersistenceOperation operation)
    {
        if (HasPersistenceProvider)
        {
            if (!_persistenceManager.IsInitialized)
            {
                _persistenceManager.Initialize(DefinitionIdentity, timeoutHelper.RemainingTime());
            }
            if (!_persistenceManager.IsLocked && Transaction.Current != null)
            {
                _persistenceManager.EnsureReadyness(timeoutHelper.RemainingTime());
            }

            // Do the tracking before preparing in case the tracking data is being pushed into
            // an extension and persisted transactionally with the instance state.
            TrackPersistence(operation);

            Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
        }

        bool success = false;
        WorkflowPersistenceContext context = null;
        TransactionScope scope = null;

        try
        {
            IDictionary<XName, InstanceValue> data = null;
            PersistencePipeline pipeline = null;
            if (HasPersistenceModule)
            {
                IEnumerable<IPersistencePipelineModule> modules = GetExtensions<IPersistencePipelineModule>();
                pipeline = new PersistencePipeline(modules, PersistenceManager.GenerateInitialData(this));
                pipeline.Collect();
                pipeline.Map();
                data = pipeline.Values;
            }

            if (HasPersistenceProvider)
            {
                data ??= PersistenceManager.GenerateInitialData(this);

                if (context == null)
                {
                    Fx.Assert(scope == null, "Should not have been able to set up a scope.");
                    InitializePersistenceContext(pipeline != null && pipeline.IsSaveTransactionRequired, timeoutHelper, out context, out scope);
                }

                _persistenceManager.Save(data, operation, timeoutHelper.RemainingTime());
            }

            if (pipeline != null)
            {
                if (context == null)
                {
                    Fx.Assert(scope == null, "Should not have been able to set up a scope if we had no context.");
                    InitializePersistenceContext(pipeline.IsSaveTransactionRequired, timeoutHelper, out context, out scope);
                }

                try
                {
                    _persistencePipelineInUse = pipeline;

                    // Need to ensure that either we see the Aborted state, AbortInstance sees us, or both.
                    Thread.MemoryBarrier();

                    if (_state == WorkflowApplicationState.Aborted)
                    {
                        throw FxTrace.Exception.AsError(new OperationCanceledException(SR.DefaultAbortReason));
                    }

                    PersistencePipeline.EndSave(pipeline.BeginSave(timeoutHelper.RemainingTime(), null, null));
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
            CompletePersistenceContext(context, scope, success);

            if (success)
            {
                if (operation != PersistenceOperation.Save)
                {
                    // Stop execution if we've given up the instance lock
                    _state = WorkflowApplicationState.Paused;

                    if (TD.WorkflowApplicationUnloadedIsEnabled())
                    {
                        TD.WorkflowApplicationUnloaded(Id.ToString());
                    }
                }
                else
                {
                    if (TD.WorkflowApplicationPersistedIsEnabled())
                    {
                        TD.WorkflowApplicationPersisted(Id.ToString());
                    }
                }

                if (operation == PersistenceOperation.Complete || operation == PersistenceOperation.Unload)
                {
                    // We did a Delete or Unload, so if we have a persistence provider, tell it to delete the owner.
                    if (HasPersistenceProvider && _persistenceManager.OwnerWasCreated)
                    {
                        // This will happen to be under the caller's transaction, if there is one.
                        // TODO, 124600, suppress the transaction
                        _persistenceManager.DeleteOwner(timeoutHelper.RemainingTime());
                    }

                    MarkUnloaded();
                }
            }
        }
    }

    [Fx.Tag.InheritThrows(From = "Unload")]
    public void Persist() => Persist(ActivityDefaults.SaveTimeout);

    [Fx.Tag.InheritThrows(From = "Unload")]
    public void Persist(TimeSpan timeout)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        TimeoutHelper timeoutHelper = new(timeout);

        RequiresPersistenceOperation operation = new();

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
    public IAsyncResult BeginPersist(AsyncCallback callback, object state) => BeginPersist(ActivityDefaults.SaveTimeout, callback, state);

    [Fx.Tag.InheritThrows(From = "Unload")]
    public IAsyncResult BeginPersist(TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return new UnloadOrPersistAsyncResult(this, timeout, PersistenceOperation.Save, false, false, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "Unload")]
#pragma warning disable CA1822 // Mark members as static
    public void EndPersist(IAsyncResult result) => UnloadOrPersistAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    // called from WorkflowApplicationIdleEventArgs
    internal ReadOnlyCollection<BookmarkInfo> GetBookmarksForIdle() => Controller.GetBookmarks();

    public ReadOnlyCollection<BookmarkInfo> GetBookmarks() => GetBookmarks(ActivityDefaults.ResumeBookmarkTimeout);

    public ReadOnlyCollection<BookmarkInfo> GetBookmarks(TimeSpan timeout)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        InstanceOperation operation = new();

        try
        {
            WaitForTurn(operation, timeout);

            ValidateStateForGetAllBookmarks();

            return Controller.GetBookmarks();
        }
        finally
        {
            NotifyOperationComplete(operation);
        }
    }

    protected internal override IAsyncResult OnBeginPersist(AsyncCallback callback, object state)
        => BeginInternalPersist(PersistenceOperation.Save, ActivityDefaults.InternalSaveTimeout, true, callback, state);

    protected internal override void OnEndPersist(IAsyncResult result) => EndInternalPersist(result);

    protected internal override IAsyncResult OnBeginAssociateKeys(ICollection<InstanceKey> keys, AsyncCallback callback, object state)
        => throw Fx.AssertAndThrow($"WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call {nameof(OnBeginAssociateKeys)}.");

    protected internal override void OnEndAssociateKeys(IAsyncResult result)
        => throw Fx.AssertAndThrow($"WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call {nameof(OnEndAssociateKeys)}.");

    protected internal override void OnDisassociateKeys(ICollection<InstanceKey> keys)
        => throw Fx.AssertAndThrow($"WorkflowApplication is sealed with CanUseKeys as false, so WorkflowInstance should not call {nameof(OnDisassociateKeys)}.");

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
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(bookmarkName));
        }

        return ResumeBookmark(new Bookmark(bookmarkName), value);
    }

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
    public BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value) => ResumeBookmark(bookmark, value, ActivityDefaults.ResumeBookmarkTimeout);

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
    public BookmarkResumptionResult ResumeBookmark(string bookmarkName, object value, TimeSpan timeout)
    {
        if (string.IsNullOrEmpty(bookmarkName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(bookmarkName));
        }

        return ResumeBookmark(new Bookmark(bookmarkName), value, timeout);
    }

    [Fx.Tag.InheritThrows(From = "BeginResumeBookmark", FromDeclaringType = typeof(WorkflowInstance))]
    public BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);
        ThrowIfHandlerThread();
        TimeoutHelper timeoutHelper = new(timeout);

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
                        Controller.FlushTrackingRecords(timeoutHelper.RemainingTime());
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
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(bookmarkName));
        }

        return BeginResumeBookmark(new Bookmark(bookmarkName), value, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
    public IAsyncResult BeginResumeBookmark(string bookmarkName, object value, TimeSpan timeout, AsyncCallback callback, object state)
    {
        if (string.IsNullOrEmpty(bookmarkName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(bookmarkName));
        }

        return BeginResumeBookmark(new Bookmark(bookmarkName), value, timeout, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
    public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        => BeginResumeBookmark(bookmark, value, ActivityDefaults.ResumeBookmarkTimeout, callback, state);

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
    public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);
        ThrowIfHandlerThread();

        return new ResumeBookmarkAsyncResult(this, bookmark, value, timeout, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "ResumeBookmark")]
#pragma warning disable CA1822 // Mark members as static
    public BookmarkResumptionResult EndResumeBookmark(IAsyncResult result) => ResumeBookmarkAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    protected internal override IAsyncResult OnBeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfHandlerThread();
        return new ResumeBookmarkAsyncResult(this, bookmark, value, true, timeout, callback, state);
    }

    protected internal override BookmarkResumptionResult OnEndResumeBookmark(IAsyncResult result) => ResumeBookmarkAsyncResult.End(result);

    private BookmarkResumptionResult ResumeBookmarkCore(Bookmark bookmark, object value)
    {
        BookmarkResumptionResult result = Controller.ScheduleBookmarkResumption(bookmark, value);

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
            TD.WorkflowApplicationIdled(Id.ToString());
        }

        Exception abortException = null;

        try
        {
            Action<WorkflowApplicationIdleEventArgs> idleHandler = Idle;

            if (idleHandler != null)
            {
                _handlerThreadId = Environment.CurrentManagedThreadId;
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
        if (Controller.State != WorkflowInstanceState.Complete)
        {
            Controller.Abort();
        }
        else
        {
            DisposeExtensions();
        }

        Exception abortException = null;

        try
        {
            Action<WorkflowApplicationEventArgs> handler = Unloaded;

            if (handler != null)
            {
                _handlerThreadId = Environment.CurrentManagedThreadId;
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
    public void Unload() => Unload(ActivityDefaults.SaveTimeout);

    [Fx.Tag.InheritThrows(From = "Unload")]
    public void Unload(TimeSpan timeout)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        TimeoutHelper timeoutHelper = new(timeout);

        RequiresPersistenceOperation operation = new();

        try
        {
            WaitForTurn(operation, timeoutHelper.RemainingTime());

            ValidateStateForUnload();
            if (_state != WorkflowApplicationState.Unloaded) // Unload on unload is a no-op
            {
                PersistenceOperation persistenceOperation;

                if (Controller.State == WorkflowInstanceState.Complete)
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
    public IAsyncResult BeginUnload(AsyncCallback callback, object state) => BeginUnload(ActivityDefaults.SaveTimeout, callback, state);

    [Fx.Tag.InheritThrows(From = "Unload")]
    public IAsyncResult BeginUnload(TimeSpan timeout, AsyncCallback callback, object state)
    {
        ThrowIfHandlerThread();

        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return new UnloadOrPersistAsyncResult(this, timeout, PersistenceOperation.Unload, false, false, callback, state);
    }

    [Fx.Tag.InheritThrows(From = "Unload")]
#pragma warning disable CA1822 // Mark members as static
    public void EndUnload(IAsyncResult result) => UnloadOrPersistAsyncResult.End(result);
#pragma warning restore CA1822 // Mark members as static

    private IDictionary<XName, InstanceValue> GetInstanceMetadata()
    {
        if (DefinitionIdentity != null)
        {
            _instanceMetadata ??= new Dictionary<XName, InstanceValue>(2);
            _ = _instanceMetadata.TryAdd(WorkflowNamespace.WorkflowHostType, new InstanceValue(Workflow45Namespace.WorkflowApplication));
            _instanceMetadata[Workflow45Namespace.DefinitionIdentity] =
                new InstanceValue(DefinitionIdentity, InstanceValueOptions.Optional);
        }
        return _instanceMetadata;
    }

    private void UpdateInstanceMetadata()
    {
        // Update the metadata to reflect the new identity after a Dynamic Update
        _persistenceManager.SetMutablemetadata(new Dictionary<XName, InstanceValue>
        {
            { Workflow45Namespace.DefinitionIdentity, new InstanceValue(DefinitionIdentity, InstanceValueOptions.Optional) }
        });
    }

    private static void ThrowIfMulticast(Delegate value)
    {
        if (value != null && value.GetInvocationList().Length > 1)
        {
            throw FxTrace.Exception.Argument(nameof(value), SR.OnlySingleCastDelegatesAllowed);
        }
    }

    private void ThrowIfAborted()
    {
        if (_state == WorkflowApplicationState.Aborted)
        {
            throw FxTrace.Exception.AsError(new WorkflowApplicationAbortedException(SR.WorkflowApplicationAborted(Id), Id));
        }
    }

    private void ThrowIfTerminatedOrCompleted()
    {
        if (_hasRaisedCompleted)
        {
            Controller.GetCompletionState(out Exception completionException);
            if (completionException != null)
            {
                throw FxTrace.Exception.AsError(new WorkflowApplicationTerminatedException(SR.WorkflowApplicationTerminated(Id), Id, completionException));
            }
            else
            {
                throw FxTrace.Exception.AsError(new WorkflowApplicationCompletedException(SR.WorkflowApplicationCompleted(Id), Id));
            }
        }
    }

    private void ThrowIfUnloaded()
    {
        if (_state == WorkflowApplicationState.Unloaded)
        {
            throw FxTrace.Exception.AsError(new WorkflowApplicationUnloadedException(SR.WorkflowApplicationUnloaded(Id), Id));
        }
    }

    private void ThrowIfNoInstanceStore()
    {
        if (!HasPersistenceProvider)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InstanceStoreRequiredToPersist));
        }
    }

    private void ThrowIfHandlerThread()
    {
        if (IsHandlerThread)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationFromHandlerThread));
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
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowApplicationAlreadyHasId));
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
        if (Controller.State != WorkflowInstanceState.Complete)
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

    private class WorkflowEventData
    {
        public WorkflowEventData(WorkflowApplication instance)
        {
            Instance = instance;
        }

        public WorkflowApplication Instance { get; private set; }

        public Func<IAsyncResult, WorkflowApplication, bool, bool> NextCallback { get; set; }

        public Exception UnhandledException { get; set; }

        public Activity UnhandledExceptionSource { get; set; }

        public string UnhandledExceptionSourceInstance { get; set; }
    }

    private class WaitForTurnData
    {
        public WaitForTurnData(Action<object, TimeoutException> callback, object state, InstanceOperation operation, WorkflowApplication instance)
        {
            Callback = callback;
            State = state;
            Operation = operation;
            Instance = instance;
        }

        public Action<object, TimeoutException> Callback { get; private set; }

        public object State { get; private set; }

        public InstanceOperation Operation { get; private set; }

        public WorkflowApplication Instance { get; private set; }
    }
}

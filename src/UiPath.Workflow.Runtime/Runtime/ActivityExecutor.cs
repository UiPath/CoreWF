// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;
using System.Activities.Runtime.DurableInstancing;
using System.Activities.Tracking;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;
using System.Transactions;

#if NET45
using System.Activities.Debugger;
#endif

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate; 
#endif

namespace System.Activities.Runtime;


[DataContract(Name = XD.Executor.Name, Namespace = XD.Runtime.Namespace)]
internal partial class ActivityExecutor : IEnlistmentNotification
{
    private static ReadOnlyCollection<BookmarkInfo> s_emptyBookmarkInfoCollection;

    private BookmarkManager _bookmarkManager;

    private BookmarkScopeManager _bookmarkScopeManager;

#if NET45
    private DebugController _debugController;
    private readonly bool _hasRaisedWorkflowStarted;
#endif

    private Guid _instanceId;
    private bool _instanceIdSet;

    private Activity _rootElement;
    private Dictionary<ActivityInstance, AsyncOperationContext> _activeOperations;
    private WorkflowInstance _host;

    private ActivityInstanceMap _instanceMap;
    private MappableObjectManager _mappableObjectManager;

    private bool _hasTrackedStarted;

    private long _nextTrackingRecordNumber;

    private ActivityInstance _rootInstance;
    private List<ActivityInstance> _executingSecondaryRootInstances;

    private Scheduler _scheduler;

    private Exception _completionException;

    private bool _shouldRaiseMainBodyComplete;

    private long _lastInstanceId;

    private LocationEnvironment _rootEnvironment;

    private IDictionary<string, object> _workflowOutputs;

    private Bookmark _mainRootCompleteBookmark;

    // This field reflects our best guess at our future completion state.
    // We set it when the main root completes but might revise the value
    // depending on what actions are taken (like CancelRootActivity being
    // called).
    private ActivityInstanceState _executionState;

    private Queue<PersistenceWaiter> _persistenceWaiters;

    private Quack<TransactionContextWaiter> _transactionContextWaiters;
    private RuntimeTransactionData _runtimeTransaction;

    private bool _isAbortPending;
    private bool _isDisposed;
    private bool _shouldPauseOnCanPersist;

    private bool _isTerminatePending;
    private Exception _terminationPendingException;

    private int _noPersistCount;

    private SymbolResolver _symbolResolver;

    private bool _throwDuringSerialization;

    private CodeActivityContext _cachedResolutionContext;
    private Location _ignorableResultLocation;

    // work item pools (for performance)
    private Pool<EmptyWorkItem> _emptyWorkItemPool;
    private Pool<ExecuteActivityWorkItem> _executeActivityWorkItemPool;
    private Pool<ExecuteSynchronousExpressionWorkItem> _executeSynchronousExpressionWorkItemPool;
    private Pool<CompletionCallbackWrapper.CompletionWorkItem> _completionWorkItemPool;
    private Pool<ResolveNextArgumentWorkItem> _resolveNextArgumentWorkItemPool;

    // context pools (for performance)
    private Pool<CodeActivityContext> _codeActivityContextPool;
    private Pool<NativeActivityContext> _nativeActivityContextPool;

    // root handles (for default Tx, Correlation, etc)
    private ExecutionPropertyManager _rootPropertyManager;

    // This list keeps track of handles that are created and initialized.
    private List<Handle> _handles;

    private bool _persistExceptions;
    private bool _havePersistExceptionsValue;

    internal ActivityExecutor() { }

    public ActivityExecutor(WorkflowInstance host)
    {
        Fx.Assert(host != null, "There must be a host.");

        _host = host;
        WorkflowIdentity = host.DefinitionIdentity;

        _bookmarkManager = new BookmarkManager();
        _scheduler = new Scheduler(new Scheduler.Callbacks(this));
    }

    public Pool<EmptyWorkItem> EmptyWorkItemPool
    {
        get
        {
            _emptyWorkItemPool ??= new PoolOfEmptyWorkItems();
            return _emptyWorkItemPool;
        }
    }

    private Pool<ExecuteActivityWorkItem> ExecuteActivityWorkItemPool
    {
        get
        {
            _executeActivityWorkItemPool ??= new PoolOfExecuteActivityWorkItems();
            return _executeActivityWorkItemPool;
        }
    }

    public Pool<ExecuteSynchronousExpressionWorkItem> ExecuteSynchronousExpressionWorkItemPool
    {
        get
        {
            _executeSynchronousExpressionWorkItemPool ??= new PoolOfExecuteSynchronousExpressionWorkItems();
            return _executeSynchronousExpressionWorkItemPool;
        }
    }

    public Pool<CompletionCallbackWrapper.CompletionWorkItem> CompletionWorkItemPool
    {
        get
        {
            _completionWorkItemPool ??= new PoolOfCompletionWorkItems();
            return _completionWorkItemPool;
        }
    }

    public Pool<CodeActivityContext> CodeActivityContextPool
    {
        get
        {
            _codeActivityContextPool ??= new PoolOfCodeActivityContexts();
            return _codeActivityContextPool;
        }
    }

    public Pool<NativeActivityContext> NativeActivityContextPool
    {
        get
        {
            _nativeActivityContextPool ??= new PoolOfNativeActivityContexts();
            return _nativeActivityContextPool;
        }
    }

    public Pool<ResolveNextArgumentWorkItem> ResolveNextArgumentWorkItemPool
    {
        get
        {
            _resolveNextArgumentWorkItemPool ??= new PoolOfResolveNextArgumentWorkItems();
            return _resolveNextArgumentWorkItemPool;
        }
    }

    public Activity RootActivity => _rootElement;

    public bool IsInitialized => _host != null;

    public bool HasPendingTrackingRecords => _host.HasTrackingParticipant && _host.TrackingProvider.HasPendingRecords;

    public bool ShouldTrack => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrack;

    public bool ShouldTrackBookmarkResumptionRecords => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackBookmarkResumptionRecords;

    public bool ShouldTrackActivityScheduledRecords => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackActivityScheduledRecords;

    public bool ShouldTrackActivityStateRecords => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackActivityStateRecords;

    public bool ShouldTrackActivityStateRecordsExecutingState => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackActivityStateRecordsExecutingState;

    public bool ShouldTrackActivityStateRecordsClosedState => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackActivityStateRecordsClosedState;

    public bool ShouldTrackCancelRequestedRecords => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackCancelRequestedRecords;

    public bool ShouldTrackFaultPropagationRecords => _host.HasTrackingParticipant && _host.TrackingProvider.ShouldTrackFaultPropagationRecords;

    public SymbolResolver SymbolResolver
    {
        get
        {
            if (_symbolResolver == null)
            {
                try
                {
                    _symbolResolver = _host.GetExtension<SymbolResolver>();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    throw FxTrace.Exception.AsError(new CallbackException(SR.CallbackExceptionFromHostGetExtension(WorkflowInstanceId), e));
                }
            }

            return _symbolResolver;
        }
    }

    // This only gets accessed by root activities which are resolving arguments.  Since that
    // could at most be the real root and any secondary roots it doesn't seem necessary
    // to cache the empty environment.
    public LocationEnvironment EmptyEnvironment => new(this, null);

    public ActivityInstanceState State
    {
        get
        {
            if ((_executingSecondaryRootInstances != null && _executingSecondaryRootInstances.Count > 0) ||
                (_rootInstance != null && !_rootInstance.IsCompleted))
            {
                // As long as some root is executing we need to return executing
                return ActivityInstanceState.Executing;
            }
            else
            {
                return _executionState;
            }
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public WorkflowIdentity WorkflowIdentity { get; internal set; }

    [DataMember]
    public Guid WorkflowInstanceId
    {
        get
        {
            if (!_instanceIdSet)
            {
                WorkflowInstanceId = _host.Id;
                if (!_instanceIdSet)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.EmptyIdReturnedFromHost(_host.GetType())));
                }
            }

            return _instanceId;
        }
        // Internal visibility for partial trust serialization purposes only.
        internal set
        {
            _instanceId = value;
            _instanceIdSet = value != Guid.Empty;
        }
    }

    public Exception TerminationException => _completionException;

    public bool IsRunning => !_isDisposed && _scheduler.IsRunning;

    public bool IsPersistable => _noPersistCount == 0;

    public bool IsAbortPending => _isAbortPending;

    public bool IsIdle => _isDisposed || _scheduler.IsIdle;

    public bool IsTerminatePending => _isTerminatePending;

    public bool KeysAllowed => _host.SupportsInstanceKeys;

    public IDictionary<string, object> WorkflowOutputs => _workflowOutputs;

    internal BookmarkScopeManager BookmarkScopeManager
    {
        get
        {
            _bookmarkScopeManager ??= new BookmarkScopeManager();
            return _bookmarkScopeManager;
        }
    }

    internal BookmarkScopeManager RawBookmarkScopeManager => _bookmarkScopeManager;

    internal BookmarkManager RawBookmarkManager => _bookmarkManager;

    internal MappableObjectManager MappableObjectManager
    {
        get
        {
            _mappableObjectManager ??= new MappableObjectManager();
            return _mappableObjectManager;
        }
    }

    public bool RequiresTransactionContextWaiterExists => _transactionContextWaiters != null && _transactionContextWaiters.Count > 0 && _transactionContextWaiters[0].IsRequires;

    public bool HasRuntimeTransaction => _runtimeTransaction != null;

    public Transaction CurrentTransaction => _runtimeTransaction?.ClonedTransaction;

    private static ReadOnlyCollection<BookmarkInfo> EmptyBookmarkInfoCollection
    {
        get
        {
            s_emptyBookmarkInfoCollection ??= new ReadOnlyCollection<BookmarkInfo>(new List<BookmarkInfo>(0));
            return s_emptyBookmarkInfoCollection;
        }
    }

    [DataMember(Name = XD.Executor.BookmarkManager, EmitDefaultValue = false)]
    internal BookmarkManager SerializedBookmarkManager
    {
        get => _bookmarkManager;
        set => _bookmarkManager = value;
    }

    [DataMember(Name = XD.Executor.BookmarkScopeManager, EmitDefaultValue = false)]
    internal BookmarkScopeManager SerializedBookmarkScopeManager
    {
        get => _bookmarkScopeManager;
        set => _bookmarkScopeManager = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "hasTrackedStarted")]
    internal bool SerializedHasTrackedStarted
    {
        get => _hasTrackedStarted;
        set => _hasTrackedStarted = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "nextTrackingRecordNumber")]
    internal long SerializedNextTrackingRecordNumber
    {
        get => _nextTrackingRecordNumber;
        set => _nextTrackingRecordNumber = value;
    }

    [DataMember(Name = XD.Executor.RootInstance, EmitDefaultValue = false)]
    internal ActivityInstance SerializedRootInstance
    {
        get => _rootInstance;
        set => _rootInstance = value;
    }

    [DataMember(Name = XD.Executor.SchedulerMember, EmitDefaultValue = false)]
    internal Scheduler SerializedScheduler
    {
        get => _scheduler;
        set => _scheduler = value;
    }

    [DataMember(Name = XD.Executor.ShouldRaiseMainBodyComplete, EmitDefaultValue = false)]
    internal bool SerializedShouldRaiseMainBodyComplete
    {
        get => _shouldRaiseMainBodyComplete;
        set => _shouldRaiseMainBodyComplete = value;
    }

    [DataMember(Name = XD.Executor.LastInstanceId, EmitDefaultValue = false)]
    internal long SerializedLastInstanceId
    {
        get => _lastInstanceId;
        set => _lastInstanceId = value;
    }

    [DataMember(Name = XD.Executor.RootEnvironment, EmitDefaultValue = false)]
    internal LocationEnvironment SerializedRootEnvironment
    {
        get => _rootEnvironment;
        set => _rootEnvironment = value;
    }

    [DataMember(Name = XD.Executor.WorkflowOutputs, EmitDefaultValue = false)]
    internal IDictionary<string, object> SerializedWorkflowOutputs
    {
        get => _workflowOutputs;
        set => _workflowOutputs = value;
    }

    [DataMember(Name = XD.Executor.MainRootCompleteBookmark, EmitDefaultValue = false)]
    internal Bookmark SerializedMainRootCompleteBookmark
    {
        get => _mainRootCompleteBookmark;
        set => _mainRootCompleteBookmark = value;
    }

    [DataMember(Name = XD.Executor.ExecutionState, EmitDefaultValue = false)]
    internal ActivityInstanceState SerializedExecutionState
    {
        get => _executionState;
        set => _executionState = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "handles")]
    internal List<Handle> SerializedHandles
    {
        get => _handles;
        set => _handles = value;
    }

    internal bool PersistExceptions
    {
        get
        {
            if (!_havePersistExceptionsValue)
            {
                // If we have an ExceptionPersistenceExtension, set our cached "persistExceptions" value to its
                // PersistExceptions property. If we don't have the extension, set the cached value to true.
                ExceptionPersistenceExtension extension = _host.GetExtension<ExceptionPersistenceExtension>();
                _persistExceptions = extension == null || extension.PersistExceptions;
                _havePersistExceptionsValue = true;
            }
            return _persistExceptions;
        }
    }

    [DataMember(Name = XD.Executor.CompletionException, EmitDefaultValue = false)]
    internal Exception SerializedCompletionException
    {
        get => PersistExceptions ? _completionException : null;
        set => _completionException = value;
    }

    [DataMember(Name = XD.Executor.TransactionContextWaiters, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Used by serialization")]
    internal TransactionContextWaiter[] SerializedTransactionContextWaiters
    {
        get => _transactionContextWaiters != null && _transactionContextWaiters.Count > 0 ? _transactionContextWaiters.ToArray() : null;
        set
        {
            Fx.Assert(value != null, "We don't serialize out null.");
            _transactionContextWaiters = new Quack<TransactionContextWaiter>(value);
        }
    }

    [DataMember(Name = XD.Executor.PersistenceWaiters, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Used by serialization")]
    internal Queue<PersistenceWaiter> SerializedPersistenceWaiters
    {
        get => _persistenceWaiters == null || _persistenceWaiters.Count == 0 ? null : _persistenceWaiters;
        set
        {
            Fx.Assert(value != null, "We don't serialize out null.");
            _persistenceWaiters = value;
        }
    }

    [DataMember(Name = XD.Executor.SecondaryRootInstances, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Used by serialization")]
    internal List<ActivityInstance> SerializedExecutingSecondaryRootInstances
    {
        get => _executingSecondaryRootInstances != null && _executingSecondaryRootInstances.Count > 0
                ? _executingSecondaryRootInstances
                : null;
        set
        {
            Fx.Assert(value != null, "We don't serialize out null.");
            _executingSecondaryRootInstances = value;
        }
    }

    [DataMember(Name = XD.Executor.MappableObjectManager, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Used by serialization")]
    internal MappableObjectManager SerializedMappableObjectManager
    {
        get => _mappableObjectManager == null || _mappableObjectManager.Count == 0 ? null : _mappableObjectManager;
        set
        {
            Fx.Assert(value != null, "value from serialization should never be null");
            _mappableObjectManager = value;
        }
    }

    // map from activity names to (active) associated activity instances
    [DataMember(Name = XD.Executor.ActivityInstanceMap, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "called from serialization")]
    internal ActivityInstanceMap SerializedProgramMapping
    {
        get
        {
            ThrowIfNonSerializable();

            if (_instanceMap == null && !_isDisposed)
            {
                _instanceMap = new ActivityInstanceMap();

                _rootInstance.FillInstanceMap(_instanceMap);
                _scheduler.FillInstanceMap(_instanceMap);

                if (_executingSecondaryRootInstances != null && _executingSecondaryRootInstances.Count > 0)
                {
                    foreach (ActivityInstance secondaryRoot in _executingSecondaryRootInstances)
                    {
                        secondaryRoot.FillInstanceMap(_instanceMap);

                        LocationEnvironment environment = secondaryRoot.Environment;

                        if (secondaryRoot.IsEnvironmentOwner)
                        {
                            environment = environment.Parent;
                        }

                        while (environment != null)
                        {
                            if (environment.HasOwnerCompleted)
                            {
                                _instanceMap.AddEntry(environment, true);
                            }

                            environment = environment.Parent;
                        }
                    }
                }
            }

            return _instanceMap;
        }

        set
        {
            Fx.Assert(value != null, "value from serialization should never be null");
            _instanceMap = value;
        }
    }

    // may be null
    internal ExecutionPropertyManager RootPropertyManager => _rootPropertyManager;

    [DataMember(Name = XD.ActivityInstance.PropertyManager, EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
    internal ExecutionPropertyManager SerializedPropertyManager
    {
        get => _rootPropertyManager;
        set
        {
            Fx.Assert(value != null, "We don't emit the default value so this should never be null.");
            _rootPropertyManager = value;
            _rootPropertyManager.OnDeserialized(null, null, null, this);
        }
    }

    public void ThrowIfNonSerializable()
    {
        if (_throwDuringSerialization)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.StateCannotBeSerialized(WorkflowInstanceId)));
        }
    }

    public void MakeNonSerializable() => _throwDuringSerialization = true;

#if DYNAMICUPDATE
    public IList<ActivityBlockingUpdate> GetActivitiesBlockingUpdate(DynamicUpdateMap updateMap)
    {
        Fx.Assert(updateMap != null, "UpdateMap must not be null.");
        Collection<ActivityBlockingUpdate> result = null;
        _instanceMap.GetActivitiesBlockingUpdate(updateMap, _executingSecondaryRootInstances, ref result);
        return result;
    }

    public void UpdateInstancePhase1(DynamicUpdateMap updateMap, Activity targetDefinition, ref Collection<ActivityBlockingUpdate> updateErrors)
    {
        Fx.Assert(updateMap != null, "UpdateMap must not be null.");
        _instanceMap.UpdateRawInstance(updateMap, targetDefinition, _executingSecondaryRootInstances, ref updateErrors);
    }

    public void UpdateInstancePhase2(DynamicUpdateMap updateMap, ref Collection<ActivityBlockingUpdate> updateErrors)
    {
        _instanceMap.UpdateInstanceByActivityParticipation(this, updateMap, ref updateErrors);
    } 
#endif

    internal List<Handle> Handles => _handles;

    // evaluate an argument/variable expression using fast-path optimizations
    public void ExecuteInResolutionContextUntyped(ActivityInstance parentInstance, ActivityWithResult expressionActivity, long instanceId, Location resultLocation)
    {
        _cachedResolutionContext ??= new CodeActivityContext(parentInstance, this);
        _cachedResolutionContext.Reinitialize(parentInstance, this, expressionActivity, instanceId);
        try
        {
            _ignorableResultLocation = resultLocation;
            resultLocation.Value = expressionActivity.InternalExecuteInResolutionContextUntyped(_cachedResolutionContext);
        }
        finally
        {
            if (!expressionActivity.UseOldFastPath)
            {
                // The old fast path allows WorkflowDataContexts to escape up one level, because
                // the resolution context uses the parent's ActivityInstance. We support that for
                // back-compat, but don't allow it on new fast-path activities.
                _cachedResolutionContext.DisposeDataContext();
            }

            _cachedResolutionContext.Dispose();
            _ignorableResultLocation = null;
        }
    }

    // evaluate an argument/variable expression using fast-path optimizations
    public T ExecuteInResolutionContext<T>(ActivityInstance parentInstance, Activity<T> expressionActivity)
    {
        Fx.Assert(expressionActivity.UseOldFastPath, "New fast path should be scheduled via ExecuteSynchronousExpressionWorkItem, which calls the Untyped overload");

        _cachedResolutionContext ??= new CodeActivityContext(parentInstance, this);
        _cachedResolutionContext.Reinitialize(parentInstance, this, expressionActivity, parentInstance.InternalId);
        T result;
        try
        {
            result = expressionActivity.InternalExecuteInResolutionContext(_cachedResolutionContext);
        }
        finally
        {
            _cachedResolutionContext.Dispose();
        }
        return result;
    }

    internal void ExecuteSynchronousWorkItem(WorkItem workItem)
    {
        workItem.Release(this);
        try
        {
            bool result = workItem.Execute(this, _bookmarkManager);
            Fx.AssertAndThrow(result, "Synchronous work item should not yield the scheduler");
        }
        finally
        {
            workItem.Dispose(this);
        }
    }

    internal void ExitNoPersistForExceptionPropagation()
    {
        if (!PersistExceptions)
        {
            ExitNoPersist();
        }
    }

    // This is called by RuntimeArgument.GetLocation (via ActivityContext.GetIgnorableResultLocation)
    // when the user tries to access the Result argument on an activity being run with SkipArgumentResolution.
    internal Location GetIgnorableResultLocation(RuntimeArgument resultArgument)
    {
        Fx.Assert(resultArgument.Owner == _cachedResolutionContext.Activity, "GetIgnorableResultLocation should only be called for activity in resolution context");
        Fx.Assert(_ignorableResultLocation != null, "ResultLocation should have been passed in to ExecuteInResolutionContext");

        return _ignorableResultLocation;
    }

#if NET45
    // Whether it is being debugged.
    bool IsDebugged()
    {
        if (_debugController == null)
        {
#if DEBUG
            if (Fx.StealthDebugger)
            {
                return false;
            }
#endif
            if (System.Diagnostics.Debugger.IsAttached)
            {
                _debugController = new DebugController(_host);
            }
        }
        return _debugController != null;
    }

    public void DebugActivityCompleted(ActivityInstance instance)
    {
        if (_debugController != null)   // Don't use IsDebugged() for perf reason.
        {
            _debugController.ActivityCompleted(instance);
        }
    }
#endif

    public void AddTrackingRecord(TrackingRecord record)
    {
        Fx.Assert(_host.TrackingProvider != null, "We should only add records if we have a tracking provider.");

        _host.TrackingProvider.AddRecord(record);
    }

    public bool ShouldTrackActivity(string name)
    {
        Fx.Assert(_host.TrackingProvider != null, "We should only add records if we have a tracking provider.");
        return _host.TrackingProvider.ShouldTrackActivity(name);
    }

    public IAsyncResult BeginTrackPendingRecords(AsyncCallback callback, object state)
    {
        Fx.Assert(_host.TrackingProvider != null, "We should only try to track if we have a tracking provider.");
        return _host.BeginFlushTrackingRecordsInternal(callback, state);
    }

    public void EndTrackPendingRecords(IAsyncResult result)
    {
        Fx.Assert(_host.TrackingProvider != null, "We should only try to track if we have a tracking provider.");
        _host.EndFlushTrackingRecordsInternal(result);
    }

    internal IDictionary<string, LocationInfo> GatherMappableVariables()
    {
        if (_mappableObjectManager != null)
        {
            return MappableObjectManager.GatherMappableVariables();
        }
        return null;
    }

#pragma warning disable CA1822 // Mark members as static
    internal void OnSchedulerThreadAcquired()
#pragma warning restore CA1822 // Mark members as static
    {
#if NET45
        if (this.IsDebugged() && !_hasRaisedWorkflowStarted)
        {
            _hasRaisedWorkflowStarted = true;
            _debugController.WorkflowStarted();
        }
#endif
    }

    public void Dispose() => Dispose(true);

    private void Dispose(bool aborting)
    {
        if (!_isDisposed)
        {
#if NET45
            if (_debugController != null)   // Don't use IsDebugged() because it may create debugController unnecessarily.
            {
                _debugController.WorkflowCompleted();
                _debugController = null;
            }
#endif

            if (_activeOperations != null && _activeOperations.Count > 0)
            {
                Fx.Assert(aborting, "shouldn't get here in the graceful close case");
                Abort(new OperationCanceledException());
            }
            else
            {
                _scheduler.ClearAllWorkItems(this);

                if (!aborting)
                {
                    _scheduler = null;
                    _bookmarkManager = null;
                    _lastInstanceId = 0;
                    _rootInstance = null;
                }

                _isDisposed = true;
            }
        }
    }

    // Called from an arbitrary thread
    public void PauseWhenPersistable() => _shouldPauseOnCanPersist = true;

    public void EnterNoPersist()
    {
        _noPersistCount++;

        if (TD.EnterNoPersistBlockIsEnabled())
        {
            TD.EnterNoPersistBlock();
        }
    }

    public void ExitNoPersist()
    {
        _noPersistCount--;

        if (TD.ExitNoPersistBlockIsEnabled())
        {
            TD.ExitNoPersistBlock();
        }

        if (_shouldPauseOnCanPersist && IsPersistable)
        {
            // shouldPauseOnCanPersist is reset at the next pause
            // notification
            _scheduler.Pause();
        }
    }

    void IEnlistmentNotification.Commit(Enlistment enlistment)
    {
        // Because of ordering we might get this notification after we've already
        // determined the outcome

        // Get a local copy of _runtimeTransaction because it is possible for
        // _runtimeTransaction to be nulled out between the time we check for null
        // and the time we try to lock it.
        RuntimeTransactionData localRuntimeTransaction = _runtimeTransaction;

        if (localRuntimeTransaction != null)
        {
            AsyncWaitHandle completionEvent = null;

            lock (localRuntimeTransaction)
            {
                completionEvent = localRuntimeTransaction.CompletionEvent;

                localRuntimeTransaction.TransactionStatus = TransactionStatus.Committed;
            }

            enlistment.Done();

            if (completionEvent != null)
            {
                completionEvent.Set();
            }
        }
        else
        {
            enlistment.Done();
        }
    }

    void IEnlistmentNotification.InDoubt(Enlistment enlistment) => ((IEnlistmentNotification)this).Rollback(enlistment);

    //Note - There is a scenario in the TransactedReceiveScope while dealing with server side WCF dispatcher created transactions, 
    //the activity instance will end up calling BeginCommit before finishing up its execution. By this we allow the executing TransactedReceiveScope activity to 
    //complete and the executor is "free" to respond to this Prepare notification as part of the commit processing of that server side transaction
    void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
    {
        // Because of ordering we might get this notification after we've already
        // determined the outcome

        // Get a local copy of _runtimeTransaction because it is possible for
        // _runtimeTransaction to be nulled out between the time we check for null
        // and the time we try to lock it.
        RuntimeTransactionData localRuntimeTransaction = _runtimeTransaction;

        if (localRuntimeTransaction != null)
        {
            bool callPrepared = false;

            lock (localRuntimeTransaction)
            {
                if (localRuntimeTransaction.HasPrepared)
                {
                    callPrepared = true;
                }
                else
                {
                    localRuntimeTransaction.PendingPreparingEnlistment = preparingEnlistment;
                }
            }

            if (callPrepared)
            {
                preparingEnlistment.Prepared();
            }
        }
        else
        {
            preparingEnlistment.Prepared();
        }
    }

    void IEnlistmentNotification.Rollback(Enlistment enlistment)
    {
        // Because of ordering we might get this notification after we've already
        // determined the outcome

        // Get a local copy of _runtimeTransaction because it is possible for
        // _runtimeTransaction to be nulled out between the time we check for null
        // and the time we try to lock it.
        RuntimeTransactionData localRuntimeTransaction = _runtimeTransaction;

        if (localRuntimeTransaction != null)
        {
            AsyncWaitHandle completionEvent = null;

            lock (localRuntimeTransaction)
            {
                completionEvent = localRuntimeTransaction.CompletionEvent;

                localRuntimeTransaction.TransactionStatus = TransactionStatus.Aborted;
            }

            enlistment.Done();

            if (completionEvent != null)
            {
                completionEvent.Set();
            }
        }
        else
        {
            enlistment.Done();
        }
    }

    public void RequestTransactionContext(ActivityInstance instance, bool isRequires, RuntimeTransactionHandle handle, Action<NativeActivityTransactionContext, object> callback, object state)
    {
        if (isRequires)
        {
            EnterNoPersist();
        }

        _transactionContextWaiters ??= new Quack<TransactionContextWaiter>();
        TransactionContextWaiter waiter = new(instance, isRequires, handle, new TransactionContextWaiterCallbackWrapper(callback, instance), state);

        if (isRequires)
        {
            Fx.Assert(_transactionContextWaiters.Count == 0 || !_transactionContextWaiters[0].IsRequires, "Either we don't have any waiters or the first one better not be IsRequires == true");

            _transactionContextWaiters.PushFront(waiter);
        }
        else
        {
            _transactionContextWaiters.Enqueue(waiter);
        }

        instance.IncrementBusyCount();
        instance.WaitingForTransactionContext = true;
    }

    public void SetTransaction(RuntimeTransactionHandle handle, Transaction transaction, ActivityInstance isolationScope, ActivityInstance transactionOwner)
    {
        _runtimeTransaction = new RuntimeTransactionData(handle, transaction, isolationScope);
        EnterNoPersist();

        // no more work to do for a host-declared transaction
        if (transactionOwner == null)
        {
            return;
        }

        Exception abortException = null;

        try
        {
            transaction.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
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
            AbortWorkflowInstance(abortException);
        }
        else
        {
            if (TD.RuntimeTransactionSetIsEnabled())
            {
                Fx.Assert(transactionOwner != null, "isolationScope and transactionOwner are either both null or both non-null");
                TD.RuntimeTransactionSet(transactionOwner.Activity.GetType().ToString(), transactionOwner.Activity.DisplayName, transactionOwner.Id, isolationScope.Activity.GetType().ToString(), isolationScope.Activity.DisplayName, isolationScope.Id);
            }
        }
    }

    public void CompleteTransaction(RuntimeTransactionHandle handle, BookmarkCallback callback, ActivityInstance callbackOwner)
    {
        if (callback != null)
        {
            Bookmark bookmark = _bookmarkManager.CreateBookmark(callback, callbackOwner, BookmarkOptions.None);

            ActivityInstance isolationScope = null;

            if (_runtimeTransaction != null)
            {
                isolationScope = _runtimeTransaction.IsolationScope;
            }

            _bookmarkManager.TryGenerateWorkItem(this, false, ref bookmark, null, isolationScope, out ActivityExecutionWorkItem workItem);
            _scheduler.EnqueueWork(workItem);
        }

        if (_runtimeTransaction != null && _runtimeTransaction.TransactionHandle == handle)
        {
            _runtimeTransaction.ShouldScheduleCompletion = true;

            if (TD.RuntimeTransactionCompletionRequestedIsEnabled())
            {
                TD.RuntimeTransactionCompletionRequested(callbackOwner.Activity.GetType().ToString(), callbackOwner.Activity.DisplayName, callbackOwner.Id);
            }
        }
    }

    private void SchedulePendingCancelation()
    {
        if (_runtimeTransaction.IsRootCancelPending)
        {
            if (!_rootInstance.IsCancellationRequested && !_rootInstance.IsCompleted)
            {
                _rootInstance.IsCancellationRequested = true;
                _scheduler.PushWork(new CancelActivityWorkItem(_rootInstance));
            }

            _runtimeTransaction.IsRootCancelPending = false;
        }
    }

    public EmptyWorkItem CreateEmptyWorkItem(ActivityInstance instance)
    {
        EmptyWorkItem workItem = EmptyWorkItemPool.Acquire();
        workItem.Initialize(instance);

        return workItem;
    }

    public bool IsCompletingTransaction(ActivityInstance instance)
    {
        if (_runtimeTransaction != null && _runtimeTransaction.IsolationScope == instance)
        {
            // We add an empty work item to keep the instance alive
            _scheduler.PushWork(CreateEmptyWorkItem(instance));

            // This will schedule the appopriate work item at the end of this work item
            _runtimeTransaction.ShouldScheduleCompletion = true;

            if (TD.RuntimeTransactionCompletionRequestedIsEnabled())
            {
                TD.RuntimeTransactionCompletionRequested(instance.Activity.GetType().ToString(), instance.Activity.DisplayName, instance.Id);
            }

            return true;
        }

        return false;
    }

    public void TerminateSpecialExecutionBlocks(ActivityInstance terminatedInstance, Exception terminationReason)
    {
        if (_runtimeTransaction != null && _runtimeTransaction.IsolationScope == terminatedInstance)
        {
            Exception abortException = null;

            try
            {
                _runtimeTransaction.Rollback(terminationReason);
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
                // It is okay for us to call AbortWorkflowInstance even if we are already
                // aborting the instance since it is an async call (IE - we asking the host
                // to re-enter the instance to abandon it.
                AbortWorkflowInstance(abortException);
            }

            SchedulePendingCancelation();

            ExitNoPersist();

            if (_runtimeTransaction.TransactionHandle.AbortInstanceOnTransactionFailure)
            {
                AbortWorkflowInstance(terminationReason);
            }

            _runtimeTransaction = null;
        }
    }

    // Returns true if we actually performed the abort and false if we had already been disposed
    private bool Abort(Exception terminationException, bool isTerminate)
    {
        if (!_isDisposed)
        {
            if (!_rootInstance.IsCompleted)
            {
                _rootInstance.Abort(this, _bookmarkManager, terminationException, isTerminate);

                // the Abort walk won't catch host-registered properties
                if (_rootPropertyManager != null)
                {
                    if (isTerminate)
                    {
                        HandleInitializationContext context = new(this, null);
                        foreach (ExecutionPropertyManager.ExecutionProperty executionProperty in _rootPropertyManager.Properties.Values)
                        {
                            if (executionProperty.Property is Handle handle)
                            {
                                handle.Uninitialize(context);
                            }
                        }
                        context.Dispose();
                    }

                    _rootPropertyManager.UnregisterProperties(null, null, true);
                }
            }

            if (_executingSecondaryRootInstances != null)
            {
                // We have to walk this list backwards because the abort
                // path removes from this collection.
                for (int i = _executingSecondaryRootInstances.Count - 1; i >= 0; i--)
                {
                    ActivityInstance secondaryRootInstance = _executingSecondaryRootInstances[i];

                    Fx.Assert(!secondaryRootInstance.IsCompleted, "We should not have any complete instances in our list.");

                    secondaryRootInstance.Abort(this, _bookmarkManager, terminationException, isTerminate);

                    Fx.Assert(_executingSecondaryRootInstances.Count == i, "We are always working from the back and we should have removed the item we just aborted.");
                }
            }

            // This must happen after we abort each activity.  This allows us to utilize code paths
            // which schedule work items.
            _scheduler.ClearAllWorkItems(this);

            if (isTerminate)
            {
                // Regardless of the previous state, a termination implies setting the
                // completion exception and completing in the Faulted state.
                _completionException = terminationException;
                _executionState = ActivityInstanceState.Faulted;
            }

            Dispose();

            return true;
        }

        return false;
    }

    // Returns true if tracing was transfered
    private bool TryTraceResume(out Guid oldActivityId)
    {
        if (TD.IsEnd2EndActivityTracingEnabled() && TD.ShouldTraceToTraceSource(EventLevel.Informational))
        {
            oldActivityId = TD.CurrentActivityId;
            TD.TraceTransfer(WorkflowInstanceId);

            if (TD.WorkflowActivityResumeIsEnabled())
            {
                TD.WorkflowActivityResume(WorkflowInstanceId);
            }

            return true;
        }
        else
        {
            oldActivityId = Guid.Empty;
            return false;
        }
    }

    // Returns true if tracing was transfered
    private bool TryTraceStart(out Guid oldActivityId)
    {
        if (TD.IsEnd2EndActivityTracingEnabled() && TD.ShouldTraceToTraceSource(EventLevel.Informational))
        {
            oldActivityId = TD.CurrentActivityId;
            TD.TraceTransfer(WorkflowInstanceId);

            if (TD.WorkflowActivityStartIsEnabled())
            {
                TD.WorkflowActivityStart(WorkflowInstanceId);
            }

            return true;
        }
        else
        {
            oldActivityId = Guid.Empty;
            return false;
        }
    }

    private void TraceSuspend(bool hasBeenResumed, Guid oldActivityId)
    {
        if (hasBeenResumed)
        {
            if (TD.WorkflowActivitySuspendIsEnabled())
            {
                TD.WorkflowActivitySuspend(WorkflowInstanceId);
            }

            TD.CurrentActivityId = oldActivityId;
        }
    }

    public bool Abort(Exception reason)
    {
        bool hasTracedResume = TryTraceResume(out Guid oldActivityId);

        bool abortResult = Abort(reason, false);

        TraceSuspend(hasTracedResume, oldActivityId);

        return abortResult;
    }

    // It must be okay for the runtime to be processing other
    // work on a different thread when this is called.  See
    // the comments in the method for justifications.
    public void AbortWorkflowInstance(Exception reason)
    {
        // 1) This flag is only ever set to true
        _isAbortPending = true;

        // 2) This causes a couple of fields to be set
        _host.Abort(reason);
        try
        {
            // 3) The host expects this to come from an unknown thread
            _host.OnRequestAbort(reason);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            throw FxTrace.Exception.AsError(new CallbackException(SR.CallbackExceptionFromHostAbort(WorkflowInstanceId), e));
        }
    }

    public void ScheduleTerminate(Exception reason)
    {
        _isTerminatePending = true;
        _terminationPendingException = reason;
    }

    public void Terminate(Exception reason)
    {
        Fx.Assert(!_isDisposed, "We should not have been able to get here if we are disposed and Abort makes choices based on isDisposed");

        bool hasTracedResume = TryTraceResume(out Guid oldActivityId);

        Abort(reason, true);

        TraceSuspend(hasTracedResume, oldActivityId);
    }

    public void CancelRootActivity()
    {
        if (_rootInstance.State == ActivityInstanceState.Executing)
        {
            if (!_rootInstance.IsCancellationRequested)
            {
                bool hasTracedResume = TryTraceResume(out Guid oldActivityId);

                bool trackCancelRequested = true;

                if (_runtimeTransaction != null && _runtimeTransaction.IsolationScope != null)
                {
                    if (_runtimeTransaction.IsRootCancelPending)
                    {
                        trackCancelRequested = false;
                    }

                    _runtimeTransaction.IsRootCancelPending = true;
                }
                else
                {
                    _rootInstance.IsCancellationRequested = true;

                    if (_rootInstance.HasNotExecuted)
                    {
                        _scheduler.PushWork(CreateEmptyWorkItem(_rootInstance));
                    }
                    else
                    {
                        _scheduler.PushWork(new CancelActivityWorkItem(_rootInstance));
                    }
                }

                if (ShouldTrackCancelRequestedRecords && trackCancelRequested)
                {
                    AddTrackingRecord(new CancelRequestedRecord(WorkflowInstanceId, null, _rootInstance));
                }

                TraceSuspend(hasTracedResume, oldActivityId);
            }
        }
        else if (_rootInstance.State != ActivityInstanceState.Closed)
        {
            // We've been asked to cancel the instance and the root
            // completed in a canceled or faulted state.  By our rules
            // this means that the instance has been canceled.  A real
            // world example if the case of UnhandledExceptionAction.Cancel
            // on a workflow whose root activity threw an exception. The
            // expected completion state is Canceled and NOT Faulted.
            _executionState = ActivityInstanceState.Canceled;
            _completionException = null;
        }
    }

    public void CancelActivity(ActivityInstance activityInstance)
    {
        Fx.Assert(activityInstance != null, "The instance must not be null.");

        // Cancel is a no-op if the activity is complete or cancel has already been requested
        if (activityInstance.State != ActivityInstanceState.Executing || activityInstance.IsCancellationRequested)
        {
            return;
        }

        // Set that we have requested cancel.  This is our only guard against scheduling
        // ActivityInstance.Cancel multiple times.
        activityInstance.IsCancellationRequested = true;

        if (activityInstance.HasNotExecuted)
        {
            _scheduler.PushWork(CreateEmptyWorkItem(activityInstance));
        }
        else
        {
            _scheduler.PushWork(new CancelActivityWorkItem(activityInstance));
        }

        if (ShouldTrackCancelRequestedRecords)
        {
            AddTrackingRecord(new CancelRequestedRecord(WorkflowInstanceId, activityInstance.Parent, activityInstance));
        }
    }

    private void PropagateException(WorkItem workItem)
    {
        ActivityInstance exceptionSource = workItem.ActivityInstance;
        Exception exception = workItem.ExceptionToPropagate;

        ActivityInstance exceptionPropagator = exceptionSource;
        FaultBookmark targetBookmark = null;

        // If we are not supposed to persist exceptions, call EnterNoPersist so that we don't persist while we are
        // propagating the exception.
        // We call ExitNoPersist when we abort an activity or when we call a fault callback. But we may end up
        // re-propagating and thus calling EnterNoPersist again.
        // We also do an exit if the workflow is aborted or the exception ends up being unhandled.
        if (!PersistExceptions)
        {
            EnterNoPersist();
        }
        while (exceptionPropagator != null && targetBookmark == null)
        {
            if (!exceptionPropagator.IsCompleted)
            {
                if (_runtimeTransaction != null && _runtimeTransaction.IsolationScope == exceptionPropagator)
                {
                    // We are propagating the exception across the isolation scope
                    _scheduler.PushWork(new AbortActivityWorkItem(exceptionPropagator, exception, CreateActivityInstanceReference(workItem.OriginalExceptionSource, exceptionPropagator)));

                    // Because we are aborting the transaction we reset the ShouldScheduleCompletion flag
                    _runtimeTransaction.ShouldScheduleCompletion = false;
                    workItem.ExceptionPropagated();
                    return;
                }
            }

            if (exceptionPropagator.IsCancellationRequested)
            {
                // Regardless of whether it is already completed or not we need
                // to honor the workflow abort

                AbortWorkflowInstance(new InvalidOperationException(SR.CannotPropagateExceptionWhileCanceling(exceptionSource.Activity.DisplayName, exceptionSource.Id), exception));
                workItem.ExceptionPropagated();
                ExitNoPersistForExceptionPropagation();
                return;
            }

            if (exceptionPropagator.FaultBookmark != null)
            {
                // This will cause us to break out of the loop
                targetBookmark = exceptionPropagator.FaultBookmark;
            }
            else
            {
                exceptionPropagator = exceptionPropagator.Parent;
            }
        }

        if (targetBookmark != null)
        {
            if (ShouldTrackFaultPropagationRecords)
            {
                AddTrackingRecord(new FaultPropagationRecord(WorkflowInstanceId,
                                                            workItem.OriginalExceptionSource,
                                                            exceptionPropagator.Parent,
                                                            exceptionSource == workItem.OriginalExceptionSource,
                                                            exception));
            }

            _scheduler.PushWork(targetBookmark.GenerateWorkItem(exception, exceptionPropagator, CreateActivityInstanceReference(workItem.OriginalExceptionSource, exceptionPropagator.Parent)));
            workItem.ExceptionPropagated();
        }
        else
        {
            if (ShouldTrackFaultPropagationRecords)
            {
                AddTrackingRecord(new FaultPropagationRecord(WorkflowInstanceId,
                                                            workItem.OriginalExceptionSource,
                                                            null,
                                                            exceptionSource == workItem.OriginalExceptionSource,
                                                            exception));
            }
        }
    }

    internal ActivityInstanceReference CreateActivityInstanceReference(ActivityInstance toReference, ActivityInstance referenceOwner)
    {
        ActivityInstanceReference reference = new(toReference);

        _instanceMap?.AddEntry(reference);
        referenceOwner.AddActivityReference(reference);

        return reference;
    }

    internal void RethrowException(ActivityInstance fromInstance, FaultContext context)
        => _scheduler.PushWork(new RethrowExceptionWorkItem(fromInstance, context.Exception, context.Source));

    internal void OnDeserialized(Activity workflow, WorkflowInstance workflowInstance)
    {
        Fx.Assert(workflow != null, "The program must be non-null");
        Fx.Assert(workflowInstance != null, "The host must be non-null");

        if (!Equals(workflowInstance.DefinitionIdentity, WorkflowIdentity))
        {
            throw FxTrace.Exception.AsError(new VersionMismatchException(workflowInstance.DefinitionIdentity, WorkflowIdentity));
        }

        _rootElement = workflow;
        _host = workflowInstance;

        if (!_instanceIdSet)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.EmptyGuidOnDeserializedInstance));
        }
        if (_host.Id != _instanceId)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.HostIdDoesNotMatchInstance(_host.Id, _instanceId)));
        }

        if (_host.HasTrackingParticipant)
        {
            _host.TrackingProvider.OnDeserialized(_nextTrackingRecordNumber);
            _host.OnDeserialized(_hasTrackedStarted);
        }

        // hookup our callback to the scheduler
        if (_scheduler != null)
        {
            _scheduler.OnDeserialized(new Scheduler.Callbacks(this));
        }

        if (_rootInstance != null)
        {
            Fx.Assert(_instanceMap != null, "We always have an InstanceMap.");
            _instanceMap.LoadActivityTree(workflow, _rootInstance, _executingSecondaryRootInstances, this);

            // We need to make sure that any "dangling" secondary root environments
            // get OnDeserialized called.
            if (_executingSecondaryRootInstances != null)
            {
                Fx.Assert(_executingSecondaryRootInstances.Count > 0, "We don't serialize out an empty list.");

                for (int i = 0; i < _executingSecondaryRootInstances.Count; i++)
                {
                    ActivityInstance secondaryRoot = _executingSecondaryRootInstances[i];
                    LocationEnvironment environment = secondaryRoot.Environment.Parent;
                    environment?.OnDeserialized(this, secondaryRoot);
                }
            }
        }
        else
        {
            _isDisposed = true;
        }
    }

    public T GetExtension<T>()
        where T : class
    {
        T extension;
        try
        {
            extension = _host.GetExtension<T>();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            throw FxTrace.Exception.AsError(new CallbackException(SR.CallbackExceptionFromHostGetExtension(WorkflowInstanceId), e));
        }

        return extension;
    }

    internal Scheduler.RequestedAction TryExecuteNonEmptyWorkItem(WorkItem workItem)
    {
        Exception setupOrCleanupException = null;
        ActivityInstance propertyManagerOwner = workItem.PropertyManagerOwner;
        try
        {
            if (propertyManagerOwner != null && propertyManagerOwner.PropertyManager != null)
            {
                try
                {
                    propertyManagerOwner.PropertyManager.SetupWorkflowThread();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    setupOrCleanupException = e;
                }
            }

            if (setupOrCleanupException == null)
            {
                if (!workItem.Execute(this, _bookmarkManager))
                {
                    return Scheduler.YieldSilently;
                }
            }
        }
        finally
        {
            // We might be multi-threaded when we execute code in
            // this finally block.  The work item might have gone
            // async and may already have called back into FinishWorkItem.
            if (propertyManagerOwner != null && propertyManagerOwner.PropertyManager != null)
            {
                // This throws only fatal exceptions
                propertyManagerOwner.PropertyManager.CleanupWorkflowThread(ref setupOrCleanupException);
            }

            if (setupOrCleanupException != null)
            {
                // This API must allow the runtime to be
                // multi-threaded when it is called.
                AbortWorkflowInstance(new OperationCanceledException(SR.SetupOrCleanupWorkflowThreadThrew, setupOrCleanupException));
            }
        }

        if (setupOrCleanupException != null)
        {
            // We already aborted the instance in the finally block so
            // now we just need to return early.
            return Scheduler.Continue;
        }
        return null;
    }

    // callback from scheduler to process a work item
    internal Scheduler.RequestedAction OnExecuteWorkItem(WorkItem workItem)
    {
        workItem.Release(this);

        // thunk out early if the work item is no longer valid (that is, we're not in the Executing state)
        if (!workItem.IsValid)
        {
            return Scheduler.Continue;
        }

        if (!workItem.IsEmpty)
        {
            // The try/catch/finally block used in executing a workItem prevents ryujit from performing
            // some optimizations. Moving the functionality back into this method may cause a performance
            // regression.
            var result = TryExecuteNonEmptyWorkItem(workItem);
            if (result != null)
            {
                return result;
            }
        }

        if (workItem.WorkflowAbortException != null)
        {
            AbortWorkflowInstance(new OperationCanceledException(SR.WorkItemAbortedInstance, workItem.WorkflowAbortException));
            return Scheduler.Continue;
        }

        // We only check this in the sync path because there are no ways of changing the keys collections from the work items that can
        // go async.  There's an assert to this effect in FinishWorkItem.
        if (_bookmarkScopeManager != null && _bookmarkScopeManager.HasKeysToUpdate)
        {
            if (!workItem.FlushBookmarkScopeKeys(this))
            {
                return Scheduler.YieldSilently;
            }

            if (workItem.WorkflowAbortException != null)
            {
                AbortWorkflowInstance(new OperationCanceledException(SR.WorkItemAbortedInstance, workItem.WorkflowAbortException));
                return Scheduler.Continue;
            }
        }

        workItem.PostProcess(this);

        if (workItem.ExceptionToPropagate != null)
        {
            PropagateException(workItem);
        }

        if (HasPendingTrackingRecords)
        {
            if (!workItem.FlushTracking(this))
            {
                return Scheduler.YieldSilently;
            }

            if (workItem.WorkflowAbortException != null)
            {
                AbortWorkflowInstance(new OperationCanceledException(SR.TrackingRelatedWorkflowAbort, workItem.WorkflowAbortException));
                return Scheduler.Continue;
            }
        }

        ScheduleRuntimeWorkItems();

        if (workItem.ExceptionToPropagate != null)
        {
            ExitNoPersistForExceptionPropagation();
            return Scheduler.CreateNotifyUnhandledExceptionAction(workItem.ExceptionToPropagate, workItem.OriginalExceptionSource);
        }

        return Scheduler.Continue;
    }

    internal IAsyncResult BeginAssociateKeys(ICollection<InstanceKey> keysToAssociate, AsyncCallback callback, object state)
        => new AssociateKeysAsyncResult(this, keysToAssociate, callback, state);

    internal static void EndAssociateKeys(IAsyncResult result) => AssociateKeysAsyncResult.End(result);

    internal void DisassociateKeys(ICollection<InstanceKey> keysToDisassociate) => _host.OnDisassociateKeys(keysToDisassociate);

    internal void FinishWorkItem(WorkItem workItem)
    {
        Scheduler.RequestedAction resumptionAction = Scheduler.Continue;

        try
        {
            Fx.Assert(_bookmarkScopeManager == null || !_bookmarkScopeManager.HasKeysToUpdate,
                "FinishWorkItem should be called after FlushBookmarkScopeKeys, or by a WorkItem that could not possibly generate keys.");

            if (workItem.WorkflowAbortException != null)
            {
                // We resume the scheduler even after abort to make sure that
                // the proper events are raised.
                AbortWorkflowInstance(new OperationCanceledException(SR.WorkItemAbortedInstance, workItem.WorkflowAbortException));
            }
            else
            {
                workItem.PostProcess(this);

                if (workItem.ExceptionToPropagate != null)
                {
                    PropagateException(workItem);
                }

                if (HasPendingTrackingRecords)
                {
                    if (!workItem.FlushTracking(this))
                    {
                        // We exit early here and will come back in at
                        // FinishWorkItemAfterTracking
                        resumptionAction = Scheduler.YieldSilently;
                        return;
                    }
                }

                if (workItem.WorkflowAbortException != null)
                {
                    // We resume the scheduler even after abort to make sure that
                    // the proper events are raised.
                    AbortWorkflowInstance(new OperationCanceledException(SR.TrackingRelatedWorkflowAbort, workItem.WorkflowAbortException));
                }
                else
                {
                    ScheduleRuntimeWorkItems();

                    if (workItem.ExceptionToPropagate != null)
                    {
                        ExitNoPersistForExceptionPropagation();
                        resumptionAction = Scheduler.CreateNotifyUnhandledExceptionAction(workItem.ExceptionToPropagate, workItem.OriginalExceptionSource);
                    }
                }
            }
        }
        finally
        {
            if (resumptionAction != Scheduler.YieldSilently)
            {
                workItem.Dispose(this);
            }
        }

        Fx.Assert(resumptionAction != Scheduler.YieldSilently, "should not reach this section if we've yielded earlier");
        _scheduler.InternalResume(resumptionAction);
    }

    internal void FinishWorkItemAfterTracking(WorkItem workItem)
    {
        Scheduler.RequestedAction resumptionAction = Scheduler.Continue;

        try
        {
            if (workItem.WorkflowAbortException != null)
            {
                // We resume the scheduler even after abort to make sure that
                // the proper events are raised.
                AbortWorkflowInstance(new OperationCanceledException(SR.TrackingRelatedWorkflowAbort, workItem.WorkflowAbortException));
            }
            else
            {
                ScheduleRuntimeWorkItems();

                if (workItem.ExceptionToPropagate != null)
                {
                    ExitNoPersistForExceptionPropagation();
                    resumptionAction = Scheduler.CreateNotifyUnhandledExceptionAction(workItem.ExceptionToPropagate, workItem.OriginalExceptionSource);
                }
            }
        }
        finally
        {
            workItem.Dispose(this);
        }

        _scheduler.InternalResume(resumptionAction);
    }

    private void ScheduleRuntimeWorkItems()
    {
        if (_runtimeTransaction != null && _runtimeTransaction.ShouldScheduleCompletion)
        {
            _scheduler.PushWork(new CompleteTransactionWorkItem(_runtimeTransaction.IsolationScope));
            return;
        }

        if (_persistenceWaiters != null && _persistenceWaiters.Count > 0 &&
            IsPersistable)
        {
            PersistenceWaiter waiter = _persistenceWaiters.Dequeue();

            while (waiter != null && waiter.WaitingInstance.IsCompleted)
            {
                // We just skip completed instance so we don't have to deal
                // with the housekeeping are arbitrary removal from our
                // queue
                waiter = _persistenceWaiters.Count == 0 ? null : _persistenceWaiters.Dequeue();
            }

            if (waiter != null)
            {
                _scheduler.PushWork(waiter.CreateWorkItem());
                return;
            }
        }
    }

    internal void AbortActivityInstance(ActivityInstance instance, Exception reason)
    {
        instance.Abort(this, _bookmarkManager, reason, true);

        if (instance.CompletionBookmark != null)
        {
            instance.CompletionBookmark.CheckForCancelation();
        }
        else if (instance.Parent != null)
        {
            instance.CompletionBookmark = new CompletionBookmark();
        }

        ScheduleCompletionBookmark(instance);
    }

    internal Exception CompleteActivityInstance(ActivityInstance targetInstance)
    {
        Exception exceptionToPropagate = null;

        // 1. Handle any root related work
        HandleRootCompletion(targetInstance);

        // 2. Schedule the completion bookmark
        // We MUST schedule the completion bookmark before
        // we dispose the environment because we take this
        // opportunity to gather up any output values.
        ScheduleCompletionBookmark(targetInstance);

#if NET45
        if (!targetInstance.HasNotExecuted)
        {
            DebugActivityCompleted(targetInstance);
        }
#endif

        // 3. Cleanup environmental resources (properties, handles, mapped locations)
        try
        {
            if (targetInstance.PropertyManager != null)
            {
                targetInstance.PropertyManager.UnregisterProperties(targetInstance, targetInstance.Activity.MemberOf);
            }

            if (IsSecondaryRoot(targetInstance))
            {
                // We need to appropriately remove references, dispose
                // environments, and remove instance map entries for
                // all environments in this chain
                LocationEnvironment environment = targetInstance.Environment;

                if (targetInstance.IsEnvironmentOwner)
                {
                    environment.RemoveReference(true);

                    if (environment.ShouldDispose)
                    {
                        // Unintialize all handles declared in this environment.  
                        environment.UninitializeHandles(targetInstance);

                        environment.Dispose();
                    }

                    environment = environment.Parent;
                }

                while (environment != null)
                {
                    environment.RemoveReference(false);

                    if (environment.ShouldDispose)
                    {
                        // Unintialize all handles declared in this environment.  
                        environment.UninitializeHandles(targetInstance);

                        environment.Dispose();

                        // This also implies that the owner is complete so we should
                        // remove it from the map
                        _instanceMap?.RemoveEntry(environment);
                    }

                    environment = environment.Parent;
                }
            }
            else if (targetInstance.IsEnvironmentOwner)
            {
                targetInstance.Environment.RemoveReference(true);

                if (targetInstance.Environment.ShouldDispose)
                {
                    // Unintialize all handles declared in this environment.  
                    targetInstance.Environment.UninitializeHandles(targetInstance);
                    targetInstance.Environment.Dispose();
                }
                else if (_instanceMap != null)
                {
                    // Someone else is referencing this environment
                    // Note that we don't use TryAdd since no-one else should have 
                    // added it before.
                    _instanceMap.AddEntry(targetInstance.Environment);
                }
            }
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            exceptionToPropagate = e;
        }

        // 4. Cleanup remaining instance related resources (bookmarks, program mapping)
        targetInstance.MarkAsComplete(_bookmarkScopeManager, _bookmarkManager);

        // 5. Track our final state
        targetInstance.FinalizeState(this, exceptionToPropagate != null);

        return exceptionToPropagate;
    }

    internal bool TryGetPendingOperation(ActivityInstance instance, out AsyncOperationContext asyncContext)
    {
        if (_activeOperations != null)
        {
            return _activeOperations.TryGetValue(instance, out asyncContext);
        }
        else
        {
            asyncContext = null;
            return false;
        }
    }

    internal void CancelPendingOperation(ActivityInstance instance)
    {
        if (TryGetPendingOperation(instance, out AsyncOperationContext asyncContext))
        {
            if (asyncContext.IsStillActive)
            {
                asyncContext.CancelOperation();
            }
        }
    }

    internal void HandleRootCompletion(ActivityInstance completedInstance)
    {
        if (completedInstance.Parent == null)
        {
            if (completedInstance == _rootInstance)
            {
                _shouldRaiseMainBodyComplete = true;

                Fx.Assert(_executionState == ActivityInstanceState.Executing, "We shouldn't have a guess at our completion state yet.");

                // We start by assuming our completion state will match the root instance.
                _executionState = _rootInstance.State;
                _rootEnvironment = _rootInstance.Environment;
            }
            else
            {
                Fx.Assert(_executingSecondaryRootInstances.Contains(completedInstance), "An instance which is not the main root and doesn't have an execution parent must be an executing secondary root.");
                _executingSecondaryRootInstances.Remove(completedInstance);
            }

            // We just had a root complete, let's see if we're all the way done
            // and should gather outputs from the root.  Note that we wait until
            // everything completes in case the root environment was detached.
            if (_rootInstance.IsCompleted
                && (_executingSecondaryRootInstances == null || _executingSecondaryRootInstances.Count == 0))
            {
                GatherRootOutputs();

                // uninitialize any host-provided handles
                if (_rootPropertyManager != null)
                {
                    // and uninitialize host-provided handles
                    HandleInitializationContext context = new(this, null);
                    foreach (ExecutionPropertyManager.ExecutionProperty executionProperty in _rootPropertyManager.Properties.Values)
                    {
                        if (executionProperty.Property is Handle handle)
                        {
                            handle.Uninitialize(context);
                        }
                    }
                    context.Dispose();

                    // unregister any properties that were registered
                    _rootPropertyManager.UnregisterProperties(null, null);
                }
            }
        }
    }

    private bool IsSecondaryRoot(ActivityInstance instance) => instance.Parent == null && instance != _rootInstance;

    private void GatherRootOutputs()
    {
        Fx.Assert(_workflowOutputs == null, "We should only get workflow outputs when we actually complete which should only happen once.");
        Fx.Assert(ActivityUtilities.IsCompletedState(_rootInstance.State), "We should only gather outputs when in a completed state.");
        Fx.Assert(_rootEnvironment != null, "We should have set the root environment");

        // We only gather outputs for Closed - not for canceled or faulted
        if (_rootInstance.State == ActivityInstanceState.Closed)
        {
            // We use rootElement here instead of _rootInstance.Activity
            // because we don't always reload the root instance (like if it
            // was complete when we last persisted).
            IList<RuntimeArgument> rootArguments = _rootElement.RuntimeArguments;

            for (int i = 0; i < rootArguments.Count; i++)
            {
                RuntimeArgument argument = rootArguments[i];

                if (ArgumentDirectionHelper.IsOut(argument.Direction))
                {
                    _workflowOutputs ??= new Dictionary<string, object>();
                    Location location = _rootEnvironment.GetSpecificLocation(argument.BoundArgument.Id);
                    if (location == null)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.NoOutputLocationWasFound(argument.Name)));
                    }
                    _workflowOutputs.Add(argument.Name, location.Value);
                }
            }
        }

        // GatherRootOutputs only ever gets called once so we can null it out the root environment now.
        _rootEnvironment = null;
    }

    internal void NotifyUnhandledException(Exception exception, ActivityInstance source)
    {
        try
        {
            _host.NotifyUnhandledException(exception, source.Activity, source.Id);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            AbortWorkflowInstance(e);
        }
    }

    internal void OnSchedulerIdle()
    {
        // If we're terminating we'll call terminate here and
        // then do the normal notification for the host.
        if (_isTerminatePending)
        {
            Fx.Assert(_terminationPendingException != null, "Should have set terminationPendingException at the same time that we set isTerminatePending = true");
            Terminate(_terminationPendingException);
            _isTerminatePending = false;
        }

        if (IsIdle)
        {
            if (_transactionContextWaiters != null && _transactionContextWaiters.Count > 0)
            {
                if (IsPersistable || (_transactionContextWaiters[0].IsRequires && _noPersistCount == 1))
                {
                    TransactionContextWaiter waiter = _transactionContextWaiters.Dequeue();

                    waiter.WaitingInstance.DecrementBusyCount();
                    waiter.WaitingInstance.WaitingForTransactionContext = false;

                    ScheduleItem(new TransactionContextWorkItem(waiter));

                    MarkSchedulerRunning();
                    ResumeScheduler();

                    return;
                }
            }

            if (_shouldRaiseMainBodyComplete)
            {
                _shouldRaiseMainBodyComplete = false;
                if (_mainRootCompleteBookmark != null)
                {
                    BookmarkResumptionResult resumptionResult = TryResumeUserBookmark(_mainRootCompleteBookmark, _rootInstance.State, false);
                    _mainRootCompleteBookmark = null;
                    if (resumptionResult == BookmarkResumptionResult.Success)
                    {
                        MarkSchedulerRunning();
                        ResumeScheduler();
                        return;
                    }
                }

                if (_executingSecondaryRootInstances == null || _executingSecondaryRootInstances.Count == 0)
                {
                    // if we got to this point we're completely done from the executor's point of view.
                    // outputs have been gathered, no more work is happening. Clear out some fields to shrink our 
                    // "completed instance" persistence size
                    Dispose(false);
                }
            }
        }

        if (_shouldPauseOnCanPersist && IsPersistable)
        {
            _shouldPauseOnCanPersist = false;
        }

        try
        {
            _host.NotifyPaused();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            AbortWorkflowInstance(e);
        }
    }

    public void Open(SynchronizationContext synchronizationContext) => _scheduler.Open(synchronizationContext);

    public void PauseScheduler() =>
        // Since we don't require calls to WorkflowInstanceControl.Pause to be synchronized
        // by the caller, we need to check for null here
        _scheduler?.Pause();

    public object PrepareForSerialization()
    {
        if (_host.HasTrackingParticipant)
        {
            _nextTrackingRecordNumber = _host.TrackingProvider.NextTrackingRecordNumber;
            _hasTrackedStarted = _host.HasTrackedStarted;
        }
        return this;
    }

    public void RequestPersist(Bookmark onPersistBookmark, ActivityInstance requestingInstance)
    {
        _persistenceWaiters ??= new Queue<PersistenceWaiter>();
        _persistenceWaiters.Enqueue(new PersistenceWaiter(onPersistBookmark, requestingInstance));
    }

    private void ScheduleCompletionBookmark(ActivityInstance completedInstance)
    {
        if (completedInstance.CompletionBookmark != null)
        {
            _scheduler.PushWork(completedInstance.CompletionBookmark.GenerateWorkItem(completedInstance, this));
        }
        else if (completedInstance.Parent != null)
        {
            // Variable defaults and argument expressions always have a parent
            // and never have a CompletionBookmark
            if (completedInstance.State != ActivityInstanceState.Closed && completedInstance.Parent.HasNotExecuted)
            {
                completedInstance.Parent.SetInitializationIncomplete();
            }

            _scheduler.PushWork(CreateEmptyWorkItem(completedInstance.Parent));
        }
    }

    // This method is called by WorkflowInstance - these are bookmark resumptions
    // originated by the host
    internal BookmarkResumptionResult TryResumeHostBookmark(Bookmark bookmark, object value)
    {
        bool hasTracedResume = TryTraceResume(out Guid oldActivityId);

        BookmarkResumptionResult result = TryResumeUserBookmark(bookmark, value, true);

        TraceSuspend(hasTracedResume, oldActivityId);

        return result;
    }

    internal BookmarkResumptionResult TryResumeUserBookmark(Bookmark bookmark, object value, bool isExternal)
    {
        if (_isDisposed)
        {
            return BookmarkResumptionResult.NotFound;
        }

        ActivityInstance isolationInstance = null;

        if (_runtimeTransaction != null)
        {
            isolationInstance = _runtimeTransaction.IsolationScope;
        }

        BookmarkResumptionResult result = _bookmarkManager.TryGenerateWorkItem(this, isExternal, ref bookmark, value, isolationInstance, out ActivityExecutionWorkItem resumeExecutionWorkItem);

        if (result == BookmarkResumptionResult.Success)
        {
            _scheduler.EnqueueWork(resumeExecutionWorkItem);

            if (ShouldTrackBookmarkResumptionRecords)
            {
                AddTrackingRecord(new BookmarkResumptionRecord(WorkflowInstanceId, bookmark, resumeExecutionWorkItem.ActivityInstance, value));
            }
        }
        else if (result == BookmarkResumptionResult.NotReady)
        {
            // We had the bookmark but this is not an appropriate time to resume it
            // so we won't do anything here
        }
        else if (bookmark == Bookmark.AsyncOperationCompletionBookmark)
        {
            Fx.Assert(result == BookmarkResumptionResult.NotFound, "This BookmarkNotFound is actually a well-known bookmark.");

            AsyncOperationContext.CompleteData data = (AsyncOperationContext.CompleteData)value;

            data.CompleteOperation();

            result = BookmarkResumptionResult.Success;
        }

        return result;
    }

    internal ReadOnlyCollection<BookmarkInfo> GetAllBookmarks()
    {
        List<BookmarkInfo> bookmarks = CollectExternalBookmarks();

        return bookmarks != null ? new ReadOnlyCollection<BookmarkInfo>(bookmarks) : EmptyBookmarkInfoCollection;
    }

    private List<BookmarkInfo> CollectExternalBookmarks()
    {
        List<BookmarkInfo> bookmarks = null;

        if (_bookmarkManager != null && _bookmarkManager.HasBookmarks)
        {
            bookmarks = new List<BookmarkInfo>();

            _bookmarkManager.PopulateBookmarkInfo(bookmarks);
        }

        _bookmarkScopeManager?.PopulateBookmarkInfo(ref bookmarks);
        return bookmarks == null || bookmarks.Count == 0 ? null : bookmarks;
    }

    internal ReadOnlyCollection<BookmarkInfo> GetBookmarks(BookmarkScope scope)
    {
        if (_bookmarkScopeManager == null)
        {
            return EmptyBookmarkInfoCollection;
        }
        else
        {
            ReadOnlyCollection<BookmarkInfo> bookmarks = _bookmarkScopeManager.GetBookmarks(scope);
            return bookmarks ?? EmptyBookmarkInfoCollection;
        }
    }

    internal IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
        => _host.OnBeginResumeBookmark(bookmark, value, timeout, callback, state);

    internal BookmarkResumptionResult EndResumeBookmark(IAsyncResult result)
        => _host.OnEndResumeBookmark(result);

    // This is only called by WorkflowInstance so it behaves like TryResumeUserBookmark with must
    // run work item set to true
    internal BookmarkResumptionResult TryResumeBookmark(Bookmark bookmark, object value, BookmarkScope scope)
    {
        // We have to perform all of this work with tracing set up
        // since we might initialize a sub-instance while generating
        // the work item.
        bool hasTracedResume = TryTraceResume(out Guid oldActivityId);

        ActivityInstance isolationInstance = null;

        if (_runtimeTransaction != null)
        {
            isolationInstance = _runtimeTransaction.IsolationScope;
        }

        bool hasOperations = _activeOperations != null && _activeOperations.Count > 0;

        BookmarkResumptionResult result = BookmarkScopeManager.TryGenerateWorkItem(this, ref bookmark, scope, value, isolationInstance, hasOperations || _bookmarkManager.HasBookmarks, out ActivityExecutionWorkItem resumeExecutionWorkItem);

        if (result == BookmarkResumptionResult.Success)
        {
            _scheduler.EnqueueWork(resumeExecutionWorkItem);

            if (ShouldTrackBookmarkResumptionRecords)
            {
                AddTrackingRecord(new BookmarkResumptionRecord(WorkflowInstanceId, bookmark, resumeExecutionWorkItem.ActivityInstance, value));
            }
        }

        TraceSuspend(hasTracedResume, oldActivityId);

        return result;
    }

    public void MarkSchedulerRunning() => _scheduler.MarkRunning();

    public void Run() => ResumeScheduler();

    private void ResumeScheduler() => _scheduler.Resume();

    internal void ScheduleItem(WorkItem workItem) => _scheduler.PushWork(workItem);

    public void ScheduleRootActivity(Activity activity, IDictionary<string, object> argumentValueOverrides, IList<Handle> hostProperties)
    {
        Fx.Assert(_rootInstance == null, "ScheduleRootActivity should only be called once");

        if (hostProperties != null && hostProperties.Count > 0)
        {
            Dictionary<string, ExecutionPropertyManager.ExecutionProperty> rootProperties = new(hostProperties.Count);
            HandleInitializationContext context = new(this, null);
            for (int i = 0; i < hostProperties.Count; i++)
            {
                Handle handle = hostProperties[i];
                handle.Initialize(context);
                rootProperties.Add(handle.ExecutionPropertyName, new ExecutionPropertyManager.ExecutionProperty(handle.ExecutionPropertyName, handle, null));
            }
            context.Dispose();

            _rootPropertyManager = new ExecutionPropertyManager(null, rootProperties);
        }

        bool hasTracedStart = TryTraceStart(out Guid oldActivityId);

        // Create and initialize the root instance
        _rootInstance = new ActivityInstance(activity)
        {
            PropertyManager = _rootPropertyManager
        };
        _rootElement = activity;

        Fx.Assert(_lastInstanceId == 0, "We should only hit this path once");
        _lastInstanceId++;

        bool requiresSymbolResolution = _rootInstance.Initialize(null, _instanceMap, null, _lastInstanceId, this);

        if (TD.ActivityScheduledIsEnabled())
        {
            TraceActivityScheduled(null, activity, _rootInstance.Id);
        }

        // Add the work item for executing the root
        _scheduler.PushWork(new ExecuteRootWorkItem(_rootInstance, requiresSymbolResolution, argumentValueOverrides));

        TraceSuspend(hasTracedStart, oldActivityId);
    }

    public void RegisterMainRootCompleteCallback(Bookmark bookmark) => _mainRootCompleteBookmark = bookmark;

    public ActivityInstance ScheduleSecondaryRootActivity(Activity activity, LocationEnvironment environment)
    {
        ActivityInstance secondaryRoot = ScheduleActivity(activity, null, null, null, environment);

        while (environment != null)
        {
            environment.AddReference();
            environment = environment.Parent;
        }

        _executingSecondaryRootInstances ??= new List<ActivityInstance>();
        _executingSecondaryRootInstances.Add(secondaryRoot);

        return secondaryRoot;
    }

    public ActivityInstance ScheduleActivity(
        Activity activity,
        ActivityInstance parent,
        CompletionBookmark completionBookmark,
        FaultBookmark faultBookmark,
        LocationEnvironment parentEnvironment)
        => ScheduleActivity(activity, parent, completionBookmark, faultBookmark, parentEnvironment, null, null);

    public ActivityInstance ScheduleDelegate(
        ActivityDelegate activityDelegate,
        IDictionary<string, object> inputParameters,
        ActivityInstance parent,
        LocationEnvironment executionEnvironment,
        CompletionBookmark completionBookmark,
        FaultBookmark faultBookmark)
    {
        Fx.Assert(activityDelegate.Owner != null, "activityDelegate must have an owner");
        Fx.Assert(parent != null, "activityDelegate should have a parent activity instance");

        ActivityInstance handlerInstance;

        if (activityDelegate.Handler == null)
        {
            handlerInstance = ActivityInstance.CreateCompletedInstance(new EmptyDelegateActivity());
            handlerInstance.CompletionBookmark = completionBookmark;
            ScheduleCompletionBookmark(handlerInstance);
        }
        else
        {
            handlerInstance = CreateUninitalizedActivityInstance(activityDelegate.Handler, parent, completionBookmark, faultBookmark);
            bool requiresSymbolResolution = handlerInstance.Initialize(parent, _instanceMap, executionEnvironment, _lastInstanceId, this, activityDelegate.RuntimeDelegateArguments.Count);

            IList<RuntimeDelegateArgument> activityDelegateParameters = activityDelegate.RuntimeDelegateArguments;
            for (int i = 0; i < activityDelegateParameters.Count; i++)
            {
                RuntimeDelegateArgument runtimeArgument = activityDelegateParameters[i];

                if (runtimeArgument.BoundArgument != null)
                {
                    string delegateParameterName = runtimeArgument.Name;

                    // Populate argument location. Set it's value in the activity handler's 
                    // instance environment only if it is a DelegateInArgument.
                    Location newLocation = runtimeArgument.BoundArgument.CreateLocation();
                    handlerInstance.Environment.Declare(runtimeArgument.BoundArgument, newLocation, handlerInstance);

                    if (ArgumentDirectionHelper.IsIn(runtimeArgument.Direction))
                    {
                        if (inputParameters != null && inputParameters.Count > 0)
                        {
                            newLocation.Value = inputParameters[delegateParameterName];
                        }
                    }
                }
            }

            if (TD.ActivityScheduledIsEnabled())
            {
                TraceActivityScheduled(parent, activityDelegate.Handler, handlerInstance.Id);
            }

            if (ShouldTrackActivityScheduledRecords)
            {
                AddTrackingRecord(new ActivityScheduledRecord(WorkflowInstanceId, parent, handlerInstance));
            }

            ScheduleBody(handlerInstance, requiresSymbolResolution, null, null);
        }

        return handlerInstance;
    }

    private static void TraceActivityScheduled(ActivityInstance parent, Activity activity, string scheduledInstanceId)
    {
        Fx.Assert(TD.ActivityScheduledIsEnabled(), "This should be checked before calling this helper.");

        if (parent != null)
        {
            TD.ActivityScheduled(parent.Activity.GetType().ToString(), parent.Activity.DisplayName, parent.Id, activity.GetType().ToString(), activity.DisplayName, scheduledInstanceId);
        }
        else
        {
            TD.ActivityScheduled(string.Empty, string.Empty, string.Empty, activity.GetType().ToString(), activity.DisplayName, scheduledInstanceId);
        }
    }

    private ActivityInstance CreateUninitalizedActivityInstance(Activity activity, ActivityInstance parent, CompletionBookmark completionBookmark, FaultBookmark faultBookmark)
    {
        Fx.Assert(activity.IsMetadataCached, "Metadata must be cached for us to process this activity.");

        // 1. Create a new activity instance and setup bookmark callbacks
        ActivityInstance activityInstance = new(activity);

        if (parent != null)
        {
            // add a bookmarks to complete at activity.Close/Fault time
            activityInstance.CompletionBookmark = completionBookmark;
            activityInstance.FaultBookmark = faultBookmark;
            parent.AddChild(activityInstance);
        }

        // 2. Setup parent and environment machinery, and add to instance's program mapping for persistence (if necessary)
        IncrementLastInstanceId();

        return activityInstance;
    }

    private void IncrementLastInstanceId()
    {
        if (_lastInstanceId == long.MaxValue)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.OutOfInstanceIds));
        }
        _lastInstanceId++;
    }

    private ActivityInstance ScheduleActivity(
        Activity activity,
        ActivityInstance parent,
        CompletionBookmark completionBookmark,
        FaultBookmark faultBookmark,
        LocationEnvironment parentEnvironment,
        IDictionary<string, object> argumentValueOverrides,
        Location resultLocation)
    {
        ActivityInstance activityInstance = CreateUninitalizedActivityInstance(activity, parent, completionBookmark, faultBookmark);
        bool requiresSymbolResolution = activityInstance.Initialize(parent, _instanceMap, parentEnvironment, _lastInstanceId, this);

        if (TD.ActivityScheduledIsEnabled())
        {
            TraceActivityScheduled(parent, activity, activityInstance.Id);
        }

        if (ShouldTrackActivityScheduledRecords)
        {
            AddTrackingRecord(new ActivityScheduledRecord(WorkflowInstanceId, parent, activityInstance));
        }

        ScheduleBody(activityInstance, requiresSymbolResolution, argumentValueOverrides, resultLocation);

        return activityInstance;
    }

    internal void ScheduleExpression(ActivityWithResult activity, ActivityInstance parent, LocationEnvironment parentEnvironment, Location resultLocation, ResolveNextArgumentWorkItem nextArgumentWorkItem)
    {
        Fx.Assert(resultLocation != null, "We should always schedule expressions with a result location.");

        if (!activity.IsMetadataCached || activity.CacheId != parent.Activity.CacheId)
        {
            throw FxTrace.Exception.Argument(nameof(activity), SR.ActivityNotPartOfThisTree(activity.DisplayName, parent.Activity.DisplayName));
        }

        if (activity.SkipArgumentResolution)
        {
            Fx.Assert(!activity.UseOldFastPath || parent.SubState == ActivityInstance.Substate.Executing,
                "OldFastPath activities should have been handled by the Populate methods, unless this is a dynamic update");

            IncrementLastInstanceId();

            ScheduleExpression(activity, parent, resultLocation, nextArgumentWorkItem, _lastInstanceId);
        }
        else
        {
            if (nextArgumentWorkItem != null)
            {
                ScheduleItem(nextArgumentWorkItem);
            }
            ScheduleActivity(activity, parent, null, null, parentEnvironment, null, resultLocation.CreateReference(true));
        }
    }

    private void ScheduleExpression(ActivityWithResult activity, ActivityInstance parent, Location resultLocation, ResolveNextArgumentWorkItem nextArgumentWorkItem, long instanceId)
    {
        if (TD.ActivityScheduledIsEnabled())
        {
            TraceActivityScheduled(parent, activity, instanceId.ToString(CultureInfo.InvariantCulture));
        }

        if (ShouldTrackActivityScheduledRecords)
        {
            AddTrackingRecord(new ActivityScheduledRecord(WorkflowInstanceId, parent, new ActivityInfo(activity, instanceId)));
        }

        ExecuteSynchronousExpressionWorkItem workItem = ExecuteSynchronousExpressionWorkItemPool.Acquire();
        workItem.Initialize(parent, activity, _lastInstanceId, resultLocation, nextArgumentWorkItem);
        _instanceMap?.AddEntry(workItem);
        ScheduleItem(workItem);
    }

    internal void ScheduleExpressionFaultPropagation(Activity activity, long instanceId, ActivityInstance parent, Exception exception)
    {
        ActivityInstance instance = new(activity);
        instance.Initialize(parent, _instanceMap, parent.Environment, instanceId, this);

        if (!parent.HasPendingWork)
        {
            // Force the parent to stay alive, and to attempt to execute its body if the fault is handled
            ScheduleItem(CreateEmptyWorkItem(parent));
        }
        PropagateExceptionWorkItem workItem = new(exception, instance);
        ScheduleItem(workItem);

        parent.SetInitializationIncomplete();
    }

    // Argument and variables resolution for root activity is defered to execution time
    // invocation of this method means that we're ready to schedule Activity.Execute()
    internal void ScheduleBody(
        ActivityInstance activityInstance,
        bool requiresSymbolResolution,
        IDictionary<string, object> argumentValueOverrides,
        Location resultLocation)
    {
        if (resultLocation == null)
        {
            ExecuteActivityWorkItem workItem = ExecuteActivityWorkItemPool.Acquire();
            workItem.Initialize(activityInstance, requiresSymbolResolution, argumentValueOverrides);

            _scheduler.PushWork(workItem);
        }
        else
        {
            _scheduler.PushWork(new ExecuteExpressionWorkItem(activityInstance, requiresSymbolResolution, argumentValueOverrides, resultLocation));
        }
    }

    public NoPersistProperty CreateNoPersistProperty() => new(this);

    public AsyncOperationContext SetupAsyncOperationBlock(ActivityInstance owningActivity)
    {
        if (_activeOperations != null && _activeOperations.ContainsKey(owningActivity))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OnlyOneOperationPerActivity));
        }

        EnterNoPersist();

        AsyncOperationContext context = new(this, owningActivity);

        _activeOperations ??= new Dictionary<ActivityInstance, AsyncOperationContext>();
        _activeOperations.Add(owningActivity, context);

        return context;
    }

    // Must always be called from a workflow thread
    public void CompleteOperation(ActivityInstance owningInstance, BookmarkCallback callback, object state)
    {
        Fx.Assert(callback != null, "Use the other overload if callback is null.");

        CompleteAsyncOperationWorkItem workItem = new(
            new BookmarkCallbackWrapper(callback, owningInstance),
            _bookmarkManager.GenerateTempBookmark(),
            state);
        CompleteOperation(workItem);
    }

    // Must always be called from a workflow thread
    public void CompleteOperation(WorkItem asyncCompletionWorkItem)
    {
        _scheduler.EnqueueWork(asyncCompletionWorkItem);
        CompleteOperation(asyncCompletionWorkItem.ActivityInstance, false);
    }

    // Must always be called from a workflow thread
    public void CompleteOperation(ActivityInstance owningInstance) => CompleteOperation(owningInstance, true);

    private void CompleteOperation(ActivityInstance owningInstance, bool exitNoPersist)
    {
        Fx.Assert(owningInstance != null, "Cannot be called with a null instance.");
        Fx.Assert(_activeOperations.ContainsKey(owningInstance), "The owning instance must be in the list if we've gotten here.");

        _activeOperations.Remove(owningInstance);

        owningInstance.DecrementBusyCount();

        if (exitNoPersist)
        {
            ExitNoPersist();
        }
    }

    internal void AddHandle(Handle handleToAdd)
    {
        _handles ??= new List<Handle>();
        _handles.Add(handleToAdd);
    }

    [DataContract]
    internal class PersistenceWaiter
    {
        private Bookmark _onPersistBookmark;
        private ActivityInstance _waitingInstance;

        public PersistenceWaiter(Bookmark onPersist, ActivityInstance waitingInstance)
        {
            OnPersistBookmark = onPersist;
            WaitingInstance = waitingInstance;
        }

        public Bookmark OnPersistBookmark
        {
            get => _onPersistBookmark;
            private set => _onPersistBookmark = value;
        }

        public ActivityInstance WaitingInstance
        {
            get => _waitingInstance;
            private set => _waitingInstance = value;
        }

        [DataMember(Name = "OnPersistBookmark")]
        internal Bookmark SerializedOnPersistBookmark
        {
            get => OnPersistBookmark;
            set => OnPersistBookmark = value;
        }

        [DataMember(Name = "WaitingInstance")]
        internal ActivityInstance SerializedWaitingInstance
        {
            get => WaitingInstance;
            set => WaitingInstance = value;
        }

        public WorkItem CreateWorkItem() => new PersistWorkItem(this);

        [DataContract]
        internal class PersistWorkItem : WorkItem
        {
            private PersistenceWaiter _waiter;

            public PersistWorkItem(PersistenceWaiter waiter)
                : base(waiter.WaitingInstance)
            {
                _waiter = waiter;
            }

            public override bool IsValid => true;

            // Persist should not pick up user transaction / identity.
            public override ActivityInstance PropertyManagerOwner => null;

            [DataMember(Name = "waiter")]
            internal PersistenceWaiter SerializedWaiter
            {
                get => _waiter;
                set => _waiter = value;
            }

            public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

            public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

            public override void TraceStarting() => TraceRuntimeWorkItemStarting();

            public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
            {
                if (executor.TryResumeUserBookmark(_waiter.OnPersistBookmark, null, false) != BookmarkResumptionResult.Success)
                {
                    Fx.Assert("This should always be resumable.");
                }

                IAsyncResult result = null;

                try
                {
                    result = executor._host.OnBeginPersist(Fx.ThunkCallback(new AsyncCallback(OnPersistComplete)), executor);

                    if (result.CompletedSynchronously)
                    {
                        executor._host.OnEndPersist(result);
                    }
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    _workflowAbortException = e;
                }

                return result == null || result.CompletedSynchronously;
            }

            private void OnPersistComplete(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ActivityExecutor executor = (ActivityExecutor)result.AsyncState;

                try
                {
                    executor._host.OnEndPersist(result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    _workflowAbortException = e;
                }

                executor.FinishWorkItem(this);
            }

            public override void PostProcess(ActivityExecutor executor)
            {
                if (ExceptionToPropagate != null)
                {
                    executor.AbortActivityInstance(_waiter.WaitingInstance, ExceptionToPropagate);
                }
            }
        }
    }

    [DataContract]
    internal class TransactionContextWaiter
    {
        public TransactionContextWaiter(ActivityInstance instance, bool isRequires, RuntimeTransactionHandle handle, TransactionContextWaiterCallbackWrapper callbackWrapper, object state)
        {
            Fx.Assert(instance != null, "Must have an instance.");
            Fx.Assert(handle != null, "Must have a handle.");
            Fx.Assert(callbackWrapper != null, "Must have a callbackWrapper");

            WaitingInstance = instance;
            IsRequires = isRequires;
            Handle = handle;
            State = state;
            CallbackWrapper = callbackWrapper;
        }

        private ActivityInstance _waitingInstance;
        public ActivityInstance WaitingInstance
        {
            get => _waitingInstance;
            private set => _waitingInstance = value;
        }

        private bool _isRequires;
        public bool IsRequires
        {
            get => _isRequires;
            private set => _isRequires = value;
        }

        private RuntimeTransactionHandle _handle;
        public RuntimeTransactionHandle Handle
        {
            get => _handle;
            private set => _handle = value;
        }

        private object _state;
        public object State
        {
            get => _state;
            private set => _state = value;
        }

        private TransactionContextWaiterCallbackWrapper _callbackWrapper;
        public TransactionContextWaiterCallbackWrapper CallbackWrapper
        {
            get => _callbackWrapper;
            private set => _callbackWrapper = value;
        }

        [DataMember(Name = "WaitingInstance")]
        internal ActivityInstance SerializedWaitingInstance
        {
            get => WaitingInstance;
            set => WaitingInstance = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "IsRequires")]
        internal bool SerializedIsRequires
        {
            get => IsRequires;
            set => IsRequires = value;
        }

        [DataMember(Name = "Handle")]
        internal RuntimeTransactionHandle SerializedHandle
        {
            get => Handle;
            set => Handle = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "State")]
        internal object SerializedState
        {
            get => State;
            set => State = value;
        }

        [DataMember(Name = "CallbackWrapper")]
        internal TransactionContextWaiterCallbackWrapper SerializedCallbackWrapper
        {
            get => CallbackWrapper;
            set => CallbackWrapper = value;
        }
    }

    [DataContract]
    internal class TransactionContextWaiterCallbackWrapper : CallbackWrapper
    {
        private static readonly Type callbackType = typeof(Action<NativeActivityTransactionContext, object>);
        private static readonly Type[] transactionCallbackParameterTypes = new Type[] { typeof(NativeActivityTransactionContext), typeof(object) };

        public TransactionContextWaiterCallbackWrapper(Action<NativeActivityTransactionContext, object> action, ActivityInstance owningInstance)
            : base(action, owningInstance) { }

        public void Invoke(NativeActivityTransactionContext context, object value)
        {
            EnsureCallback(callbackType, transactionCallbackParameterTypes);
            Action<NativeActivityTransactionContext, object> callback = (Action<NativeActivityTransactionContext, object>)Callback;
            callback(context, value);
        }
    }

    // This class is not DataContract since we only create instances of it while we
    // are in no-persist zones
    private class RuntimeTransactionData
    {
        public RuntimeTransactionData(RuntimeTransactionHandle handle, Transaction transaction, ActivityInstance isolationScope)
        {
            TransactionHandle = handle;
            OriginalTransaction = transaction;
            ClonedTransaction = transaction.Clone();
            IsolationScope = isolationScope;
            TransactionStatus = TransactionStatus.Active;
        }

        public AsyncWaitHandle CompletionEvent { get; set; }

        public PreparingEnlistment PendingPreparingEnlistment { get; set; }

        public bool HasPrepared { get; set; }

        public bool ShouldScheduleCompletion { get; set; }

        public TransactionStatus TransactionStatus { get; set; }

        public bool IsRootCancelPending { get; set; }

        public RuntimeTransactionHandle TransactionHandle { get; private set; }

        public Transaction ClonedTransaction { get; private set; }

        public Transaction OriginalTransaction { get; private set; }

        public ActivityInstance IsolationScope { get; private set; }

        [Fx.Tag.Throws(typeof(Exception), "Doesn't handle any exceptions coming from Rollback.")]
        public void Rollback(Exception reason)
        {
            Fx.Assert(OriginalTransaction != null, "We always have an original transaction.");

            OriginalTransaction.Rollback(reason);
        }
    }

    private class AssociateKeysAsyncResult : TransactedAsyncResult
    {
        private static readonly AsyncCompletion associatedCallback = new(OnAssociated);
        private readonly ActivityExecutor _executor;

        public AssociateKeysAsyncResult(ActivityExecutor executor, ICollection<InstanceKey> keysToAssociate, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _executor = executor;

            IAsyncResult result;
            using (PrepareTransactionalCall(_executor.CurrentTransaction))
            {
                result = _executor._host.OnBeginAssociateKeys(keysToAssociate, PrepareAsyncCompletion(associatedCallback), this);
            }
            if (SyncContinue(result))
            {
                Complete(true);
            }
        }

        private static bool OnAssociated(IAsyncResult result)
        {
            AssociateKeysAsyncResult thisPtr = (AssociateKeysAsyncResult)result.AsyncState;
            thisPtr._executor._host.OnEndAssociateKeys(result);
            return true;
        }

        public static void End(IAsyncResult result) => End<AssociateKeysAsyncResult>(result);
    }

    private class PoolOfNativeActivityContexts : Pool<NativeActivityContext>
    {
        protected override NativeActivityContext CreateNew() => new();
    }

    private class PoolOfCodeActivityContexts : Pool<CodeActivityContext>
    {
        protected override CodeActivityContext CreateNew() => new();
    }

    //This is used in ScheduleDelegate when the handler is null. We use this dummy activity to 
    //set as the 'Activity' of the completed ActivityInstance.
    private class EmptyDelegateActivity : NativeActivity
    {
        internal EmptyDelegateActivity() { }

        protected override void Execute(NativeActivityContext context) => Fx.Assert(false, "This activity should never be executed. It is a dummy activity");
    }
}

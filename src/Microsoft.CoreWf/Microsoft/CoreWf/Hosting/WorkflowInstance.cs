// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Runtime.DurableInstancing;
using CoreWf.Tracking;
using CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace CoreWf.Hosting
{
    [Fx.Tag.XamlVisible(false)]
    public abstract class WorkflowInstance
    {
        private static readonly IDictionary<string, LocationInfo> s_emptyMappedVariablesDictionary = new ReadOnlyDictionary<string, LocationInfo>(new Dictionary<string, LocationInfo>(0));

        private const int True = 1;
        private const int False = 0;

        private WorkflowInstanceControl _controller;
        private TrackingProvider _trackingProvider;
        private SynchronizationContext _syncContext;
        private LocationReferenceEnvironment _hostEnvironment;
        private ActivityExecutor _executor;
        private int _isPerformingOperation;
        private bool _isInitialized;
        private WorkflowInstanceExtensionCollection _extensions;

        // Tracking for one-time actions per in-memory instance
        private bool _hasTrackedResumed;
        private bool _hasTrackedCompletion;

        private bool _isAborted;
        private Exception _abortedException;

        //#if DEBUG
        //        StackTrace abortStack;
        //#endif

        protected WorkflowInstance(Activity workflowDefinition)
            : this(workflowDefinition, null)
        {
        }

        protected WorkflowInstance(Activity workflowDefinition, WorkflowIdentity definitionIdentity)
        {
            if (workflowDefinition == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("workflowDefinition");
            }

            this.WorkflowDefinition = workflowDefinition;
            this.DefinitionIdentity = definitionIdentity;
        }

        public abstract Guid Id
        {
            get;
        }

        internal bool HasTrackingParticipant
        {
            get;
            private set;
        }

        internal bool HasTrackedStarted
        {
            get;
            private set;
        }

        internal bool HasPersistenceModule
        {
            get;
            private set;
        }

        public SynchronizationContext SynchronizationContext
        {
            get
            {
                return _syncContext;
            }
            set
            {
                ThrowIfReadOnly();
                _syncContext = value;
            }
        }

        public LocationReferenceEnvironment HostEnvironment
        {
            get
            {
                return _hostEnvironment;
            }
            set
            {
                ThrowIfReadOnly();
                _hostEnvironment = value;
            }
        }

        public Activity WorkflowDefinition
        {
            get;
            private set;
        }

        public WorkflowIdentity DefinitionIdentity
        {
            get;
            private set;
        }

        protected bool IsReadOnly
        {
            get
            {
                return _isInitialized;
            }
        }

        protected internal abstract bool SupportsInstanceKeys
        {
            get;
        }

        // this is going away
        internal TrackingProvider TrackingProvider
        {
            get
            {
                Fx.Assert(HasTrackingParticipant, "we should only be called if we have a tracking participant");
                return _trackingProvider;
            }
        }

        protected WorkflowInstanceControl Controller
        {
            get
            {
                if (!_isInitialized)
                {
                    throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ControllerInvalidBeforeInitialize));
                }

                return _controller;
            }
        }

        // host-facing access to our cascading ExtensionManager resolution
        protected internal T GetExtension<T>() where T : class
        {
            if (_extensions != null)
            {
                return _extensions.Find<T>();
            }
            else
            {
                return default(T);
            }
        }

        protected internal IEnumerable<T> GetExtensions<T>() where T : class
        {
            if (_extensions != null)
            {
                return _extensions.FindAll<T>();
            }
            else
            {
                return new T[0];
            }
        }

        // locks down the given extensions manager and runs cache metadata on the workflow definition
        protected void RegisterExtensionManager(WorkflowInstanceExtensionManager extensionManager)
        {
            ValidateWorkflow(extensionManager);
            _extensions = WorkflowInstanceExtensionManager.CreateInstanceExtensions(this.WorkflowDefinition, extensionManager);
            if (_extensions != null)
            {
                this.HasPersistenceModule = _extensions.HasPersistenceModule;
            }
        }

        // dispose the extensions that implement IDisposable
        protected void DisposeExtensions()
        {
            if (_extensions != null)
            {
                _extensions.Dispose();
                _extensions = null;
            }
        }

        //protected static IList<ActivityBlockingUpdate> GetActivitiesBlockingUpdate(object deserializedRuntimeState, DynamicUpdateMap updateMap)
        //{
        //    ActivityExecutor executor = deserializedRuntimeState as ActivityExecutor;
        //    if (executor == null)
        //    {
        //        throw CoreWf.Internals.FxTrace.Exception.Argument("deserializedRuntimeState", SR.InvalidRuntimeState);
        //    }
        //    if (updateMap == null)
        //    {
        //        throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("updateMap");
        //    }

        //    DynamicUpdateMap rootMap = updateMap;
        //    if (updateMap.IsForImplementation)
        //    {
        //        rootMap = updateMap.AsRootMap();
        //    }
        //    IList<ActivityBlockingUpdate> result = executor.GetActivitiesBlockingUpdate(rootMap);
        //    if (result == null)
        //    {
        //        result = new List<ActivityBlockingUpdate>();
        //    }

        //    return result;
        //}

        // used for Create scenarios where you are providing root information
        protected void Initialize(IDictionary<string, object> workflowArgumentValues, IList<Handle> workflowExecutionProperties)
        {
            ThrowIfAborted();
            ThrowIfReadOnly();
            _executor = new ActivityExecutor(this);

            EnsureDefinitionReady();
            // workflowArgumentValues signals whether we are a new or loaded instance, so we can't pass in null.
            // workflowExecutionProperties is allowed to be null
            InitializeCore(workflowArgumentValues ?? ActivityUtilities.EmptyParameters, workflowExecutionProperties);
        }

        // used for Load scenarios where you are rehydrating a WorkflowInstance
        protected void Initialize(object deserializedRuntimeState)
        {
            //    Initialize(deserializedRuntimeState, null);
            //}        

            //protected void Initialize(object deserializedRuntimeState, DynamicUpdateMap updateMap)
            //{
            ThrowIfAborted();
            ThrowIfReadOnly();
            _executor = deserializedRuntimeState as ActivityExecutor;

            if (_executor == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument("deserializedRuntimeState", SR.InvalidRuntimeState);
            }
            _executor.ThrowIfNonSerializable();

            EnsureDefinitionReady();

            WorkflowIdentity originalDefinitionIdentity = _executor.WorkflowIdentity;
            //bool success = false;
            //Collection<ActivityBlockingUpdate> updateErrors = null;
            //try
            //{
            //if (updateMap != null)
            //{
            //    // check if map is for implementaiton,                    
            //    if (updateMap.IsForImplementation)
            //    {
            //        // if so, the definition root must be an activity 
            //        // with no public/imported children and no public/imported delegates.
            //        if (DynamicUpdateMap.CanUseImplementationMapAsRoot(this.WorkflowDefinition))
            //        {
            //            updateMap = updateMap.AsRootMap();
            //        }
            //        else
            //        {
            //            throw CoreWf.Internals.FxTrace.Exception.AsError(new InstanceUpdateException(SR.InvalidImplementationAsWorkflowRoot));
            //        }
            //    }

            //    updateMap.ThrowIfInvalid(this.WorkflowDefinition);

            //    this.executor.WorkflowIdentity = this.DefinitionIdentity;

            //    this.executor.UpdateInstancePhase1(updateMap, this.WorkflowDefinition, ref updateErrors);
            //    ThrowIfDynamicUpdateErrorExists(updateErrors);
            //}

            InitializeCore(null, null);

            //if (updateMap != null)
            //{
            //    this.executor.UpdateInstancePhase2(updateMap, ref updateErrors);
            //    ThrowIfDynamicUpdateErrorExists(updateErrors);
            //    // Track that dynamic update is successful
            //    if (this.Controller.TrackingEnabled)
            //    {
            //        this.Controller.Track(new WorkflowInstanceUpdatedRecord(this.Id, this.WorkflowDefinition.DisplayName, originalDefinitionIdentity, this.executor.WorkflowIdentity));
            //    }
            //}

            //success = true;
            //}
            //catch (InstanceUpdateException updateException)
            //{
            //    // Can't track through the controller because initialization failed
            //    if (this.HasTrackingParticipant && this.TrackingProvider.ShouldTrackWorkflowInstanceRecords)
            //    {
            //        IList<ActivityBlockingUpdate> blockingActivities = updateException.BlockingActivities;
            //        if (blockingActivities.Count == 0)
            //        {
            //            blockingActivities = new List<ActivityBlockingUpdate>
            //            {
            //                new ActivityBlockingUpdate(this.WorkflowDefinition, this.WorkflowDefinition.Id, updateException.Message)
            //            }.AsReadOnly();
            //        }
            //        this.TrackingProvider.AddRecord(new WorkflowInstanceUpdatedRecord(this.Id, this.WorkflowDefinition.DisplayName, originalDefinitionIdentity, this.DefinitionIdentity, blockingActivities));
            //    }
            //    throw;
            //}
            //finally
            //{
            //if (updateMap != null && !success)
            //{
            //    executor.MakeNonSerializable();
            //}
            //}            
        }

        //void ThrowIfDynamicUpdateErrorExists(Collection<ActivityBlockingUpdate> updateErrors)
        //{
        //    if (updateErrors != null && updateErrors.Count > 0)
        //    {
        //        // update error found
        //        // exit early

        //        throw CoreWf.Internals.FxTrace.Exception.AsError(new InstanceUpdateException(updateErrors));
        //    }
        //}

        private void ValidateWorkflow(WorkflowInstanceExtensionManager extensionManager)
        {
            if (!WorkflowDefinition.IsRuntimeReady)
            {
                LocationReferenceEnvironment localEnvironment = _hostEnvironment;
                if (localEnvironment == null)
                {
                    LocationReferenceEnvironment parentEnvironment = null;
                    if (extensionManager != null && extensionManager.SymbolResolver != null)
                    {
                        parentEnvironment = extensionManager.SymbolResolver.AsLocationReferenceEnvironment();
                    }
                    localEnvironment = new ActivityLocationReferenceEnvironment(parentEnvironment);
                }
                IList<ValidationError> validationErrors = null;
                ActivityUtilities.CacheRootMetadata(WorkflowDefinition, localEnvironment, ProcessActivityTreeOptions.FullCachingOptions, null, ref validationErrors);
                ActivityValidationServices.ThrowIfViolationsExist(validationErrors);
            }
        }

        private void EnsureDefinitionReady()
        {
            if (_extensions != null)
            {
                _extensions.Initialize();
                if (_extensions.HasTrackingParticipant)
                {
                    this.HasTrackingParticipant = true;
                    if (_trackingProvider == null)
                    {
                        _trackingProvider = new TrackingProvider(this.WorkflowDefinition);
                    }
                    else
                    {
                        // TrackingProvider could be non-null if an earlier initialization attempt failed.
                        // This happens when WorkflowApplication calls Abort after a load failure. In this
                        // case we want to preserve any pending tracking records (e.g. DU failure).
                        _trackingProvider.ClearParticipants();
                    }
                    foreach (TrackingParticipant trackingParticipant in GetExtensions<TrackingParticipant>())
                    {
                        _trackingProvider.AddParticipant(trackingParticipant);
                    }
                }
            }
            else
            {
                // need to ensure the workflow has been validated since the host isn't using extensions (and so didn't register anything)
                ValidateWorkflow(null);
            }
        }

        private void InitializeCore(IDictionary<string, object> workflowArgumentValues, IList<Handle> workflowExecutionProperties)
        {
            Fx.Assert(this.WorkflowDefinition.IsRuntimeReady, "EnsureDefinitionReady should have been called");
            Fx.Assert(_executor != null, "at this point, we better have an executor");

            // Do Argument validation for root activities
            WorkflowDefinition.HasBeenAssociatedWithAnInstance = true;

            if (workflowArgumentValues != null)
            {
                IDictionary<string, object> actualInputs = workflowArgumentValues;

                if (object.ReferenceEquals(actualInputs, ActivityUtilities.EmptyParameters))
                {
                    actualInputs = null;
                }

                if (this.WorkflowDefinition.RuntimeArguments.Count > 0 || (actualInputs != null && actualInputs.Count > 0))
                {
                    ActivityValidationServices.ValidateRootInputs(this.WorkflowDefinition, actualInputs);
                }

                _executor.ScheduleRootActivity(this.WorkflowDefinition, actualInputs, workflowExecutionProperties);
            }
            else
            {
                _executor.OnDeserialized(this.WorkflowDefinition, this);
            }

            _executor.Open(this.SynchronizationContext);
            _controller = new WorkflowInstanceControl(this, _executor);
            _isInitialized = true;

            if (_extensions != null && _extensions.HasWorkflowInstanceExtensions)
            {
                WorkflowInstanceProxy proxy = new WorkflowInstanceProxy(this);

                for (int i = 0; i < _extensions.WorkflowInstanceExtensions.Count; i++)
                {
                    IWorkflowInstanceExtension extension = _extensions.WorkflowInstanceExtensions[i];
                    extension.SetInstance(proxy);
                }
            }
        }

        protected void ThrowIfReadOnly()
        {
            if (_isInitialized)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowInstanceIsReadOnly(this.Id)));
            }
        }

        protected internal abstract IAsyncResult OnBeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state);
        protected internal abstract BookmarkResumptionResult OnEndResumeBookmark(IAsyncResult result);

        protected internal abstract IAsyncResult OnBeginPersist(AsyncCallback callback, object state);
        protected internal abstract void OnEndPersist(IAsyncResult result);

        protected internal abstract void OnDisassociateKeys(ICollection<InstanceKey> keys);

        protected internal abstract IAsyncResult OnBeginAssociateKeys(ICollection<InstanceKey> keys, AsyncCallback callback, object state);
        protected internal abstract void OnEndAssociateKeys(IAsyncResult result);

        internal IAsyncResult BeginFlushTrackingRecordsInternal(AsyncCallback callback, object state)
        {
            return OnBeginFlushTrackingRecords(callback, state);
        }

        internal void EndFlushTrackingRecordsInternal(IAsyncResult result)
        {
            OnEndFlushTrackingRecords(result);
        }

        protected void FlushTrackingRecords(TimeSpan timeout)
        {
            if (this.HasTrackingParticipant)
            {
                this.TrackingProvider.FlushPendingRecords(timeout);
            }
        }

        protected IAsyncResult BeginFlushTrackingRecords(TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (this.HasTrackingParticipant)
            {
                return this.TrackingProvider.BeginFlushPendingRecords(timeout, callback, state);
            }
            else
            {
                return new CompletedAsyncResult(callback, state);
            }
        }

        protected void EndFlushTrackingRecords(IAsyncResult result)
        {
            if (this.HasTrackingParticipant)
            {
                this.TrackingProvider.EndFlushPendingRecords(result);
            }
            else
            {
                CompletedAsyncResult.End(result);
            }
        }

        protected virtual IAsyncResult OnBeginFlushTrackingRecords(AsyncCallback callback, object state)
        {
            return this.Controller.BeginFlushTrackingRecords(ActivityDefaults.TrackingTimeout, callback, state);
        }

        protected virtual void OnEndFlushTrackingRecords(IAsyncResult result)
        {
            this.Controller.EndFlushTrackingRecords(result);
        }

        internal void NotifyPaused()
        {
            if (_executor.State != ActivityInstanceState.Executing)
            {
                TrackCompletion();
            }

            OnNotifyPaused();
        }

        protected abstract void OnNotifyPaused();

        internal void NotifyUnhandledException(Exception exception, Activity source, string sourceInstanceId)
        {
            if (_controller.TrackingEnabled)
            {
                ActivityInfo faultSourceInfo = new ActivityInfo(source.DisplayName, source.Id, sourceInstanceId, source.GetType().FullName);
                _controller.Track(new WorkflowInstanceUnhandledExceptionRecord(this.Id, this.WorkflowDefinition.DisplayName, faultSourceInfo, exception, this.DefinitionIdentity));
            }

            OnNotifyUnhandledException(exception, source, sourceInstanceId);
        }

        protected abstract void OnNotifyUnhandledException(Exception exception, Activity source, string sourceInstanceId);

        protected internal abstract void OnRequestAbort(Exception reason);

        internal void OnDeserialized(bool hasTrackedStarted)
        {
            this.HasTrackedStarted = hasTrackedStarted;
        }

        private void StartOperation(ref bool resetRequired)
        {
            StartReadOnlyOperation(ref resetRequired);

            // isRunning can only flip to true by an operation and therefore
            // we don't have to worry about this changing under us
            if (_executor.IsRunning)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeRunning));
            }
        }

        private void StartReadOnlyOperation(ref bool resetRequired)
        {
            bool wasPerformingOperation = false;
            try
            {
            }
            finally
            {
                wasPerformingOperation = Interlocked.CompareExchange(ref _isPerformingOperation, True, False) == True;

                if (!wasPerformingOperation)
                {
                    resetRequired = true;
                }
            }

            if (wasPerformingOperation)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeOperationInProgress));
            }
        }

        private void FinishOperation(ref bool resetRequired)
        {
            if (resetRequired)
            {
                _isPerformingOperation = False;
            }
        }

        internal void Abort(Exception reason)
        {
            if (!_isAborted)
            {
                _isAborted = true;
                if (reason != null)
                {
                    _abortedException = reason;
                }

                if (_extensions != null)
                {
                    _extensions.Cancel();
                }

                if (_controller.TrackingEnabled)
                {
                    // During abort we only track this one record
                    if (reason != null)
                    {
                        string message = reason.Message;
                        if (reason.InnerException != null)
                        {
                            message = SR.WorkflowAbortedReason(reason.Message, reason.InnerException.Message);
                        }
                        _controller.Track(new WorkflowInstanceAbortedRecord(this.Id, this.WorkflowDefinition.DisplayName, message, this.DefinitionIdentity));
                    }
                }
                //#if DEBUG
                //                if (!Fx.FastDebug)
                //                {
                //                    if (reason != null)
                //                    {
                //                        reason.ToString();
                //                    }
                //                    this.abortStack = new StackTrace();
                //                }
                //#endif
            }
        }

        private void ValidatePrepareForSerialization()
        {
            ThrowIfAborted();
            if (!this.Controller.IsPersistable)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.PrepareForSerializationRequiresPersistability));
            }
        }

        private void ValidateScheduleResumeBookmark()
        {
            ThrowIfAborted();
            ThrowIfNotIdle();
        }

        private void ValidateGetBookmarks()
        {
            ThrowIfAborted();
        }

        private void ValidateGetMappedVariables()
        {
            ThrowIfAborted();
        }

        private void ValidatePauseWhenPersistable()
        {
            ThrowIfAborted();
            if (this.Controller.IsPersistable)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.PauseWhenPersistableInvalidIfPersistable));
            }
        }

        private void Terminate(Exception reason)
        {
            // validate we're in an ok state
            ThrowIfAborted();

            // terminate the runtime
            _executor.Terminate(reason);

            // and track if necessary
            TrackCompletion();
        }

        private void TrackCompletion()
        {
            if (_controller.TrackingEnabled && !_hasTrackedCompletion)
            {
                ActivityInstanceState completionState = _executor.State;

                if (completionState == ActivityInstanceState.Faulted)
                {
                    Fx.Assert(_executor.TerminationException != null, "must have a termination exception if we're faulted");
                    _controller.Track(new WorkflowInstanceTerminatedRecord(this.Id, this.WorkflowDefinition.DisplayName, _executor.TerminationException.Message, this.DefinitionIdentity));
                }
                else if (completionState == ActivityInstanceState.Closed)
                {
                    _controller.Track(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Completed, this.DefinitionIdentity));
                }
                else
                {
                    Fx.AssertAndThrow(completionState == ActivityInstanceState.Canceled, "Cannot be executing a workflow instance when WorkflowState was completed.");
                    _controller.Track(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Canceled, this.DefinitionIdentity));
                }
                _hasTrackedCompletion = true;
            }
        }

        private void TrackResumed()
        {
            // track if necessary
            if (!_hasTrackedResumed)
            {
                if (this.Controller.TrackingEnabled)
                {
                    if (!this.HasTrackedStarted)
                    {
                        this.TrackingProvider.AddRecord(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Started, this.DefinitionIdentity));
                        this.HasTrackedStarted = true;
                    }
                    else
                    {
                        this.TrackingProvider.AddRecord(new WorkflowInstanceRecord(this.Id, this.WorkflowDefinition.DisplayName, WorkflowInstanceStates.Resumed, this.DefinitionIdentity));
                    }
                }
                _hasTrackedResumed = true;
            }
        }

        private void Run()
        {
            // validate we're in an ok state
            ThrowIfAborted();

            TrackResumed();

            // and let the scheduler go
            _executor.MarkSchedulerRunning();
        }

        private void ScheduleCancel()
        {
            // validate we're in an ok state
            ThrowIfAborted();

            TrackResumed();

            _executor.CancelRootActivity();
        }

        private BookmarkResumptionResult ScheduleBookmarkResumption(Bookmark bookmark, object value)
        {
            // validate we're in an ok state
            ValidateScheduleResumeBookmark();

            TrackResumed();

            return _executor.TryResumeHostBookmark(bookmark, value);
        }

        private BookmarkResumptionResult ScheduleBookmarkResumption(Bookmark bookmark, object value, BookmarkScope scope)
        {
            // validate we're in an ok state
            ValidateScheduleResumeBookmark();

            TrackResumed();

            return _executor.TryResumeBookmark(bookmark, value, scope);
        }


        private void ThrowIfAborted()
        {
            if (_isAborted || (_executor != null && _executor.IsAbortPending))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WorkflowInstanceAborted(this.Id)));
            }
        }

        private void ThrowIfNotIdle()
        {
            if (!_executor.IsIdle)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarksOnlyResumableWhileIdle));
            }
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.NestedTypesShouldNotBeVisible,
        //Justification = "these are effectively protected methods, but encapsulated in a struct to avoid naming conflicts")]
        protected struct WorkflowInstanceControl
        {
            private ActivityExecutor _executor;
            private WorkflowInstance _instance;

            internal WorkflowInstanceControl(WorkflowInstance instance, ActivityExecutor executor)
            {
                _instance = instance;
                _executor = executor;
            }

            public bool IsPersistable
            {
                get
                {
                    return _executor.IsPersistable;
                }
            }

            public bool HasPendingTrackingRecords
            {
                get
                {
                    return _instance.HasTrackingParticipant && _instance.TrackingProvider.HasPendingRecords;
                }
            }

            public bool TrackingEnabled
            {
                get
                {
                    return _instance.HasTrackingParticipant && _instance.TrackingProvider.ShouldTrackWorkflowInstanceRecords;
                }
            }

            public WorkflowInstanceState State
            {
                get
                {
                    WorkflowInstanceState result;

                    if (_instance._isAborted)
                    {
                        result = WorkflowInstanceState.Aborted;
                    }
                    else if (!_executor.IsIdle)
                    {
                        result = WorkflowInstanceState.Runnable;
                    }
                    else
                    {
                        if (_executor.State == ActivityInstanceState.Executing)
                        {
                            result = WorkflowInstanceState.Idle;
                        }
                        else
                        {
                            result = WorkflowInstanceState.Complete;
                        }
                    }

                    return result;
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is WorkflowInstanceControl))
                {
                    return false;
                }

                WorkflowInstanceControl other = (WorkflowInstanceControl)obj;
                return other._instance == _instance;
            }

            public override int GetHashCode()
            {
                return _instance.GetHashCode();
            }

            public static bool operator ==(WorkflowInstanceControl left, WorkflowInstanceControl right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(WorkflowInstanceControl left, WorkflowInstanceControl right)
            {
                return !left.Equals(right);
            }

            public ReadOnlyCollection<BookmarkInfo> GetBookmarks()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartReadOnlyOperation(ref resetRequired);

                    _instance.ValidateGetBookmarks();

                    return _executor.GetAllBookmarks();
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public ReadOnlyCollection<BookmarkInfo> GetBookmarks(BookmarkScope scope)
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartReadOnlyOperation(ref resetRequired);

                    _instance.ValidateGetBookmarks();

                    return _executor.GetBookmarks(scope);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public IDictionary<string, LocationInfo> GetMappedVariables()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartReadOnlyOperation(ref resetRequired);

                    _instance.ValidateGetMappedVariables();

                    IDictionary<string, LocationInfo> mappedLocations = _instance._executor.GatherMappableVariables();
                    if (mappedLocations != null)
                    {
                        mappedLocations = new ReadOnlyDictionary<string, LocationInfo>(mappedLocations);
                    }
                    else
                    {
                        mappedLocations = WorkflowInstance.s_emptyMappedVariablesDictionary;
                    }
                    return mappedLocations;
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public void Run()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    _instance.Run();
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }

                _executor.Run();
            }

            public void RequestPause()
            {
                // No validations for this because we do not
                // require calls to Pause to be synchronized
                // by the caller
                _executor.PauseScheduler();
            }

            // Calls Pause when IsPersistable goes from false->true
            public void PauseWhenPersistable()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    _instance.ValidatePauseWhenPersistable();

                    _executor.PauseWhenPersistable();
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public void ScheduleCancel()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    _instance.ScheduleCancel();
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public void Terminate(Exception reason)
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    _instance.Terminate(reason);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public BookmarkResumptionResult ScheduleBookmarkResumption(Bookmark bookmark, object value)
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    return _instance.ScheduleBookmarkResumption(bookmark, value);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public BookmarkResumptionResult ScheduleBookmarkResumption(Bookmark bookmark, object value, BookmarkScope scope)
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    return _instance.ScheduleBookmarkResumption(bookmark, value, scope);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public void Abort()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    // No validations

                    _executor.Dispose();

                    _instance.Abort(null);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public void Abort(Exception reason)
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartOperation(ref resetRequired);

                    // No validations

                    _executor.Abort(reason);

                    _instance.Abort(reason);
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
            //Justification = "Only want to allow WorkflowInstanceRecord subclasses for WorkflowInstance-level tracking")]
            public void Track(WorkflowInstanceRecord instanceRecord)
            {
                if (_instance.HasTrackingParticipant)
                {
                    _instance.TrackingProvider.AddRecord(instanceRecord);
                }
            }

            public void FlushTrackingRecords(TimeSpan timeout)
            {
                _instance.FlushTrackingRecords(timeout);
            }

            public IAsyncResult BeginFlushTrackingRecords(TimeSpan timeout, AsyncCallback callback, object state)
            {
                return _instance.BeginFlushTrackingRecords(timeout, callback, state);
            }

            public void EndFlushTrackingRecords(IAsyncResult result)
            {
                _instance.EndFlushTrackingRecords(result);
            }

            public object PrepareForSerialization()
            {
                bool resetRequired = false;

                try
                {
                    _instance.StartReadOnlyOperation(ref resetRequired);

                    _instance.ValidatePrepareForSerialization();

                    return _executor.PrepareForSerialization();
                }
                finally
                {
                    _instance.FinishOperation(ref resetRequired);
                }
            }

            public ActivityInstanceState GetCompletionState()
            {
                return _executor.State;
            }

            //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
            //Justification = "Arch approved design. Requires the out argument for extra information provided")]
            public ActivityInstanceState GetCompletionState(out Exception terminationException)
            {
                terminationException = _executor.TerminationException;
                return _executor.State;
            }

            //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
            //Justification = "Arch approved design. Requires the out argument for extra information provided")]
            public ActivityInstanceState GetCompletionState(out IDictionary<string, object> outputs, out Exception terminationException)
            {
                outputs = _executor.WorkflowOutputs;
                terminationException = _executor.TerminationException;
                return _executor.State;
            }

            public Exception GetAbortReason()
            {
                return _instance._abortedException;
            }
        }
    }
}

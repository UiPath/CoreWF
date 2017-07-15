// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf;
using CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml.Linq;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Tracking;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Runtime
{
    public class TestWorkflowRuntime : IDisposable
    {
        #region Fields

        private bool _usingExpectedWorkflowInstanceTrace;
        private TestActivity _hostActivity;
        private RemoteWorkflowRuntime _remoteWorkflowRuntime;
        private TestWorkflowRuntimeConfiguration _testWorkflowRuntimeConfiguration;
        private OrderedTraces _workflowInstanceTrace;
        private WorkflowInstanceAction _previousAction;
        private WorkflowIdentity _previousIdentity;
        #endregion

        #region Constructors

        internal TestWorkflowRuntime(TestActivity activity, InstanceStore instanceStore = null, PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            Initialize(activity, null, null, instanceStore, idleAction);
        }

        internal TestWorkflowRuntime(TestActivity activity, WorkflowIdentity identity, InstanceStore instanceStore = null, PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            Initialize(activity, identity, null, instanceStore, idleAction);
        }

        internal TestWorkflowRuntime(TestActivity activity, TestWorkflowRuntimeConfiguration testWorkflowRuntimeConfiguration, InstanceStore instanceStore = null,
            PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            if (testWorkflowRuntimeConfiguration == null)
            {
                throw new ArgumentNullException("testWorkflowRuntimeConfiguration");
            }
            Initialize(activity, null, testWorkflowRuntimeConfiguration, instanceStore, idleAction);
        }

        #endregion

        #region Properties

        public CoreWf.Hosting.WorkflowInstanceExtensionManager Extensions
        {
            get { return _remoteWorkflowRuntime.Extensions; }
        }

        public ActualTrace ActualTrace
        {
            get
            {
                return _remoteWorkflowRuntime.ActualTrace;
            }
        }

        public Dictionary<string, object> Inputs
        {
            set { _remoteWorkflowRuntime.Inputs = value; }
        }

        public IDictionary<string, object> Outputs
        {
            get { return _remoteWorkflowRuntime.Outputs; }
        }

        public Type PersistenceProviderFactoryType
        {
            get { return _remoteWorkflowRuntime.InstanceStoreType; }
            set { _remoteWorkflowRuntime.InstanceStoreType = value; }
        }

        public bool SetDefaultOwner
        {
            get { return _remoteWorkflowRuntime.SetDefaultOwner; }
            set { _remoteWorkflowRuntime.SetDefaultOwner = value; }
        }

        public TestSymbolResolver TestSymbolResolver
        {
            get { return _remoteWorkflowRuntime.TestSymbolResolver; }
            set { _remoteWorkflowRuntime.TestSymbolResolver = value; }
        }

        public Type WorkflowRuntimeAdapterType
        {
            get { return _remoteWorkflowRuntime.WorkflowRuntimeAdapterType; }
            set { _remoteWorkflowRuntime.WorkflowRuntimeAdapterType = value; }
        }

        public TimeSpan WaitForCompletionTimeout
        {
            get { return _remoteWorkflowRuntime.WaitForCompletionTimeout; }
            set { _remoteWorkflowRuntime.WaitForCompletionTimeout = value; }
        }

        public TimeSpan WaitForIdleTimeout
        {
            get { return _remoteWorkflowRuntime.WaitForIdleTimeout; }
            set { _remoteWorkflowRuntime.WaitForIdleTimeout = value; }
        }

        public Guid CurrentWorkflowInstanceId
        {
            get
            {
                return _remoteWorkflowRuntime.CurrentWorkflowInstanceId;
            }
        }

        //Be cautious on using this property as your test will not be able to run under partial trust or multi-machine scenarios
        public RemoteWorkflowRuntime Unsafe_RemoteWorkflowRuntime
        {
            get
            {
                return _remoteWorkflowRuntime;
            }
        }
        #endregion

        #region Events
        public event EventHandler<TestWorkflowAbortedEventArgs> OnWorkflowAborted
        {
            add
            {
                this.CheckIfMultipleDelegates(1);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowAborted += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowAborted -= value;
            }
        }

        public event EventHandler<TestWorkflowUnloadedEventArgs> OnWorkflowUnloaded
        {
            add
            {
                this.CheckIfMultipleDelegates(1);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowUnloaded += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowUnloaded -= value;
            }
        }

        public event EventHandler<TestWorkflowIdleEventArgs> OnWorkflowIdle
        {
            add
            {
                this.CheckIfMultipleDelegates(2);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowIdle += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowIdle -= value;
            }
        }

        public event EventHandler<TestWorkflowCompletedEventArgs> OnWorkflowCompleted
        {
            add
            {
                this.CheckIfMultipleDelegates(3);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowCompleted += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowCompleted -= value;
            }
        }

        public event EventHandler<TestWorkflowUnhandledExceptionEventArgs> OnWorkflowUnhandledException
        {
            add
            {
                this.CheckIfMultipleDelegates(4);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowUnhandledException += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowUnhandledException -= value;
            }
        }

        public event EventHandler<TestWorkflowIdleAndPersistableEventArgs> OnWorkflowIdleAndPersistable
        {
            add
            {
                this.CheckIfMultipleDelegates(5);
                this.CheckIfStaticDelegate(value);
                _remoteWorkflowRuntime.OnWorkflowIdleAndPersistable += value;
            }
            remove
            {
                _remoteWorkflowRuntime.OnWorkflowIdleAndPersistable -= value;
            }
        }

        #endregion

        #region Public API
        public ActualTrace ActualTrackingData()
        {
            string trackingParticipantName = string.Empty;
            //foreach (TrackingConfiguration config in TestConfiguration.Current.TrackingServiceConfigurations)
            //{
            //    if (config.TrackingParticipantType == TrackingParticipantType.InMemoryTrackingParticipant)
            //    {
            //        trackingParticipantName = config.TrackingParticipantName;
            //        break;
            //    }
            //}
            return ActualTrackingData(trackingParticipantName);
        }

        public ActualTrace ActualTrackingData(string trackingParticipantName)
        {
            return _remoteWorkflowRuntime.ActualTrackingData(trackingParticipantName);
        }

        public void CreateWorkflow()
        {
            _remoteWorkflowRuntime.CreateWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.Initialized);
        }

        public void ExecuteWorkflow()
        {
            _remoteWorkflowRuntime.ExecuteWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.Initialized);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
        }

        public void ExecuteWorkflowInvoker(Dictionary<string, object> inputs, Dictionary<string, object> expectedOutputs, ICollection<object> extensions)
        {
            this.Inputs = inputs;
            _remoteWorkflowRuntime.ExecuteWorkflowInvoker(extensions);
            this.ValidateOutputs(expectedOutputs);
        }

        public void ExecuteWorkflowInvoker(Dictionary<string, object> inputs, Dictionary<string, object> expectedOutputs, ICollection<object> extensions, TimeSpan invokeTimeout)
        {
            this.Inputs = inputs;
            _remoteWorkflowRuntime.ExecuteWorkflowInvoker(extensions, invokeTimeout);
            this.ValidateOutputs(expectedOutputs);
        }

        public BookmarkResumptionResult ResumeBookMark(string bookMarkName, Object value)
        {
            BookmarkResumptionResult result = _remoteWorkflowRuntime.ResumeBookMark(bookMarkName, value);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
            return result;
        }

        public BookmarkResumptionResult ResumeBookMark(string bookMarkName, Object value, TimeSpan timeout)
        {
            BookmarkResumptionResult result = _remoteWorkflowRuntime.ResumeBookMark(bookMarkName, value, timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
            return result;
        }

        public TestWorkflowRuntimeAsyncResult BeginResumeBookMark(string bookMarkName, Object value, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginResumeBookMark(bookMarkName, value, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginResumeBookMark(string bookMarkName, Object value, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginResumeBookMark(bookMarkName, value, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public BookmarkResumptionResult EndResumeBookMark(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            BookmarkResumptionResult result = _remoteWorkflowRuntime.EndResumeBookMark(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
            return result;
        }

        public void ResumeWorkflow()
        {
            _remoteWorkflowRuntime.ResumeWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
        }

        public void ResumeWorkflow(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.ResumeWorkflow(timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginResumeWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginResumeWorkflow(timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginResumeWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginResumeWorkflow(asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndResumeWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndResumeWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.ResumedWorkflow);
        }

        public void PersistWorkflow()
        {
            _remoteWorkflowRuntime.PersistWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.PersistedWorkflow);
        }

        public void PersistWorkflow(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.PersistWorkflow(timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.PersistedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginPersistWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginPersistWorkflow(timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginPersistWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginPersistWorkflow(asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndPersistWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndPersistWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.PersistedWorkflow);
        }

        public void LoadAndUpdateRunnableWorkflow(string newDefinitionIdentity, TimeSpan timeout, bool hintUpdatePass = true, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.UpdateRunnableWorkflow(newDefinitionIdentity, timeout, false, extensionTypes);
            WorkflowInstanceAction action = hintUpdatePass == true ? WorkflowInstanceAction.UpdatedWorkflow : WorkflowInstanceAction.UpdateFailedWorkflow;
            UpdateExpectedWorkflowInstanceState(action);
        }

        public void LoadAndUpdateRunnableWorkflow(string newDefinitionIdentity, bool hintUpdatePass = true, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.UpdateRunnableWorkflow(newDefinitionIdentity, TimeSpan.Zero, true, extensionTypes);
            WorkflowInstanceAction action = hintUpdatePass == true ? WorkflowInstanceAction.UpdatedWorkflow : WorkflowInstanceAction.UpdateFailedWorkflow;
            UpdateExpectedWorkflowInstanceState(action);
        }

        public void LoadAndUpdateWorkflow(string newDefinitionIdentity, bool hintUpdatePass = true, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.UpdateWorkflow(newDefinitionIdentity, TimeSpan.Zero, true, extensionTypes);
            WorkflowInstanceAction action = hintUpdatePass == true ? WorkflowInstanceAction.UpdatedWorkflow : WorkflowInstanceAction.UpdateFailedWorkflow;
            UpdateExpectedWorkflowInstanceState(action);
        }

        public void LoadAndUpdateWorkflow(string newDefinitionIdentity, TimeSpan timeout, bool hintUpdatePass = true, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.UpdateWorkflow(newDefinitionIdentity, timeout, false, extensionTypes);
            WorkflowInstanceAction action = hintUpdatePass == true ? WorkflowInstanceAction.UpdatedWorkflow : WorkflowInstanceAction.UpdateFailedWorkflow;
            UpdateExpectedWorkflowInstanceState(action);
        }

        public void LoadWorkflow(params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.LoadWorkflow(TimeSpan.Zero, true, extensionTypes);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadWorkflow(TimeSpan timeout, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.LoadWorkflow(timeout, false, extensionTypes);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableWorkflow(params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.LoadRunnableWorkflow(TimeSpan.Zero, true, extensionTypes);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableWorkflow(TimeSpan timeout, params Type[] extensionTypes)
        {
            _remoteWorkflowRuntime.LoadRunnableWorkflow(timeout, false, extensionTypes);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadWorkflow(AsyncCallback asyncCallback, object state, params Type[] extensionTypes)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadWorkflow(asyncCallback, state, extensionTypes);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state, params Type[] extensionTypes)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadWorkflow(timeout, asyncCallback, state, extensionTypes);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndLoadWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndLoadWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void InitiateWorkflowForLoad()
        {
            _remoteWorkflowRuntime.InitiateWorkflowForLoad();
        }

        public void LoadInitiatedWorkflow()
        {
            _remoteWorkflowRuntime.LoadInitiatedWorkflow(true, TimeSpan.Zero);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadInitiatedWorkflow(TimeSpan loadTimeSpan)
        {
            _remoteWorkflowRuntime.LoadInitiatedWorkflow(false, loadTimeSpan);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadInitiatedWorkflowforUpdate(System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadInitiatedWorkflowForUpdate(Guid.Empty, false, timeout.Value);
            }
            else
            {
                _remoteWorkflowRuntime.LoadInitiatedWorkflowForUpdate(Guid.Empty, true, TimeSpan.Zero);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadInitiatedWorkflowforUpdate(Guid hintInstanceId, System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadInitiatedWorkflowForUpdate(hintInstanceId, false, timeout.Value);
            }
            else
            {
                _remoteWorkflowRuntime.LoadInitiatedWorkflowForUpdate(hintInstanceId, true, TimeSpan.Zero);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflow()
        {
            _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflow(true, TimeSpan.Zero);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflow(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflow(false, timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflowForInstance(TimeSpan timeout, Guid hintInstanceId)
        {
            _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflow(false, timeout, hintInstanceId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflowForUpdate(Guid hintInstanceId, System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(false, timeout.Value, hintInstanceId);
            }
            else
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(true, TimeSpan.Zero, hintInstanceId);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflowForUpdate(System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(false, timeout.Value);
            }
            else
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(true, TimeSpan.Zero);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void ConfigureDefaultInstanceOwnerMetadata40(Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            _remoteWorkflowRuntime.ConfigureStoreCustomMetadata40(instanceOwnerMetadata);
        }

        public void ConfigureDefaultInstanceOwnerMetadata45(Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            _remoteWorkflowRuntime.ConfigureStoreCustomMetadata45(instanceOwnerMetadata);
        }

        public void LoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, false, timeout.Value);
            }
            else
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, true, TimeSpan.Zero);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void LoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, Guid hintInstanceId, System.Nullable<TimeSpan> timeout = null)
        {
            if (timeout.HasValue)
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, false, timeout.Value, hintInstanceId);
            }
            else
            {
                _remoteWorkflowRuntime.LoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, true, TimeSpan.Zero, hintInstanceId);
            }
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadInitiatedWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadInitiatedWorkflow(true, TimeSpan.Zero, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadInitiatedWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadInitiatedWorkflow(false, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void UnloadWorkflow()
        {
            _remoteWorkflowRuntime.UnloadWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.UnloadedWorkflow);
        }

        public void UnloadWorkflow(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.UnloadWorkflow(timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.UnloadedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginUnloadWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginUnloadWorkflow(timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginUnloadWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginUnloadWorkflow(asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndUnloadWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndUnloadWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.UnloadedWorkflow);
        }

        public void AbortWorkflow(string reason)
        {
            _remoteWorkflowRuntime.AbortWorkflow(reason);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.AbortedWorkflow);
        }

        public void CancelWorkflow()
        {
            _remoteWorkflowRuntime.CancelWorkflow();
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.CancelWorkflow);
        }

        public void CancelWorkflow(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.CancelWorkflow(timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.CancelWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginCancelWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginCancelWorkflow(timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginCancelWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginCancelWorkflow(asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndCancelWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndCancelWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.CancelWorkflow);
        }

        public void TerminateWorkflow(string reason)
        {
            _remoteWorkflowRuntime.TerminateWorkflow(reason);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.TerminateWorkflow);
        }

        public void TerminateWorkflow(string reason, TimeSpan timeout)
        {
            _remoteWorkflowRuntime.TerminateWorkflow(reason, timeout);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.TerminateWorkflow);
        }

        public void AddExtension(object extension)
        {
            if (null == extension)
            {
                throw new Exception("Failed to Create Extension object");
            }
            _remoteWorkflowRuntime.AddExtension(extension);
        }

        public void AddExtension<TExtension>(params object[] constructorParams)
        {
            TExtension extension = (TExtension)Activator.CreateInstance(typeof(TExtension), constructorParams);
            if (null == extension)
            {
                throw new Exception("Failed to Create Extension object");
            }
            _remoteWorkflowRuntime.AddExtension(extension);
        }

        //This method does not work for partial trust
        public void AddExtension<TExtension>(Func<TExtension> extensionProvider)
            where TExtension : class
        {
            _remoteWorkflowRuntime.AddExtension(extensionProvider);
        }

        public TestWorkflowRuntimeAsyncResult BeginTerminateWorkflow(string reason, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginTerminateWorkflow(reason, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginTerminateWorkflow(string reason, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginTerminateWorkflow(reason, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndTerminateWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndTerminateWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.TerminateWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadRunnableInitiatedWorkflow(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadRunnableInitiatedWorkflow(true, TimeSpan.Zero, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadRunnableInitiatedWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadRunnableInitiatedWorkflow(false, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndLoadRunnableInitiatedWorkflow(TestWorkflowRuntimeAsyncResult asyncResult)
        {
            _remoteWorkflowRuntime.EndLoadRunnableInitiatedWorkflow(asyncResult.AsyncResultId);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, true, TimeSpan.Zero, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, false, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndLoadRunnableInitiatedWorkflowForUpdate(TestWorkflowRuntimeAsyncResult asyncResult, string oldDefinitionIdentity, string identityMask, AsyncCallback asyncCallback, object state)
        {
            _remoteWorkflowRuntime.EndLoadRunnableInitiatedWorkflowForUpdate(asyncResult.AsyncResultId, oldDefinitionIdentity, identityMask, true, TimeSpan.Zero, asyncCallback, state);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void EndLoadRunnableInitiatedWorkflowForUpdate(TestWorkflowRuntimeAsyncResult asyncResult, string oldDefinitionIdentity, string identityMask, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            _remoteWorkflowRuntime.EndLoadRunnableInitiatedWorkflowForUpdate(asyncResult.AsyncResultId, oldDefinitionIdentity, identityMask, false, timeout, asyncCallback, state);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadInitiatedWorkflowForUpdate(AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadInitiatedWorkflowForUpdate(true, TimeSpan.Zero, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public TestWorkflowRuntimeAsyncResult BeginLoadInitiatedWorkflowForUpdate(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            Guid asyncResultGuid = _remoteWorkflowRuntime.BeginLoadInitiatedWorkflowForUpdate(false, timeout, asyncCallback, state);
            return new TestWorkflowRuntimeAsyncResult(asyncResultGuid, this);
        }

        public void EndLoadInitiatedWorkflowForUpdate(TestWorkflowRuntimeAsyncResult asyncResult, AsyncCallback asyncCallback, object state)
        {
            _remoteWorkflowRuntime.EndLoadInitiatedWorkflowForUpdate(asyncResult.AsyncResultId, true, TimeSpan.Zero, asyncCallback, state);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        public void EndLoadInitiatedWorkflowForUpdate(TestWorkflowRuntimeAsyncResult asyncResult, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            _remoteWorkflowRuntime.EndLoadInitiatedWorkflowForUpdate(asyncResult.AsyncResultId, false, timeout, asyncCallback, state);
            UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.LoadedWorkflow);
        }

        #region GetBookmarks
        public ReadOnlyCollection<CoreWf.Hosting.BookmarkInfo> GetBookmarks()
        {
            return _remoteWorkflowRuntime.GetBookmarks();
        }

        public ReadOnlyCollection<CoreWf.Hosting.BookmarkInfo> GetBookmarks(TimeSpan timeout)
        {
            return _remoteWorkflowRuntime.GetBookmarks(timeout);
        }
        #endregion


        public void WaitForRunnableEvent()
        {
            this.WaitForRunnableEvent(TimeSpan.FromSeconds(30));
        }

        public void WaitForRunnableEvent(TimeSpan timeout)
        {
            _remoteWorkflowRuntime.WaitForRunnableEvent(timeout);
        }

        public void SetDefaultOwnerMetadata(XName metadataName, InstanceValue instanceValue)
        {
            this.SetDefaultOwner = true;
            _remoteWorkflowRuntime.SetDefaultOwnerMetadata(metadataName, instanceValue);
        }

        public void SetInstanceMetadata(Dictionary<XName, object> metadataDic)
        {
            _remoteWorkflowRuntime.SetInstanceMetadata(metadataDic);
        }

        public void Dispose()
        {
            _remoteWorkflowRuntime.Dispose();
        }

        #endregion

        #region WaitFor*

        public WorkflowTrackingWatcher GetWatcher()
        {
            WorkflowTrackingWatcher watcher = _remoteWorkflowRuntime.GetTrackingWatcher();
            watcher.ExpectedTraces = GetExpectedTrace();
            watcher.ExpectedInstanceTraces = (_usingExpectedWorkflowInstanceTrace) ? GetExpectedWorkflowInstanceTrace() : null;
            return watcher;
        }

        // Everything else inside of this region is legacy and shouldnt be used.

        public void WaitForIdle()
        {
            WaitForIdle(1);
        }

        public void WaitForIdle(int numOccurrences)
        {
            if (_remoteWorkflowRuntime.HasInMemoryParticipant())
            {
                //only wait for trace if there is an InMemoryTrackingParticipant
                //this is only for tracking tests that use Sql/EtwTrackingParticipant
                //runtime tests always run with an InMemoryTP, so this will always enter
                this.WaitForTrace(new WorkflowInstanceTrace(_remoteWorkflowRuntime.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Idle), numOccurrences);
            }
        }

        public void WaitForUnloaded()
        {
            WaitForUnloaded(1);
            //This is the time that it takes to really unload the instance and save it to the store
            System.Threading.Thread.CurrentThread.Join((int)TimeSpan.FromSeconds(2).TotalMilliseconds);
        }

        public void WaitForUnloaded(int numOccurrences)
        {
            this.WaitForTrace(new WorkflowInstanceTrace(_remoteWorkflowRuntime.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Unloaded), numOccurrences);
        }

        public void WaitForCompletion()
        {
            WaitForCompletion(true, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCompletion(bool validate)
        {
            WaitForCompletion(validate, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCompletion(ExpectedTrace expectedTrace)
        {
            WaitForCompletion(true, 1, expectedTrace, GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCompletion(int numOccurrences, ExpectedTrace expectedTrace)
        {
            WaitForCompletion(true, numOccurrences, expectedTrace, GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCompletion(ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            WaitForCompletion(true, 1, expectedTrace, expectedWorkflowInstanceTrace);
        }

        public void WaitForCanceled()
        {
            WaitForCanceled(true, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCanceled(bool validate)
        {
            WaitForCanceled(validate, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCanceled(ExpectedTrace expectedTrace)
        {
            WaitForCanceled(true, 1, expectedTrace, GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCanceled(int numOccurrences, ExpectedTrace expectedTrace)
        {
            WaitForCanceled(true, numOccurrences, expectedTrace, GetExpectedWorkflowInstanceTrace());
        }

        public void WaitForCanceled(ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            WaitForCanceled(true, 1, expectedTrace, expectedWorkflowInstanceTrace);
        }

        public void WaitForAborted(out Exception exception)
        {
            WaitForAborted(true, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForAborted(out Exception exception, bool validate)
        {
            WaitForAborted(validate, 1, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForAborted(int numOccurrences, out Exception exception, bool validate)
        {
            WaitForAborted(validate, numOccurrences, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForAborted(out Exception exception, ExpectedTrace expectedTrace)
        {
            WaitForAborted(true, 1, expectedTrace, GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForAborted(out Exception exception, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            WaitForAborted(true, 1, expectedTrace, expectedWorkflowInstanceTrace, out exception);
        }

        public void WaitForTerminated(int numOccurrences, out Exception exception)
        {
            WaitForTerminated(true, numOccurrences, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForTerminated(int numOccurrences, out Exception exception, bool validate)
        {
            WaitForTerminated(validate, numOccurrences, GetExpectedTrace(), GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForTerminated(int numOccurrences, out Exception exception, ExpectedTrace expectedTrace)
        {
            WaitForTerminated(true, numOccurrences, expectedTrace, GetExpectedWorkflowInstanceTrace(), out exception);
        }

        public void WaitForTerminated(int numOccurrences, out Exception exception, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            WaitForTerminated(true, numOccurrences, expectedTrace, expectedWorkflowInstanceTrace, out exception);
        }

        public void WaitForActivityStatusChange(string activityDisplayName, TestActivityInstanceState targetState)
        {
            this.WaitForActivityStatusChange(activityDisplayName, targetState, 1);
        }

        public void WaitForActivityStatusChange(string activityDisplayName, TestActivityInstanceState targetState, int numOccurrences)
        {
            this.WaitForTrace(new ActivityTrace(activityDisplayName, ActivityTrace.GetActivityInstanceState(targetState)), numOccurrences);
        }

        public void WaitForTrace(IActualTraceStep trace)
        {
            _remoteWorkflowRuntime.WaitForTrace(trace, 1);
        }

        public void WaitForTrace(IActualTraceStep trace, int numOccurrences)
        {
            if (trace is WorkflowInstanceTrace && _remoteWorkflowRuntime.DefinitionIdentity != null)
            {
                WorkflowInstanceTrace instanceTrace = (WorkflowInstanceTrace)trace;
                if (instanceTrace.WorkflowDefinitionIdentity == null)
                {
                    instanceTrace.WorkflowDefinitionIdentity = _remoteWorkflowRuntime.DefinitionIdentity;
                }
            }

            _remoteWorkflowRuntime.WaitForTrace(trace, numOccurrences);
        }

        private void WaitForCompletion(bool validate, int numOccurrences, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            if (_usingExpectedWorkflowInstanceTrace)
            {
                UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.WaitForCompletedWorkflow);
            }

            WorkflowTrackingWatcher watcher = GetWatcher();
            watcher.ExpectedTraces = expectedTrace;
            watcher.ExpectedInstanceTraces = expectedWorkflowInstanceTrace;
            watcher.WaitForWorkflowCompletion(validate, numOccurrences);
        }

        private void WaitForCanceled(bool validate, int numOccurrences, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            if (_usingExpectedWorkflowInstanceTrace)
            {
                UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.WaitForCompletedWorkflow);
            }

            WorkflowTrackingWatcher watcher = GetWatcher();
            watcher.ExpectedTraces = expectedTrace;
            watcher.ExpectedInstanceTraces = expectedWorkflowInstanceTrace;
            watcher.WaitForWorkflowCanceled(validate, numOccurrences);
        }

        private void WaitForAborted(bool validate, int numOccurrences, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace, out Exception exception)
        {
            if (_usingExpectedWorkflowInstanceTrace)
            {
                UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.WaitForAbortedWorkflow);
            }

            WorkflowTrackingWatcher watcher = GetWatcher();
            watcher.ExpectedTraces = expectedTrace;
            watcher.ExpectedInstanceTraces = expectedWorkflowInstanceTrace;
            watcher.WaitForWorkflowAborted(out exception, validate, numOccurrences);
        }

        private void WaitForTerminated(bool validate, int numOccurrences, ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace, out Exception exception)
        {
            if (_usingExpectedWorkflowInstanceTrace)
            {
                UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction.WaitForTerminatedWorkflow);
            }

            WorkflowTrackingWatcher watcher = GetWatcher();
            watcher.ExpectedTraces = expectedTrace;
            watcher.ExpectedInstanceTraces = expectedWorkflowInstanceTrace;
            watcher.WaitForWorkflowTerminated(out exception, validate, numOccurrences);
            exception = null;
        }

        #endregion

        #region Helpers


        private void Initialize(TestActivity testActivity, WorkflowIdentity definitionIdentity, TestWorkflowRuntimeConfiguration testWorkflowRuntimeConfiguration = null, InstanceStore instanceStore = null,
            PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            if (testActivity == null)
            {
                throw new ArgumentNullException("activity");
            }

            // if (TestParameters.IsPartialTrustRun)
            // { //For PT we need to compile Workflow having VB value beforehand 
            //     testActivity.ProductActivity = PartialTrustExpressionHelper.ReplaceLambdasAndCompileVB(testActivity.ProductActivity);
            // }

            _hostActivity = testActivity;

            if (testWorkflowRuntimeConfiguration == null)
            {
                //testWorkflowRuntimeConfiguration = TestWorkflowRuntimeConfiguration.GenerateDefaultConfiguration(testActivity, definitionIdentity);
            }

            //To do: Dev11 M2: CSDMain176701: Remove all unintended UsePartialTrust parameters in adp4.5 test cases and 
            //enable PartialTrust for standalone WorkflowApplication/WorkflowInvoker host by uncommenting below code
            //In Dev11 M1: Need to uncomment below to run tests under PT for prototyping.
            //if (TestParameters.UsePartialTrust)
            //{
            //    testWorkflowRuntimeConfiguration.TestHostingConfiguration.AppDomainConfigurationName = AppDomainConfiguration.PartialTrustAppDomain;
            //}

            //this.testWorkflowRuntimeConfiguration = testWorkflowRuntimeConfiguration;
            // this.testWorkflowRuntimeConfiguration.LockConfiguration();
            // this.remoteWorkflowRuntime = HostingObjectFactory.CreateWorkflowRuntime(testActivity, testWorkflowRuntimeConfiguration);
            _remoteWorkflowRuntime = new RemoteWorkflowRuntime(_hostActivity.ProductActivity, null, null, instanceStore, idleAction);

            Activity act = testActivity.ProductActivity;
            // if (!TestParameters.DisableXamlRoundTrip)
            // {
            //     act = (Activity)XamlTestDriver.XamlTestDriver.RoundTrip(testActivity.ProductActivity);
            // }
            // else
            // {
            //     act = testActivity.ProductActivity;
            // }

            _hostActivity.SetProductActivityBypassGenerateDisplayName(act);
            _workflowInstanceTrace = new OrderedTraces();
        }

        private void ValidateOutputs(IDictionary<string, object> expectedOutputs)
        {
            //Log.TraceInternal("ValidateOutputs: Validating...");
            IDictionary<string, object> actualOutputs = this.Outputs;
            if (expectedOutputs == null)
            {
                expectedOutputs = new Dictionary<string, object>();
            }
            if (actualOutputs == null)
            {
                actualOutputs = new Dictionary<string, object>();
            }

            StringBuilder sb = new StringBuilder();
            if (expectedOutputs.Count != actualOutputs.Count)
            {
                sb.AppendLine("expectedOutputs.Count != actualOutputs.Count");
            }

            foreach (KeyValuePair<string, object> expected in expectedOutputs)
            {
                if (!actualOutputs.ContainsKey(expected.Key))
                {
                    sb.AppendLine(string.Format("actualOutputs does not contain expected key '{0}'", expected.Key));
                }
                else if (expected.Value == null && actualOutputs[expected.Key] != null)
                {
                    sb.AppendLine(string.Format("expectedOutputs['{0}']={1} is different than actualOutputs['{2}']={3}", expected.Key, expected.Value, expected.Key, actualOutputs[expected.Key]));
                }
                else if (expected.Value != null && !expected.Value.Equals(actualOutputs[expected.Key]))
                {
                    sb.AppendLine(string.Format("expectedOutputs['{0}']={1} is different than actualOutputs['{2}']={3}", expected.Key, expected.Value, expected.Key, actualOutputs[expected.Key]));
                }
            }

            foreach (string actualKey in actualOutputs.Keys)
            {
                if (!expectedOutputs.ContainsKey(actualKey))
                {
                    sb.AppendLine(string.Format("expectedOutputs does not contain actual key '{0}'", actualKey));
                }
            }

            if (sb.Length > 0)
            {
                //Log.TraceInternal("Expected Outputs:");
                foreach (string expectedKey in expectedOutputs.Keys)
                {
                    //Log.TraceInternal("expected['{0}']={1}", expectedKey, expectedOutputs[expectedKey].ToString());
                }
                //Log.TraceInternal("Actual Outputs:");
                foreach (string actualKey in actualOutputs.Keys)
                {
                    //Log.TraceInternal("actual['{0}']={1}", actualKey, actualOutputs[actualKey].ToString());
                }
                //Log.TraceInternal("Errors found:");
                //Log.TraceInternal(sb.ToString());
                throw new Exception("FAIL, error while validating in TestWorkflowRuntime.ValidateOutputs");
            }
            //Log.TraceInternal("ValidateOutputs: Validation complete");
        }

        private ExpectedTrace GetExpectedTrace()
        {
            ExpectedTrace expectedTraces = _hostActivity.GetExpectedTrace();
            if (_remoteWorkflowRuntime.DefinitionIdentity != null)
            {
                foreach (WorkflowTraceStep step in expectedTraces.Trace.Steps)
                {
                    if (step is WorkflowInstanceTrace)
                    {
                        WorkflowInstanceTrace instanceTrace = (WorkflowInstanceTrace)step;
                        if (instanceTrace.WorkflowDefinitionIdentity == null)
                        {
                            instanceTrace.WorkflowDefinitionIdentity = _remoteWorkflowRuntime.DefinitionIdentity;
                        }
                    }
                }
            }
            return expectedTraces;
        }


        internal ExpectedTrace GetExpectedWorkflowInstanceTrace()
        {
            _usingExpectedWorkflowInstanceTrace = true;
            ExpectedTrace expectedWorkflowInstanceTrace = new ExpectedTrace(_workflowInstanceTrace);
            expectedWorkflowInstanceTrace.AddVerifyTypes(typeof(WorkflowInstanceTrace));
            return expectedWorkflowInstanceTrace;
        }

        private void CheckIfStaticDelegate<TEventArgs>(EventHandler<TEventArgs> eventHandler) where TEventArgs : EventArgs
        {
            //if (!eventHandler.Method.IsStatic)
            //{
            //    throw new Exception("EventHandler delegate should be static method. Non-static EventHandler delegate may not work well for MultiMachine scenario.");
            //}
        }

        private void CheckIfMultipleDelegates(int eventId)
        {
            if (_remoteWorkflowRuntime.GetEventHandlerCount(eventId) > 0)
            {
                throw new Exception("Currently TestWorkflowRuntime support single event handlers. Please use adaptors for multiple event handlers");
            }
        }

        private void InsertDeletedTraceIfNecessary()
        {
            // Insert deleted trace if persistence provider exists
            // There is no deleted trace if no persistence provider is used
            if (this.PersistenceProviderFactoryType != null)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Deleted)
                {
                    Optional = true
                });
            }
        }

        public void UpdateExpectedWorkflowInstanceState(WorkflowInstanceAction nextAction)
        {
            //you just started the workflow
            if (_previousAction == WorkflowInstanceAction.Initialized && nextAction == WorkflowInstanceAction.ResumedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Started));
            }

            //you just loaded the workflow and resuming
            if (_previousAction == WorkflowInstanceAction.LoadedWorkflow && nextAction == WorkflowInstanceAction.ResumedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Resumed));
            }

            //you just unloaded the workflow and resuming
            if (_previousAction == WorkflowInstanceAction.UnloadedWorkflow && nextAction == WorkflowInstanceAction.ResumedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Resumed));
            }


            //if you are trying to abort the workflow
            if (nextAction == WorkflowInstanceAction.WaitForAbortedWorkflow)
            {
                //This is all because test activities currently can't generate "unexpectedexception" trace
                if (_previousAction != WorkflowInstanceAction.AbortedWorkflow)
                {
                    _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.UnhandledException));
                }
                _workflowInstanceTrace.Steps.Add(new WorkflowAbortedTrace(this.CurrentWorkflowInstanceId, null));
            }

            //if you are trying to complete the workflow
            if (nextAction == WorkflowInstanceAction.WaitForCompletedWorkflow)
            {
                if (_previousAction == WorkflowInstanceAction.CancelWorkflow)
                {
                    _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Canceled));
                }
                else
                {
                    _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Completed));
                }
                InsertDeletedTraceIfNecessary();
            }

            //if you are trying to terminate the workflow
            if (nextAction == WorkflowInstanceAction.WaitForTerminatedWorkflow)
            {
                //This is all because test activities currently can't generate "unexpectedexception" trace
                if (_previousAction != WorkflowInstanceAction.TerminateWorkflow)
                {
                    _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.UnhandledException));
                }
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Terminated));
                InsertDeletedTraceIfNecessary();
            }

            //if you are trying to delete the workflow
            if (nextAction == WorkflowInstanceAction.DeletedWorkflow)
            {
                InsertDeletedTraceIfNecessary();
            }

            //if you are trying to persist the workflow -- we don't count the action of persist activity
            if (nextAction == WorkflowInstanceAction.PersistedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Persisted));
            }

            //if you are trying to unload the workflow
            if (nextAction == WorkflowInstanceAction.UnloadedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Unloaded));
            }

            //If you are trying to update workflow
            if (nextAction == WorkflowInstanceAction.UpdatedWorkflow)
            {
                //this.workflowInstanceTrace.Steps.Add(new WorkflowInstanceUpdatedTrace(this.CurrentWorkflowInstanceId, this.previousIdentity, this.remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Updated));
            }

            //If you are trying to update workflow
            if (nextAction == WorkflowInstanceAction.UpdateFailedWorkflow)
            {
                //this.workflowInstanceTrace.Steps.Add(new WorkflowInstanceUpdatedTrace(this.CurrentWorkflowInstanceId, this.previousIdentity, this.remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.UpdateFailed));
            }

            //you just updated the workflow and resuming
            if (_previousAction == WorkflowInstanceAction.UpdatedWorkflow && nextAction == WorkflowInstanceAction.ResumedWorkflow)
            {
                _workflowInstanceTrace.Steps.Add(new WorkflowInstanceTrace(this.CurrentWorkflowInstanceId, _remoteWorkflowRuntime.DefinitionIdentity, WorkflowInstanceState.Resumed));
            }

            _previousAction = nextAction;
            _previousIdentity = _remoteWorkflowRuntime.DefinitionIdentity;
        }
        #endregion
    }

    public enum WorkflowInstanceAction
    {
        Initialized,
        PersistedWorkflow,
        ResumedWorkflow,
        UnloadedWorkflow,
        LoadedWorkflow,
        DeletedWorkflow,
        AbortedWorkflow,
        CancelWorkflow,
        TerminateWorkflow,
        WaitForTerminatedWorkflow,
        WaitForAbortedWorkflow,
        WaitForCompletedWorkflow,
        UpdatedWorkflow,
        UpdateFailedWorkflow,
    };

    public class TestWorkflowRuntimeAsyncResult
    {
        private TestWorkflowRuntime _workflowRuntime;

        public TestWorkflowRuntimeAsyncResult(Guid asyncResultId, TestWorkflowRuntime workflowRuntime)
        {
            this.AsyncResultId = asyncResultId;
            _workflowRuntime = workflowRuntime;
        }
        internal Guid AsyncResultId { get; set; }
    }

    public struct TestWorkflowRuntimeAsyncState
    {
        public WorkflowApplication Instance { get; set; }
        public object State { get; set; }
    }

    public class TestWorkflowRuntimeEventArgs : EventArgs
    {
        public WorkflowApplication WorkflowApplication { get; private set; }

        internal TestWorkflowRuntimeEventArgs(WorkflowApplication instance)
        {
            this.WorkflowApplication = instance;
        }
    }

    public class TestWorkflowAbortedEventArgs : TestWorkflowRuntimeEventArgs
    {
        public WorkflowApplicationAbortedEventArgs EventArgs { get; private set; }

        internal TestWorkflowAbortedEventArgs(WorkflowApplication instance, WorkflowApplicationAbortedEventArgs args)
            : base(instance)
        {
            this.EventArgs = args;
        }
    };

    public class TestWorkflowIdleEventArgs : TestWorkflowRuntimeEventArgs
    {
        public WorkflowApplicationIdleEventArgs EventArgs { get; private set; }

        internal TestWorkflowIdleEventArgs(WorkflowApplication instance, WorkflowApplicationIdleEventArgs args)
            : base(instance)
        {
            this.EventArgs = args;
        }
    };

    public class TestWorkflowIdleAndPersistableEventArgs : TestWorkflowRuntimeEventArgs
    {
        public WorkflowApplicationIdleEventArgs EventArgs { get; private set; }

        public PersistableIdleAction Action { get; set; }

        internal TestWorkflowIdleAndPersistableEventArgs(WorkflowApplication instance, WorkflowApplicationIdleEventArgs args)
            : base(instance)
        {
            this.Action = PersistableIdleAction.None;
            this.EventArgs = args;
        }
    };

    public class TestWorkflowUnloadedEventArgs : TestWorkflowRuntimeEventArgs
    {
        internal TestWorkflowUnloadedEventArgs(WorkflowApplication instance, WorkflowApplicationEventArgs args)
            : base(instance)
        {
            this.EventArgs = args;
        }

        public WorkflowApplicationEventArgs EventArgs { get; private set; }
    }

    public class TestWorkflowCompletedEventArgs : TestWorkflowRuntimeEventArgs
    {
        public WorkflowApplicationCompletedEventArgs EventArgs { get; private set; }

        internal TestWorkflowCompletedEventArgs(WorkflowApplication instance, WorkflowApplicationCompletedEventArgs args)
            : base(instance)
        {
            this.EventArgs = args;
        }
    };

    public class TestWorkflowUnhandledExceptionEventArgs : TestWorkflowRuntimeEventArgs
    {
        public WorkflowApplicationUnhandledExceptionEventArgs EventArgs { get; private set; }

        internal TestWorkflowUnhandledExceptionEventArgs(WorkflowApplication instance, WorkflowApplicationUnhandledExceptionEventArgs args)
            : base(instance)
        {
            this.EventArgs = args;
            this.Action = UnhandledExceptionAction.Abort;
        }

        public UnhandledExceptionAction Action
        {
            get;
            set;
        }
    };
}

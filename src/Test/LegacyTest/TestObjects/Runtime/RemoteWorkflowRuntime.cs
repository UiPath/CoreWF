// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.DurableInstancing;
using System.Activities.Hosting;
using System.Activities.Runtime.DurableInstancing;
using System.Activities.Tracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using LegacyTest.Test.Common.TestObjects.Tracking;
using LegacyTest.Test.Common.TestObjects.Utilities;
using LegacyTest.Test.Common.TestObjects.Utilities.Sql;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Runtime
{
    public class RemoteWorkflowRuntime //: MarshalByRefObject
    {
        private Exception _lastException;

        private WorkflowApplication _workflowApplication;
        private WorkflowInvoker _workflowInvoker;
        private Activity _productActivity;
        //WorkflowCompletionState completionState;
        private TestWorkflowRuntimeConfiguration _testWorkflowRuntimeConfiguration;

        private Guid _workflowInstanceId;

        private InstanceStore _instanceStore;
        private readonly PersistableIdleAction _idleAction;

        //private InstanceStoreVersion _instanceStoreVersion;
        private Type _instanceStoreType;
        //PersistenceProviderHelper persistenceProviderHelper;
        //TestPersistenceProviderInfo persistenceProviderInfo;
        private InstanceHandle _defaultOwnerInstanceHandle;
        private Dictionary<XName, InstanceValue> _defaultOwnerMetadata;
        private XName _instanceHostTypeValue40;

        private TimeSpan _waitForCompletionTimeout;
        private TimeSpan _waitForIdleTimeout;
        private Type _workflowRuntimeAdapterType;
        private IWorkflowRuntimeAdapter _workflowRuntimeAdapter;
        private AsyncResultCollection _asyncResultCollection;

        private IDictionary<string, object> _outputs;
        private SymbolResolver _symbolResolver;
        private Dictionary<string, object> _inputs;

        private List<TrackingConfiguration> _trackingConfig;

        public const string CompletedOrAbortedHandlerCalled = "CompletedOrAbortedHandlerCalled";

        internal RemoteWorkflowRuntime(string productActivityXaml, TestWorkflowRuntimeConfiguration config) : this(productActivityXaml, config, null) { }

        internal RemoteWorkflowRuntime(string productActivityXml, TestWorkflowRuntimeConfiguration config, Object testCase)
        {
            //    // Do not override an existing TestCase.Current context. Allowing that would have unwanted side effects that are impossible to debug
            //    if (TestCase.Current == null && testCase != null)
            //    {
            //        PartialTrustTestCase.SetCurrentTestCase(testCase);
            //    }
            //    try
            //    {
            //        // Need to do this to load the assemblies we will need.
            //        PartialTrustCaller.LoadWFApplicationRequiredAssemblies();

            //        Activity activity = (Activity)XamlTestDriver.XamlTestDriver.Deserialize(productActivityXml);
            //        Init(activity, config);
            //    }
            //    catch (Exception) // jasonv - approved; logs offending xaml, rethrows
            //    {
            //        //Log.TraceInternal("Unable to deserialize XAML - ");
            //        //Log.TraceInternal("Xaml : " + productActivityXml);

            //        throw;
            //    }
        }

        internal RemoteWorkflowRuntime(Activity activity, TestWorkflowRuntimeConfiguration config) : this(activity, config, null) { }

        internal RemoteWorkflowRuntime(Activity activity, TestWorkflowRuntimeConfiguration config, Object testCase, InstanceStore instanceStore = null,
            PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            // Do not override an existing TestCase.Current context. Allowing that would have unwanted side effects that are impossible to debug
            //if (TestCase.Current == null && testCase != null)
            //{
            //    TestCase.SetCurrent(testCase);
            //}

            _instanceStore = instanceStore;
            _idleAction = idleAction;

            Init(activity, config);
        }

        private void Init(Activity activity, TestWorkflowRuntimeConfiguration config)
        {
            _productActivity = activity;
            _testWorkflowRuntimeConfiguration = config;
            _instanceStoreType = null;// TestParameters.GetPersistenceProviderFactoryType(config.HostType);
            _asyncResultCollection = new AsyncResultCollection();
            _waitForCompletionTimeout = TimeSpan.FromMinutes(5);
            _waitForIdleTimeout = TimeSpan.FromMinutes(5);
            //_instanceStoreVersion = InstanceStoreVersion.Version40; // TestParameters.GetInstanceStoreVersion();

            _defaultOwnerMetadata = new Dictionary<XName, InstanceValue>();
            string WFInstanceScopeName = "RemoteWorkflowRuntimeInstance_" + Guid.NewGuid().ToString();
            _instanceHostTypeValue40 = XName.Get(WFInstanceScopeName, "LegacyTest.Test.Common.TestObjects.Runtime.Hosting.Self.RemoteWorkflowRuntime");
            this.SetDefaultOwner = false;
        }

        public Activity ProductActivity
        {
            get { return _productActivity; }
            set { _productActivity = value; }
        }

        public InstanceStore InstanceStore
        {
            get { return _instanceStore; }
            set { _instanceStore = value; }
        }

        internal WorkflowInstanceExtensionManager Extensions
        {
            get { return _workflowApplication.Extensions; }
        }

        internal ActualTrace ActualTrace
        {
            get
            {
                ThrowIfWorkflowInstanceInvalid();
                return TestTraceManager.Instance.GetInstanceActualTrace(_workflowInstanceId);
            }
        }

        internal Dictionary<string, object> Inputs
        {
            set { _inputs = value; }
        }

        internal Exception LastException
        {
            get
            {
                ThrowIfWorkflowInstanceInvalid();
                return _lastException;
            }
        }

        internal IDictionary<string, object> Outputs
        {
            get
            {
                return _outputs;
            }
        }

        internal TestSymbolResolver TestSymbolResolver
        {
            get
            {
                ThrowIfWorkflowInstanceInvalid();
                TestSymbolResolver testSymbolResolver = new TestSymbolResolver();
                if (_symbolResolver != null && _symbolResolver.Count > 0)
                {
                    foreach (string key in _symbolResolver.Keys)
                    {
                        testSymbolResolver.Data.Add(key, _symbolResolver[key]);
                    }
                }
                return testSymbolResolver;
            }
            set
            {
                if (_symbolResolver != null)
                {
                    throw new Exception("SymbolResolver is already set.");
                }
                _symbolResolver = new SymbolResolver();
                if (value.Data != null && value.Data.Count > 0)
                {
                    foreach (string key in value.Data.Keys)
                    {
                        _symbolResolver.Add(key, value.Data[key]);
                    }
                }
            }
        }

        internal Type InstanceStoreType
        {
            get
            {
                return _instanceStoreType;
            }
            set
            {
                //if (value == null || value.IsSubclassOf(typeof(InstanceStore)))
                //{
                //    this.instanceStoreType = value;
                //}
                //else
                //{
                //    throw new ArgumentException("InstanceStoreType should of type InstanceStore", "InstanceStoreType");
                //}
            }
        }

        internal bool SetDefaultOwner
        {
            get;
            set;
        }

        internal TimeSpan WaitForCompletionTimeout
        {
            get { return _waitForCompletionTimeout; }
            set { _waitForCompletionTimeout = value; }
        }

        internal TimeSpan WaitForIdleTimeout
        {
            get { return _waitForIdleTimeout; }
            set { _waitForIdleTimeout = value; }
        }

        internal Guid CurrentWorkflowInstanceId
        {
            get
            {
                ThrowIfWorkflowInstanceInvalid();
                return _workflowApplication.Id;
            }
        }

        internal WorkflowIdentity DefinitionIdentity
        {
            get
            {
                ThrowIfWorkflowInstanceInvalid();
                return _workflowApplication.DefinitionIdentity;
            }
        }

        internal Type WorkflowRuntimeAdapterType
        {
            get { return _workflowRuntimeAdapterType; }
            set
            {
                if (value == null)
                {
                    _workflowRuntimeAdapterType = value;
                    _workflowRuntimeAdapter = null;
                }
                else if (Array.Exists(value.GetInterfaces(), t => (t.Name == (typeof(IWorkflowRuntimeAdapter).Name))))
                {
                    _workflowRuntimeAdapterType = value;
                    _workflowRuntimeAdapter = (IWorkflowRuntimeAdapter)Activator.CreateInstance(_workflowRuntimeAdapterType);
                }
                else
                {
                    throw new InvalidOperationException("WorkflowRuntimeAdapterType must be a type that implements IWorkflowRuntimeAdapter");
                }
            }
        }

        //internal WorkflowCompletionState CompletionState
        //{
        //    get { return this.completionState; }
        //}

        internal List<TrackingConfiguration> TrackingConfigurations
        {
            get { return _trackingConfig; }
        }

        internal event EventHandler<TestWorkflowAbortedEventArgs> OnWorkflowAborted;
        internal event EventHandler<TestWorkflowIdleEventArgs> OnWorkflowIdle;
        internal event EventHandler<TestWorkflowIdleAndPersistableEventArgs> OnWorkflowIdleAndPersistable;
        internal event EventHandler<TestWorkflowCompletedEventArgs> OnWorkflowCompleted;
        internal event EventHandler<TestWorkflowUnhandledExceptionEventArgs> OnWorkflowUnhandledException;
        internal event EventHandler<TestWorkflowUnloadedEventArgs> OnWorkflowUnloaded;


        internal ActualTrace ActualTrackingData(string trackingParticipantName)
        {
            ThrowIfWorkflowInstanceInvalid();
            return TestTrackingDataManager.GetInstance(_workflowApplication.Id).GetActualTrackingData(trackingParticipantName);
        }

        internal bool HasInMemoryParticipant()
        {
            ThrowIfWorkflowInstanceInvalid();
            return true;
        }

        internal void CreateWorkflow()
        {
            if (_productActivity == null)
            {
                throw new NullReferenceException("ProductActivity is null");
            }

            if (_workflowRuntimeAdapter is IWorkflowRuntimeAdapter2 workflowRuntimeAdapter2)
            {
                _workflowApplication = workflowRuntimeAdapter2.CreateInstance(_productActivity);
            }
            else
            {
                WorkflowIdentity definitionIdentity = null;
                //WorkflowIdentity.TryParse(this.testWorkflowRuntimeConfiguration.DefinitionIdentity, out definitionIdentity);

                if (_inputs != null)
                {
                    _workflowApplication = new WorkflowApplication(_productActivity, _inputs, definitionIdentity);
                }
                else
                {
                    _workflowApplication = new WorkflowApplication(_productActivity, definitionIdentity);
                }
                _workflowApplication.Extensions.Add(TestRuntime.TraceListenerExtension);
                if (_symbolResolver != null)
                {
                    AddSymbolResolver();
                }
            }

            //if (this.testWorkflowRuntimeConfiguration.OnIdleAction == TestOnIdleAction.Unload)
            //{
            //    this.SetDefaultOwner = true;
            //}
            this.SetDefaultOwner = true;
            AddInstanceStoreToWorkflowInstance();

            //Add Tracking according to the tracking configuration
            AddTrackingProviderToWorkflowInstance(_workflowApplication.Id);

            InitWorkflowInstanceWithEvents();
            if (_workflowRuntimeAdapter != null)
            {
                _workflowRuntimeAdapter.OnInstanceCreate(_workflowApplication);
            }
            //TestTraceManager.Instance.MarkInstanceAsKnown(this.workflowApplication.Id);
            _workflowInstanceId = _workflowApplication.Id;
        }


        internal void CreateWorkflowInvoker(ICollection<object> extensions)
        {
            _workflowInvoker = new WorkflowInvoker(_productActivity);
            if (extensions != null)
            {
                foreach (object extension in extensions)
                {
                    _workflowInvoker.Extensions.Add(extension);
                }
            }
            _workflowInvoker.Extensions.Add(TestRuntime.TraceListenerExtension);
        }

        internal void ExecuteWorkflow()
        {
            CreateWorkflow();
            ResumeWorkflow();
        }

        internal void ExecuteWorkflowInvoker(ICollection<object> extensions)
        {
            CreateWorkflowInvoker(extensions);
            ResumeWorkflowInvoker();
        }

        internal void ExecuteWorkflowInvoker(ICollection<object> extensions, TimeSpan invokeTimeout)
        {
            CreateWorkflowInvoker(extensions);
            ResumeWorkflowInvoker(invokeTimeout);
        }

        internal BookmarkResumptionResult ResumeBookMark(string bookMarkName, Object value)
        {
            ThrowIfWorkflowInstanceInvalid();
            return _workflowApplication.ResumeBookmark(bookMarkName, value);
        }

        internal BookmarkResumptionResult ResumeBookMark(string bookMarkName, Object value, TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            return _workflowApplication.ResumeBookmark(bookMarkName, value, timeout);
        }

        internal Guid BeginResumeBookMark(string bookMarkName, Object value, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };

            IAsyncResult asyncResult = _workflowApplication.BeginResumeBookmark(bookMarkName, value, timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginResumeBookMark(string bookMarkName, Object value, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginResumeBookmark(bookMarkName, value, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal BookmarkResumptionResult EndResumeBookMark(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            return _workflowApplication.EndResumeBookmark(asyncResult);
        }


        //#region PauseWorkflow
        internal void ResumeWorkflow()
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Run();
        }

        internal void ResumeWorkflowInvoker()
        {
            ThrowIfWorkflowInvokerInvalid();
            if (_inputs == null)
            {
                _outputs = _workflowInvoker.Invoke();
            }
            else
            {
                _outputs = _workflowInvoker.Invoke(_inputs);
            }
        }

        internal void ResumeWorkflowInvoker(TimeSpan invokeTimeout)
        {
            ThrowIfWorkflowInvokerInvalid();
            if (_inputs == null)
            {
                _outputs = _workflowInvoker.Invoke(invokeTimeout);
            }
            else
            {
                _outputs = _workflowInvoker.Invoke(_inputs, invokeTimeout);
            }
        }


        internal void ResumeWorkflow(TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Run(timeout);
        }

        internal Guid BeginResumeWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginRun(timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginResumeWorkflow(AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginRun(asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndResumeWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            _workflowApplication.EndRun(asyncResult);
        }

        internal void PersistWorkflow()
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Persist();
        }

        internal void PersistWorkflow(TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Persist(timeout);
        }

        internal Guid BeginPersistWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginPersist(timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginPersistWorkflow(AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginPersist(asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndPersistWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            _workflowApplication.EndPersist(asyncResult);
        }

        internal Guid BeginLoadWorkflow(AsyncCallback asyncCallback, object state, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);

            return BeginLoadWorkflowHelper(extensionCollection, true, TimeSpan.Zero, asyncCallback, state);
        }

        internal Guid BeginLoadWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);

            return BeginLoadWorkflowHelper(extensionCollection, false, timeout, asyncCallback, state);
        }

        internal void EndLoadWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            //this.workflowApplication.EndLoad(asyncResult);
        }

        internal void LoadWorkflow(TimeSpan timeout, bool isDefaultTimeout, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);
            LoadWorkflowHelper(true, extensionCollection, isDefaultTimeout, timeout);
        }

        internal void LoadRunnableWorkflow(TimeSpan timeout, bool isDefaultTimeout, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);
            LoadWorkflowHelper(false, extensionCollection, isDefaultTimeout, timeout);
        }

        internal void UpdateRunnableWorkflow(string newDefinitionIdentity, TimeSpan timeout, bool isDefaultTimeout, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);
            LoadAndUpdateWorkflowHelper(newDefinitionIdentity, false, extensionCollection, isDefaultTimeout, timeout);
        }

        internal void UpdateWorkflow(string newDefinitionIdentity, TimeSpan timeout, bool isDefaultTimeout, params Type[] extensionTypes)
        {
            List<object> extensionCollection = new List<object>();
            CreateExtensionObjects(extensionTypes, extensionCollection);
            LoadAndUpdateWorkflowHelper(newDefinitionIdentity, true, extensionCollection, isDefaultTimeout, timeout);
        }

        internal void InitiateWorkflowForLoad(List<object> extensionCollection = null)
        {
            if (_workflowInstanceId == Guid.Empty)
            {
                throw new InvalidOperationException("LoadWorkflow failed as InstanceId is not valid");
            }

            WorkflowIdentity definitionIdentity = null;
            //WorkflowIdentity.TryParse(this.testWorkflowRuntimeConfiguration.DefinitionIdentity, out definitionIdentity);

            _workflowApplication = new WorkflowApplication(_productActivity, definitionIdentity);
            _workflowApplication.Extensions.Add(TestRuntime.TraceListenerExtension);
            AddInstanceStoreToWorkflowInstance();
            InitWorkflowInstanceWithEvents();

            if (extensionCollection != null)
            {
                foreach (object extension in extensionCollection)
                {
                    _workflowApplication.Extensions.Add(extension);
                }
            }
        }

        internal void LoadInitiatedWorkflowForUpdate(Guid hintInstanceId, bool isDefaultTimeout, TimeSpan timeout)
        {
            if (hintInstanceId != Guid.Empty)
            {
                _workflowInstanceId = hintInstanceId;
            }

            AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            WorkflowApplicationInstance wfAppInstance = RetrieveWorkflowApplicationInstance(isDefaultTimeout, timeout);
            LoadUpdateWorkflowApplicationInstance(wfAppInstance, isDefaultTimeout, timeout);
        }

        internal void LoadInitiatedWorkflowForUpdate(bool isDefaultTimeout, TimeSpan timeout)
        {
            LoadInitiatedWorkflowForUpdate(Guid.Empty, isDefaultTimeout, timeout);
        }

        internal void LoadInitiatedWorkflow(bool isDefaultTimeout, TimeSpan timeout)
        {
            AddTrackingProviderToWorkflowInstance(_workflowInstanceId);

            if (isDefaultTimeout)
            {
                if (_workflowRuntimeAdapter is IWorkflowRuntimeAdapter2 workflowRuntimeAdapter2)
                {
                    workflowRuntimeAdapter2.LoadInstance(_workflowApplication, _workflowInstanceId);
                    //Adapter should add extensions
                }
                else
                {
                    _workflowApplication.Load(_workflowInstanceId);
                }
            }
            else
            {
                _workflowApplication.Load(_workflowInstanceId, timeout);
            }

            if (_workflowInstanceId != _workflowApplication.Id)
            {
                throw new InvalidOperationException("LoadInitiatedWorkflow provided a different Guid then we were using");
            }

            if (_workflowRuntimeAdapter != null)
            {
                _workflowRuntimeAdapter.OnInstanceLoad(_workflowApplication);
            }
        }

        internal Guid BeginLoadInitiatedWorkflow(bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            AddTrackingProviderToWorkflowInstance(_workflowInstanceId);

            IAsyncResult asyncResult = null;
            //if (isDefaultTimeout)
            //{
            //    asyncResult = this.workflowApplication.BeginLoad(this.workflowInstanceId, asyncCallback, state);
            //}
            //else
            //{
            //    asyncResult = this.workflowApplication.BeginLoad(this.workflowInstanceId, timeout, asyncCallback, state);
            //}

            if (_workflowInstanceId != _workflowApplication.Id)
            {
                throw new InvalidOperationException("BeginLoadInitiatedWorkflow provided a different Guid then we were using");
            }

            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void LoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout)
        {
            LoadRunnableInitiatedWorkflowForUpdate(oldDefinitionIdentity, identityMask, isDefaultTimeout, timeout, _workflowInstanceId);
        }

        //internal DynamicUpdateMap RetrieveDynamicUpdateMap()
        //{
        //    DynamicUpdateMap updateMap = null;
        //    DynamicUpdateConfigurer dynamicUpdateConfigurer = this.testWorkflowRuntimeConfiguration.Settings.Find<DynamicUpdateConfigurer>();

        //    //If DynamicUpdaConfigure is null means that we are not running a "real" dynamicUpdate scenario 
        //    // E.g.: Tests that will validate WorkflowApplication Versioning (WorkflowApplication.GetRunnableInstance)
        //    if (dynamicUpdateConfigurer != null)
        //    {
        //        updateMap = dynamicUpdateConfigurer.UpdateMap;
        //    }
        //    else
        //    {
        //        Log.TraceInternal("Dynamic update map not found");
        //    }

        //    return updateMap;
        //}

        internal WorkflowApplicationInstance RetrieveWorkflowApplicationInstance(bool isDefaultTimeout, TimeSpan timeout)
        {
            if (this.SetDefaultOwner)
            {
                return RetrieveWorkflowApplicationInstanceByDefaultMetadata(isDefaultTimeout, timeout);
            }
            else
            {
                return RetrieveWorkflowApplicationInstanceByInstance(isDefaultTimeout, timeout);
            }
        }

        internal WorkflowApplicationInstance RetrieveWorkflowApplicationInstanceByInstance(bool isDefaultTimeout, TimeSpan timeout)
        {
            WorkflowApplicationInstance wfAppInstance = null;

            if (isDefaultTimeout)
            {
                wfAppInstance = WorkflowApplication.GetInstance(_workflowInstanceId, _workflowApplication.InstanceStore);
            }
            else
            {
                wfAppInstance = WorkflowApplication.GetInstance(_workflowInstanceId, _workflowApplication.InstanceStore, timeout);
            }
            if (wfAppInstance.InstanceId != _workflowInstanceId)
            {
                throw new InvalidOperationException("The instance loaded using WorkflowApplicationInstance is not the correct one.");
            }

            return wfAppInstance;
        }

        internal WorkflowApplicationInstance RetrieveWorkflowApplicationInstanceByDefaultMetadata(bool isDefaultTimeout, TimeSpan timeout)
        {
            WorkflowApplicationInstance wfAppInstance = null;

            if (isDefaultTimeout)
            {
                wfAppInstance = WorkflowApplication.GetRunnableInstance(_workflowApplication.InstanceStore);
            }
            else
            {
                wfAppInstance = WorkflowApplication.GetRunnableInstance(_workflowApplication.InstanceStore, timeout);
            }


            if (wfAppInstance.InstanceId != _workflowInstanceId)
            {
                throw new InvalidOperationException("The instance loaded using WorkflowApplicationInstance is not the correct one.");
            }

            return wfAppInstance;
        }

        internal WorkflowApplicationInstance RetrieveWorkflowApplicationInstance(string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout)
        {
            WorkflowIdentityFilter filter = (WorkflowIdentityFilter)Enum.Parse(typeof(WorkflowIdentityFilter), identityMask, true);
            WorkflowIdentity.TryParse(oldDefinitionIdentity, out WorkflowIdentity oldIdentity);

            this.DeleteDefaultInstanceOwner();
            this.CreateDefaultInstanceOwner(oldIdentity, filter);

            return RetrieveWorkflowApplicationInstanceByDefaultMetadata(isDefaultTimeout, timeout);
        }

        internal void LoadUpdateWorkflowApplicationInstance(WorkflowApplicationInstance wfAppInstance, bool isDefaultTimeout, TimeSpan timeout)
        {
            //DynamicUpdateMap updateMap = RetrieveDynamicUpdateMap();

            //if (isDefaultTimeout)
            //{
            //    if (updateMap == null)
            //    {
            //        this.workflowApplication.Load(wfAppInstance);
            //    }
            //    else
            //    {
            //        this.workflowApplication.Load(wfAppInstance, updateMap);
            //    }
            //}
            //else
            //{
            //    if (updateMap == null)
            //    {
            //        this.workflowApplication.Load(wfAppInstance, timeout);
            //    }
            //    else
            //    {
            //        this.workflowApplication.Load(wfAppInstance, updateMap, timeout);
            //    }
            //}

            //if (this.workflowInstanceId != this.workflowApplication.Id)
            //{
            //    throw new InvalidOperationException("LoadInitiatedWorkflowForUpdate provided a different Guid then we were using");
            //}

            //if (this.workflowRuntimeAdapter != null)
            //{
            //    this.workflowRuntimeAdapter.OnInstanceLoad(this.workflowApplication);
            //}
        }

        internal Guid BeginLoadUpdateWorkflowApplicationInstance(WorkflowApplicationInstance wfAppInstance, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };

            IAsyncResult asyncResult = null;


            //DynamicUpdateMap updateMap = RetrieveDynamicUpdateMap();

            //if (isDefaultTimeout)
            //{
            //    if (updateMap == null)
            //    {
            //        asyncResult = this.workflowApplication.BeginLoad(wfAppInstance, asyncCallback, asyncState);
            //    }
            //    else
            //    {
            //        asyncResult = this.workflowApplication.BeginLoad(wfAppInstance, updateMap, asyncCallback, asyncState);
            //    }
            //}
            //else
            //{
            //    if (updateMap == null)
            //    {
            //        asyncResult = this.workflowApplication.BeginLoad(wfAppInstance, timeout, asyncCallback, asyncState);
            //    }
            //    else
            //    {
            //        asyncResult = this.workflowApplication.BeginLoad(wfAppInstance, updateMap, timeout, asyncCallback, asyncState);
            //    }
            //}

            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndLoadUpdateWorkflowApplicationInstance(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();

            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);

            //this.workflowApplication.EndLoad(asyncResult);

            if (_workflowInstanceId != _workflowApplication.Id)
            {
                throw new InvalidOperationException("LoadInitiatedWorkflowForUpdate provided a different Guid then we were using");
            }

            if (_workflowRuntimeAdapter != null)
            {
                _workflowRuntimeAdapter.OnInstanceLoad(_workflowApplication);
            }
        }

        internal void LoadRunnableInitiatedWorkflowForUpdate(bool isDefaultTimeout, TimeSpan timeout)
        {
            LoadRunnableInitiatedWorkflowForUpdate(isDefaultTimeout, timeout, _workflowInstanceId);
        }

        internal void LoadRunnableInitiatedWorkflowForUpdate(bool isDefaultTimeout, TimeSpan timeout, Guid hintInstanceId)
        {
            _workflowInstanceId = hintInstanceId;

            if (_workflowInstanceId != Guid.Empty)
            {
                AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            }

            WorkflowApplicationInstance wfAppInstance = RetrieveWorkflowApplicationInstance(isDefaultTimeout, timeout);
            LoadUpdateWorkflowApplicationInstance(wfAppInstance, isDefaultTimeout, timeout);
        }
        internal void LoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout, Guid hintInstanceId)
        {
            _workflowInstanceId = hintInstanceId;

            if (_workflowInstanceId != Guid.Empty)
            {
                AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            }

            WorkflowApplicationInstance wfAppInstance = RetrieveWorkflowApplicationInstance(oldDefinitionIdentity, identityMask, isDefaultTimeout, timeout);
            LoadUpdateWorkflowApplicationInstance(wfAppInstance, isDefaultTimeout, timeout);
        }


        internal void LoadRunnableInitiatedWorkflow(bool isDefaultTimeout, TimeSpan timeout)
        {
            LoadRunnableInitiatedWorkflow(isDefaultTimeout, timeout, _workflowInstanceId);
        }

        internal void LoadRunnableInitiatedWorkflow(bool isDefaultTimeout, TimeSpan timeout, Guid hintInstanceId)
        {
            _workflowInstanceId = hintInstanceId;

            if (_workflowInstanceId != Guid.Empty)
            {
                AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            }

            if (isDefaultTimeout)
            {
                _workflowApplication.LoadRunnableInstance();
            }
            else
            {
                _workflowApplication.LoadRunnableInstance(timeout);
            }
            if (_workflowApplication.Id != _workflowInstanceId)
            {
                throw new InvalidOperationException("Load should not be called on unknown instance");
            }
            _workflowInstanceId = _workflowApplication.Id;
            if (_workflowRuntimeAdapter != null)
            {
                _workflowRuntimeAdapter.OnInstanceLoad(_workflowApplication);
            }
        }


        internal Guid BeginLoadRunnableInitiatedWorkflow(bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            if (_workflowInstanceId != Guid.Empty)
            {
                AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            }

            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };

            IAsyncResult asyncResult = null;

            //if (isDefaultTimeout)
            //{
            //    asyncResult = this.workflowApplication.BeginLoadRunnableInstance(asyncCallback, asyncState);
            //}
            //else
            //{
            //    asyncResult = this.workflowApplication.BeginLoadRunnableInstance(timeout, asyncCallback, asyncState);
            //}

            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }


        internal void EndLoadRunnableInitiatedWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();

            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            //this.workflowApplication.EndLoad(asyncResult);

            if (_workflowApplication.Id != _workflowInstanceId)
            {
                throw new InvalidOperationException("Load should not be called on unknown instance");
            }
            _workflowInstanceId = _workflowApplication.Id;
            if (_workflowRuntimeAdapter != null)
            {
                _workflowRuntimeAdapter.OnInstanceLoad(_workflowApplication);
            }
        }

        internal Guid BeginLoadRunnableInitiatedWorkflowForUpdate(string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            if (_workflowInstanceId != Guid.Empty)
            {
                AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            }

            Guid asynResultRetriveId = BeginRetrieveWorkflowApplicationInstance(oldDefinitionIdentity, identityMask, isDefaultTimeout, timeout, asyncCallback, state);
            return asynResultRetriveId;
        }

        internal void EndLoadRunnableInitiatedWorkflowForUpdate(Guid asynResultRetriveId, string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            WorkflowApplicationInstance wfAppInstance = EndRetrieveWorkflowApplicationInstance(oldDefinitionIdentity, identityMask, asynResultRetriveId);
            Guid asynResultLoadId = BeginLoadUpdateWorkflowApplicationInstance(wfAppInstance, isDefaultTimeout, timeout, asyncCallback, state);
            EndLoadUpdateWorkflowApplicationInstance(asynResultLoadId);
        }

        internal Guid BeginLoadInitiatedWorkflowForUpdate(bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            AddTrackingProviderToWorkflowInstance(_workflowInstanceId);
            Guid asynResultRetriveId = BeginRetrieveWorkflowApplicationInstance(null, null, isDefaultTimeout, timeout, asyncCallback, state);
            return asynResultRetriveId;
        }

        internal void EndLoadInitiatedWorkflowForUpdate(Guid asynResultRetriveId, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            WorkflowApplicationInstance wfAppInstance = EndRetrieveWorkflowApplicationInstance(null, null, asynResultRetriveId);
            Guid asynResultLoadId = BeginLoadUpdateWorkflowApplicationInstance(wfAppInstance, isDefaultTimeout, timeout, asyncCallback, state);
            EndLoadUpdateWorkflowApplicationInstance(asynResultLoadId);
        }

        internal Guid BeginRetrieveWorkflowApplicationInstance(string oldDefinitionIdentity, string identityMask, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            bool isLoadbyInstance = false;

            if (oldDefinitionIdentity == null && identityMask == null)
            {
                isLoadbyInstance = true;
            }

            IAsyncResult asyncResult = null;

            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };

            if (isLoadbyInstance)
            {
                //if (isDefaultTimeout)
                //{
                //    asyncResult = WorkflowApplication.BeginGetInstance(this.workflowInstanceId, this.workflowApplication.InstanceStore, asyncCallback, asyncState);
                //}
                //else
                //{
                //    asyncResult = WorkflowApplication.BeginGetInstance(this.workflowInstanceId, this.workflowApplication.InstanceStore, timeout, asyncCallback, asyncState);
                //}
            }
            else
            {
                WorkflowIdentityFilter filter = (WorkflowIdentityFilter)Enum.Parse(typeof(WorkflowIdentityFilter), identityMask, true);
                WorkflowIdentity.TryParse(oldDefinitionIdentity, out WorkflowIdentity oldIdentity);

                CreateDefaultInstanceOwner(oldIdentity, filter);

                //if (isDefaultTimeout)
                //{
                //    asyncResult = WorkflowApplication.BeginGetRunnableInstance(this.workflowApplication.InstanceStore, asyncCallback, asyncState);
                //}
                //else
                //{
                //    asyncResult = WorkflowApplication.BeginGetRunnableInstance(this.workflowApplication.InstanceStore, timeout, asyncCallback, asyncState);
                //}
            }

            return _asyncResultCollection.AddAsyncResult(asyncResult); ;
        }

        internal WorkflowApplicationInstance EndRetrieveWorkflowApplicationInstance(string oldDefinitionIdentity, string identityMask, Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();

            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            //bool isLoadbyInstance = false;
            WorkflowApplicationInstance wfAppInstance = null;

            if (oldDefinitionIdentity == null && identityMask == null)
            {
                //isLoadbyInstance = true;
            }

            //if (isLoadbyInstance)
            //{
            //    wfAppInstance = WorkflowApplication.EndGetInstance(asyncResult);
            //}
            //else
            //{
            //    wfAppInstance = WorkflowApplication.EndGetRunnableInstance(asyncResult);
            //}

            if (wfAppInstance.InstanceId != _workflowInstanceId)
            {
                throw new InvalidOperationException("The instance loaded using WorkflowApplicationInstance is not the correct one.");
            }

            return wfAppInstance;
        }

        private void CreateExtensionObjects(Type[] extensionTypes, List<object> extensionCollection)
        {
            foreach (Type extensionType in extensionTypes)
            {
                extensionCollection.Add(Activator.CreateInstance(extensionType));
            }
        }

        private void LoadWorkflowHelper(bool isLoadById, List<object> extensionCollection, bool isDefaultTimeout, TimeSpan timeout)
        {
            InitiateWorkflowForLoad(extensionCollection);
            if (isLoadById)
            {
                LoadInitiatedWorkflow(isDefaultTimeout, timeout);
            }
            else
            {
                LoadRunnableInitiatedWorkflow(isDefaultTimeout, timeout);
            }
        }

        public void LoadAndUpdateWorkflowHelper(string newDefinitionIdentity, bool isLoadById, List<object> extensionCollection, bool isDefaultTimeout, TimeSpan timeout)
        {
            //TestActivity newDefinition;
            //WorkflowIdentity newIdentity = null;
            //string oldIdentity = null;

            //DynamicUpdateConfigurer dynamicUpdateConfigurer = this.testWorkflowRuntimeConfiguration.Settings.Find<DynamicUpdateConfigurer>();
            //if (dynamicUpdateConfigurer == null)
            //{
            //    throw new InvalidOperationException("DynamicUpdateConfigurer not set");
            //}

            //oldIdentity = this.testWorkflowRuntimeConfiguration.DefinitionIdentity;
            //this.testWorkflowRuntimeConfiguration.DefinitionIdentity = newDefinitionIdentity;
            //WorkflowIdentity.TryParse(this.testWorkflowRuntimeConfiguration.DefinitionIdentity, out newIdentity);

            //newDefinition = dynamicUpdateConfigurer.GetDefinition(newIdentity);
            //if (newDefinition == null)
            //{
            //    throw new InvalidOperationException(string.Format("Not able to find in the DynamicUpdateConfigurer a definition for the specified definition identity string ({0})", newDefinitionIdentity));
            //}

            //dynamicUpdateConfigurer.WorkflowDefinitionIdentity = newIdentity;
            //this.productActivity = newDefinition.ProductActivity;
            //InitiateWorkflowForLoad(extensionCollection);
            //dynamicUpdateConfigurer.Configure(this.workflowApplication, this.testWorkflowRuntimeConfiguration.Settings);
            //if (isLoadById)
            //{
            //    LoadInitiatedWorkflowForUpdate(isDefaultTimeout, timeout);
            //}
            //else
            //{
            //    LoadRunnableInitiatedWorkflowForUpdate(oldIdentity, WorkflowIdentityFilter.Exact.ToString(), isDefaultTimeout, timeout, this.workflowInstanceId);
            //}
        }


        private Guid BeginLoadWorkflowHelper(List<object> extensionCollection, bool isDefaultTimeout, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            InitiateWorkflowForLoad(extensionCollection);

            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };

            return BeginLoadInitiatedWorkflow(isDefaultTimeout, timeout, asyncCallback, asyncState);
        }


        internal void UnloadWorkflow()
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Unload();
        }

        internal void UnloadWorkflow(TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Unload(timeout);
        }

        internal Guid BeginUnloadWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginUnload(timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginUnloadWorkflow(AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginUnload(asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndUnloadWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            _workflowApplication.EndUnload(asyncResult);
        }

        internal void AbortWorkflow(string reason)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Abort(reason);
        }

        internal void CancelWorkflow()
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Cancel();
        }

        internal void CancelWorkflow(TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Cancel(timeout);
        }

        internal Guid BeginCancelWorkflow(TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginCancel(timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginCancelWorkflow(AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginCancel(asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndCancelWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            _workflowApplication.EndCancel(asyncResult);
        }

        internal void TerminateWorkflow(string reason)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Terminate(reason);
        }

        internal void TerminateWorkflow(string reason, TimeSpan timeout)
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Terminate(reason, timeout);
        }

        internal Guid BeginTerminateWorkflow(string reason, TimeSpan timeout, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginTerminate(reason, timeout, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal Guid BeginTerminateWorkflow(string reason, AsyncCallback asyncCallback, object state)
        {
            ThrowIfWorkflowInstanceInvalid();
            TestWorkflowRuntimeAsyncState asyncState = new TestWorkflowRuntimeAsyncState()
            {
                Instance = _workflowApplication,
                State = state
            };
            IAsyncResult asyncResult = _workflowApplication.BeginTerminate(reason, asyncCallback, asyncState);
            return _asyncResultCollection.AddAsyncResult(asyncResult);
        }

        internal void EndTerminateWorkflow(Guid asyncResultId)
        {
            ThrowIfWorkflowInstanceInvalid();
            IAsyncResult asyncResult = _asyncResultCollection.GetAsyncResult(asyncResultId, true);
            _workflowApplication.EndTerminate(asyncResult);
        }

        internal void WaitForRunnableEvent(TimeSpan timeout)
        {
            if (this.SetDefaultOwner == false)
            {
                throw new InvalidOperationException("WaitForRunnableEvent is possible only when DefaultOwner is true");
            }
            bool isfoundRunnable = false;
            foreach (InstancePersistenceEvent ppEvent in _instanceStore.WaitForEvents(_defaultOwnerInstanceHandle, timeout))
            {
                if (ppEvent.Equals(HasRunnableWorkflowEvent.Value))
                {
                    isfoundRunnable = true;
                    break;
                }
            }
            if (!isfoundRunnable)
            {
                throw new InvalidOperationException("Store does not supported HasRunnableWorkflowEvent.");
            }
        }

        internal void SetDefaultOwnerMetadata(XName metadataName, InstanceValue instanceValue)
        {
            _defaultOwnerMetadata[metadataName] = instanceValue;
        }

        internal void SetInstanceMetadata(Dictionary<XName, object> metadataDic)
        {
            _workflowApplication.AddInitialInstanceValues(metadataDic);
        }

        public void SetInstanceStore(InstanceStore store)
        {
            _workflowApplication.InstanceStore = store;
        }

        internal ReadOnlyCollection<BookmarkInfo> GetBookmarks()
        {
            return _workflowApplication.GetBookmarks();
        }

        internal ReadOnlyCollection<BookmarkInfo> GetBookmarks(TimeSpan timeout)
        {
            return _workflowApplication.GetBookmarks(timeout);
        }

        internal void AddTrackingProviderToWorkflowInstance(Guid instanceId)
        {
            _trackingConfig = new List<TrackingConfiguration>();

            TrackingConfiguration inMemoryTrackingConfig = new TrackingConfiguration
            {
                TrackingParticipantName = "TestTrackingParticipant",
                TrackingParticipantType = TrackingParticipantType.InMemoryTrackingParticipant,
                TestProfileType = TestProfileType.AllTrackpointsProfile
            };
            _trackingConfig.Add(inMemoryTrackingConfig);

            TestTrackingDataManager.GetInstance(instanceId).InstantiateTrackingParticipants(_trackingConfig);

            foreach (TrackingParticipant trackingParticipant in TestTrackingDataManager.GetInstance(instanceId).GetTrackingParticipants())
            {
                _workflowApplication.Extensions.Add(trackingParticipant);
            }

            //ThrowIfWorkflowInstanceInvalid();

            //WorkflowTrackingParticipantsConfigurer trackingParticipantsConfigurer = this.testWorkflowRuntimeConfiguration.Settings.Find<WorkflowTrackingParticipantsConfigurer>();
            //if (trackingParticipantsConfigurer == null)
            //{
            //    TestTrackingDataManager.GetInstance(instanceId).InstantiateTrackingParticipants(TestConfiguration.Current.TrackingServiceConfigurations);
            //    foreach (TrackingParticipant trackingParticipant in TestTrackingDataManager.GetInstance(instanceId).GetTrackingParticipants())
            //    {
            //        this.workflowApplication.Extensions.Add(trackingParticipant);
            //    }
            //}
            //else
            //{
            //    trackingParticipantsConfigurer.Configure(this.workflowApplication, this.testWorkflowRuntimeConfiguration.Settings);
            //}
        }

        internal void AddExtension(object extension)
        {
            ThrowIfWorkflowInstanceInvalid();
            if (null == extension)
            {
                throw new Exception("Parameter extension cannot be null");
            }

            _workflowApplication.Extensions.Add(extension);
        }

        public void AddExtension<TExtension>(Func<TExtension> extensionProvider)
            where TExtension : class
        {
            ThrowIfWorkflowInstanceInvalid();
            _workflowApplication.Extensions.Add(extensionProvider);
        }

        internal void WaitForTrace(IActualTraceStep trace, int numOccurances)
        {
            TestTraceManager.Instance.WaitForTrace(_workflowApplication.Id, trace, numOccurances);
        }

        internal void Dispose()
        {
            // WorkflowApplication is not Disposable //
            if (_defaultOwnerInstanceHandle != null)
            {
                // this will also abort any pending BeginWaitForEvents async calls
                _defaultOwnerInstanceHandle.Free();
            }
        }

        // This should not be called if any other, incomplete tests are still using the same InstanceStore
        private void CreateDefaultInstanceOwner(WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
        {
            ThrowIfWorkflowInstanceInvalid();
            //Log.TraceInternal("[RemoteWorkflowRuntime] Creating DefaultInstanceOwner for InstanceStore using default 4.5 metadata");
            WorkflowApplication.CreateDefaultInstanceOwner(_workflowApplication.InstanceStore, definitionIdentity, identityFilter);
        }

        private void CreateDefaultInstanceOwner(InstanceStore store, WorkflowIdentity definitionIdentity, WorkflowIdentityFilter identityFilter)
        {
            //Log.TraceInternal("[RemoteWorkflowRuntime] Creating DefaultInstanceOwner for InstanceStore using default 4.5 metadata");
            WorkflowApplication.CreateDefaultInstanceOwner(store, definitionIdentity, identityFilter);
        }

        internal void DeleteDefaultInstanceOwner()
        {
            ThrowIfWorkflowInstanceInvalid();
            //Log.TraceInternal("[RemoteWorkflowRuntime] Deleting DefaultInstanceOwner");
            //WorkflowApplication.DeleteDefaultInstanceOwner(this.workflowApplication.InstanceStore);
        }

        private void OnWorkflowInstanceCompleted(WorkflowApplicationCompletedEventArgs e)
        {
            if (this.OnWorkflowCompleted != null)
            {
                OnWorkflowCompleted(null, new TestWorkflowCompletedEventArgs(_workflowApplication, e));
            }
            _lastException = e.TerminationException;
            //this.completionState = WorkflowCompletionState.Completed;
            _outputs = e.Outputs;
            //Add trace to test trace framework
            TestTraceManager.Instance.AddTrace(this.CurrentWorkflowInstanceId, new SynchronizeTrace(CompletedOrAbortedHandlerCalled));
            SynchronizeTrace.Trace(this.CurrentWorkflowInstanceId, CompletedOrAbortedHandlerCalled);
        }

        private void OnWorkflowInstanceAborted(WorkflowApplicationAbortedEventArgs e)
        {
            if (this.OnWorkflowAborted != null)
            {
                OnWorkflowAborted(null, new TestWorkflowAbortedEventArgs(_workflowApplication, e));
            }
            _lastException = e.Reason;
            //this.completionState = WorkflowCompletionState.Aborted;
            //Add trace to test trace framework
            TestTraceManager.Instance.AddTrace(this.CurrentWorkflowInstanceId, new SynchronizeTrace(CompletedOrAbortedHandlerCalled));
            SynchronizeTrace.Trace(this.CurrentWorkflowInstanceId, CompletedOrAbortedHandlerCalled);

            WorkflowAbortedTrace.Trace(this.CurrentWorkflowInstanceId, e.Reason);
        }

        private void OnWorkflowInstanceIdle(WorkflowApplicationIdleEventArgs args)
        {
            if (this.OnWorkflowIdle != null)
            {
                this.OnWorkflowIdle(null, new TestWorkflowIdleEventArgs(_workflowApplication, args));
            }
        }

        private PersistableIdleAction OnWorkflowInstanceIdleAndPersistable(WorkflowApplicationIdleEventArgs args)
        {
            TestWorkflowIdleAndPersistableEventArgs testWorkflowIdleAndPersistableEventArgs = new TestWorkflowIdleAndPersistableEventArgs(_workflowApplication, args);
            if (this.OnWorkflowIdleAndPersistable != null)
            {
                OnWorkflowIdleAndPersistable(null, testWorkflowIdleAndPersistableEventArgs);
            }

            return testWorkflowIdleAndPersistableEventArgs.Action;
        }

        private UnhandledExceptionAction OnWorkflowInstanceUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs e)
        {
            TestWorkflowUnhandledExceptionEventArgs testWorkflowUnhandledExceptionEventArgs = new TestWorkflowUnhandledExceptionEventArgs(_workflowApplication, e);
            _lastException = e.UnhandledException;
            if (this.OnWorkflowUnhandledException != null)
            {
                OnWorkflowUnhandledException(null, testWorkflowUnhandledExceptionEventArgs);
            }

            return testWorkflowUnhandledExceptionEventArgs.Action;
        }

        private void OnWorkflowInstanceUnloaded(WorkflowApplicationEventArgs args)
        {
            if (this.OnWorkflowUnloaded != null)
            {
                this.OnWorkflowUnloaded(null, new TestWorkflowUnloadedEventArgs(_workflowApplication, args));
            }
        }

        private void ThrowIfWorkflowInstanceInvalid()
        {
            if (null == _workflowApplication)
            {
                throw new Exception("WorkflowApplication is null. Start workflow using ExecuteWorkflow.");
            }
        }

        private void ThrowIfWorkflowInvokerInvalid()
        {
            if (null == _workflowInvoker)
            {
                throw new Exception("WorkflowInvoker is null. Start workflow using ExecuteWorkflowInvoker.");
            }
        }

        private void InitInstanceStore()
        {
            ThrowIfWorkflowInstanceInvalid();

            if (_instanceStoreType == null)
            {
                //Log.TraceInternal("[RemoteWorkflowRuntime] Not adding persistence. The instanceStoreType is null.");
                return;
            }

            //This should happen as late as possible so that the user has a chance to set the InstanceStoreType property
            //if (this.persistenceProviderInfo == null)
            //{
            //    this.persistenceProviderInfo = new TestPersistenceProviderInfo()
            //    {
            //        FactoryType = this.InstanceStoreType,
            //        SqlDatabaseName = TestParameters.SqlPersistenceDatabaseName,
            //        SqlConnection = TestParameters.SqlPersistenceConnectionString,
            //        CanUseKeys = TestParameters.SqlPersistenceCanUseKeys,
            //        DBVersion = this.instanceStoreVersion
            //    };
            //}

            //if (this.persistenceProviderHelper == null)
            //{
            //    this.persistenceProviderHelper = new PersistenceProviderHelper(this.persistenceProviderInfo);
            //}

            if (_instanceStore != null)
            {
                //this.instanceStore = this.persistenceProviderHelper.CreateWorkflowInstanceStore(this.instanceStoreVersion);
                //this.instanceStore = this.persistenceProviderHelper.CreateWorkflowInstanceStore();
                this.ConfigureInstanceStore();
            }
        }


        private void AddInstanceStoreToWorkflowInstance()
        {
            InitInstanceStore();

            if (_instanceStore == null)
            {
                return;
            }

            // Add PersistenceProvider if it is not already added
            _workflowApplication.InstanceStore = _instanceStore;

            //Set InstanceScope for this workflow application
            //if (this.SetDefaultOwner)
            //{
            //    object hostTypeValue;
            //    if (IsUsingDefaultOwnerMetadata40())
            //    {
            //        hostTypeValue = this.instanceHostTypeValue40;
            //    }
            //    else
            //    {
            //        hostTypeValue = Workflow45Namespace.WorkflowApplication;
            //    }

            //    if (this.defaultOwnerMetadata.ContainsKey(WorkflowNamespace.WorkflowHostTypeName))
            //    {
            //        hostTypeValue = this.defaultOwnerMetadata[WorkflowNamespace.WorkflowHostTypeName].Value;
            //    }
            //    this.workflowApplication.AddInitialInstanceValues(new Dictionary<XName, object>() { { WorkflowNamespace.WorkflowHostTypeName, hostTypeValue } });
            //}
        }

        private bool IsUsingDefaultOwnerMetadata40()
        {
            // Assuming that if using null identity it is a 4.0 scenario. 
            //return (this.testWorkflowRuntimeConfiguration.DefinitionIdentity == null);
            return true;
        }

        //Assuming that if identity == null, the tester wants to use a 4.0 scenario.
        //In case of 4.5 scenario, the tester will have to Set the identity to a valid value. 
        //LoadRunnable has to be called with identityFilter or the user has to set the defualtOwner themselves. 
        //In the first case we will use WFApp.CreateDefaultInstanceOwner to cofnigure the Store. in the second case, 
        //the tester has to use ConfigureStoreCustomMetadata40 or ConfigureStoreCustomMetadata45.
        private void ConfigureInstanceStore()
        {
            if (this.SetDefaultOwner && _instanceStore.DefaultInstanceOwner == null)
            {
                _defaultOwnerInstanceHandle = _instanceStore.CreateInstanceHandle();
                if (IsUsingDefaultOwnerMetadata40())
                {
                    //if (!defaultOwnerMetadata.ContainsKey(WorkflowNamespace.WorkflowHostTypeName))
                    //{
                    //    this.SetDefaultOwnerMetadata(WorkflowNamespace.WorkflowHostTypeName, new InstanceValue(this.instanceHostTypeValue40));
                    //}

                    ConfigureStoreCustomMetadata40(_instanceStore, _defaultOwnerInstanceHandle, _defaultOwnerMetadata);
                }
                else
                {
                    if (_defaultOwnerMetadata.Count > 0)
                    {
                        ConfigureStoreCustomMetadata45(_instanceStore, _defaultOwnerInstanceHandle, _defaultOwnerMetadata);
                    }
                    else
                    {
                        CreateDefaultInstanceOwner(_instanceStore, this.DefinitionIdentity, WorkflowIdentityFilter.Exact);
                    }
                }
            }
        }

        internal void ConfigureStoreCustomMetadata40(Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            this.DeleteDefaultInstanceOwner();
            if (_defaultOwnerInstanceHandle == null)
            {
                _defaultOwnerInstanceHandle = _instanceStore.CreateInstanceHandle();
            }
            ConfigureStoreCustomMetadata40(_instanceStore, _defaultOwnerInstanceHandle, instanceOwnerMetadata);
        }

        private void ConfigureStoreCustomMetadata40(InstanceStore instanceStore, InstanceHandle instanceHandle, Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            //Log.TraceInternal("[RemoteWorkflowRuntime] Creating DefaultInstanceOwner for InstanceStore using 4.0 metadata");
            CreateWorkflowOwnerCommand createOwnerCommand = new CreateWorkflowOwnerCommand();
            foreach (KeyValuePair<XName, InstanceValue> ownerMetadata in instanceOwnerMetadata)
            {
                createOwnerCommand.InstanceOwnerMetadata.Add(ownerMetadata.Key, ownerMetadata.Value);
            }
            InstanceView view = instanceStore.Execute(instanceHandle, createOwnerCommand, TimeSpan.FromSeconds(30));
            if (view != null && view.InstanceOwner != null)
            {
                instanceStore.DefaultInstanceOwner = view.InstanceOwner;
            }
        }

        internal void ConfigureStoreCustomMetadata45(Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            this.DeleteDefaultInstanceOwner();
            if (_defaultOwnerInstanceHandle == null)
            {
                _defaultOwnerInstanceHandle = _instanceStore.CreateInstanceHandle();
            }
            ConfigureStoreCustomMetadata45(_instanceStore, _defaultOwnerInstanceHandle, instanceOwnerMetadata);
        }

        private void ConfigureStoreCustomMetadata45(InstanceStore instanceStore, InstanceHandle instanceHandle, Dictionary<XName, InstanceValue> instanceOwnerMetadata)
        {
            //Log.TraceInternal("[RemoteWorkflowRuntime] Creating DefaultInstanceOwner for InstanceStore using custom 4.5 metadata");

            CreateWorkflowOwnerWithIdentityCommand createOwnerCommand = new CreateWorkflowOwnerWithIdentityCommand();
            foreach (KeyValuePair<XName, InstanceValue> ownerMetadata in instanceOwnerMetadata)
            {
                createOwnerCommand.InstanceOwnerMetadata.Add(ownerMetadata.Key, ownerMetadata.Value);
            }
            InstanceView view = instanceStore.Execute(instanceHandle, createOwnerCommand, TimeSpan.FromSeconds(30));
            if (view != null && view.InstanceOwner != null)
            {
                _instanceStore.DefaultInstanceOwner = view.InstanceOwner;
            }
        }

        private void InitWorkflowInstanceWithEvents()
        {
            // Add the Completed and Idle events
            _workflowApplication.Completed = OnWorkflowInstanceCompleted;
            _workflowApplication.Idle = OnWorkflowInstanceIdle;
            _workflowApplication.PersistableIdle = OnWorkflowInstanceIdleAndPersistable;
            _workflowApplication.Aborted = OnWorkflowInstanceAborted;
            _workflowApplication.OnUnhandledException = OnWorkflowInstanceUnhandledException;
            _workflowApplication.Unloaded = OnWorkflowInstanceUnloaded;

            if (_instanceStore != null)
            {
                _workflowApplication.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs args)
                {
                    //this.instanceStore.BeginWaitForEvents(
                    //    this.defaultOwnerInstanceHandle,
                    //    this.waitForCompletionTimeout,
                    //    new AsyncCallback(WorkflowTimerExpired),
                    //    null);

                    return _idleAction;
                };
            }
            //if (testWorkflowRuntimeConfiguration.OnIdleAction != TestOnIdleAction.Default)
            //{
            //    this.workflowApplication.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs args)
            //    {
            //        if (testWorkflowRuntimeConfiguration.OnIdleAction == TestOnIdleAction.Unload)
            //        {
            //            Log.TraceInternal("[RemoteWorkflowRuntime] PersistableIdle handler Subscribing for available instances");
            //            this.instanceStore.BeginWaitForEvents(
            //                this.defaultOwnerInstanceHandle,
            //                this.waitForCompletionTimeout,
            //                new AsyncCallback(WorkflowTimerExpired),
            //                null);

            //            return PersistableIdleAction.Unload;
            //        }

            //        if (testWorkflowRuntimeConfiguration.OnIdleAction == TestOnIdleAction.UnloadNoAutoLoad)
            //        {
            //            return PersistableIdleAction.Unload;
            //        }

            //        if (testWorkflowRuntimeConfiguration.OnIdleAction == TestOnIdleAction.Persist)
            //        {
            //            return PersistableIdleAction.Persist;
            //        }

            //        return PersistableIdleAction.None;
            //    };
            //}
        }


        private void WorkflowTimerExpired(IAsyncResult result)
        {
            //Here is the scenario where this method will be used
            //Assumptions:
            //  * We have a sequence of serial delays. 
            //  * The first delay got executed
            //  * The WF got unloaded and the timer expired. 
            //
            //  At this point, SWIS will fire an event indicating that it has runnable instances 
            // and WorkflowTimerExpired will get called. So we can LoadRunnableWorkflow. 
            // However, SWIS will keep firing the event. To reset this event, so the second delay 
            // would be also treated as a Durable delay, we have to load the instance and make it fail. 
            // As it will be the socnd time that the instnace will be loaded, an InstanceNotReadyException 
            // is expected to throw


            if (_defaultOwnerInstanceHandle.IsValid)
            {
                //Log.TraceInternal("[RemoteWorkflowRuntime] Loading runnable instance");

                this.LoadRunnableWorkflow(TimeSpan.Zero, true);

                // need to try loading second time and to fail, so the timer expired event (instance ready event) can be reset
                // putting on a temp WFApp to not mess with this.worklfowApplication
                WorkflowApplication tempWorkflowApplication = new WorkflowApplication(_productActivity);
                tempWorkflowApplication.Extensions.Add(TestRuntime.TraceListenerExtension);
                tempWorkflowApplication.InstanceStore = _instanceStore;
                try
                {
                    // This method is expected to throw InstanceNotReadyException
                    tempWorkflowApplication.LoadRunnableInstance();
                    throw new Exception("tempWorkflowApplication.Load(); was expected to throw InstanceNotReadyException");
                }
                catch (InstanceNotReadyException)
                {
                    // InstanceNotReadyException is expected so the event will be reset
                }

                _workflowApplication.Run();
            }
        }

        private void AddSymbolResolver()
        {
            ThrowIfWorkflowInstanceInvalid();
            if (_symbolResolver != null)
            {
                _workflowApplication.Extensions.Add(_symbolResolver);
            }
        }

        internal int GetEventHandlerCount(int eventId)
        {
            Delegate[] invocationList;

            switch (eventId)
            {
                case 1:
                    if (null == OnWorkflowAborted)
                    {
                        return 0;
                    };
                    invocationList = OnWorkflowAborted.GetInvocationList();
                    break;
                case 2:
                    if (null == OnWorkflowIdle)
                    {
                        return 0;
                    }
                    invocationList = OnWorkflowIdle.GetInvocationList();
                    break;
                case 3:
                    if (null == OnWorkflowCompleted)
                    {
                        return 0;
                    }
                    invocationList = OnWorkflowCompleted.GetInvocationList();
                    break;
                case 4:
                    if (null == OnWorkflowUnhandledException)
                    {
                        return 0;
                    }
                    invocationList = OnWorkflowUnhandledException.GetInvocationList();
                    break;
                case 5:
                    if (null == OnWorkflowIdleAndPersistable)
                    {
                        return 0;
                    }
                    invocationList = OnWorkflowIdleAndPersistable.GetInvocationList();
                    break;
                default:
                    throw new ArgumentException("eventId is not correct");
            }

            return (invocationList == null ? 0 : invocationList.Length);
        }

        internal WorkflowTrackingWatcher GetTrackingWatcher()
        {
            return new WorkflowTrackingWatcher(this.CurrentWorkflowInstanceId, this);
        }

        private class AsyncResultCollection
        {
            private Dictionary<Guid, IAsyncResult> _asyncResultCollection = new Dictionary<Guid, IAsyncResult>();

            internal Guid AddAsyncResult(IAsyncResult asyncResult)
            {
                Guid asyncResultGuid = Guid.NewGuid();
                _asyncResultCollection.Add(asyncResultGuid, asyncResult);
                return asyncResultGuid;
            }

            internal IAsyncResult GetAsyncResult(Guid asyncResultKey, bool isRemove)
            {
                if (_asyncResultCollection.TryGetValue(asyncResultKey, out IAsyncResult asyncResult))
                {
                    if (isRemove)
                    {
                        _asyncResultCollection.Remove(asyncResultKey);
                    }
                }
                else
                {
                    throw new Exception("Could not find IAsyncResult for Id : " + asyncResultKey);
                }

                return asyncResult;
            }
        }
    }
}

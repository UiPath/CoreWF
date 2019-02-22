// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Hosting;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace TestCases.Runtime.Common
{
    public static class RuntimeHelper
    {
        private static readonly TimeSpan s_DEFAULT_TIMEOUT = TimeSpan.FromMinutes(1.0);

        public static void InvokeActivity(Activity activity)
        {
            //Can we get default timeout used by product?
            InvokeActivity(activity, s_DEFAULT_TIMEOUT);
        }

        public static void InvokeActivity(Activity activity, TimeSpan timeout)
        {
            //Log.Info("Using ActivityInvoke()");
            WorkflowInvoker.Invoke(activity, timeout);
        }

        public static void InvokeActivity(Activity activity, SymbolResolver symbolResolver)
        {
            InvokeActivity(activity, symbolResolver, s_DEFAULT_TIMEOUT);
        }

        public static IDictionary<string, object> InvokeActivity(Activity activity, IDictionary<string, object> dictionary)
        {
            return InvokeActivity(activity, dictionary, s_DEFAULT_TIMEOUT);
        }

        public static IDictionary<string, object> InvokeActivity(Activity activity, IDictionary<string, object> dictionary, TimeSpan timeout)
        {
            return WorkflowInvoker.Invoke(activity, dictionary, timeout);
        }


        public static WorkflowApplication CreateWorkflow(Activity activity, Dictionary<string, object> inputs, params object[] extensions)
        {
            WorkflowApplication instance = new WorkflowApplication(activity, inputs);

            if (extensions != null)
            {
                for (int i = 0; i < extensions.Length; i++)
                {
                    instance.Extensions.Add(extensions[i]);
                }
            }
            return instance;
        }

        public static WorkflowApplication CreateWorkflowApplication(Activity activity, AutoResetEvent completedOrAbortedEvent)
        {
            WorkflowApplication instance = new WorkflowApplication(activity)
            {
                Completed = (arg) =>
                {
                    completedOrAbortedEvent.Set();
                },
                Aborted = (arg) =>
                {
                    completedOrAbortedEvent.Set();
                }
            };
            return instance;
        }

        public static IDictionary<string, object> RunWorkflow(WorkflowApplication instance)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            Exception workflowException = null;
            IDictionary<string, object> outputs = null;
            instance.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
            {
                if (e.TerminationException != null)
                {
                    workflowException = e.TerminationException;
                }

                outputs = e.Outputs;

                mre.Set();
            };


            instance.Run();
            mre.WaitOne();
            if (workflowException != null)
            {
                throw workflowException;
            }

            return outputs;
        }

        public static IDictionary<string, object> ExecuteWorkflow(Activity activity)
        {
            return (RunWorkflow(CreateWorkflow(activity, null)));
        }


        public static void RunInAllOptions(Activity activity)
        {
            InvokeActivity(activity);
            ExecuteWorkflow(activity);
        }

        public static void HandleException(Exception e)
        {
            Console.WriteLine(e);
            //Log.Info(e);
        }

        public static void ValidateWorkflow(Guid workflowInstanceId, TestActivity testWorkflowDefinition)
        {
            ActualTrace actualTrace = TestTraceManager.Instance.GetInstanceActualTrace(workflowInstanceId);
            ExpectedTrace expectedTrace = testWorkflowDefinition.GetExpectedTrace();

            actualTrace.Validate(testWorkflowDefinition.GetExpectedTrace());
        }

        //public static void AddAPersistenceProvider(WorkflowApplication instance, Type persistenceProviderFactoryType)
        //{
        //    PersistenceProviderHelper helper = new PersistenceProviderHelper(persistenceProviderFactoryType);
        //    instance.InstanceStore = helper.CreateWorkflowInstanceStore();
        //}

        //public static void AddTrackingProvider(WorkflowApplication instance)
        //{
        //    TestTrackingDataManager.GetInstance(instance.Id).InstantiateTrackingParticipants(TestConfiguration.Current.TrackingServiceConfigurations);
        //    foreach (TrackingParticipant trackingParticipant in TestTrackingDataManager.GetInstance(instance.Id).GetTrackingParticipants())
        //    {
        //        instance.Extensions.Add(trackingParticipant);
        //    }
        //}

        //public static PersistenceProvider GetPersistenceProvider(Type persistenceProviderFactoryType, Guid instanceId)
        //{
        //    PersistenceProviderHelper persistenceProviderHelper = new PersistenceProviderHelper(persistenceProviderFactoryType);
        //    PersistenceProviderFactory persistenceProviderFactory = persistenceProviderHelper.CreateInstancePersistenceProviderFactory(true, true);
        //    return persistenceProviderFactory.CreateProvider(instanceId);
        //}

        //This method is used to create a dummy parameter instance
        public static object GetAParameterInstance(ParameterInfo paraInfo)
        {
            //For those types that without a default constructor
            if (paraInfo.ParameterType == typeof(Bookmark))
            {
                return new Bookmark("Read1");
            }

            if (paraInfo.ParameterType == typeof(BookmarkScope))
            {
                return new BookmarkScope(Guid.NewGuid());
            }

            if (paraInfo.ParameterType == typeof(AsyncCallback))
            {
                return null;
            }

            if (paraInfo.ParameterType == typeof(WorkflowInstanceRecord))
            {
                return new WorkflowInstanceRecord(Guid.NewGuid(), "Dummy", "Dummy");
            }

            if (paraInfo.ParameterType == typeof(WorkflowApplicationInstance))
            {
                return GetADummyWorkflowApplicationInstance();
            }

            if (paraInfo.ParameterType == typeof(string))
            {
                return "temp";
            }

            if (paraInfo.ParameterType == typeof(TimeSpan))
            {
                return new TimeSpan(0, 0, 5);
            }

            if (paraInfo.ParameterType == typeof(IAsyncResult))
            {
                return new CompletedAsyncResult(null, null);
            }

            //if (paraInfo.ParameterType == typeof(InstanceStore))
            //{
            //    return new SqlWorkflowInstanceStore();
            //}

            //if (paraInfo.ParameterType == typeof(DynamicUpdateMap))
            //{
            //    return GetADummyDynamicUpdateMap();
            //}

            if (paraInfo.ParameterType == typeof(IDictionary<XName, object>))
            {
                return new Dictionary<XName, object>();
            }

            if (paraInfo.IsOut)
            {
                return null;
            }
            //Use the default constructor
            return Activator.CreateInstance(paraInfo.ParameterType);
        }

        public static void MakeAGenericMethod(ref MethodInfo methodInfo)
        {
            if (methodInfo.ContainsGenericParameters)
            {
                Type[] argumentType = methodInfo.GetGenericArguments();
                for (int i = 0; i < argumentType.Length; i++)
                {
                    argumentType[i] = typeof(System.Object);
                }

                methodInfo = methodInfo.MakeGenericMethod(argumentType);
            }
        }

        public static void CallAndValidateAllMethods(object objectToCall, Type exceptionType, List<string> methodsToExclude = null, string exceptionMessage = "")
        {
            Type typeToGetMethod = objectToCall.GetType();
            MethodInfo[] methodInfos = typeToGetMethod.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            bool failed = false;
            foreach (MethodInfo methodInfo in methodInfos)
            {
                //Exclude properties
                if (!methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
                {
                    if (methodsToExclude == null || !IsInMethods(methodInfo.Name, methodsToExclude))
                    {
                        InvokeAndVerifyAMethod(objectToCall, exceptionType, exceptionMessage, typeToGetMethod, ref failed, methodInfo);
                    }
                }
            }

            if (failed == true)
            {
                throw new Exception("At least one method invoke validation failed, see the trace for the details");
            }
        }

        public static void CallAndValidateMethods(object objectToCall, Type exceptionType, List<string> methodsToInclude, string exceptionMessage = "")
        {
            Type typeToGetMethod = objectToCall.GetType();
            MethodInfo[] methodInfos = typeToGetMethod.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            bool failed = false;
            foreach (MethodInfo methodInfo in methodInfos)
            {
                //Exclude properties
                if (!methodInfo.Name.StartsWith("get_") && !methodInfo.Name.StartsWith("set_"))
                {
                    if (IsInMethods(methodInfo.Name, methodsToInclude))
                    {
                        InvokeAndVerifyAMethod(objectToCall, exceptionType, exceptionMessage, typeToGetMethod, ref failed, methodInfo);
                    }
                }
            }

            if (failed == true)
            {
                throw new Exception("At least one method invoke validation failed, see the trace for the details");
            }
        }

        public static bool IsInMethods(string name, List<string> methods)
        {
            if (methods == null)
            {
                return false;
            }

            foreach (string method in methods)
            {
                //We need an exact match for non Async End method
                if (name.Equals(method))
                {
                    return true;
                }
                else if (name.StartsWith(method) && method.Equals("End"))
                {
                    return true;
                }
            }

            return false;
        }

        private static void InvokeAndVerifyAMethod(object objectToCall, Type expectedExceptionType, string expectedExceptionMessage, Type typeToCall, ref bool failed, MethodInfo methodInfo)
        {
            failed = false;
            MakeAGenericMethod(ref methodInfo);
            object[] methodParameters = null;
            ParameterInfo[] paraInfos = methodInfo.GetParameters();
            if (paraInfos.Length != 0)
            {
                methodParameters = new object[paraInfos.Length];
                for (int i = 0; i < paraInfos.Length; i++)
                {
                    methodParameters[i] = RuntimeHelper.GetAParameterInstance(paraInfos[i]);
                }
            }

            try
            {
                methodInfo.Invoke(objectToCall, methodParameters);
            }
            catch (TargetInvocationException exception)
            {
                if (exception.InnerException.GetType() != expectedExceptionType)
                {
                    failed = true;
                }
                else if (expectedExceptionMessage != string.Empty && exception.InnerException.Message.CompareTo(expectedExceptionMessage) != 0)
                {
                    failed = true;
                }

                return;
            }
            catch (Exception)
            {
                failed = true;
                return;
            }
        }

        public static void ValidateActivityTree(IEnumerable<Activity> expectedActivities, IEnumerable<Activity> actualActivities)
        {
            List<Activity> internalExpectedActivities = new List<Activity>();
            List<Activity> internalActualActivities = new List<Activity>();
            internalExpectedActivities.AddRange(expectedActivities);
            internalActualActivities.AddRange(actualActivities);
            internalExpectedActivities.Sort((Activity x, Activity y) => x.DisplayName.CompareTo(y.DisplayName));
            internalActualActivities.Sort((Activity x, Activity y) => x.DisplayName.CompareTo(y.DisplayName));

            StringBuilder sbExpectedActivities = new StringBuilder();
            StringBuilder sbActualActivities = new StringBuilder();
            string strExpectedActivities;
            string strActualActivities;

            foreach (Activity activity in internalExpectedActivities)
            {
                sbExpectedActivities.AppendLine(activity.DisplayName);
            }
            strExpectedActivities = sbExpectedActivities.ToString();

            foreach (Activity activity in internalActualActivities)
            {
                sbActualActivities.AppendLine(activity.DisplayName);
            }
            strActualActivities = sbActualActivities.ToString();

            if (!strExpectedActivities.Equals(strActualActivities))
            {
                throw new Exception("Expected and Actual Activities are not the same");
            }
        }

        private static WorkflowApplicationInstance GetADummyWorkflowApplicationInstance()
        {
            WorkflowIdentity wfIdentity = new WorkflowIdentity("GetAWorkflowApplicationInstanceParameter", new Version(1, 0), null);
            TestSequence wfDefinition = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine("testWriteLine1", "In TestWriteLine1"),
                    new TestReadLine<string>("ReadLine1", "testReadLine1"),
                    new TestWriteLine("testWriteLine2", "In TestWriteLine2")
                },
            };

            WorkflowApplicationInstance waInstance;
            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime workflowRuntime = TestRuntime.CreateTestWorkflowRuntime(wfDefinition, null, jsonStore, PersistableIdleAction.Unload))
            {
                //PersistenceProviderHelper pphelper = new PersistenceProviderHelper(workflowRuntime.PersistenceProviderFactoryType);
                //InstanceStore store = pphelper.CreateWorkflowInstanceStore();

                workflowRuntime.ExecuteWorkflow();
                workflowRuntime.WaitForIdle();
                workflowRuntime.UnloadWorkflow();
                workflowRuntime.WaitForUnloaded();

                Guid worklfowInstanceId = workflowRuntime.CurrentWorkflowInstanceId;
                waInstance = WorkflowApplication.GetInstance(worklfowInstanceId, jsonStore);
                waInstance.Abandon();
            }

            return waInstance;
        }
    }
}

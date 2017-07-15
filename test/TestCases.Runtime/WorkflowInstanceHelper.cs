// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Runtime.Common;

namespace TestCases.Runtime.WorkflowInstanceTest
{
    public class WorkflowInstanceHelper
    {
        public const string UnloadMessage = "Workflow Completed and Unloaded";
        public const string TraceMessage_TriedAllOperations = "Have tried calling all WorkflowApplication operations";

        public enum CallBackType
        {
            Idle = 0,
            PersistableIdle = 1,
            Unloaded = 2,
            Aborted = 3,
            Completed = 4,
            OnUnhandledException = 5
        }
        public static void wfRuntime_OnUnload(object sender, TestWorkflowUnloadedEventArgs e)
        {
            //Log.Info("In OnUnload");
            TestTraceManager.Instance.AddTrace(e.WorkflowApplication.Id, new UserTrace(UnloadMessage));
            //UserTrace.Trace(e.WorkflowApplication.Id, UnloadMessage);
        }

        public static void workflowInstance_UnloadIdleAction(object sender, TestWorkflowIdleAndPersistableEventArgs e)
        {
            //Log.Info("workflowInstance_IdleAndPersistable");
            WorkflowApplication instance = e.WorkflowApplication;

            e.Action = PersistableIdleAction.Unload;
        }

        public static void CallWFAppOperationsFromACallback(WorkflowApplication workflowApplication, CallBackType callbackType = CallBackType.Idle)
        {
            Type exceptionType = typeof(InvalidOperationException);
            string exceptionMessage = ExceptionStrings.CannotPerformOperationFromHandlerThread;

            RuntimeHelper.CallAndValidateAllMethods(workflowApplication, exceptionType, new List<string>() { "End", "Abort", "Load", "BeginLoad", "LoadRunnableInstance", "BeginLoadRunnableInstance", "AddInitialInstanceValues" }, exceptionMessage);

            exceptionMessage = string.Format(ExceptionStrings.WorkflowInstanceIsReadOnly, workflowApplication.Id);
            RuntimeHelper.CallAndValidateMethods(workflowApplication, exceptionType, new List<string>() { "AddInitialInstanceValues", "LoadRunnableInstance", "BeginLoadRunnableInstance" }, exceptionMessage);

            if (callbackType == CallBackType.Aborted)
            {
                exceptionType = typeof(WorkflowApplicationAbortedException);
                exceptionMessage = string.Format(ExceptionStrings.WorkflowApplicationAborted, workflowApplication.Id);
            }
            RuntimeHelper.CallAndValidateMethods(workflowApplication, exceptionType, new List<string>() { "Load", "BeginLoad" }, exceptionMessage);
        }

        public static void VerifyWFApplicationEventArgs<T>(WorkflowApplication workflowApplication, WorkflowApplicationEventArgs eventArgs, int expectedExtensionsCount)
            where T : class
        {
            if (eventArgs.InstanceId != workflowApplication.Id)
            {
                throw new Exception("Expected instance ID is: " + workflowApplication.Id + " Actual is: " + eventArgs.InstanceId);
            }

            int actualCount = eventArgs.GetInstanceExtensions<T>().Count<T>();
            if (actualCount != expectedExtensionsCount)
            {
                throw new Exception("Expected number of extensions is: " + expectedExtensionsCount + " Actual is: " + actualCount);
            }
        }
    }

    public class OperationOrderTracePersistExtension : PersistenceIOParticipant
    {
        public const string TraceSave = "Save";
        private Guid _instanceId;

        public OperationOrderTracePersistExtension(Guid instanceId) :
            base(false, false)
        {
            _instanceId = instanceId;
        }

        protected override void Abort()
        {
            // do nothing
        }

        protected override IAsyncResult BeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            //UserTrace.Trace(this.instanceId, TraceSave);
            TestTraceManager.Instance.AddTrace(_instanceId, new UserTrace(TraceSave));
            return base.BeginOnSave(readWriteValues, writeOnlyValues, timeout, callback, state);
        }
    }
}

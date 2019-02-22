// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Runtime.WorkflowInstanceTest
{
    public class WFIWorkflowInstanceExtensionTests
    {
        /// <summary>
        /// Verify extensions added by GetAdditionalExtensions method
        /// </summary>        
        [Fact]
        public void VerifyAdditionalExtensionsAdded()
        {
            TestActivity workflow = new TestReadLine<int>("Read1", "Read1");
            TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(workflow);
            runtime.OnWorkflowUnloaded += WorkflowInstanceHelper.wfRuntime_OnUnload;
            runtime.CreateWorkflow();
            runtime.Extensions.Add(new AdditionalExtensionsAdded(new Collection<object>() { new OperationOrderTracePersistExtension(runtime.CurrentWorkflowInstanceId) }));

            runtime.ResumeWorkflow();
            runtime.WaitForIdle();
            runtime.ResumeBookMark("Read1", 1);

            ExpectedTrace expectedTrace = workflow.GetExpectedTrace();
            expectedTrace.AddIgnoreTypes(typeof(UserTrace));
            runtime.WaitForCompletion(expectedTrace);
            runtime.WaitForTrace(new UserTrace(WorkflowInstanceHelper.UnloadMessage));
            runtime.WaitForTrace(new UserTrace(OperationOrderTracePersistExtension.TraceSave));
        }

        /// <summary>
        /// Check workflow ID and Workflowdefinition have been correctly set
        /// </summary>        
        [Fact]
        public void CheckWorkflowProperties()
        {
            TestActivity workflow = new TestWriteLine("Write1", "Write a line");

            TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(workflow);
            runtime.CreateWorkflow();
            runtime.Extensions.Add(new CheckWorkflowPropertiesExtension(runtime.CurrentWorkflowInstanceId, workflow.ProductActivity));

            runtime.ResumeWorkflow();
            ExpectedTrace expectedTrace = workflow.GetExpectedTrace();
            expectedTrace.AddIgnoreTypes(typeof(UserTrace));
            runtime.WaitForCompletion(expectedTrace);
        }

        /// <summary>
        /// Verify exception thrown by interface methods have been propagated back to the caller of Run()
        /// </summary>        
        [Fact]
        public void ThrowFromInterfaceMethods()
        {
            TestActivity workflow = new TestWriteLine("Write1", "Write a line");

            TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(workflow);
            runtime.PersistenceProviderFactoryType = null;
            runtime.CreateWorkflow();

            ThrowFromAdditionalExtensions throwFromAdditionalExtensions = new ThrowFromAdditionalExtensions();
            CheckExceptionPropagated(throwFromAdditionalExtensions, "Throw from AdditionalExtensionsAdded", runtime);

            ThrowFromSetInstance throwFromSetInstance = new ThrowFromSetInstance();
            CheckExceptionPropagated(throwFromSetInstance, "Throw from SetInstance", runtime);

            runtime.ResumeWorkflow();
            ExpectedTrace expectedTrace = workflow.GetExpectedTrace();
            expectedTrace.AddIgnoreTypes(typeof(UserTrace));
            runtime.WaitForCompletion(expectedTrace);
        }

        private void CheckExceptionPropagated(ThrowFromInterfaceMethodsBase throwExceptionExtension, string exceptionMessage, TestWorkflowRuntime runtime)
        {
            throwExceptionExtension.ExceptionMessage = exceptionMessage;
            throwExceptionExtension.IsThrow = true;

            runtime.Extensions.Add(throwExceptionExtension);
            ExceptionHelpers.CheckForException(typeof(Exception), throwExceptionExtension.ExceptionMessage, delegate
            {
                runtime.ResumeWorkflow();
            });
            throwExceptionExtension.IsThrow = false;
        }
    }

    internal class AdditionalExtensionsAdded : IWorkflowInstanceExtension
    {
        private readonly Collection<object> _additionalExtensions;

        public AdditionalExtensionsAdded(Collection<object> additionalExtensions)
        {
            _additionalExtensions = additionalExtensions;
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            return _additionalExtensions;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance) { }
    }

    internal class ThrowFromInterfaceMethodsBase
    {
        public string ExceptionMessage { get; set; }
        public bool IsThrow { get; set; }
    }

    internal class ThrowFromAdditionalExtensions : ThrowFromInterfaceMethodsBase, IWorkflowInstanceExtension
    {
        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            if (base.IsThrow)
            {
                throw new Exception(base.ExceptionMessage);
            }
            return null;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance) { }
    }

    internal class ThrowFromSetInstance : ThrowFromInterfaceMethodsBase, IWorkflowInstanceExtension
    {
        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            return null;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            if (base.IsThrow)
            {
                throw new Exception(base.ExceptionMessage);
            }
        }
    }

    internal class CheckWorkflowPropertiesExtension : IWorkflowInstanceExtension
    {
        private Activity _expectedWorkflowDefinition;
        private readonly Guid _expectedWorkflowId;

        public CheckWorkflowPropertiesExtension(Guid expectedWorkflowId, Activity expectedWorkflowDefinition)
        {
            _expectedWorkflowId = expectedWorkflowId;
            _expectedWorkflowDefinition = expectedWorkflowDefinition;
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            return null;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            if (instance.Id != _expectedWorkflowId)
            {
                throw new Exception("Expected workflow ID: " + _expectedWorkflowId + "Actual is: " + instance.Id);
            }
            if (instance.WorkflowDefinition.GetType() != _expectedWorkflowDefinition.GetType())
            {
                throw new Exception("Expected workflowDefinition type: " + _expectedWorkflowDefinition.GetType() + "Actual is " + instance.WorkflowDefinition.GetType());
            }
            if (instance.WorkflowDefinition.DisplayName != _expectedWorkflowDefinition.DisplayName)
            {
                throw new Exception("Expected workflowDefinition display name is: " + _expectedWorkflowDefinition.DisplayName + "Actual is " + instance.WorkflowDefinition.DisplayName);
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Runtime.DurableInstancing;
using System.Activities.Validation;
using System;
using System.Collections.Generic;
using System.Text;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Runtime
{
    public static class TestRuntime
    {
        public static TestTraceListenerExtension TraceListenerExtension = new TestTraceListenerExtension();

        static TestRuntime()
        {
            //WCFVersioningHelper.TargetBuildVersionTo_V4_5();
        }

        #region Standalone workflow

        /*public static TestWorkflowRuntime CreateTestWorkflowRuntime(TestActivity testActivity, params TestServiceRuntime[] serviceRuntimes)
        {
            TestWorkflowRuntimeConfiguration wrc = TestWorkflowRuntimeConfiguration.GenerateDefaultConfiguration(testActivity);

            if (serviceRuntimes != null && serviceRuntimes.Length > 0)
            {
                foreach (TestServiceRuntime serviceRuntime in serviceRuntimes)
                {
                    ClientOperationUtility.BindClientOperations(testActivity, serviceRuntime.Configuration, wrc.Settings, false);
                }
            }

            return new TestWorkflowRuntime(testActivity, wrc);
        } */

        public static TestWorkflowRuntime CreateTestWorkflowRuntime(TestActivity testActivity, WorkflowIdentity definitionIdentity = null, InstanceStore instanceStore = null,
            PersistableIdleAction idleAction = PersistableIdleAction.None)
        {
            TestWorkflowRuntimeConfiguration wrc = new TestWorkflowRuntimeConfiguration();
            return new TestWorkflowRuntime(testActivity, wrc, instanceStore, idleAction);
        }

        public static TestWorkflowRuntime CreateTestWorkflowRuntime(TestActivity testActivity, TestWorkflowRuntimeConfiguration testWorkflowRuntimeConfiguration)
        {
            return new TestWorkflowRuntime(testActivity, testWorkflowRuntimeConfiguration);
        }

        public static void RunAndValidateWorkflowWithoutConstraintValidation(TestActivity testActivity)
        {
            RunAndValidateWorkflow(testActivity, testActivity.GetExpectedTrace(), null, null, false);
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity)
        {
            RunAndValidateWorkflow(testActivity, testActivity.GetExpectedTrace());
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity, WorkflowIdentity definitionIdentity)
        {
            RunAndValidateWorkflow(testActivity, testActivity.GetExpectedTrace(), definitionIdentity);
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity, ExpectedTrace expectedTrace, WorkflowIdentity definitionIdentity = null)
        {
            RunAndValidateWorkflow(testActivity, expectedTrace, new List<TestConstraintViolation>(), null, true, definitionIdentity);
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity, List<TestConstraintViolation> constraints)
        {
            RunAndValidateWorkflow(testActivity, testActivity.GetExpectedTrace(), constraints, null);
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity, List<TestConstraintViolation> constraints, ValidationSettings validatorSettings)
        {
            RunAndValidateWorkflow(testActivity, testActivity.GetExpectedTrace(), constraints, validatorSettings);
        }

        public static void RunAndValidateWorkflow(TestActivity testActivity, ExpectedTrace expectedTrace, List<TestConstraintViolation> constraints, ValidationSettings validatorSettings)
        {
            RunAndValidateWorkflow(testActivity, expectedTrace, constraints, validatorSettings, true);
        }

        private static void RunAndValidateWorkflow(TestActivity testActivity, ExpectedTrace expectedTrace, List<TestConstraintViolation> constraints, ValidationSettings validatorSettings, bool runValidations, WorkflowIdentity definitionIdentity = null)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = new TestWorkflowRuntime(testActivity, definitionIdentity))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForCompletion(expectedTrace);
            }
        }

        private static void HostWorkflowAsService(TestActivity testActivity, ExpectedTrace expectedTrace, Type abortedExceptionType, Dictionary<string, string> abortedExceptionProperties)
        {
            //Log.TraceInternal("Standalone Workflow is being hosted as Service.");

            //Host it as a service
            /*using (TestWorkflowServiceRuntime serviceRuntime = CreateTestWorkflowServiceRuntime(testActivity))
            {
                serviceRuntime.Configuration.AddWorkflowCreationEndPoint();
                serviceRuntime.StartService();

                //Log.TraceInternal("Wait 7sec for IIS to catch-up."); 
                System.Threading.Thread.CurrentThread.Join(TimeSpan.FromSeconds(7));

                WorkflowCreationClient creationClient = serviceRuntime.CreateWorkflowCreationClient();

                Guid workflowInstanceId = creationClient.Create();

                if (abortedExceptionType == null)
                {
                    if (expectedTrace != null)
                        serviceRuntime.WaitForCompletion(workflowInstanceId, expectedTrace);
                    else
                        serviceRuntime.WaitForCompletion(workflowInstanceId, true);
                }
                else
                {
                    Exception exception;
                    if (expectedTrace != null)
                        serviceRuntime.WaitForAborted(workflowInstanceId, out exception, expectedTrace);
                    else
                        serviceRuntime.WaitForAborted(workflowInstanceId, out exception, true);

                    ExceptionHelpers.ValidateException(exception, abortedExceptionType, abortedExceptionProperties);
                }

                creationClient.Close();

                serviceRuntime.StopService();
            }*/ // Should have our WorkflowApplication or WorkflowInvoker implementation
        }

        public static bool ValidateConstraints(TestActivity activity, List<TestConstraintViolation> expectedConstraints, ValidationSettings validatorSettings)
        {
            //Log.TraceInternal("ValidateConstraints: Validating...");

            StringBuilder sb = new StringBuilder();
            bool hasBuildConstraintError = false;

            ValidationResults validationResults;
            if (validatorSettings != null)
            {
                validationResults = ActivityValidationServices.Validate(activity.ProductActivity, validatorSettings);
            }
            else
            {
                validationResults = ActivityValidationServices.Validate(activity.ProductActivity);
            }

            hasBuildConstraintError = (validationResults.Errors.Count > 0);

            if (expectedConstraints.Count != (validationResults.Errors.Count + validationResults.Warnings.Count))
            {
                sb.AppendLine("expectedConstraintViolations.Count != actualConstraintViolations.Count");
            }

            bool matched = false;
            foreach (TestConstraintViolation expected in expectedConstraints)
            {
                foreach (ValidationError error in validationResults.Errors)
                {
                    if (expected.IsMatching(error))
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched) // try warnings
                {
                    foreach (ValidationError warning in validationResults.Warnings)
                    {
                        if (expected.IsMatching(warning))
                        {
                            matched = true;
                            break;
                        }
                    }
                }
                if (!matched)
                {
                    sb.AppendLine(String.Format(
                        "Expected Constraint '{0}' not found in Actual Constraints",
                        expected));
                }
                matched = false;
            }

            //Log.TraceInternal("Expected Constraints:");
            foreach (TestConstraintViolation expectedConstraint in expectedConstraints)
            {
                //Log.TraceInternal("{0}", expectedConstraint);
            }
            //Log.TraceInternal("Actual Constraints:");
            foreach (ValidationError error in validationResults.Errors)
            {
                //Log.TraceInternal("{0}", TestConstraintViolation.ActualConstraintViolationToString(error));
            }

            foreach (ValidationError warning in validationResults.Warnings)
            {
                //Log.TraceInternal("{0}", TestConstraintViolation.ActualConstraintViolationToString(warning));
            }

            if (sb.Length > 0)
            {
                //Log.TraceInternal("Errors found:");
                //Log.TraceInternal(sb.ToString());
                throw new Exception("FAIL, error while validating in TestWorkflowRuntime.ValidateConstraints");
            }
            //Log.TraceInternal("ValidateConstraints: Validation complete.");

            return !hasBuildConstraintError;
        }

        public static void ValidateWorkflowErrors(TestActivity testActivity, List<TestConstraintViolation> constraints, string onOpenExceptionString)
        {
            ValidateWorkflowErrors(testActivity, constraints, typeof(InvalidWorkflowException), onOpenExceptionString);
        }

        public static void ValidateWorkflowErrors(TestActivity testActivity, List<TestConstraintViolation> constraints, Type onOpenExceptionType, string onOpenExceptionString)
        {
            ValidateWorkflowErrors(testActivity, constraints, onOpenExceptionType, onOpenExceptionString, null);
        }

        public static void ValidateWorkflowErrors(TestActivity testActivity, List<TestConstraintViolation> constraints, Type onOpenExceptionType, string onOpenExceptionString, ValidationSettings validatorSettings)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = new TestWorkflowRuntime(testActivity))
            {
                ValidateConstraints(testActivity, constraints, validatorSettings);
                ValidateInstantiationException(testActivity, onOpenExceptionType, onOpenExceptionString);
            }
        }

        public static void ValidateInstantiationException(TestActivity testActivity, string errorString)
        {
            ValidateInstantiationException(testActivity, typeof(InvalidWorkflowException), errorString);
        }

        public static void ValidateInstantiationException(TestActivity testActivity, Type exceptionType, string errorString)
        {
            Dictionary<string, string> exception = new Dictionary<string, string>();
            exception.Add("Message", errorString);
            ExceptionHelpers.CheckForException(
                exceptionType,
                exception,
                delegate
                {
                    WorkflowApplication instance = new WorkflowApplication(testActivity.ProductActivity);
                    instance.Extensions.Add(TestRuntime.TraceListenerExtension);
                    instance.Run();
                },
                true);
        }

        public static void RunAndValidateAbortedException(TestActivity activity, Type exceptionType, Dictionary<string, string> exceptionProperties)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = new TestWorkflowRuntime(activity))
            {
                testWorkflowRuntime.ExecuteWorkflow();


                testWorkflowRuntime.WaitForAborted(out Exception exception, true);

                ExceptionHelpers.ValidateException(exception, exceptionType, exceptionProperties);
            }
        }

        public static void RunAndValidateUsingWorkflowInvoker(TestActivity testActivity, Dictionary<string, object> inputs, Dictionary<string, object> expectedOutputs, ICollection<object> extensions)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = new TestWorkflowRuntime(testActivity))
            {
                testWorkflowRuntime.ExecuteWorkflowInvoker(inputs, expectedOutputs, extensions);
            }
        }

        public static void RunAndValidateUsingWorkflowInvoker(TestActivity testActivity, Dictionary<string, object> inputs, Dictionary<string, object> expectedOutputs, ICollection<object> extensions, TimeSpan invokeTimeout)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = new TestWorkflowRuntime(testActivity))
            {
                testWorkflowRuntime.ExecuteWorkflowInvoker(inputs, expectedOutputs, extensions, invokeTimeout);
            }
        }

        #endregion
    }
}

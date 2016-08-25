// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using TestCases.Activities.Common;
using System;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities.Pick
{
    public class Cancellation
    {
        /// <summary>
        /// Cancel the workflow instance while Trigger is executing.
        /// Cancel On Trigger
        /// </summary>        
        [Fact]
        public void CancelOnTrigger()
        {
            TestPick pick = new TestPick
            {
                DisplayName = "PickActivity",
                ExpectedOutcome = Outcome.Canceled,
                Branches =
                {
                    new TestPickBranch
                    {
                        DisplayName = "Branch1",
                        Trigger = new TestBlockingActivity("Block1")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        }
                    },
                    new TestPickBranch
                    {
                        DisplayName = "Branch2",
                        Trigger = new TestBlockingActivity("Block2")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        }
                    }
                }
            };

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(pick))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Block2", TestActivityInstanceState.Executing);
                System.Threading.Thread.Sleep(1000);
                runtime.CancelWorkflow();
                ExpectedTrace trace = pick.GetExpectedTrace();
                runtime.WaitForCanceled(trace);
            }
        }

        /// <summary>
        /// Cancel the workflow instance while Action is executing.
        /// Cancel On Action
        /// </summary>        
        [Fact]
        public void CancelOnAction()
        {
            TestPick pick = new TestPick
            {
                DisplayName = "PickActivity",
                ExpectedOutcome = Outcome.Canceled,
                Branches =
                {
                    new TestPickBranch
                    {
                        DisplayName = "Branch1",
                        Trigger = new TestDelay()
                        {
                            Duration = new TimeSpan(0, 0, 3),
                        },
                        Action = new TestBlockingActivity("Block1")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        }
                    },
                    new TestPickBranch
                    {
                        DisplayName = "Branch2",
                        Trigger = new TestBlockingActivity("Block2")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        }
                    }
                }
            };

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(pick))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Block1", TestActivityInstanceState.Executing);
                System.Threading.Thread.Sleep(1000);
                runtime.CancelWorkflow();
                ExpectedTrace trace = pick.GetExpectedTrace();
                runtime.WaitForCanceled(trace);
            }
        }


        /// <summary>
        /// Pick with two branches, one is triggered, and the other is cancelled, but its custom activity overrides the Cancel state with Cancelled state.
        /// Add a custom activity to the non-winning branch. When the branch is cancelled, the custom activity overrides the Cancel state with Cancel state. Verify that the activity doesn't hang.
        /// Two branches, one triggered, and the custom activity branch overrides the Cancel state with Cancel state.
        /// </summary>        
        [Fact]
        public void CustomActivityOverridesBranchCancelStateWithCancelState()
        {
            const string triggerMessage = "Trigger branch's trigger is executing.";
            const string actionMessage = "Trigger branch's action is executing.";

            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                ExpectedOutcome = new Outcome(OutcomeState.Completed) { IsOverrideable = false },
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch_Branch",
                        Trigger = new TestWriteLine("TriggeredBranch_Trigger")
                        {
                            Message = triggerMessage,
                            HintMessage = triggerMessage,
                        },
                        Action = new TestWriteLine("TriggeredBranch_Action")
                        {
                            Message = actionMessage,
                            HintMessage = actionMessage,
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "CancelledCustomActivity_Branch",
                        ExpectedOutcome = Outcome.Canceled,
                        Trigger = new TestBlockingActivityWithWriteLineInCancel("CancelledCustomActivity_Trigger", OutcomeState.Canceled)
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("CancelledCustomActivity_Action")
                        {
                            Message = "CancelledCustomActivity_Action - not supposed to show",
                        },
                    },
                }
            };

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Pick with two branches, one is triggered, and the other is cancelled, but its custom activity overrides the Cancel state with Completed state.
        /// Add a custom activity to the non-winning branch. When the branch is cancelled, the custom activity overrides the Cancel state with Completed state. Verify that the activity doesn't hang when the custom activity comes out with Completed instead of Cancelled state.
        /// Two branches, one triggered, and the custom activity branch overrides the Cancel state with Completed state.
        /// </summary>        
        [Fact]
        public void CustomActivityOverridesBranchCancelStateWithCompletedState()
        {
            const string triggerMessage = "Trigger branch's trigger is executing.";
            const string actionMessage = "Trigger branch's action is executing.";

            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                ExpectedOutcome = new Outcome(OutcomeState.Completed) { IsOverrideable = false },
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch_Branch",
                        Trigger = new TestWriteLine("TriggeredBranch_Trigger")
                        {
                            Message = triggerMessage,
                            HintMessage = triggerMessage,
                        },
                        Action = new TestWriteLine("TriggeredBranch_Action")
                        {
                            Message = actionMessage,
                            HintMessage = actionMessage,
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "ClosedCustomActivity_Branch",
                        ExpectedOutcome = Outcome.Completed,  // custom activity's outcome is Closed, therefore this branch is also Closed 
                        Trigger = new TestBlockingActivityWithWriteLineInCancel("ClosedCustomActivity_Trigger", OutcomeState.Completed)
                        {
                            ExpectedOutcome = Outcome.Completed,
                        },
                        Action = new TestWriteLine("ClosedCustomActivity_Action")
                        {
                            Message = "ClosedCustomActivity_Action - not supposed to show",
                        },
                    },
                }
            };

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Pick with two branches, one is triggered, and the other is cancelled, but its custom activity overrides the Cancel state with throwing exception.
        /// Add a custom activity to the non-winning branch. When the branch is cancelled, the custom activity overrides the Cancel state with Faulted state (exception thrown). In Beta2's design, the runtime will abort the workflow instance.
        /// Two branches, one triggered, and the custom activity branch overrides the Cancel state with throwing exception.
        /// </summary>        
        [Fact]
        public void CustomActivityOverridesBranchCancelStateWithThrow()
        {
            const string triggerMessage = "Trigger branch's trigger is executing.";

            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch_Branch",
                        Trigger = new TestWriteLine("TriggeredBranch_Trigger")
                        {
                            Message = triggerMessage,
                            HintMessage = triggerMessage,
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "FaultedCustomActivity_Branch",
                        Trigger = new TestBlockingActivityWithWriteLineInCancel("FaultedCustomActivity_Trigger", OutcomeState.Faulted)
                        {
                            ExpectedOutcome = Outcome.UncaughtException(),
                        },
                        Action = new TestWriteLine("FaultedCustomActivity_Action")
                        {
                            Message = "FaultedCustomActivity_Action - not supposed to show",
                        },
                    },
                }
            };

            // wrapping the custom activity with a try-catch doesn't work here
            // try-catch doesn't catch an exception that comes from a Cancel
            // by design in Beta2, any exception thrown from Cancel will Abort the workflow immediately
            // Note: for this test case (and similar ones e.g. Parallel), we can only compare the Exception type
            //       because this is a special case, we cannot validate the traces as exception is thrown from
            //       Cancel and by design runtime doesn't catch it. As a result, there is no chance to validate.
            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(pick))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                Exception outException = null;
                testWorkflowRuntime.WaitForAborted(out outException, false);
                // Due to how we get the tracking information, the exception is not the original exception and 
                // we cannot check the InnerException property.
                Assert.NotNull(outException);
                //if (outException == null || outException.InnerException == null || !outException.InnerException.GetType().Equals(typeof(TestCaseException)))
                //{
                //    throw new TestCaseException(String.Format("Workflow was supposed to Abort with a TestCaseException, but this is the exception: {0}", outException.ToString()));
                //}
                //else
                //{
                //    //Log.Info("Workflow aborted as excpected");
                //}
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using TestCases.Activities.Common;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities.Pick
{
    public class FaultHandling
    {
        /// <summary>
        /// Fault occurs in Trigger, and catch by outer TryCatchFinally. Other Trigger are expected to be cancelled.
        /// Throw In Trigger
        /// </summary>        
        [Fact]
        public void ThrowInTrigger()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch1",
                        Trigger = new TestBlockingActivity("Block1")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch",
                        Trigger = new TestThrow<ApplicationException>("ThrowInTrigger")
                        {
                            ExceptionExpression = (context => new ApplicationException("Fault in trigger")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(ApplicationException)),
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
                        ExpectedOutcome = Outcome.Canceled,
                        HintTriggerScheduled = true,
                        Trigger = new TestBlockingActivity("Block3")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action3")
                        {
                            Message = "Action3",
                        },
                    },
                }
            };

            TestTryCatch testTCF = new TestTryCatch()
            {
                DisplayName = "TryCatch",
                Try = pick,
                Catches =
                {
                    new TestCatch<ApplicationException>()
                    {
                        Body = new TestWriteLine("CatchWriteLine")
                        {
                            Message = "Caught",
                        }
                    }
                }
            };

            ExpectedTrace trace = testTCF.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(testTCF, trace);
        }

        /// <summary>
        /// Fault occurs in Action, and caught by outer TryCathcFainally
        /// Fault occurs in Action, and catch by outer TryCathcFainally
        /// Throw In Action
        /// </summary>        
        [Fact]
        public void ThrowInAction()
        {
            TestPick pick = new TestPick
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch
                    {
                        DisplayName = "Triggered",
                        Trigger = new TestWriteLine("Trigger1")
                        {
                            Message = "Trigger1",
                        },

                        Action = new TestThrow<ApplicationException>("ThrowInAction")
                        {
                            ExceptionExpression = (context =>  new ApplicationException("Fault in trigger")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(ApplicationException)),
                        }
                    },

                    new TestPickBranch
                    {
                        DisplayName = "NoTriggered",
                        ExpectedOutcome = Outcome.Canceled,

                        Trigger = new TestWriteLine("Trigger2")
                        {
                            Message = "Trigger2",
                            ExpectedOutcome = Outcome.Completed,
                        },

                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2"
                        }
                    }
                }
            };

            TestTryCatch testTCF = new TestTryCatch
            {
                DisplayName = "TryCatch",
                Try = pick,
                Catches =
                {
                    new TestCatch<ApplicationException>()
                    {
                        Body = new TestWriteLine("CatchWriteLine")
                        {
                            Message = "Caught",
                        }
                    }
                }
            };

            ExpectedTrace trace = testTCF.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(testTCF, trace);
        }
    }
}

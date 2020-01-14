// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities.Pick
{
    public class Scenario
    {
        /// <summary>
        /// Pick with three branches, one Trigger is completed and the Action will be executed, other two Triggers (blocking activities) will be cancelled.
        /// Three branches, one triggered
        /// </summary>        
        [Fact]
        public void ThreeBranchsOneTriggered()
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
                        Trigger = new TestBlockingActivity("Block2"),
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
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

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(pick))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Block2", TestActivityInstanceState.Executing);
                System.Threading.Thread.Sleep(1000);
                runtime.ResumeBookMark("Block2", null);
                ExpectedTrace trace = pick.GetExpectedTrace();
                runtime.WaitForCompletion(trace);
            }
        }

        /// <summary>
        /// Pick with three immediately complete Trigger (WriteLine), one event is completed and other two Triggers will be cancelled.
        /// Immediately Complete Triggers
        /// </summary>        
        [Fact]
        public void ImmediatelyCompleteTriggers()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch1",
                        Trigger = new TestWriteLine("Trigger1")
                        {
                            Message = "Trigger1 Executing",
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
                        ExpectedOutcome = Outcome.Canceled,
                        Trigger = new TestWriteLine("Trigger2")
                        {
                            Message = "Trigger2 Executing",
                            ExpectedOutcome = Outcome.Completed,
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch3",
                        ExpectedOutcome = Outcome.Canceled,
                        Trigger = new TestWriteLine("Trigger3")
                        {
                            Message = "Trigger3 Executing",
                            ExpectedOutcome = Outcome.Completed,
                        },
                        Action = new TestWriteLine("Action3")
                        {
                            Message = "Action3",
                        },
                    },
                }
            };

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Pick with only one branch, the event is triggered and the action will be executed.
        /// One Branch
        /// </summary>        
        [Fact]
        public void OneBranch()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "OneBranch",
                        Trigger = new TestWriteLine("Trigger1")
                        {
                            Message = "Trigger1 Executing",
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                }
            };

            TestRuntime.RunAndValidateWorkflow(pick);
        }

        /// <summary>
        /// One of the Pick branch with Trigger, but doesn’t have action. And this event is triggered.
        /// Branch Without Action
        /// </summary>        
        [Fact]
        public void BranchWithoutAction()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch1",
                        Trigger = new TestWriteLine("Trigger1")
                        {
                            Message = "Trigger1 Executing",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
                        ExpectedOutcome = Outcome.Canceled,
                        Trigger = new TestBlockingActivity("Trigger2")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch3",
                        ExpectedOutcome = Outcome.Canceled,
                        Trigger = new TestBlockingActivity("Trigger3")
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

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Access the variable define in PickBranch from Trigger and Action. Make sure the change in Trigger will reflect to Action.
        /// Variable Accessibility
        /// </summary>        
        [Fact]
        public void VariableAccessibility()
        {
            string hintMessage = "Hello Microsoft";
            Variable<string> message = VariableHelper.Create<string>("messageVar");
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "OneBranch",
                        Variables =
                        {
                            message,
                        },
                        Trigger = new TestAssign<string>("ChangeMessage")
                        {
                            Value = hintMessage,
                            ToVariable = message,
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            MessageVariable = message,
                            HintMessage = hintMessage,
                        },
                    },
                }
            };

            TestRuntime.RunAndValidateWorkflow(pick);
        }

        /// <summary>
        /// Pick has no branches, there should be a warning.
        /// Pick with zero branch. (validation error?)
        /// Zero Branch
        /// </summary>        
        [Fact]
        public void ZeroBranch()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "ZeroBranchPick",
            };

            TestRuntime.RunAndValidateWorkflow(pick);
        }

        /// <summary>
        /// Pick's Trigger is a nested Pick.
        /// Pick’s Trigger is a nested Pick.
        /// Nested Pick in Trigger
        /// </summary>        
        [Fact]
        public void NestedPickInTrigger()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "Pick",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "Nested Pick Trigger",
                        Trigger = new TestPick()
                        {
                            DisplayName = "Nested Pick",
                            Branches =
                            {
                                new TestPickBranch()
                                {
                                    DisplayName = "Nested PickBranch1",
                                    Trigger = new TestBlockingActivity("Block1"),
                                    Action = new TestWriteLine
                                    {
                                        Message = "Actions1 in Nested Pick",
                                    }
                                },

                                new TestPickBranch()
                                {
                                    DisplayName = "Nested PickBranch2",
                                    Trigger = new TestBlockingActivity("NoTriggered1")
                                    {
                                        ExpectedOutcome = Outcome.Canceled,
                                    },
                                    Action = new TestWriteLine
                                    {
                                        Message = "Action2 in Nested Pick",
                                    }
                                }
                            }
                        },

                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Actions1",
                        }
                    },

                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggered PickBranch",
                        Trigger = new TestBlockingActivity("NoTriggered2")
                        {
                            ExpectedOutcome = Outcome.Canceled
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
                runtime.ResumeBookMark("Block1", null);
                ExpectedTrace trace = pick.GetExpectedTrace();
                runtime.WaitForCompletion(trace);
            }
        }

        /// <summary>
        /// Pick's Action is a nested Pick.
        /// Pick’s Action is a nested Pick.
        /// Nested Pick in Action
        /// </summary>        
        [Fact]
        public void NestedPickInAction()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "Pick",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "Nested Pick In Action Branch",

                        Trigger = new TestBlockingActivity("Block1"),
                        Action = new TestPick()
                        {
                            DisplayName = "Nested Pick",
                            Branches =
                            {
                                new TestPickBranch()
                                {
                                    DisplayName = "Nested PickBranch1",
                                    Trigger = new TestBlockingActivity("Block2"),
                                    Action = new TestWriteLine
                                    {
                                        Message = "Actions1 in Nested Pick",
                                    }
                                },

                                new TestPickBranch()
                                {
                                    DisplayName = "Nested PickBranch2",
                                    Trigger = new TestBlockingActivity("NoTriggered1")
                                    {
                                        ExpectedOutcome = Outcome.Canceled,
                                    },
                                    Action = new TestWriteLine
                                    {
                                        Message = "Action2 in Nested Pick",
                                    }
                                }
                            }
                        }
                    },

                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggered PickBranch",
                        Trigger = new TestBlockingActivity("NoTriggered2")
                        {
                            ExpectedOutcome = Outcome.Canceled
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
                runtime.ResumeBookMark("Block1", null);
                runtime.WaitForActivityStatusChange("Block2", TestActivityInstanceState.Executing);
                System.Threading.Thread.Sleep(1000);
                runtime.ResumeBookMark("Block2", null);
                ExpectedTrace trace = pick.GetExpectedTrace();
                runtime.WaitForCompletion(trace);
            }
        }

        /// <summary>
        /// Variables have same name in different branches.
        /// Variable have same name in different branches
        /// Same Variable Name
        /// </summary>        
        [Fact]
        public void SameVariableName()
        {
            Variable<string> var1 = VariableHelper.CreateInitialized<string>("var1", "Variable in Branch1");
            Variable<string> var2 = VariableHelper.CreateInitialized<string>("var1", "Variable in Branch2");

            TestPick pick = new TestPick()
            {
                DisplayName = "Pick",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "Branch1",
                        Variables =
                        {
                            var1
                        },
                        Trigger = new TestDelay()
                        {
                            Duration = new TimeSpan(0, 0, 3)
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            HintMessage = "Variable in Branch1",
                            MessageExpression = (env)=>var1.Get(env)
                        }
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "Branch2",
                        Variables =
                        {
                            var2
                        },
                        Trigger = new TestBlockingActivity("Block2")
                        {
                            ExpectedOutcome = Outcome.Canceled
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2"
                        }
                    },
                }
            };

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Variable have same name in PickBranch and outer scope.
        /// Variable have same name in PickBranch and outer scope
        /// Same Variable Name Nest
        /// </summary>        
        [Fact]
        public void SameVariableNameNest()
        {
            //TestParameters.DisableXamlRoundTrip = true;
            Variable<string> var1 = VariableHelper.CreateInitialized<string>("var1", "Variable in Pick");
            Variable<string> var2 = VariableHelper.CreateInitialized<string>("var1", "Variable in Branch1");

            TestSequence seq = new TestSequence("TestSeq")
            {
                Variables =
                {
                    var1
                },
                Activities =
                {
                    new TestPick()
                    {
                        DisplayName = "Pick",

                        Branches =
                        {
                            new TestPickBranch()
                            {
                                DisplayName = "Branch1",
                                Variables =
                                {
                                    var2
                                },
                                Trigger = new TestDelay()
                                {
                                    Duration = new TimeSpan(0, 0, 3)
                                },
                                Action = new TestWriteLine("Action1")
                                {
                                    HintMessage = "Variable in Branch1",
                                    MessageExpression = (env)=>var2.Get(env)
                                }
                            },
                            new TestPickBranch()
                            {
                                DisplayName = "Branch2",
                                Trigger = new TestBlockingActivity("Block2")
                                {
                                    ExpectedOutcome = Outcome.Canceled
                                },
                                Action = new TestWriteLine("Action2")
                                {
                                    Message = "Action2"
                                }
                            },
                        }
                    }
                }
            };

            ExpectedTrace trace = seq.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(seq, trace);
        }

        /// <summary>
        /// Executing two workflow instance with Pick activity together
        /// Execute Together
        /// </summary>        
        [Fact]
        public void ExecuteTogether()
        {
            Variable<int> var1 = VariableHelper.CreateInitialized<int>("var1", 1);

            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        Variables =
                        {
                            var1
                        },
                        DisplayName = "TriggeredBranch1",
                        Trigger = new TestSequence
                        {
                            Activities =
                            {
                                new TestDelay()
                                {
                                    Duration = new TimeSpan(0, 0, 1)
                                },
                                new TestAssign<int>()
                                {
                                     ToVariable = var1,
                                     ValueExpression = (env)=>(var1.Get(env) + 1),
                                }
                            }
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            HintMessage = "2",
                            MessageExpression = ((env)=>(var1.Get(env).ToString()))
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
                        Trigger = new TestBlockingActivity("Block")
                        {
                            ExpectedOutcome = Outcome.Canceled,
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    }
                }
            };

            ExpectedTrace trace = pick.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(pick, trace);
            TestRuntime.RunAndValidateWorkflow(pick, trace);
        }

        /// <summary>
        /// Executing Pick in ParallelForEach
        /// Execute Parallel
        /// </summary>        
        [Fact]
        public void ExecuteParallel()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "TriggeredBranch1",
                        Trigger = new TestDelay()
                        {
                            Duration = new TimeSpan(0, 0, 1)
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "NoTriggeredBranch2",
                        Trigger = new TestDelay()
                        {
                            ExpectedOutcome = Outcome.Canceled,
                            Duration = new TimeSpan(0, 0, 2)
                        },
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    }
                }
            };

            List<string> values = new List<string> { "a", "b" };
            TestParallelForEach<string> testPFE = new TestParallelForEach<string>()
            {
                Body = pick,
                HintValues = values,
                ValuesExpression = (context => new List<string> { "a", "b" }),
                CompletionCondition = false,
            };

            ExpectedTrace expected = testPFE.GetExpectedTrace();
            expected.AddVerifyTypes(typeof(UserTrace));

            TestRuntime.RunAndValidateWorkflow(testPFE, expected);
        }

        /// <summary>
        /// Pick has Delay in one branch, CancellationScope in another branch. The Delay will win.
        /// Delay & CancellationScope racing scenario. Delay wins.
        /// </summary>        
        [Fact]
        public void CancellationScopeInPick()
        {
            // CancellationScope does not have test OM
            CancellationScope cancel = new CancellationScope()
            {
                DisplayName = "TestCancellationScope",
                Body = new TestSequence()
                {
                    Activities =
                    {
                        new TestDelay("LongDelay", new TimeSpan(0, 0, 5))
                    }
                }.ProductActivity,
                CancellationHandler = new TestSequence()
                {
                    Activities =
                    {
                        new TestWriteLine()
                        {
                            Message = "Cancelled"
                        }
                    }
                }.ProductActivity
            };

            TestCustomActivity testCancel = TestCustomActivity<CancellationScope>.CreateFromProduct(cancel);

            testCancel.ActivitySpecificTraces.Add(
                new UnorderedTraces()
                {
                    Steps =
                    {
                        new UserTrace("Cancelled"),
                    }
                });

            TestPick pick = new TestPick()
            {
                Branches =
                {
                    new TestPickBranch()
                    {
                        Trigger = new TestDelay("ShortDelay", new TimeSpan(0, 0, 1))
                    },
                    new TestPickBranch()
                    {
                        Trigger = testCancel
                    }
                }
            };

            ExpectedTrace expectedTrace = pick.GetExpectedTrace();
            expectedTrace.AddVerifyTypes(typeof(UserTrace));

            TestRuntime.RunAndValidateWorkflow(pick, expectedTrace);
        }
    }
}

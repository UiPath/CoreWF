// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common;
using Xunit;

namespace TestCases.Activities
{
    public class ParallelActivity
    {
        /// <summary>
        /// Add two branches
        /// </summary>        
        [Fact]
        public void SimpleParallel()
        {
            TestWriteLine writeLine1 = new TestWriteLine
            {
                Message = "writeLine1",
                DisplayName = "writeLine1"
            };

            TestWriteLine writeLine2 = new TestWriteLine
            {
                Message = "writeLine2",
                DisplayName = "writeLine2"
            };

            TestParallel parallelActivity = new TestParallel("Parallel Activity");
            parallelActivity.Branches.Add(writeLine1);
            parallelActivity.Branches.Add(writeLine2);

            ExpectedTrace trace = parallelActivity.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelActivity, trace);
        }

        /// <summary>
        /// Have no branches
        /// </summary>        
        [Fact]
        public void NoBranches()
        {
            TestParallel parallelActivity = new TestParallel("Parallel Activity");

            TestRuntime.RunAndValidateWorkflow(parallelActivity);
        }

        /// <summary>
        /// Have one branch
        /// </summary>        
        [Fact]
        public void OneBranch()
        {
            TestWriteLine writeLine1 = new TestWriteLine
            {
                Message = "writeLine1",
                DisplayName = "writeLine1"
            };

            TestParallel parallelActivity = new TestParallel("Parallel Activity");
            parallelActivity.Branches.Add(writeLine1);

            ExpectedTrace trace = parallelActivity.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelActivity, trace);
        }

        /// <summary>
        /// Multiple activities with multiple children each branch
        /// </summary>        
        [Fact]
        public void MultipleBranchesMultipleChildren()
        {
            TimeSpan time = new TimeSpan(0, 0, 2);

            DelegateInArgument<string> currentVariable = new DelegateInArgument<string>() { Name = "currentVariable" };
            DelegateInArgument<string> currentVariable1 = new DelegateInArgument<string>() { Name = "currentVariable1" };
            DelegateInArgument<string> currentVariable2 = new DelegateInArgument<string>() { Name = "currentVariable2" };
            DelegateInArgument<string> currentVariable3 = new DelegateInArgument<string>() { Name = "currentVariable3" };


            #region Sequence
            TestSequence sequence = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine("WritelineAct1", "Hello"),
                    new TestIf("If act1", HintThenOrElse.Then)
                    {
                        Condition = true,
                        ThenActivity = new TestWriteLine("Writeline in then1")
                        {
                             Message = "I am writeline in if activity"
                        }
                    },

                    new TestDelay("Delay act1", time),

                    new TestParallelForEach<string>("Parallel For Each In sequence")
                    {
                        HintValues = new List<string>() { "Element1", "Element2" },
                        ValuesExpression = (context => new List<string>() { "Element1", "Element2" }),
                        CurrentVariable = currentVariable,
                        Body = new TestWriteLine()
                        {
                            MessageExpression = (env) => (string) currentVariable.Get(env),
                            HintMessageList = {"Element2", "Element1"}
                        },

                        HintIterationCount = 2
                    },

                    new TestTryCatch()
                    {
                        Try = new TestThrow<NullReferenceException>()
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<NullReferenceException>()
                        }
                    }
                }
            };
            #endregion // Sequence

            #region Sequence1
            TestSequence sequence1 = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine("WritelineAct2", "Hello"),
                    new TestIf("If act2", HintThenOrElse.Then)
                    {
                        Condition = true,
                        ThenActivity = new TestWriteLine("Writeline in then", "I am writeline in if activity")
                    },

                    new TestDelay("Delay act2", time),

                    new TestParallelForEach<string>("Parallel For Each In sequence1")
                    {
                        HintValues = new List<string>() { "Element1", "Element2" },
                        ValuesExpression = (context => new List<string>() { "Element1", "Element2" }),
                        CurrentVariable = currentVariable1,
                        Body = new TestWriteLine("Writeline in PFE")
                        {
                            MessageExpression = (env) => (string) currentVariable1.Get(env),
                            HintMessageList = {"Element2", "Element1"}
                        },

                        HintIterationCount = 2
                    },

                    new TestTryCatch()
                    {
                        Try = new TestThrow<NullReferenceException>()
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<NullReferenceException>()
                        }
                    }
                }
            };

            #endregion // Sequence1

            #region Sequence2
            TestSequence sequence2 = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine("WritelineAct3", "Hello"),
                    new TestIf("If act3", HintThenOrElse.Then)
                    {
                        Condition = true,
                        ThenActivity = new TestWriteLine("Writeline in then", "I am writeline in if activity")
                    },

                    new TestDelay("Delay act3", time),

                    new TestParallelForEach<string>("Parallel For Each In sequence2")
                    {
                        HintValues = new List<string>() { "Element1", "Element2" },
                        ValuesExpression = (context => new List<string>() { "Element1", "Element2" }),
                        CurrentVariable = currentVariable2,
                        Body = new TestWriteLine("Writeline in PFE")
                        {
                            MessageExpression = (env) => (string) currentVariable2.Get(env),
                            HintMessageList = {"Element2", "Element1"}
                        },

                        HintIterationCount = 2
                    },

                    new TestTryCatch()
                    {
                        Try = new TestThrow<NullReferenceException>()
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<NullReferenceException>()
                        }
                    }
                }
            };
            #endregion // Sequence2

            #region Sequence3

            TestSequence sequence3 = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine("WritelineAct4", "Hello"),
                    new TestIf("If act4", HintThenOrElse.Then)
                    {
                        Condition = true,
                        ThenActivity = new TestWriteLine("Writeline in then","I am writeline in if activity" )
                    },

                    new TestDelay("Delay act4", time),

                    new TestParallelForEach<string>("Parallel For Each In sequence3")
                    {
                        HintValues = new List<string>() { "Element1", "Element2" },
                        ValuesExpression = (context => new List<string>() { "Element1", "Element2" }),
                        CurrentVariable = currentVariable3,
                        Body = new TestWriteLine("Writeline in PFE")
                        {
                            MessageExpression = (env) => (string) currentVariable3.Get(env),
                            HintMessageList = {"Element2", "Element1"}
                        },

                        HintIterationCount = 2
                    },

                    new TestTryCatch()
                    {
                        Try = new TestThrow<NullReferenceException>()
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },

                        Catches =
                        {
                            new TestCatch<NullReferenceException>()
                        }
                    }
                }
            };
            #endregion Sequence2

            TestParallel parallelAct = new TestParallel("ParallelActivity")
            {
                Branches =
                {
                    new TestSequence("First Sequence")
                    {
                        Activities = {sequence}
                    },

                    // Second sequence 
                    new TestSequence("Second sequence")
                    {
                        Activities = {sequence1}
                    },

                    // Third sequence
                    new TestSequence("Third sequence")
                    {
                        Activities = {sequence2}
                    },

                    // Fourth Sequence
                    new TestSequence("Fourth Sequence")
                    {
                        Activities = {sequence3}
                    }
                },

                HintNumberOfBranchesExecution = 4
            };

            ExpectedTrace trace = parallelAct.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelAct, trace);
        }

        /// <summary>
        /// Try to persisit in different branches of Parallel
        /// </summary>        
        [Fact]
        public void PersistWithinBranch()
        {
            TestParallel parallel = new TestParallel("Parallel Act")
            {
                Branches =
                {
                    new TestSequence("seq1")
                    {
                        Activities =
                        {
                            new TestBlockingActivity("Blocking activity 1")
                        }
                    },

                    new TestSequence("seq2")
                    {
                        Activities =
                        {
                           new TestBlockingActivity("Blocking activity 2")
                        }
                    },
                    new TestSequence("seq3")
                    {
                        Activities =
                        {
                           new TestBlockingActivity("Blocking activity 3")
                        }
                    },

                    new TestWriteLine("writeline after seq3")
                    {
                        Message = "HI"
                    }
                }
            };

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(parallel, null, jsonStore, PersistableIdleAction.None))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Blocking activity 1", TestActivityInstanceState.Executing);
                runtime.PersistWorkflow();
                runtime.ResumeBookMark("Blocking activity 1", null);

                runtime.WaitForActivityStatusChange("Blocking activity 2", TestActivityInstanceState.Executing);
                runtime.PersistWorkflow();
                runtime.ResumeBookMark("Blocking activity 2", null);

                runtime.WaitForActivityStatusChange("Blocking activity 3", TestActivityInstanceState.Executing);
                runtime.PersistWorkflow();
                runtime.ResumeBookMark("Blocking activity 3", null);

                runtime.WaitForCompletion();
            }
        }

        /// <summary>
        /// ParallelWithinParallel
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void ParallelWithinParallel()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestParallel parallel = new TestParallel("ParallelActivity")
            {
                Branches =
                {
                    new TestParallelForEach<string>("Parallel For Each in Parallel")
                    {
                        Body = new TestSequence("Seq in Parallel For Each")
                        {
                            Activities =
                            {
                                new TestWhile("While Act")
                                {
                                    Variables = {counter},
                                    ConditionExpression = (env) => (bool)(counter.Get(env) < 3),
                                    Body = new TestSequence("Seq in While")
                                    {
                                        Activities =
                                        {
                                            new TestAssign<int>("Assign activity")
                                            {
                                                ToVariable = counter,
                                                ValueExpression = (env) => (int) counter.Get(env) + 1
                                            },

                                            new TestWriteLine("Writeline in While")
                                            {
                                                Message = "I am a message in while body"
                                            }
                                        }
                                    },

                                    HintIterationCount = 3
                                }
                            }
                        },

                        HintValues = new List<string>() { "Hello", "How", "Are", "You" },
                        ValuesExpression = (context =>new List<string>() { "Hello", "How", "Are", "You" }),
                    },

                    new TestParallel("Parallel in Parallel")
                    {
                        Branches =
                        {
                            new TestWriteLine("Writeline", "Hello"),
                            new TestWriteLine("Writeline2", "World")
                        },

                        CompletionCondition = true,
                        HintNumberOfBranchesExecution = 1,
                    }
                },
                HintNumberOfBranchesExecution = 2
            };

            ExpectedTrace trace = parallel.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallel, trace);
        }

        /// <summary>
        /// Parallel with WorkflowInvoker
        /// </summary>        
        [Fact]
        public void ParallelWithWorkFlowInvoker()
        {
            TestWriteLine writeLine1 = new TestWriteLine
            {
                Message = "writeLine1",
                DisplayName = "writeLine1"
            };

            TestWriteLine writeLine2 = new TestWriteLine
            {
                Message = "writeLine2",
                DisplayName = "writeLine2"
            };

            TestParallel parallelActivity = new TestParallel("Parallel Activity");
            parallelActivity.Branches.Add(writeLine1);
            parallelActivity.Branches.Add(writeLine2);

            TestRuntime.RunAndValidateUsingWorkflowInvoker(parallelActivity, null, null, null);
        }

        /// <summary>
        /// Parallel.CompletionCondition evaluates to true, if a child of Parallel overrides Cancel but does not call base.Cancel(context)
        /// </summary>        
        [Fact]
        public void ParallelWithAChildThatOverridesCancelAndCompletionConditionIsTrue()
        {
            Variable<bool> cancelIt = new Variable<bool> { Name = "cancelIt", Default = false };

            TestParallel parallelActivity = new TestParallel("Parallel Activity")
            {
                ExpectedOutcome = new Outcome(OutcomeState.Completed) { IsOverrideable = false },
                HintNumberOfBranchesExecution = 2,
                Variables = { cancelIt },
                CompletionConditionVariable = cancelIt,
                Branches =
                {
                    new TestSequence
                    {
                        Activities =
                        {
                            new TestDelay{ Duration = new TimeSpan(1)},
                            new TestAssign<bool>{ ToVariable = cancelIt, Value = true},
                        }
                    },
                    new TestBlockingActivityWithWriteLineInCancel("writeLineInCancel", OutcomeState.Completed)
                    {
                        ExpectedOutcome = new Outcome(OutcomeState.Completed, OutcomeState.Canceled),
                    },
                },
            };

            TestRuntime.RunAndValidateWorkflow(parallelActivity);
        }

        /// <summary>
        /// Parallel.CompletionCondition evaluates to true, if a child of Parallel overrides Cancel and throws an exception there.
        /// </summary>     
        [Fact(Skip = "Test cases not executed as part of suites and don't seem to pass on desktop. The aborted reason is NOT a TestCaseException, but it looks like the test framework is creating the exception")]
        public void ParallelWithAChildThatThrowsInCancelAndCompletionConditionIsTrue()
        {
            Variable<bool> cancelIt = new Variable<bool> { Name = "cancelIt", Default = false };

            TestParallel parallelActivity = new TestParallel("Parallel Activity")
            {
                HintNumberOfBranchesExecution = 2,
                Variables = { cancelIt },
                CompletionConditionVariable = cancelIt,
                Branches =
                {
                    new TestSequence
                    {
                        Activities =
                        {
                            new TestDelay{ Duration = new TimeSpan(1)},
                            new TestAssign<bool>{ ToVariable = cancelIt, Value = true},
                        }
                    },
                    new TestBlockingActivityWithWriteLineInCancel("writeLineInCancel", OutcomeState.Faulted)
                    {
                        ExpectedOutcome = Outcome.UncaughtException()
                    },
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parallelActivity))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForAborted(out Exception outException, false);
                if (outException == null || outException.InnerException == null || !outException.InnerException.GetType().Equals(typeof(TestCaseException)))
                {
                    throw new TestCaseException(String.Format("Workflow was suuposed to Abort with a TestCaseException, but this is the exception: {0}", outException.ToString()));
                }
                else
                {
                    //Log.Info("Workflow aborted as excpected");
                }
            }
        }

        //[Fact]
        //public void DifferentArguments()
        //{
        //    //Testing Different argument types for Parallel.CompletionCondition
        //    // DelegateInArgument
        //    // DelegateOutArgument
        //    // Activity<T>
        //    // Variable<T> , Activity<T> and Expression is already implemented.

        //    DelegateInArgument<bool> delegateInArgument = new DelegateInArgument<bool>("Condition");
        //    DelegateOutArgument<bool> delegateOutArgument = new DelegateOutArgument<bool>("Output");

        //    TestCustomActivity<InvokeFunc<bool, bool>> invokeFunc = TestCustomActivity<InvokeFunc<bool, bool>>.CreateFromProduct(
        //       new InvokeFunc<bool, bool>
        //       {
        //           Argument = false,
        //           Func = new ActivityFunc<bool, bool>
        //           {
        //               Argument = delegateInArgument,
        //               Result = delegateOutArgument,
        //               Handler = new CoreWf.Statements.Sequence
        //               {
        //                   DisplayName = "Sequence1",
        //                   Activities =
        //                    {
        //                        new CoreWf.Statements.Parallel
        //                        {
        //                            DisplayName = "Parallel1",
        //                            CompletionCondition =  ExpressionServices.Convert<bool>( ctx=> delegateInArgument.Get(ctx) ),
        //                            Branches =
        //                            {
        //                                new CoreWf.Statements.WriteLine{ DisplayName = "W1", Text = new InArgument<string>( new VisualBasicValue<string>("Condition & \"\" ") ) },
        //                                new CoreWf.Statements.Assign<bool>
        //                                {
        //                                    DisplayName = "Assign1",
        //                                    Value = true,
        //                                    To = delegateInArgument,
        //                                },
        //                                new CoreWf.Statements.Delay { DisplayName = "Delay1", Duration = new TimeSpan(0, 0, 1) }
        //                            }
        //                        },
        //                        new CoreWf.Statements.Assign<bool>
        //                                {
        //                                    DisplayName = "Assign2",
        //                                    Value = false,
        //                                    To = delegateOutArgument,
        //                                },
        //                        new CoreWf.Statements.Parallel
        //                        {
        //                            DisplayName = "Parallel2",
        //                            CompletionCondition =  ExpressionServices.Convert<bool>( ctx=> delegateOutArgument.Get(ctx) ),
        //                            Branches =
        //                            {
        //                                new CoreWf.Statements.WriteLine{ DisplayName = "W2", Text = new InArgument<string>( new VisualBasicValue<string>("Output & \"\" ") ) },
        //                                new CoreWf.Statements.Assign<bool>
        //                                {
        //                                    DisplayName = "Assign3",
        //                                    Value = true,
        //                                    To = delegateOutArgument,
        //                                },
        //                                new CoreWf.Statements.Delay { DisplayName = "Delay2", Duration = new TimeSpan(0, 0, 1) }
        //                            }
        //                        }
        //                    },
        //               }
        //           }
        //       }
        //       );

        //    TestSequence sequenceForTracing = new TestSequence
        //    {
        //        DisplayName = "Sequence1",
        //        Activities =
        //        {
        //            new TestParallel
        //            {
        //                DisplayName = "Parallel1",
        //                ActivitySpecificTraces = 
        //                {
        //                    new UnorderedTraces()
        //                    {
        //                        Steps =
        //                        {
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
        //                                    }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("Assign1", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("Assign1", ActivityInstanceState.Closed),
        //                                }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("Delay1", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("Delay1", ActivityInstanceState.Canceled),
        //                                }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("W1", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("W1", ActivityInstanceState.Closed),
        //                                }
        //                            },
        //                        }
        //                    },
        //                }
        //            },
        //            new TestAssign<bool>()
        //            {
        //                DisplayName = "Assign2",
        //            },
        //            new TestParallel
        //            {
        //                DisplayName = "Parallel2",
        //                ActivitySpecificTraces = 
        //                {
        //                      new UnorderedTraces()
        //                    {
        //                        Steps =
        //                        {
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
        //                                    }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("Assign3", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("Assign3", ActivityInstanceState.Closed),
        //                                }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("Delay2", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("Delay2", ActivityInstanceState.Canceled),
        //                                }
        //                            },
        //                            new OrderedTraces()
        //                            {
        //                                Steps = 
        //                                {
        //                                    new ActivityTrace("W2", ActivityInstanceState.Executing),
        //                                    new ActivityTrace("W2", ActivityInstanceState.Closed),
        //                                }
        //                            },
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    };

        //    invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);


        //    TestIf root = new TestIf(HintThenOrElse.Then)
        //    {
        //        ConditionActivity = invokeFunc,
        //        ThenActivity = new TestWriteLine { Message = "True", HintMessage = "True" },
        //        ElseActivity = new TestWriteLine { Message = "False", HintMessage = "This is not expected" },
        //    };

        //    //change this line back to check for constraints!
        //    TestRuntime.RunAndValidateWorkflowWithoutConstraintValidation(root);
        //}
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using System.Activities.Expressions;
using Xunit;

namespace TestCases.Activities
{
    public class DoWhile : IDisposable
    {
        public void Dispose()
        {
        }

        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void BasicDoWhileTest()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    new TestDoWhile("dowhile act")
                    {
                        ConditionExpression =((env) => ((int)counter.Get(env)) < 10),
                        Body =

                            new TestSequence("test sequence act")
                            {
                                Activities =
                                {
                                    new TestWriteLine("write hello", "Its a small world after all"),
                                    new TestAssign<int>("Increment Counter")
                                    {
                                        ToVariable =counter,
                                        ValueExpression =((env) => (((int)counter.Get(env))) + 1),
                                        // OR you can assign value directly (not using expression)
                                        // Value = 15 
                                    },
                                },
                            },

                            HintIterationCount = 10,
                    }
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void BasicDoWhileExecuteOnce()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerseq");
            TestAssign<int> increment = new TestAssign<int>("Increment Counter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestDoWhile doWhile = new TestDoWhile("dowhile")
            {
                ConditionExpression = (env) => ((int)counter.Get(env)) < 0,
                Body = innerSequence,
                HintIterationCount = 1,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };
            increment.ToVariable = counter;
            increment.ValueExpression = ((env) => (((int)counter.Get(env))) + 1);


            innerSequence.Activities.Add(writeLine);
            innerSequence.Activities.Add(increment);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(doWhile);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Set condition to a valid rule and run with an activity in it. Condition is true initially but made false in the do part.
        /// </summary>        
        [Fact]
        public void SimpleDoWhileConditionTrueSetToFalse()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner seq");
            TestAssign<int> o1o = new TestAssign<int>("Hong");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestDoWhile doWhile = new TestDoWhile("do while")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) == 0),
                Body = innerSequence,
                HintIterationCount = 1,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "The world is changing all the time"
            };
            o1o.ToVariable = counter;
            // Use the mod make it as Zero-One-Zero.
            o1o.ValueExpression = ((env) => ((((int)counter.Get(env))) + 1) % 2);

            innerSequence.Activities.Add(writeLine);
            innerSequence.Activities.Add(o1o);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(doWhile);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Set  condition to a valid rule and run 5-6 iterations without any activities in.
        /// </summary>        
        [Fact]
        public void SimpleEmptyDoWhile()
        {
            //  Test case description:
            //  Set  condition to a valid rule and run 5-6 iterations without any activities in.
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestDoWhile doWhile = new TestDoWhile("dowhile")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 0),
                Body = new TestSequence("inner seq"),
                HintIterationCount = 1,
                Variables = { counter }
            };
            doWhile.Body = null;
            TestRuntime.RunAndValidateWorkflow(doWhile);
        }

        /// <summary>
        /// Set condition to null
        /// </summary>        
        [Fact]
        public void DoWhileConditionNull()
        {
            //  Test case description:
            //  Set condition to null
            TestDoWhile doWhile = new TestDoWhile("dowhile")
            {
                Condition = true,
                Body = new TestSequence("inner seq"),
                HintIterationCount = 0,
            };
            System.Activities.Statements.DoWhile productDoWhile = (System.Activities.Statements.DoWhile)doWhile.ProductActivity;
            productDoWhile.Condition = null;

            TestRuntime.ValidateInstantiationException(doWhile, String.Format("Condition must be set before DoWhile activity '{0}' can be used.", doWhile.DisplayName));
        }

        /// <summary>
        /// Set condition to a valid rule and run with an activity in it. Condition is false initially, then set to
        /// Set condition to a valid rule and run with an activity in it. Condition is false initially, then set to true in the do part.
        /// </summary>        
        [Fact]
        public void SimpleDoWhileConditionFalseSetToTrue()
        {
            //  Test case description:
            //  Set condition to a valid rule and run with an activity in it. Condition is false initially, then set to
            //  true in the do part. 

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner sequence");
            TestAssign<int> o1o = new TestAssign<int>("Hong");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestDoWhile doWhile = new TestDoWhile("do while")
            {
                ConditionExpression = ((env) => (counter.Get(env) == 1)),
                Body = innerSequence,
                HintIterationCount = 2,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "set the counter to be 1"
            };
            o1o.ToVariable = counter;
            // Use the mod make it as Zero-One-Zero.
            o1o.ValueExpression = ((env) => (counter.Get(env) + 1) % 2);

            innerSequence.Activities.Add(writeLine);
            innerSequence.Activities.Add(o1o);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(doWhile);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }


        /// <summary>
        /// Set condition to a valid rule and run with an activity in it. Condition is true
        /// </summary>        
        [Fact]
        public void SimpleDoWhileConditionTrue()
        {
            //  Test case description:
            //  Set condition to a valid rule and run with an activity in it. Condition is true

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    new TestDoWhile("dowhile act")
                    {
                        ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                        Body =

                            new TestSequence("test sequence act")
                            {
                                Activities =
                                {
                                    new TestWriteLine("write hello", "Its a small world after all"),

                                    new TestIncrement("Increment Counter")
                                    {
                                        CounterVariable = counter,
                                    },
                                },
                            },
                        HintIterationCount = 10,
}
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Set condition to a valid rule and run with an activity in it. Condition is false.
        /// </summary>        
        [Fact]
        public void SimpleDoWhileConditionFalse()
        {
            //  Test case description:
            //  Set condition to a valid rule and run with an activity in it. Condition is false.
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    new TestDoWhile("dowhile act")
                    {
                        ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                        Body =

                            new TestSequence("test sequence act")
                            {
                                Activities =
                                {
                                    new TestWriteLine("write hello", "Its a small world after all"),

                                    new TestIncrement("Increment Counter" , 10)
                                    {
                                        CounterVariable = counter,
                                    },
                                },
                            },
                        HintIterationCount = 1,
}
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestDoWhile doWhile = new TestDoWhile("dowhile act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                Body = new TestSequence("test sequence act")
                {
                    Activities =
                    {
                        new TestWriteLine("wrong activity", "Its a big world after all"),
                        new TestAssign<int>("Increment Counter")
                        {
                            ToVariable =counter,
                            ValueExpression =((env) => (((int)counter.Get(env))) + 1),
                        },
                    },
                },
                HintIterationCount = 5,
            };
            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    doWhile,
                }
            };

            WorkflowInspectionServices.GetActivities(doWhile.ProductActivity);

            doWhile.ConditionExpression = (env) => ((int)counter.Get(env)) < 5;
            doWhile.Body = new TestSequence("test sequence act")
            {
                Activities =
                {
                    new TestWriteLine("write hello", "Its a small world after all"),
                    new TestAssign<int>("Increment Counter")
                    {
                        ToVariable =counter,
                        ValueExpression =((env) => (((int)counter.Get(env))) + 1),
                    },
                },
            };

            // We need to recache the metadata now that we've changed the tree
            WorkflowInspectionServices.CacheMetadata(outerSequence.ProductActivity);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Do-while with exception
        /// </summary>        
        [Fact]
        public void DoWhileFault()
        {
            //  Test case description:
            //  Do-while with exception
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestSequence seq = new TestSequence
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    new TestDoWhile("dowhile")
                    {
                        ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                        Body = new TestSequence("test sequence act")
                        {
                            Activities =
                            {
                                new TestWriteLine("executing act", "i will survive, i will execute"),
                                new TestAssign<int>("Increment Counter")
                                {
                                    ToVariable =counter,
                                    ValueExpression =((env) => (((int)counter.Get(env))) + 1),
                                },
                                //new TestThrow<SharedContextItemInvalidUnlockException>("Exception")
                                new TestThrow<InvalidOperationException>("Exception")
                                {
                                    //ExceptionExpression = (context => new SharedContextItemInvalidUnlockException("key!", "something other than normal messages"))
                                    ExceptionExpression = (context => new InvalidOperationException("key! something other than normal messages"))
                                },
                                new TestWriteLine("executing act", "i wont survive, i wont execute"),
},
                        },
                        HintIterationCount = 1,
                    }
                }
            };
            Dictionary<string, string> excProperties = new Dictionary<string, string>();
            //TestRuntime.RunAndValidateAbortedException(seq, typeof(SharedContextItemInvalidUnlockException), excProperties);
            TestRuntime.RunAndValidateAbortedException(seq, typeof(InvalidOperationException), excProperties);
        }

        /// <summary>
        /// Change the value of a variable in an inner activity and see the changed value in the next loop. Also
        /// Change the value of a variable in an inner activity and see the changed value in the next loop. Also change the value of a variable that is in the do while's scope
        /// </summary>        
        [Fact]
        public void DoWhileVariableScopingTest()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            Variable<int> tempVariable = VariableHelper.CreateInitialized<int>("tempVariable", 2);

            Variable<int> doWhileVar = VariableHelper.CreateInitialized<int>("doWhileVar", -1);

            TestSequence sequence = new TestSequence("Outer Sequence")
            {
                Variables =
                {
                    counter,
                    tempVariable
                },

                Activities =
                {
                    new TestAssign<int>("Assign activity")
                    {
                        ToVariable = tempVariable,
                        Value = 3
                    },

                    new TestDoWhile("Do While")
                    {
                        ConditionExpression = (env) => (int)counter.Get(env) < 3,

                        Variables =
                        {
                            doWhileVar
                        },

                        Body = new TestSequence("Body of Do While")
                        {
                            Activities =
                            {
                                new TestIf("If in body of DoWhile", HintThenOrElse.Then)
                                {
                                    ConditionExpression = (env) => (int)tempVariable.Get(env) == 3,
                                    ThenActivity = new TestWriteLine("WriteLine in then of If")
                                    {
                                        Message = "I should execute"
                                    },

                                    ElseActivity = new TestWriteLine("Writeline if Else")
                                    {
                                        Message = "I should not execute"
                                    }
                                },

                                new TestAssign<int>("Increment Counter")
                                {
                                    ToVariable = counter,
                                    ValueExpression = (env) => (int)counter.Get(env) + 1
                                },

                                new TestAssign<int>("Change Do WHile Scope var")
                                {
                                    ToVariable = doWhileVar,
                                    Value = -2
                                },

                               new TestIf("Test if")
                               {
                                   ConditionExpression = (env) => (bool)(doWhileVar.Get(env).Equals(-2)),
                                   ThenActivity = new TestWriteLine("Writeline for do while var test")
                                   {
                                       Message = "Yes got the correct value"
                                   },

                                   ElseActivity = new TestWriteLine("WriteLine in else part")
                                   {
                                       Message = "No I should not execute"
                                   }
                               }
                            }
                        },

                        HintIterationCount = 3
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// DoWhileConditionThrowsException
        /// </summary>        
        [Fact]
        public void DoWhileConditionThrowsException()
        {
            Variable<int> intVar = VariableHelper.CreateInitialized<int>("intVar", 3);

            TestDoWhile doWhile = new TestDoWhile("Do While")
            {
                ConditionOutcome = Outcome.UncaughtException(typeof(DivideByZeroException)),
                Variables =
                {
                    intVar
                },

                ConditionExpression = ((env) => (1 / ((int)(intVar.Get(env)) - 3) == 0)),
                Body = new TestWriteLine("Writeline")
                {
                    Message = "This will be displayed once"
                },

                HintIterationCount = 1
            };

            TestRuntime.RunAndValidateAbortedException(doWhile, typeof(DivideByZeroException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Set condition to be an infinite loop (i.e. always true, or directly to true) and put an activity in it that will have fault
        /// </summary>        
        [Fact]
        public void DoWhileInfiniteLoopFaultAfterHundredLoops()
        {
            //  Test case description:
            //  Set condition to be an infinite loop (i.e. always true, or directly to true) and put an activity in it
            //  that will have fault 

            Variable<int> counter = new Variable<int>("Counter", 0);
            List<HintThenOrElse> hints = new List<HintThenOrElse>();

            for (int i = 0; i < 100; i++)
            {
                hints.Add(HintThenOrElse.Else);
            }
            hints.Add(HintThenOrElse.Then);

            TestDoWhile doWhile = new TestDoWhile
            {
                Variables = { counter },
                ConditionExpression = (e) => counter.Get(e) >= 0,
                Body = new TestSequence
                {
                    Activities =
                    {
                        new TestIf(hints.ToArray())
                        {
                            ConditionExpression = (e) => counter.Get(e) == 100,
                            ThenActivity = new TestThrow<Exception>()
                        },
                        new TestIncrement
                        {
                            CounterVariable = counter,
                            IncrementCount = 1
                        }
                    }
                },
                HintIterationCount = 101
            };
            TestRuntime.RunAndValidateAbortedException(doWhile, typeof(Exception), null);
        }

        /// <summary>
        /// Nested do-whiles deep up to 3-5 levels. Set valid conditions trace the order to be likewhile1 loop1
        /// Nested do-whiles deep up to 3-5 levels. Set valid conditions trace the order to be likewhile1 loop1 while2 loop 1 while 3 loop1 while1 loop1 while2 loop1 whle3 loop2while1 loop1 while2 loop2 while3 loop1whlie1 loop1 while2 loop2 while3 loop2…while1 loop2 while2 loop2 while3 loop2
        /// Since this is marked as WebHosted scenario and it's the first one in the Functional Suite, I'm increasing the timeout by 1 minute bcz it does configurations for IIS, Vdir that take time.
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void NestedDoWhilesAndOtherLoops()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestSequence seq = new TestSequence("OuterSequence")
            {
                Variables =
                {
                    counter
                },
                Activities =
               {
                   new TestDoWhile("Do While")
                   {
                      ConditionExpression = (env) => (int)counter.Get(env) < 1,

                       Body = new TestSequence("First Sequence")
                       {
                           Activities =
                           {
                               new TestDoWhile("Do While in 1st seq")
                               {
                                   ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                   Body = new TestSequence("Second sequence")
                                   {
                                       Activities =
                                       {
                                           new TestWhile("While in 2nd Seq")
                                           {
                                               ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                               Body = new TestSequence("Third Sequence")
                                               {
                                                   Activities =
                                                   {
                                                       new TestWhile("WHile in 3rd seq")
                                                       {
                                                           ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                                           Body = new TestSequence("Fourth sequence")
                                                           {
                                                               Activities =
                                                               {
                                                                   new TestWhile("while in 4th seq")
                                                                   {
                                                                       ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                                                       Body = new TestSequence("Fifthe Sequence")
                                                                       {
                                                                           Activities =
                                                                           {
                                                                               new TestWhile("While in 5th seq")
                                                                               {
                                                                                   ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                                                                   Body = new TestSequence("Sixth Sequence")
                                                                                   {
                                                                                       Activities =
                                                                                       {
                                                                                           new TestWhile("while in 6th seq")
                                                                                           {
                                                                                               ConditionExpression = (env) => (int)counter.Get(env) < 1,
                                                                                               Body = new TestSequence()
                                                                                               {
                                                                                                   Activities =
                                                                                                   {
                                                                                                      new  TestWriteLine()
                                                                                                      {
                                                                                                          Message = "Hi There"
                                                                                                      },
                                                                                                      new TestAssign<int>("Assign 6th")
                                                                                                      {
                                                                                                          ToVariable = counter,
                                                                                                          ValueExpression = (env) => (int)counter.Get(env) + 1
                                                                                                      }
                                                                                                   }
                                                                                               },

                                                                                               HintIterationCount = 1
                                                                                           },

                                                                                           new TestAssign<int>("Assign 6th")
                                                                                           {
                                                                                               ToVariable = counter,
                                                                                               ValueExpression = (env) => (int)counter.Get(env) + 1
                                                                                           }
                                                                                       }
                                                                                   },
                                                                                   HintIterationCount = 1
                                                                               },
                                                                               new TestAssign<int>("assign 5th")
                                                                               {
                                                                                   ToVariable = counter,
                                                                                   ValueExpression = (env) => (int)counter.Get(env) + 1
                                                                               }
                                                                           }
                                                                       },
                                                                       HintIterationCount = 1
                                                                   },

                                                                   new TestAssign<int>("Assign 4th")
                                                                   {
                                                                       ToVariable = counter,
                                                                       ValueExpression = (env) => (int)counter.Get(env) + 1
                                                                   }
                                                               }
                                                           },
                                                           HintIterationCount = 1
                                                       },
                                                       new TestAssign<int>("Assign 3rd")
                                                       {
                                                           ToVariable = counter,
                                                           ValueExpression = (env) => (int)counter.Get(env) + 1
                                                       }
                                                   }
                                               },
                                               HintIterationCount = 1
                                           },
                                           new TestAssign<int>("Assign 2nd")
                                            {
                                                ToVariable = counter,
                                                ValueExpression = (env) => (int)counter.Get(env) + 1
                                            }
                                       }
                                   },
                                   HintIterationCount = 1
                               },
                               new TestAssign<int>("Assign do while")
                               {
                                   ToVariable = counter,
                                   ValueExpression = (env) => (int)counter.Get(env) + 1
                               }
                           }
                       },
                       HintIterationCount = 1
                   },

                   new TestAssign<int>("Assign do while")
                   {
                       ToVariable = counter,
                       ValueExpression = (env) => (int)counter.Get(env) + 1
                   }
               }
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Dowhile cancelled
        /// </summary>        
        [Fact]
        public void DoWhileCancelled()
        {
            TestDoWhile doWhile = new TestDoWhile("Do while")
            {
                Condition = true,
                Body = new TestSequence("Do While Body")
                {
                    Activities =
                    {
                        new TestBlockingActivity("BlockingActivity", "Bookmark")
                        {
                            ExpectedOutcome = Outcome.Canceled
                        },

                        new TestWriteLine("Writeline")
                        {
                            Message = "Hello"
                        }
                    }
                },

                HintIterationCount = 1,
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(doWhile))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                // This test is not run on desktop and I don't think it would pass there if it did run.
                //testWorkflowRuntime.WaitForCompletion();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Run do While with WorkFlowInvoker
        /// </summary>        
        [Fact]
        public void DoWhileWithWorkFlowInvoker()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestAssign<int> increment = new TestAssign<int>("Increment Counter")
            {
                ToVariable = counter,
                ValueExpression = ((env) => (((int)counter.Get(env))) + 1)
            };

            TestDoWhile doWhile = new TestDoWhile("dowhile")
            {
                ConditionExpression = (env) => ((int)counter.Get(env)) < 2,
                Body = increment,
                HintIterationCount = 2,
                Variables = { counter }
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };

            TestRuntime.RunAndValidateUsingWorkflowInvoker(doWhile, null, null, null);
        }

        /// <summary>
        /// DoWhile is persisted in the middle of execution
        /// </summary>        
        [Fact]
        public void DoWhilePersisted()
        {
            Variable<int> counter = new Variable<int>("Counter", 0);
            TestBlockingActivity blocking = new TestBlockingActivity("B");

            TestDoWhile doWhile = new TestDoWhile
            {
                Variables = { counter },
                ConditionExpression = (env) => counter.Get(env) < 1,
                Body = new TestSequence
                {
                    Activities =
                    {
                        blocking,
                        new TestIncrement
                        {
                            IncrementCount = 1,
                            CounterVariable = counter
                        }
                    }
                },
                HintIterationCount = 1
            };

            WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(doWhile, null, jsonStore, PersistableIdleAction.None))
            {
                runtime.ExecuteWorkflow();

                runtime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                runtime.PersistWorkflow();

                runtime.ResumeBookMark("B", null);

                runtime.WaitForCompletion();
            }
        }


        /// <summary>
        /// Create DoWhile with the constructor which takes in Activity<bool>
        /// </summary>        
        [Fact]
        public void DoWhileConditionInConstructor()
        {
            TestExpressionEvaluator<bool> condition = new TestExpressionEvaluator<bool>
            {
                ExpressionResult = false
            };

            TestDoWhile doWhile = new TestDoWhile(condition)
            {
                Body = new TestWriteLine("Hello", "Hello"),
                HintIterationCount = 1
            };

            TestRuntime.RunAndValidateWorkflow(doWhile);
        }

        /// <summary>
        /// The structure is Sequence(DoWhile(Sequence)), and all the three layers have same display name. Both sequences declare varibles with same name.
        /// The variables have a same name inside and outside the while.
        /// </summary>        
        [Fact]
        public void DoWhileVariblesHaveSameName()
        {
            const string activityName = "activity";
            const string varibleName = "counter";

            Variable<int> counter1 = VariableHelper.CreateInitialized<int>(varibleName, 0);
            Variable<int> counter2 = VariableHelper.CreateInitialized<int>(varibleName, 0);

            TestSequence outerSequence = new TestSequence(activityName)
            {
                Variables = { counter1 },
                Activities =
                {
                    new TestDoWhile(activityName)
                    {
                        ConditionExpression = ((env) => ((int)counter1.Get(env)) < 3),
                        HintIterationCount = 3,
                        Body = new TestSequence(activityName)
                        {
                            Activities =
                            {
                                new TestSequence(activityName)
                                {
                                    Variables = { counter2 },
                                    Activities =
                                    {
                                        new TestWriteLine("Trace Counter 2", (env => counter2.Get(env).ToString()), "0"),
                                    }
                                },
                                new TestAssign<int>("Add One")
                                {
                                    ToVariable = counter1,
                                    ValueExpression = ((env) => (((int)counter1.Get(env))) + 1),
                                },
                            }
                        }
                    },
                    new TestWriteLine("Trace Counter")
                    {
                        MessageExpression = (env => counter1.Get(env).ToString()),
                        HintMessage = "3",
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Create DoWhile with the constructor which takes in Activity<bool>
        /// </summary>        
        [Fact]
        public void DoWhileConditionExpressionInConstructor()
        {
            Variable<bool> var = new Variable<bool>("var", false);

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestDoWhile(env => var.Get(env).Equals(true))
                    {
                        Body = new TestWriteLine("Hello", "Hello"),
                        HintIterationCount = 1
                    }
                },
                Variables =
                {
                    var
                }
            };



            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void DifferentArguments()
        {
            //Testing Different argument types for DoWhile.Condition
            // DelegateInArgument
            // DelegateOutArgument
            // Variable<T> , Activity<T> and Expression is already implemented.

            DelegateInArgument<bool> delegateInArgument = new DelegateInArgument<bool>("Condition");
            DelegateOutArgument<bool> delegateOutArgument = new DelegateOutArgument<bool>("Output");

            TestCustomActivity<InvokeFunc<bool, bool>> invokeFunc = TestCustomActivity<InvokeFunc<bool, bool>>.CreateFromProduct(
                new InvokeFunc<bool, bool>
                {
                    Argument = true,
                    Func = new ActivityFunc<bool, bool>
                    {
                        Argument = delegateInArgument,
                        Result = delegateOutArgument,
                        Handler = new System.Activities.Statements.Sequence
                        {
                            DisplayName = "sequence1",
                            Activities =
                            {
                                new System.Activities.Statements.DoWhile
                                {
                                    DisplayName = "DoWhile1",
                                    Condition =  ExpressionServices.Convert<bool>( ctx=> delegateInArgument.Get(ctx) ),
                                    Body = new System.Activities.Statements.Assign<bool>
                                    {
                                        DisplayName = "Assign1",
                                        To = delegateInArgument,
                                        Value =  new Not<bool, bool>{ DisplayName = "Not1",  Operand = delegateInArgument}
                                    },
                                },
                                new System.Activities.Statements.Assign<bool>
                                {
                                    DisplayName = "Assign2",
                                    To = delegateOutArgument,
                                    Value =  new Not<bool, bool>{ DisplayName = "Not2", Operand = delegateInArgument},
                                },
                                new System.Activities.Statements.DoWhile
                                {
                                    DisplayName = "DoWhile2",
                                    Condition =  ExpressionServices.Convert<bool>( ctx=> !delegateOutArgument.Get(ctx) ),
                                    Body = new System.Activities.Statements.Assign<bool>
                                    {
                                        DisplayName = "Assign3",
                                        To = delegateOutArgument,
                                        Value =  new Not<bool, bool>{ DisplayName = "Not3", Operand = delegateInArgument}
                                    },
                                },
                            },
                        }
                    }
                }
                );

            TestSequence sequenceForTracing = new TestSequence
            {
                DisplayName = "sequence1",
                Activities =
                {
                    new TestDoWhile
                    {
                        DisplayName="DoWhile1",
                        ActivitySpecificTraces =
                        {
                             new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("Assign1", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not1", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not1", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign1", ActivityInstanceState.Closed),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
                                }
                            },
                        }
                    },
                    new TestAssign<bool>
                        {
                            DisplayName ="Assign2",
                            ValueActivity = new Test.Common.TestObjects.Activities.Expressions.TestNot<bool, bool>{DisplayName ="Not2"}
                        },
                    new TestDoWhile
                    {
                        DisplayName="DoWhile2",
                        ActivitySpecificTraces =
                        {
                            new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("Assign3", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not3", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not3", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign3", ActivityInstanceState.Closed),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Closed),
                                }
                            },
                        }
                    },
                }
            };
            invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);


            TestIf root = new TestIf(HintThenOrElse.Then)
            {
                ConditionActivity = invokeFunc,
                ThenActivity = new TestWriteLine { Message = "True", HintMessage = "True" },
                ElseActivity = new TestWriteLine { Message = "False", HintMessage = "This is not expected" },
            };

            TestRuntime.RunAndValidateWorkflow(root);
        }
        //[TestCase(TestType = TestType.NotComplete,
        //          Title = @"")]
        //public void DoWhileBodyAddArguments() 
        //{
        //    //  Test case description:
        //    //  Add arguments to the while activities body

        //    throw new NotImplementedException("Test case method DoWhileBodyAddArguments is not implemented.");
        //}

    }
}

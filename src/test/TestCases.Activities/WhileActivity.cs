// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities
{
    public class WhileActivity
    {
        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void BasicWhileTest()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("Seq");
            TestAssign<int> increment = new TestAssign<int>("Increment Counter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                Body = innerSequence,
                HintIterationCount = 10,
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
            outerSequence.Activities.Add(whileAct);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void BasicWhileNotRunTest()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerseq");
            TestAssign<int> increment = new TestAssign<int>("Increment COunter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 0),
                Body = innerSequence,
                HintIterationCount = 0,
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
            outerSequence.Activities.Add(whileAct);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Change the value of a variable in an inner activity and see the changed value in the next loop. Also change the value of a variable that is in the while's scope.
        /// </summary>        
        [Fact]
        public void WhileVariableScopingTest()
        {
            const string innerVaribleAtWhileSequenceName = "innerIntWhileSequence";
            const string outerVaribleName = "outerInt";
            const string counterVaribleName = "counter";

            Variable<int> counter = VariableHelper.CreateInitialized<int>(counterVaribleName, 0);
            Variable<int> outerVarible = VariableHelper.CreateInitialized<int>(outerVaribleName, 0);
            Variable<int> innerVaribleAtWhileSequence = VariableHelper.CreateInitialized<int>(innerVaribleAtWhileSequenceName, 0);

            TestSequence outerSequence = new TestSequence("outerSequence")
            {
                Variables =
                {
                    outerVarible,
                    counter,
                },
                Activities =
                {
                    new TestWhile("while act")
                    {
                        ConditionExpression = ((env) => ((int)counter.Get(env)) < 5),
                        HintIterationCount = 5,
                        Body = new TestSequence("innerseq")
                        {
                            Variables = {innerVaribleAtWhileSequence},
                            Activities =
                            {
                                new TestAssign<int>("Add counter")
                                {
                                    ToVariable = counter ,
                                    ValueExpression = ((env) => (((int)counter.Get(env))) + 1),
                                },
                                new TestAssign<int>("Add outerInt")
                                {
                                    ToVariable = outerVarible ,
                                    ValueExpression = ((env) => (((int)outerVarible.Get(env))) + 1),
                                },
                                new TestAssign<int>("Add innerIntWhileSequence")
                                {
                                    ToVariable = innerVaribleAtWhileSequence,
                                    ValueExpression = ((env) => (((int)innerVaribleAtWhileSequence.Get(env))) + 1),
                                },
                                new TestWriteLine("Trace innerIntWhileSequence", (env => innerVaribleAtWhileSequence.Get(env).ToString()), "1"),
                            },
                        },
                    },

                    new TestWriteLine("Check outerInt",(env => outerVarible.Get(env).ToString()), "5"),
                },
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }


        /// <summary>
        /// The structure is Sequence(While(Sequence)), and all the three layers have same display name. Both sequences declare variables with same name.
        /// </summary>        
        [Fact]
        public void WhileVariblesHaveSameNameAtThreeLayers()
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
                    new TestWhile(activityName)
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
        /// Set  condition to a valid rule and run 5-6 iterations without any activities in.
        /// </summary>        
        [Fact]
        public void SimpleEmptyWhile()
        {
            //  Test case description:
            //  Set  condition to a valid rule and run 5-6 iterations without any activities in.

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence rootSequence = new TestSequence("rootSequence");
            rootSequence.Variables.Add(counter);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 0),
                Body = new TestSequence("innerseq"),
                HintIterationCount = 10,
            };
            rootSequence.Activities.Add(whileAct);

            whileAct.Body = null;
            TestRuntime.RunAndValidateWorkflow(rootSequence);
        }

        /// <summary>
        /// Set condition to null
        /// </summary>        
        [Fact]
        public void WhileConditionNull()
        {
            //  Test case description:
            //  Set condition to null

            TestWhile whileAct = new TestWhile("while act")
            {
                Body = new TestSequence("innerseq"),
                HintIterationCount = 0,
            };
            ((System.Activities.Statements.While)whileAct.ProductActivity).Condition = null;

            TestRuntime.ValidateInstantiationException(whileAct, String.Format(ErrorStrings.WhileRequiresCondition, whileAct.DisplayName));
        }

        /// <summary>
        /// Nested whiles in other loops
        /// </summary>        
        [Fact]
        public void NestedWhilesAndOtherLoops()
        {
            //  Test case description:
            //  Nested whiles deep up to 3-5 levels. Set valid conditions trace the order to be likewhile1 loop1 while2
            //  loop 1 while 3 loop1 while1 loop1 while2 loop1 whle3 loop2while1 loop1 while2 loop2 while3 loop1whlie1
            //  loop1 while2 loop2 while3 loop2…while1 loop2 while2 loop2 while3 loop2 

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner seq");
            TestAssign<int> increment = new TestAssign<int>("increase count");

            Variable<int> doWhileCounter = VariableHelper.CreateInitialized<int>("counter", 0);
            Variable<int> loopCounter = VariableHelper.CreateInitialized<int>("loopcounter", 0);

            TestAssign<int> loopCounterIncrement = new TestAssign<int>("increase loop counter")
            {
                ToVariable = loopCounter,
                ValueExpression = ((env) => ((int)loopCounter.Get(env)) + 1)
            };

            increment.ToVariable = doWhileCounter;
            increment.ValueExpression = ((env) => ((int)doWhileCounter.Get(env)) + 1);

            TestForEach<string> foreachAct = new TestForEach<string>("ForEach")
            {
                Body = innerSequence,
                ValuesExpression = (context => new List<string>() { "var1", "var2", "var3" }),
                HintIterationCount = 3,
            };

            TestDoWhile doWhile = new TestDoWhile("do while")
            {
                ConditionExpression = ((env) => ((int)doWhileCounter.Get(env)) < 9),
                Body = foreachAct,
                HintIterationCount = 3,
            };

            TestSequence whileSequence = new TestSequence("sequence1");
            TestSequence innerIfSequence = new TestSequence("inner if sequence");
            TestAssign<int> whileIncrement = new TestAssign<int>("increase count2");

            Variable<int> whileCounter = VariableHelper.CreateInitialized<int>("counter2", 0);

            TestSequence ifSequence = new TestSequence("ifSequence");


            TestWhile whileAct = new TestWhile("while")
            {
                ConditionExpression = ((env) => ((int)whileCounter.Get(env)) < 10),
                Body = ifSequence,
                HintIterationCount = 10,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };
            TestWriteLine writeLine2 = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };
            whileIncrement.ToVariable = whileCounter;
            whileIncrement.ValueExpression = ((env) => ((int)whileCounter.Get(env)) + 1);

            TestIf ifAct = new TestIf("ifact 1", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)whileCounter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("w1", "I'm a non-executing funny writeLine"),
                ElseActivity = writeLine2,
            };
            TestIf ifAct2 = new TestIf("if act 2", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)whileCounter.Get(env)) < 10),
                ThenActivity = innerIfSequence,
            };

            TestIf checkLoopCount = new TestIf("check loop count", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)loopCounter.Get(env)) == 90),
                ThenActivity = writeLine,
            };

            ifSequence.Activities.Add(ifAct);
            ifSequence.Activities.Add(ifAct2);
            innerIfSequence.Activities.Add(whileIncrement);
            innerIfSequence.Activities.Add(loopCounterIncrement);
            whileSequence.Variables.Add(whileCounter);
            whileSequence.Activities.Add(whileAct);

            innerSequence.Activities.Add(increment);
            innerSequence.Activities.Add(whileSequence);
            outerSequence.Activities.Add(doWhile);
            outerSequence.Activities.Add(checkLoopCount);
            outerSequence.Variables.Add(doWhileCounter);
            outerSequence.Variables.Add(loopCounter);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }


        /// <summary>
        /// Set condition to null
        /// Set condition to a valid rule and run with an activity in it. Condition is true
        /// </summary>        
        [Fact]
        public void SimpleWhileConditionTrue()
        {
            //  Test case description:
            //  Set condition to a valid rule and run with an activity in it. Condition is true - IMPLEMENTED IN BASIC WHILETEST 

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("Seq");
            TestIncrement increment = new TestIncrement("test increment");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                Body = innerSequence,
                HintIterationCount = 10,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };
            increment.CounterVariable = counter;

            innerSequence.Activities.Add(writeLine);
            innerSequence.Activities.Add(increment);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(whileAct);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => false),
                Body = new TestSequence("Seq"),
            };
            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables = { counter },
                Activities =
                {
                    whileAct,
                },
            };

            WorkflowInspectionServices.GetActivities(whileAct.ProductActivity);

            whileAct.ConditionExpression = (env) => ((int)counter.Get(env)) < 1;
            whileAct.Body = new TestSequence("Inner Seq")
            {
                Activities =
                {
                    new TestWriteLine("write hello")
                    {
                        Message = "Its a small world after all",
                    },
                    new TestAssign<int>("Increment Counter")
                    {
                        ValueExpression = ((env) => (((int)counter.Get(env))) + 1),
                        ToVariable = counter,
                    },
                },
            };
            whileAct.HintIterationCount = 1;

            // Now that we've changed the tree we need to recache
            WorkflowInspectionServices.CacheMetadata(outerSequence.ProductActivity);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Set condition to a valid rule and run with an activity in it. Condition is false.
        /// </summary>        
        [Fact]
        public void SimpleWhileConditionFalse()
        {
            //  Test case description:
            //  Set condition to a valid rule and run with an activity in it. Condition is false. 

            Variable<int> counter = VariableHelper.CreateInitialized<int>("countervar", 10);

            TestSequence outerSequence = new TestSequence("sequence")
            {
                Variables = { counter },
                Activities =
                {
                    new TestWhile("while")
                    {
                        ConditionExpression = ((env) => ((int)counter.Get(env)) < 3),
                        HintIterationCount = 0,
                        Body = new TestSequence("sequence")
                        {
                            Activities =
                            {
                                new TestSequence("sequence")
                                {
                                    Activities =
                                    {
                                        new TestWriteLine("not execute", "i shudnt execute"),
                                    }
                                },
                                new TestAssign<int>("Add One")
                                {
                                    ToVariable = counter,
                                    ValueExpression = ((env) => (((int)counter.Get(env))) + 1),
                                },
                            }
                        }
                    },
                }
            };

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Set condition to be an infinite loop (i.e. always true, or directly to true) and put an activity in it that will have fault
        /// </summary>        
        [Fact]
        public void WhileInfiniteLoopFaultAfterHundredLoops()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("Seq");
            TestIncrement increment = new TestIncrement("test increment");

            TestSequence inNestedSequence = new TestSequence("Sequence in Nested while");
            TestThrow<ArithmeticException> throwTestActivity = new TestThrow<ArithmeticException>();


            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 200),
                Body = innerSequence,
                HintIterationCount = 200,
            };

            inNestedSequence.Activities.Add(whileAct);
            inNestedSequence.Activities.Add(throwTestActivity);

            TestWhile whileActNested = new TestWhile("while act")
            {
                ConditionExpression = ((env) => (true)),
                Body = inNestedSequence,
                HintIterationCount = 200,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };

            increment.CounterVariable = counter;

            innerSequence.Activities.Add(writeLine);
            innerSequence.Activities.Add(increment);

            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(whileActNested);

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(ArithmeticException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Throw exception in while Body (throw an exception in while condition needs to be added)
        /// Throw exception in while and in while condition
        /// </summary>        
        [Fact]
        public void WhileWithException()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("Seq");
            TestIncrement increment = new TestIncrement("test increment");

            TestThrow<ArithmeticException> throwArt = new TestThrow<ArithmeticException>("throw");

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                Body = innerSequence,
                HintIterationCount = 10,
            };

            increment.CounterVariable = counter;
            innerSequence.Activities.Add(throwArt);
            innerSequence.Activities.Add(increment);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(whileAct);
            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(ArithmeticException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Run While using WorkFlowInvoker
        /// </summary>        
        [Fact]
        public void WhileWithWorkFlowInvoker()
        {
            TestWhile whileAct = new TestWhile
            {
                Body = new TestWriteLine("w1", "This should not be written"),
                Condition = false,
                HintIterationCount = -1
            };

            TestRuntime.RunAndValidateUsingWorkflowInvoker(whileAct, null, null, null);
        }

        /// <summary>
        /// While activity with null body.
        /// </summary>        
        [Fact]
        public void WhileWithNullBody()
        {
            TestWhile whileAct = new TestWhile
            {
                Body = null,
                Condition = false,
                HintIterationCount = -1
            };
            TestRuntime.RunAndValidateWorkflow(whileAct);
        }

        [Fact]
        public void WhileWithExceptionFromCondition()
        {
            //  Test case description:
            //  Throw exception in while and in while condition

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("Seq");
            TestAssign<int> increment = new TestAssign<int>("Increment Counter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                Body = innerSequence,
                HintIterationCount = 10,
            };

            ExceptionThrowingActivitiy<bool> throwFromCondition = new ExceptionThrowingActivitiy<bool>();
            ((System.Activities.Statements.While)whileAct.ProductActivity).Condition = throwFromCondition;
            increment.ToVariable = counter;
            increment.ValueExpression = ((env) => (((int)counter.Get(env))) + 1);
            innerSequence.Activities.Add(increment);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(whileAct);
            OrderedTraces trace = new OrderedTraces();
            trace.Steps.Add(new ActivityTrace(outerSequence.DisplayName, ActivityInstanceState.Executing));
            trace.Steps.Add(new ActivityTrace(whileAct.DisplayName, ActivityInstanceState.Executing));

            OrderedTraces ordered = new OrderedTraces();
            UnorderedTraces unordered = new UnorderedTraces();
            unordered.Steps.Add(ordered);
            unordered.Steps.Add(new ActivityTrace(throwFromCondition.DisplayName, ActivityInstanceState.Executing));
            unordered.Steps.Add(new ActivityTrace(throwFromCondition.DisplayName, ActivityInstanceState.Faulted));
            trace.Steps.Add(unordered);

            ExpectedTrace expected = new ExpectedTrace(trace);
            expected.AddIgnoreTypes(typeof(WorkflowAbortedTrace));
            expected.AddIgnoreTypes(typeof(SynchronizeTrace));

            TestWorkflowRuntime tr = TestRuntime.CreateTestWorkflowRuntime(outerSequence);
            tr.CreateWorkflow();
            tr.ResumeWorkflow();
            tr.WaitForAborted(out Exception exc, expected);

            Assert.True((exc.GetType() == typeof(DataMisalignedException)) && exc.Message == "I am Miss.Aligned!");
        }

        /// <summary>
        /// While cancelled while executing.
        /// Cancel while during execution
        /// </summary>        
        [Fact]
        public void WhileCancelled()
        {
            Variable<int> count = new Variable<int> { Name = "Counter", Default = 0 };
            TestBlockingActivity blocking = new TestBlockingActivity("Bookmark") { ExpectedOutcome = Outcome.Canceled };
            TestWhile whileAct = new TestWhile

            {
                Variables = { count },
                Body = blocking,
                ConditionExpression = (e => count.Get(e) < 5),
                HintIterationCount = 1,
                ExpectedOutcome = Outcome.Canceled
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(whileAct))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.CancelWorkflow();

                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Test the cancellation behavior when Condition is executing.
        /// </summary>
        [Fact]
        public void WhileCancelledDuringConditionExecution()
        {
            TestWhile whileActivity = new TestWhile("While")
            {
                HintIterationCount = 0,
                ExpectedOutcome = Outcome.Canceled,
                ConditionActivity = new TestBlockingActivity<bool>("Blocking")
                {
                    ExpectedOutcome = Outcome.Canceled,
                },
                Body = new TestWriteLine("WriteLine", "WriteLine - Body"),
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(whileActivity))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("Blocking", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Persist While.
        /// Persist while in the middle fo the execution
        /// </summary>        
        [Fact]
        public void WhilePersisted()
        {
            Variable<int> count = new Variable<int> { Name = "Counter", Default = 0 };

            TestWhile whileAct = new TestWhile
            {
                Variables = { count },
                Body = new TestSequence
                {
                    Activities =
                    {
                        new TestBlockingActivity("Bookmark"),
                        new TestIncrement { CounterVariable = count, IncrementCount = 1 }
                    }
                },
                ConditionExpression = (e => count.Get(e) < 1),
                HintIterationCount = 1
            };

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(whileAct, null, jsonStore, PersistableIdleAction.None))
            {
                runtime.ExecuteWorkflow();

                runtime.WaitForIdle();

                runtime.PersistWorkflow();

                runtime.ResumeBookMark("Bookmark", null);

                runtime.WaitForCompletion();
            }
        }

        [Fact]
        public void WhileVariableNotInScope()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWhile whileAct = new TestWhile("while act")
            {
                Condition = false,
                Body = new TestAssign<int>
                {
                    Value = 12,
                    ToVariable = counter,
                },
                HintIterationCount = 10,
            };

            string constraint1 = string.Format(ErrorStrings.VariableShouldBeOpen, counter.Name);
            string constraint2 = string.Format(ErrorStrings.VariableNotVisible, counter.Name);
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>(2);
            constraints.Add(new TestConstraintViolation(constraint1, "VariableReference<Int32>"));
            constraints.Add(new TestConstraintViolation(constraint2, "VariableReference<Int32>"));
            TestRuntime.ValidateWorkflowErrors(whileAct, constraints, constraint1);
        }
        /// <summary>
        /// Create While with the constructor which takes in Activity<bool>
        /// </summary>        
        [Fact]
        public void WhileConditionInConstructor()
        {
            TestExpressionEvaluator<bool> condition = new TestExpressionEvaluator<bool>
            {
                ExpressionResult = false
            };

            TestWhile whileAct = new TestWhile(condition)
            {
                Body = new TestWriteLine("Hello", "Hello"),
                HintIterationCount = 0
            };

            TestRuntime.RunAndValidateWorkflow(whileAct);
        }
        /// <summary>
        /// Create While with the constructor which takes in Activity<bool>
        /// </summary>        
        [Fact]
        public void WhileConditionExpressionInConstructor()
        {
            Variable<bool> var = new Variable<bool>("var", false);

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestWhile(env => var.Get(env).Equals(true))
                    {
                        Body = new TestWriteLine("Hello", "Hello"),
                        HintIterationCount = 0
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
            //Testing Different argument types for While.Condition
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
                           DisplayName = "Sequence1",
                           Activities =
                                    {
                                        new System.Activities.Statements.While
                                        {
                                            DisplayName = "While1",
                                            Condition =  ExpressionServices.Convert<bool>( ctx=> delegateInArgument.Get(ctx) ),
                                            Body = new System.Activities.Statements.Assign<bool>
                                            {
                                                DisplayName = "Assign1",
                                                To = delegateInArgument,
                                                Value =  new System.Activities.Expressions.Not<bool, bool>{ Operand = delegateInArgument, DisplayName = "Not<Boolean,Boolean>"}
                                            },
                                        },
                                        new System.Activities.Statements.Assign<bool>
                                        {
                                            DisplayName = "Assign2",
                                            To = delegateOutArgument,
                                            Value =  new System.Activities.Expressions.Not<bool, bool>{ Operand = delegateInArgument, DisplayName = "Not<Boolean,Boolean>"},
                                        },
                                        new System.Activities.Statements.While
                                        {
                                            DisplayName = "While2",
                                            Condition =  ExpressionServices.Convert<bool>( ctx=> delegateOutArgument.Get(ctx) ),
                                            Body = new System.Activities.Statements.Assign<bool>
                                            {
                                                DisplayName = "Assign3",
                                                To = delegateOutArgument,
                                                Value =  new System.Activities.Expressions.Not<bool, bool>{ Operand = delegateOutArgument, DisplayName = "Not<Boolean,Boolean>"}
                                            },
                                        },
                                    },
                       }
                   }
               }
               );

            TestSequence sequenceForTracing = new TestSequence
            {
                DisplayName = "Sequence1",
                Activities =
                {
                    new TestDoWhile
                    {
                        DisplayName="While1",
                        ActivitySpecificTraces =
                        {
                             new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign1", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Closed),
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
                            ValueActivity = new Test.Common.TestObjects.Activities.Expressions.TestNot<bool, bool>{DisplayName ="Not<Boolean,Boolean>"}
                        },
                    new TestDoWhile
                    {
                        DisplayName="While2",
                        ActivitySpecificTraces =
                        {
                            new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign3", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("Not<Boolean,Boolean>", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign3", ActivityInstanceState.Closed),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("DelegateArgumentValue<Boolean>", ActivityInstanceState.Closed),
                                }
                            },
                        }
                    },
                }
            };
            invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);


            TestIf root = new TestIf(HintThenOrElse.Else)
            {
                ConditionActivity = invokeFunc,
                ThenActivity = new TestWriteLine { Message = "True", HintMessage = "This is not expected" },
                ElseActivity = new TestWriteLine { Message = "False", HintMessage = "False" },
            };

            TestRuntime.RunAndValidateWorkflow(root);
        }
    }
    public class ExceptionThrowingActivitiy<T> : NativeActivity<bool>
    {
        protected override void Execute(NativeActivityContext context)
        {
            throw new DataMisalignedException("I am Miss.Aligned!");
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            //None
        }
    }
}

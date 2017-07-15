// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using CoreWf.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace TestCases.Activities
{
    public class If : IDisposable
    {
        public void Dispose()
        {
        }

        /// <summary>
        /// Test If constructor by ActivityT
        /// </summary>        
        // removed due to usage of TestVisualBasicValue<>
        //[Fact]
        //public void ConstructorForActivityT()
        //{
        //    Variable<bool> var = new Variable<bool>("var", false);

        //    TestSequence seq = new TestSequence()
        //    {
        //        Activities = 
        //        {
        //            new TestIf(new TestVisualBasicValue<bool>("var"), HintThenOrElse.Else)
        //            {
        //                ThenActivity = new TestWriteLine("w", "I'm Funny"),
        //                ElseActivity = new TestWriteLine("w2", "I'm not Funny!")
        //            }
        //        },
        //        Variables =
        //        {
        //            var
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void BasicIfTest()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerseq");
            TestAssign<int> increment = new TestAssign<int>("Increment Counter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence ifSequence = new TestSequence("ifSequence");


            TestDoWhile whileAct = new TestDoWhile("dowhile")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                Body = ifSequence,
                HintIterationCount = 10,
            };

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";
            increment.ToVariable = counter;
            increment.ValueExpression = ((env) => ((int)counter.Get(env)) + 1);

            TestIf ifAct = new TestIf("if act", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "Shouldnt appear on screen"),
                ElseActivity = writeLine
            };

            TestIf ifAct2 = new TestIf("if act2", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = innerSequence,
            };

            ifSequence.Activities.Add(ifAct);
            ifSequence.Activities.Add(ifAct2);
            innerSequence.Activities.Add(increment);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(whileAct);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Have no branches
        /// </summary>        
        [Fact]
        public void NoIfOrElseBranches()
        {
            TestIf ifAct = new TestIf("MyIf", HintThenOrElse.Then)
            {
                Condition = true,
            };


            TestRuntime.RunAndValidateWorkflow(ifAct);
        }

        /// <summary>
        /// Have single branched if-else (just if)
        /// </summary>        
        [Fact]
        public void SimpleIfThenOnly()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerseq");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestSequence ifSequence = new TestSequence("ifSequence");

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";

            TestIf ifAct = new TestIf("if act", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = innerSequence,
            };

            ifSequence.Activities.Add(ifAct);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(ifSequence);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }


        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// </summary>        
        [Fact]
        public void SetIfConditionToNull()
        {
            //  Test case description:
            //  Set condition to null 
            TestIf ifAct = new TestIf("if act", HintThenOrElse.Then)
            {
                ConditionVariable = null,
                ThenActivity = new TestWriteLine("write hello", "Its a small world after all"),
            };
            TestSequence seq = new TestSequence();
            seq.Activities.Add(ifAct);

            string exceptionMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Condition");
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(exceptionMessage, ifAct.ProductActivity, false));

            TestRuntime.ValidateWorkflowErrors(seq, constraints, exceptionMessage);
        }

        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// Have if else activity in which the first condition is just "true" and if there will be any restriction on doing so.
        /// </summary>        
        [Fact]
        public void IfConditionSetToTrue()
        {
            //  Test case description:
            //  Set condition to null 
            TestIf ifAct = new TestIf("if act", HintThenOrElse.Then)
            {
                Condition = true,
                ThenActivity = new TestWriteLine("Write Hello", "It's a small world after all"),
            };

            TestRuntime.RunAndValidateUsingWorkflowInvoker(ifAct, null, null, null);
        }

        /// <summary>
        /// Put it in a while loop  and in the first couple of iterations go through one bracnh, in the restgo
        ///                             through another.
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void IfInWhileSometimesIfThenTrueSometimesElseTrue()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWriteLine writeTrue = new TestWriteLine("writeTrue");
            writeTrue.Message = "I say you are RIGHT!";

            TestWriteLine writeFalse = new TestWriteLine("writeFalse");
            writeFalse.Message = "I say you are WRONG!";

            TestIf ifAct = new TestIf("if act",
                HintThenOrElse.Then,
                HintThenOrElse.Else,
                HintThenOrElse.Then,
                HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) % 2 == 0),
                ThenActivity = writeTrue,
                ElseActivity = writeFalse,
            };

            TestAssign<int> increment = new TestAssign<int>("Add One");
            increment.ToVariable = counter;
            increment.ValueExpression = (env) => (((int)counter.Get(env))) + 1;

            TestSequence sequence = new TestSequence("innerSequence");

            sequence.Activities.Add(ifAct);
            sequence.Activities.Add(increment);

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = (env) => ((int)counter.Get(env)) < 4,
                Body = sequence,
                HintIterationCount = 4,
            };

            TestSequence rootSequence = new TestSequence("rootSequence");
            rootSequence.Activities.Add(whileAct);
            rootSequence.Variables.Add(counter);

            TestRuntime.RunAndValidateWorkflow(rootSequence);
        }

        /// <summary>
        /// Have simple if-else scenario with then executing first and then else
        /// </summary>        
        [Fact]
        public void SimpleIfElse()
        {
            //  Test case description:
            //  Have simple if-else scenario with if, and else

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("sequence act");
            TestAssign<int> changeCounter = new TestAssign<int>("Elif");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";
            changeCounter.ToVariable = counter;
            changeCounter.ValueExpression = (env) => ((int)counter.Get(env)) + 15;

            TestIf ifAct = new TestIf("if act1", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting", "Wont Execute"),
                ElseActivity = changeCounter
            };

            TestIf ifAct2 = new TestIf("if act 2", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = innerSequence,
            };

            outerSequence.Activities.Add(ifAct);
            outerSequence.Activities.Add(ifAct2);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(counter);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Have single branched if-else (just else)
        /// </summary>        
        [Fact]
        public void SimpleIfElseOnly()
        {
            //  Test case description:
            //  Have single branched if-else (just else)

            TestSequence sequence = new TestSequence("Sequence1");
            TestIf ifAct = new TestIf("MyIf", HintThenOrElse.Else)
            {
                Condition = false,
                ElseActivity = new TestWriteLine("Else Branch", "Else Exeuting"),
            };

            sequence.Activities.Add(ifAct);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Nested if else conditions are opposite. 10 levels deep
        /// </summary>        
        [Fact]
        public void NestedIfElseWhenTheConditionsAreTotallyOpposite()
        {
            //  Test case description:
            //   Have nested if-else activities in which: if branch checks if A is true,Its first child is another if
            //  else and checks if A is false, meaning that it will not be executed at all. In this case we probably
            //  will not be having a validation errror that foresees that it wont be executed however this case is
            //  still valid to check the behavior - this leads to the question of if there will be "detection of
            //  unreachable code"


            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner sequence");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 5);

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";

            TestIf ifAct = new TestIf("if1", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "NotExecuting"),
                ElseActivity = innerSequence,
            };

            TestIf ifAct2 = new TestIf("if2", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = ifAct,
            };

            TestIf ifAct3 = new TestIf("if3", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "NotExecuting"),
                ElseActivity = ifAct2,
            };

            TestIf ifAct4 = new TestIf("if4", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = ifAct3,
            };

            TestIf ifAct5 = new TestIf("if5", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "NotExecuting"),
                ElseActivity = ifAct4,
            };

            TestIf ifAct6 = new TestIf("if6", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = ifAct5,
            };

            TestIf ifAct7 = new TestIf("if7", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "NotExecuting"),
                ElseActivity = ifAct6,
            };

            TestIf ifAct8 = new TestIf("if8", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = ifAct7,
            };

            TestIf ifAct9 = new TestIf("if9", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestWriteLine("NotExecuting Writeline", "NotExecuting"),
                ElseActivity = ifAct8,
            };

            TestIf ifAct10 = new TestIf("if10", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = ifAct9,
            };

            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(counter);
            outerSequence.Activities.Add(ifAct10);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// throw exception in then body of if
        /// </summary>        
        [Fact]
        public void ThrowExceptionInThenBody()
        {
            //  Test case description:
            //  2 bracnhes:throw exception in the condition throw exception in if body, else executingthrow exception
            //  in else body if executingthrow exception in both branches if executingthrow exception in both branches
            //  else executing

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner sequence");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";

            TestIf ifAct = new TestIf("if act", HintThenOrElse.Then)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 10),
                ThenActivity = innerSequence
            };

            TestThrow<ArithmeticException> throwArt = new TestThrow<ArithmeticException>("throw");
            innerSequence.Activities.Add(throwArt);
            outerSequence.Activities.Add(ifAct);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(counter);

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(ArithmeticException), new Dictionary<string, string>());
        }

        /// <summary>
        /// throw exception in else body of if
        /// </summary>        
        [Fact]
        public void ThrowExceptionInElseBody()
        {
            //  Test case description:
            //  2 bracnhes:throw exception in the condition throw exception in if body, else executingthrow exception
            //  in else body if executingthrow exception in both branches if executingthrow exception in both branches
            //  else executing

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner seq");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestWriteLine writeLine = new TestWriteLine("write hello");
            writeLine.Message = "Its a small world after all";

            TestIf ifAct = new TestIf("if act", HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) > 10),
                ThenActivity = new TestSequence("sequence in if"),
                ElseActivity = innerSequence
            };

            TestThrow<ArithmeticException> throwArt = new TestThrow<ArithmeticException>("throw");
            innerSequence.Activities.Add(throwArt);
            outerSequence.Activities.Add(ifAct);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(counter);

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(ArithmeticException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestIf testIf = new TestIf("If", HintThenOrElse.Then, HintThenOrElse.Else)
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 1),
                ThenActivity = new TestWriteLine("WriteLine Then")
                {
                    Message = "Executing activity in Then branch",
                },
                ElseActivity = new TestWriteLine("WriteLine Else")
                {
                    Message = "Executing activity in Else branch",
                },
            };

            TestSequence outerSequence = new TestSequence("Outer sequence")
            {
                Variables = { counter },
                Activities =
                {
                    new TestDoWhile("DoWhile")
                    {
                        ConditionExpression = ((env) => counter.Get(env) < 2),
                        Body = new TestSequence("Inner sequence")
                        {
                            Activities =
                            {
                                testIf,
                                new TestAssign<int>("Increment Counter")
                                {
                                    ValueExpression = (env) => counter.Get(env) + 1,
                                    ToVariable = counter,
                                },
                            }
                        },
                        HintIterationCount = 2,
                    }
                },
            };

            WorkflowInspectionServices.GetActivities(testIf.ProductActivity);

            testIf.ThenActivity = new TestWriteLine("Then")
            {
                Message = "In Then branch",
            };
            testIf.ElseActivity = new TestWriteLine("Else")
            {
                Message = "In Else branch",
            };

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Try to add variable to else body
        /// Try to add variable to if else body
        /// </summary>        
        [Fact]
        public void IfElseBodyHasVariable()
        {
            const string varValue = "hello world";
            Variable<string> var = VariableHelper.CreateInitialized<string>("aVariable", varValue);

            TestIf testIf = new TestIf("MyIf", HintThenOrElse.Else)
            {
                Condition = false,
                ThenActivity = new TestWriteLine("Then Branch", "it would not be displayed"),
                ElseActivity = new TestSequence("Else Branch")
                {
                    Variables =
                    {
                        var,
                    },
                    Activities =
                    {
                        new TestWriteLine("Else WriteLine")
                        {
                            MessageVariable = var,
                            HintMessage = varValue
                        },
                    },
                },
            };

            TestRuntime.RunAndValidateWorkflow(testIf);
        }

        /// <summary>
        /// Try to add variable to then body
        /// Try to add variable to if then body
        /// </summary>        
        [Fact]
        public void IfThenBodyHasVariable()
        {
            const string varValue = "hello world";
            Variable<string> var = VariableHelper.CreateInitialized<string>("aVariable", varValue);

            TestIf testIf = new TestIf("MyIf", HintThenOrElse.Then)
            {
                Condition = true,
                ThenActivity = new TestSequence("then Branch")
                {
                    Variables =
                    {
                        var,
                    },
                    Activities =
                    {
                        new TestWriteLine("Then WriteLine",var, varValue),
                    },
                },
                ElseActivity = new TestWriteLine("else Branch", "it would not be displayed"),
            };

            TestRuntime.RunAndValidateWorkflow(testIf);
        }

        /// <summary>
        /// Nested if's or then/else having the same variable name as the parent
        /// </summary>        
        [Fact]
        public void IfThenElseVariableScoping()
        {
            const string variableName = "aVariable";
            Variable<string> sequenceVar = VariableHelper.CreateInitialized<string>(variableName, "sequenceVar");
            Variable<string> outterThenVar = VariableHelper.CreateInitialized<string>(variableName, "outterThenVar");
            Variable<string> innerElseVar = VariableHelper.CreateInitialized<string>(variableName, "innerElseVar");

            TestSequence sequence = new TestSequence("Sequence1")
            {
                Variables =
                {
                    sequenceVar,
                },
                Activities =
                {
                    new TestIf("MyIf", HintThenOrElse.Then)
                    {
                        Condition = true,
                        ThenActivity = new TestSequence("then Branch")
                        {
                            Variables =
                            {
                                outterThenVar,
                            },
                            Activities =
                            {
                                new TestIf("MyIf", HintThenOrElse.Else)
                                {
                                    Condition = false,
                                    ThenActivity = new TestWriteLine("w1", "I'm a funny writeLine") { HintMessage = "I'm a funny writeLine" },
                                    ElseActivity = new TestSequence("Then Branch")
                                    {
                                        Variables =
                                        {
                                            innerElseVar,
                                        },
                                        Activities =
                                        {
                                            new TestWriteLine("write sequenceVar", sequenceVar, "sequenceVar"),
                                            new TestWriteLine("write outterThenVar", outterThenVar,"outterThenVar"),
                                            new TestWriteLine("write innerElseVar", innerElseVar, "innerElseVar"),
                                        },
                                    },
                                }
                            },
                        },
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Cancel during execution of then body
        /// Cancel during execution
        /// </summary>        
        [Fact]
        public void CancelIfDuringExecution()
        {
            TestIf testIf = new TestIf("MyIf", HintThenOrElse.Then)
            {
                Condition = true,
                ThenActivity = new TestSequence("Then Branch")
                {
                    Activities =
                    {
                        new TestCustomActivity<BlockingActivity>("BlockingActivity",null)
                        {
                            ExpectedOutcome = Outcome.Canceled
                        },
                        new TestWriteLine("Test WriteLine", "The message would not be displayed"),
                    },
                },
                ElseActivity = new TestWriteLine("Else Branch", "The message would not be displayed"),
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testIf))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Cancel during execution of else
        /// </summary>        
        [Fact]
        public void CancelElseDuringExecution()
        {
            TestIf testIf = new TestIf("MyIf", HintThenOrElse.Else)
            {
                Condition = false,
                ThenActivity = new TestWriteLine("Then Branch", "The message would not be displayed"),
                ElseActivity = new TestSequence("Else Branch")
                {
                    Activities =
                    {
                        new TestCustomActivity<BlockingActivity>("BlockingActivity",null)
                        {
                            ExpectedOutcome = Outcome.Canceled
                        },
                        new TestWriteLine("Test WriteLine", "The message would not be displayed"),
                    },
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(testIf))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Throw exception in if condition, and make sure both branches won't be executed
        /// </summary>        
        [Fact]
        public void ThrowExceptionInIfCondition()
        {
            Variable<int> intVar = VariableHelper.CreateInitialized<int>("intVariable", 3);
            TestSequence sequence = new TestSequence("Sequence1")
            {
                Variables =
                {
                    intVar,
                },
                Activities =
                {
                    new TestIf("if act",HintThenOrElse.Neither)
                    {
                        ExpectedOutcome = Outcome.UncaughtException(typeof(DivideByZeroException)),
                        ConditionExpression = ((env) => (1/((int)(intVar.Get(env))-3) == 0)),
                        ThenActivity = new TestSequence("Then Branch"),
                        ElseActivity = new TestSequence("Else Branch"),
                    }
                }
            };
            TestRuntime.RunAndValidateAbortedException(sequence, typeof(DivideByZeroException), new Dictionary<string, string>());
        }

        /// <summary>
        /// If we throw exception in else body, and set condition to yes, we won't get exception in else body
        /// </summary>        
        [Fact]
        public void ThrowExceptionInElseBodySetConditionToYes()
        {
            Variable<int> intVar = VariableHelper.CreateInitialized<int>("intVariable", 3);

            TestSequence sequence = new TestSequence("Sequence1")
            {
                Variables =
                {
                    intVar,
                },
                Activities =
                {
                    new TestIf("if act",HintThenOrElse.Then)
                    {
                        ConditionExpression = ((env) => true),
                        ThenActivity = new TestWriteLine("Then Branch", "hello everyone, I am here:)"),
                        ElseActivity = new TestWriteLine("Else Branch")
                        {
                            ExpectedOutcome = Outcome.UncaughtException( typeof(DivideByZeroException) ),
                            MessageExpression = ((env) => (1/((int)(intVar.Get(env))-3) == 0).ToString()),
                        },
                    },
                },
            };
            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// If we throw exception in then body, and set condition to false, we won't get exception in then body
        /// </summary>        
        [Fact]
        public void ThrowExceptionInThenBodySetConditionToFalse()
        {
            Variable<int> intVar = VariableHelper.CreateInitialized<int>("intVariable", 3);

            TestSequence sequence = new TestSequence("Sequence1")
            {
                Variables =
                {
                    intVar,
                },
                Activities =
                {
                    new TestIf("if act",HintThenOrElse.Else)
                    {
                        ConditionExpression = ((env) => false),
                        ThenActivity = new TestWriteLine("Then Branch")
                        {
                            ExpectedOutcome = Outcome.UncaughtException( typeof(DivideByZeroException) ),
                            MessageExpression = ((env) => (1/((int)(intVar.Get(env))-3) == 0).ToString()),
                        },
                        ElseActivity = new TestWriteLine("Else Branch", "hello everyone, I am here:)"),
                    },
                },
            };
            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Run If activity using WorkflowInvoker
        /// </summary>        
        [Fact]
        public void IfWithWorkflowInvoker()
        {
            TestIf ifAct = new TestIf("MyIf", HintThenOrElse.Then)
            {
                ThenActivity = new TestWriteLine("w", "I'm Funny"),
                ElseActivity = new TestWriteLine("w", "I'm not Funny!")
            };


            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("Condition", true);
            TestRuntime.RunAndValidateUsingWorkflowInvoker(ifAct, args, null, null);
        }

        /// <summary>
        /// Create If with the constructor which takes in InArgument<bool>
        /// </summary>        
        [Fact]
        public void IfConditionWithInArgument()
        {
            Variable<bool> var = new Variable<bool>("var", false);

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestIf(var, HintThenOrElse.Else)
                    {
                        ThenActivity = new TestWriteLine("w", "I'm Funny"),
                        ElseActivity = new TestWriteLine("w2", "I'm not Funny!")
                    }
                },
                Variables =
                {
                    var
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Create If with the constructor which takes in InArgument<bool>
        /// </summary>        
        [Fact]
        public void IfConditionExpressionInConstructor()
        {
            Variable<bool> var = new Variable<bool>("var", false);

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestIf(env => var.Get(env).Equals(true), HintThenOrElse.Else)
                    {
                        ThenActivity = new TestWriteLine("w", "I'm Funny"),
                        ElseActivity = new TestWriteLine("w2", "I'm not Funny!")
                    }
                },
                Variables =
                {
                    var
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Above scenario with persistence to see if there will be any change in the behavior and if we are able tp preserve the state and the rules fine.
        /// IfInWhileWithPersistence
        /// </summary>        
        [Fact]
        public void IfInWhileWithPersistence()
        {
            //  Test case description:
            //  Above scenario with persistence to see if there will be any change in the behavior and if we are able
            //  tp preserve the state and the rules fine. 

            Variable<int> count = new Variable<int> { Name = "Counter", Default = 0 };

            TestWhile whileAct = new TestWhile
            {
                Variables = { count },
                Body = new TestSequence
                {
                    Activities =
                    {
                        new TestIf(HintThenOrElse.Then)
                        {
                            Condition = true,
                            ThenActivity = new TestBlockingActivity("Bookmark"),
                        },
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
        public void DifferentArguments()
        {
            //Testing Different argument types for If.Condition
            // DelegateInArgument
            // DelegateOutArgument
            // Activity<T>
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
                       Handler = new CoreWf.Statements.Sequence
                       {
                           DisplayName = "Sequence1",
                           Activities =
                            {
                                new CoreWf.Statements.If
                                {
                                    DisplayName = "If1",
                                    Condition = delegateInArgument,
                                    Then = new CoreWf.Statements.Sequence
                                    {
                                        DisplayName = "Sequence2",
                                        Activities =
                                        {
                                            new CoreWf.Statements.Assign<bool>
                                            {
                                                DisplayName = "Assign1",
                                                Value = delegateInArgument,
                                                To = delegateOutArgument,
                                            },
                                            new CoreWf.Statements.If
                                            {
                                                DisplayName = "If2",
                                                Condition = delegateOutArgument,
                                                Then = new CoreWf.Statements.WriteLine
                                                {
                                                    DisplayName = "W1",
                                                    Text = "Tested DelegateIn and DelegateOut arguments in If condition"
                                                },
                                            }
                                        }
                                    }
                                }
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
                    new TestIf(HintThenOrElse.Then)
                    {
                        DisplayName="If1",
                        ThenActivity = new TestSequence
                        {
                            DisplayName = "Sequence2",
                            Activities =
                            {
                                new TestAssign<bool>{ DisplayName = "Assign1"},
                                new TestIf( HintThenOrElse.Then)
                                {
                                    DisplayName = "If2",
                                    ThenActivity = new TestSequence("W1"),
                                }
                            }
                        }
                    }
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
    }
}

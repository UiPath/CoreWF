// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TestCases.Activities
{
    using System;
    using System.Collections.Generic;
    using Test.Common.TestObjects.Activities;
    using Test.Common.TestObjects.Activities.Tracing;
    using Test.Common.TestObjects.Runtime;
    using Test.Common.TestObjects.Utilities;
    using Test.Common.TestObjects.Utilities.Validation;
    using TestCases.Activities.Common;
    using Xunit;
    using TAC = TestCases.Activities.Common;

    public class Rethrow
    {
        /// <summary>
        /// Throw an exception catch it and Rethrow. Make sure we have two TryCatchs that only one of them Rethrow.
        /// </summary>        
        [Fact]
        public void ThrowCatchRethrow()
        {
            TestTryCatch ttc = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("this is expected uncaught exception")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>
                    {
                        Body = new TestRethrow
                        {
                            ExpectedOutcome = Outcome.UncaughtException(typeof(TAC.ApplicationException)),
                        }
                    }
                }
            };
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            TestRuntime.RunAndValidateAbortedException(ttc, typeof(TAC.ApplicationException), exceptionProperties);
        }

        /// <summary>
        /// Rethrow in a 4 level TryCatch. Make sure the exception thrown from level 4 (deepest level) can make be rethrown in level 1
        /// </summary>        
        [Fact]
        public void ThrowInNestedTryCatchRethrowInTheTopTryCatch()
        {
            TestTryCatch root = new TestTryCatch("parentTryCatch");
            root.Catches.Add(
                            new TestCatch<TAC.ApplicationException>
                            {
                                Body = new TestRethrow
                                {
                                    ExpectedOutcome = Outcome.UncaughtException(typeof(TAC.ApplicationException)),
                                }
                            });

            TestTryCatch level1 = new TestTryCatch("level1");
            level1.Catches.Add(new TestCatch<ArithmeticException> { Body = new TestProductWriteline("don't write this 1") });
            root.Try = level1;

            TestTryCatch level2 = new TestTryCatch("level2");
            level2.Catches.Add(new TestCatch<ArithmeticException> { Body = new TestProductWriteline("don't write this 2") });
            level1.Try = level2;

            TestTryCatch level3 = new TestTryCatch("level3");
            level3.Catches.Add(new TestCatch<ArithmeticException> { Body = new TestProductWriteline("don't write this 3") });
            level2.Try = level3;

            level3.Try = new TestSequence
            {
                Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("this is expected uncaught exception")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
            };

            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            TestRuntime.RunAndValidateAbortedException(root, typeof(TAC.ApplicationException), exceptionProperties);
        }

        /// <summary>
        /// Rethrow and catch
        /// </summary>        
        [Fact]
        public void RethrowAndCatch()
        {
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestTryCatch("parent TryCatch")
                {
                    Try = new TestSequence
                    {
                        Activities =
                        {
                            new TestProductWriteline("W1"),
                            new TestThrow<TAC.ApplicationException>
                            {
                                ExceptionExpression = (context => new TAC.ApplicationException("this is expected uncaught exception")),
                                ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
                            }
                        }
                    },
                    Catches =
                    {
                        new TestCatch<TAC.ApplicationException>
                        {
                            Body = new TestRethrow
                            {
                                ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
                            }
                        }
                    }
                },
                Catches =
                    {
                        new TestCatch<TAC.ApplicationException>
                        {
                            Body = new TestWriteLine
                            {
                                Message = "You catched the exception :)"
                            }
                        }
                    }
            };

            TestRuntime.RunAndValidateWorkflow(root);
        }

        /// <summary>
        /// Use Rethrow outside of trycatch (negative case)
        /// RethrowOutSideOfTryCatch
        /// </summary>        
        [Fact]
        public void RethrowOutSideOfTryCatch()
        {
            TestRethrow tr = new TestRethrow();
            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    tr,
                    new TestWriteLine

                    {
                        ExpectedOutcome = Outcome.None,
                        Message = "this should not run",
                        HintMessage = "nothing"
                    }
                }
            };

            TestRuntime.ValidateInstantiationException(seq, string.Format(ErrorStrings.RethrowNotInATryCatch, tr.DisplayName));
        }

        /// <summary>
        /// Use Rethrow in a non-immediate children of trycatch.
        /// </summary>        
        [Fact]
        public void RethrowInNonImediateChildOfTryCatch()
        {
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("this is expected uncaught exception")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>
                    {
                        Body = new TestSequence
                        {
                            Activities =
                            {
                                new TestSequence
                                {
                                    Activities =
                                    {
                                        new TestRethrow
                                        {
                                            ExpectedOutcome = Outcome.UncaughtException(typeof(TAC.ApplicationException)),
                                        }
                                    }
                                }
                            }
                        }
}
                }
            };
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();

            TestRuntime.RunAndValidateAbortedException(root, typeof(TAC.ApplicationException), exceptionProperties);
        }

        /// <summary>
        /// Use Rethrow in finally section of try catch (negative case)
        /// </summary>        
        [Fact]
        public void RethrowInFinally()
        {
            TestRethrow tr = new TestRethrow();
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("abcd")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>()
},
                Finally = new TestSequence
                {
                    Activities =
                    {
                        new TestSequence
                        {
                            Activities =
                            {
                                tr
                            }
                        }
                    }
                }
            };

            TestRuntime.ValidateInstantiationException(root, string.Format(ErrorStrings.RethrowNotInATryCatch, tr.DisplayName));
        }

        /// <summary>
        /// Use rethrow in try section of try catch (negative case)
        /// </summary>        
        [Fact]
        public void RethrowInTry()
        {
            TestRethrow tr = new TestRethrow();
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("abcd")),
                        },
                        tr
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>
                    {
                        Body = new TestRethrow()
                    }
                },
            };

            TestRuntime.ValidateInstantiationException(root, string.Format(ErrorStrings.RethrowNotInATryCatch, tr.DisplayName));
        }

        /// <summary>
        /// Persist after Catch before rethrow
        /// </summary>        
        [Fact]
        public void PersistAfterCatchBeforeRethrow()
        {
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("abcd")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>
                    {
                        Body = new TestSequence
                        {
                            Activities =
                            {
                                new TestBlockingActivity("Blocking1", "B1"),
                                new TestRethrow
                                {
                                    ExpectedOutcome = Outcome.UncaughtException(typeof(TAC.ApplicationException)),
                                }
                            }
                        }
                    }
                }
            };

            WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(root, null, jsonStore, System.Activities.PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("Blocking1", TestActivityInstanceState.Executing);
                testWorkflowRuntime.PersistWorkflow();
                testWorkflowRuntime.ResumeBookMark("Blocking1", null);

                testWorkflowRuntime.WaitForAborted(out Exception resultedException);

                if (!(resultedException is TAC.ApplicationException))
                    throw resultedException;
            }
        }

        /// <summary>
        /// Two consecutive Rethrow, the second should not execute
        /// </summary>        
        [Fact]
        public void TwoConsecutiveRethrow()
        {
            TestTryCatch root = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TAC.ApplicationException>
                        {
                            ExceptionExpression = (context => new TAC.ApplicationException("this is expected uncaught exception")),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TAC.ApplicationException)),
}
                    }
                },
                Catches =
                {
                    new TestCatch<TAC.ApplicationException>
                    {
                        Body = new TestSequence
                        {
                            Activities =
                            {
                                new TestRethrow
                                {
                                    ExpectedOutcome = Outcome.UncaughtException(typeof(TAC.ApplicationException)),
                                },
                                 new TestRethrow
                                {
                                    ExpectedOutcome = Outcome.None,
                                }
                            }
                        }
}
                }
            };

            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            TestRuntime.RunAndValidateAbortedException(root, typeof(TAC.ApplicationException), exceptionProperties);
        }

        /// <summary>
        /// Make sure Rethrow, rethrows exceptions that are not thrown by Throw activity and holds all the values of the original exception (inner exception, call stack ,etc.)
        /// </summary>        
        [Fact]
        public void RethrowExceptionFromInvokeMethodWithAllExceptionPropertiesSet()
        {
            TestInvokeMethod im = new TestInvokeMethod
            {
                TargetObject = new TestArgument<CustomClassForRethrow>(Direction.In, "TargetObject", (context => new CustomClassForRethrow())),
                MethodName = "M1",
                ExpectedOutcome = Outcome.CaughtException(typeof(TestCaseException)),
            };
            TestTryCatch tc = new TestTryCatch();
            TestCatch<TestCaseException> tcCatch = new TestCatch<TestCaseException>
            {
                Body = new TestRethrow
                {
                    ExpectedOutcome = Outcome.UncaughtException(typeof(TestCaseException))
                }
            };
            tc.Try = im;
            tc.Catches.Add(tcCatch);

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tc))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForAborted(out Exception outEx);
                Dictionary<string, string> errorProperty = new Dictionary<string, string>();
                errorProperty.Add("Message", "this should be caught");
                ExceptionHelpers.ValidateException(outEx, typeof(TestCaseException), errorProperty);
            }
        }

        /// <summary>
        /// Rethrow from a private child activity of the catch handler:Try    ThrowCatch   UserHandleExceptionActivity        UserPrivateChildActivity            Rethrow
        /// </summary>        
        [Fact]
        public void RethrowFromCatchHandlerOfPrivateActivity()
        {
            string message = "this is expected uncaught exception";
            TestCustomActivity<TestRethrowInPrivateChildren> rethrowAct = new TestCustomActivity<TestRethrowInPrivateChildren>()
            {
                ExpectedOutcome = Outcome.UncaughtException(),
            };
            rethrowAct.CustomActivityTraces.Add(new ActivityTrace("Rethrow", System.Activities.ActivityInstanceState.Executing));
            rethrowAct.CustomActivityTraces.Add(new ActivityTrace("Rethrow", System.Activities.ActivityInstanceState.Faulted));

            TestTryCatch ttc = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<TestCaseException>
                        {
                            ExceptionExpression = (context => new TestCaseException(message)),
                            ExpectedOutcome = Outcome.CaughtException(typeof(TestCaseException)),
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<TestCaseException>
                    {
                        Body = rethrowAct
                    }
                }
            };

            //The validation error has a prefix:
            string validationError = string.Format(ErrorStrings.ValidationErrorPrefixForHiddenActivity, "2: " + rethrowAct.DisplayName)
                                     +
                                     string.Format(ErrorStrings.RethrowMustBeAPublicChild, "Rethrow");

            TestRuntime.ValidateInstantiationException(ttc, validationError);
        }

        /// <summary>
        /// Rethrow custom exception with non-serializable property.
        /// </summary>        
        [Fact]
        public void RethrowCustomExceptionWithNonSerializableProperty()
        {
            //TestParameters.DisableXamlRoundTrip = true;
            string message = "this is expected uncaught exception";
            TestTryCatch ttc = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<CustomExceptionWithNonSerializableProperty>
                        {
                            ExceptionExpression = context => new CustomExceptionWithNonSerializableProperty(message){ NonSerializableP= new NonSerializableType("A")},
                            ExpectedOutcome = Outcome.CaughtException(typeof(CustomExceptionWithNonSerializableProperty)),
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<CustomExceptionWithNonSerializableProperty>
                    {
                        Body = new TestRethrow
                        {
                            ExpectedOutcome = Outcome.UncaughtException(typeof(CustomExceptionWithNonSerializableProperty)),
                        }
                    }
                }
            };
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            TestRuntime.RunAndValidateAbortedException(ttc, typeof(CustomExceptionWithNonSerializableProperty), exceptionProperties);
        }

        /// <summary>
        /// Throw a custom inherited exception. Have catch for both the child and parent and Rethrow. Make sure the rethrown exception is the right one (parent/child) and properties are set.
        /// </summary>        
        [Fact]
        public void CustomParentChildInheritedExceptionCatchAndRethrow()
        {
            TestTryCatch ttc = new TestTryCatch("parent TryCatch")
            {
                Try = new TestSequence
                {
                    Activities =
                    {
                        new TestProductWriteline("W1"),
                        new TestThrow<CustomException>
                        {
                            ExceptionExpression = context => new CustomException("this is expected uncaught exception"),
                            ExpectedOutcome = Outcome.CaughtException(typeof(CustomException)),
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<CustomException>
                    {
                        Body = new TestSequence("seq1 in Catch-TAC.ApplicationException-")
                        {
                            Activities =
                            {
                                new TestWriteLine
                                {
                                    Message = "This should be printed",
                                    HintMessage = "This should be printed",
},
                                new TestRethrow
                                {
                                    DisplayName = "LastRethrow",
                                    ExpectedOutcome = Outcome.UncaughtException(typeof(CustomException)),
                                }
                            }
                        }
                    },
                    new TestCatch<Exception>
                    {
                        Body = new TestSequence("seq1 in Catch-Exception-")
                        {
                            ExpectedOutcome = Outcome.None,
                            Activities =
                            {
                                new TestWriteLine
                                {
                                    Message = "This should not be printed",
                                },
                                new TestRethrow
                                {
                                    DisplayName = "FirstRethrow",
                                }
                            }
                        }
                    },
                }
            };
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            TestRuntime.RunAndValidateAbortedException(ttc, typeof(CustomException), exceptionProperties);
        }
    }

    internal class TestRethrowInPrivateChildren : System.Activities.NativeActivity
    {
        private readonly System.Activities.Statements.Rethrow _body = new System.Activities.Statements.Rethrow() { DisplayName = "Rethrow" };
        protected override void CacheMetadata(System.Activities.NativeActivityMetadata metadata)
        {
            metadata.AddImplementationChild(_body);
        }

        protected override void Execute(System.Activities.NativeActivityContext context)
        {
            context.ScheduleActivity(_body);
        }
    }

    public class CustomClassForRethrow
    {
        private void P1()
        {
            throw new TestCaseException("this should be caught");
        }
        private void P2()
        {
            P1();
        }

        public void M1()
        {
            P2();
        }
    }

    public class CustomExceptionWithNonSerializableProperty : Exception
    {
        public CustomExceptionWithNonSerializableProperty()
            : base()
        {
        }

        public CustomExceptionWithNonSerializableProperty(string message)
            : base(message)
        {
        }

        public NonSerializableType NonSerializableP
        {
            get;
            set;
        }
    }

    public class NonSerializableType
    {
        public NonSerializableType(string input)
        {
            P1 = input;
        }
        public string P1 { get; set; }
    }
}

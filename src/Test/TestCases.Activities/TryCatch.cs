// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities
{
    public class CustomUtility
    {
        public static string CustomMessage = "This is a custom Message.";
    }

    public class TryCatch
    {
        #region Empty tcf

        /// <summary>
        /// Empty tcf, run in workflow.
        /// </summary>        
        [Fact]
        public void EmptyAll()
        {
            try
            {
                // run workflow
                TestRuntime.RunAndValidateAbortedException(
                    CreateTryCatchFinally(null, null, null, WFType.SEQ, false),
                    typeof(InvalidWorkflowException), null);
                throw new Exception("This should be validated out");
            }
            catch (InvalidWorkflowException)
            {
            }
            catch (Exception)
            {
                throw new Exception("Wrong exception thrown");
            }
        }

        /// <summary>
        /// tcf where Try is empty but has catches and finally.
        /// </summary>        
        [Fact]
        public void EmptyTry()
        {
            // catch
            TestCatch<Exception> testcatch1 = new TestCatch<Exception>();
            TestCatch<MemberAccessException> testcatch2 = new TestCatch<MemberAccessException>();
            TestCatch[] catches = new TestCatch[] { testcatch1, testcatch2 };

            // finally
            TestSequence finalSeq = new TestSequence();
            finalSeq.Activities.Add(new TestWriteLine("Final", "Final"));

            // Run test
            TestRuntime.RunAndValidateWorkflow(
                CreateTryCatchFinally(null, catches, finalSeq, WFType.SEQ, false));
        }

        /// <summary>
        /// Validate that Catch and Finally must not bt Null
        /// </summary>        
        [Fact]
        public void EmptyCatchFinally()
        {
            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestThrow<ArgumentException>());

            try
            {
                // run workflow
                TestRuntime.RunAndValidateAbortedException(
                    (CreateTryCatchFinally(trySeq, null, null, WFType.SEQ, false)),
                    typeof(InvalidWorkflowException), null);
            }
            catch (InvalidWorkflowException)
            {
            }
            catch (Exception)
            {
                throw new Exception("Wrong exception thrown");
            }
        }

        #endregion

        #region Execute t/c/f activity

        /// <summary>
        /// Simple tcf scenario, no surrounding workflow elements. Exception is thrown then caught.
        /// </summary>        
        [Fact]
        public void TryCatchFinallyActivityOnly()
        {
            TestTryCatch tcf = new TestTryCatch
            {

                // try
                Try = new TestThrow<ArgumentException>()
                {
                    ExpectedOutcome = Outcome.CaughtException()
                }
            };

            // catch
            TestCatch<ArgumentException> tc = new TestCatch<ArgumentException>
            {
                Body = new TestWriteLine("Hello world!", "Hello world!")
            };
            tcf.Catches.Add(tc);

            // finally
            tcf.Finally = new TestWriteLine("Finally", "Finally");

            // Run test
            TestRuntime.RunAndValidateWorkflow(tcf);
        }

        #endregion

        #region Simple TCF

        /// <summary>
        /// Simple tcf scenario in a sequential wf. Exception is thrown then caught.
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void SimpleTryCatchSequential()
        {
            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestWriteLine("Try", "Try"));
            trySeq.Activities.Add(new TestThrow<ArgumentException>());

            // catch
            TestCatch[] catches = new TestCatch[] {
                new TestCatch<ArgumentException>()
                {
                    HintHandleException = true,
                    Body = new TestWriteLine("Catch", "Catch")
                } };

            // finally
            TestSequence finalSeq = new TestSequence();
            finalSeq.Activities.Add(new TestWriteLine("Final", "Final"));

            // Run test
            TestRuntime.RunAndValidateWorkflow
                    (CreateTryCatchFinally(trySeq, catches, finalSeq, WFType.SEQ, false));
        }

        /// <summary>
        /// Simple tcf scenario in a flowchart wf. Exception is thrown then caught.
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void SimpleTryCatchFlowchart()
        {
            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestWriteLine("Try", "Try"));
            trySeq.Activities.Add(new TestThrow<ArgumentException>());
            trySeq.Activities.Add(new TestWriteLine("NotCalled", "NotCalled"));

            // catch
            TestCatch[] catches = new TestCatch[] {
                new TestCatch<ArgumentException>() };
            catches[0].HintHandleException = true;
            catches[0].Body = new TestWriteLine("Catch", "Catch");

            // finally
            TestSequence finalSeq = new TestSequence();
            finalSeq.Activities.Add(new TestWriteLine("Final", "Final"));

            TestRuntime.RunAndValidateWorkflow
                    (CreateTryCatchFinally(trySeq, catches, finalSeq, WFType.FLOW, false));
        }

        #endregion

        #region Inherited Exceptions

        /// <summary>
        /// Exception is thrown in try and not caught.
        /// Exception is thrown in try and not caught. Catch contains a child exception of thrown exception.
        /// </summary>        
        [Fact]
        public void InheritedExceptions1()
        {
            // exception which is raised
            Exception exc = new Exception();

            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestWriteLine("Try", "Try"));
            TestThrow<Exception> tt = new TestThrow<Exception>("TestThrow1")
            {
                ExceptionExpression = (context => new Exception())
            };
            trySeq.Activities.Add(tt);

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<ArgumentException>() };

            // create and run
            TestActivity act = CreateTryCatchFinally(trySeq, catches, null, WFType.SEQ, true);
            TestRuntime.RunAndValidateAbortedException(act, exc.GetType(),
                new Dictionary<string, string> { { "Message", exc.Message } });
        }

        /// <summary>
        /// Exception is thrown in try and caught.
        /// Exception is thrown in try and caught. Catch contains a base of thrown child exception.
        /// </summary>
        /// Disabled and failed in desktop        
        //[Fact]
        private void InheritedExceptions2()
        {
            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestWriteLine("Try", "Try"));
            trySeq.Activities.Add(new TestThrow<ArgumentException>("TestThrow1"));

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<Exception>() };
            catches[0].HintHandleException = true;

            // create and run
            TestActivity act = CreateTryCatchFinally(trySeq, catches, null, WFType.SEQ, true);
            TestRuntime.RunAndValidateWorkflow(act);
        }

        /// <summary>
        /// Exception is thrown in try and caught.
        /// Exception is thrown in try and caught. Catch contains a base and child exception.
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void InheritedExceptions3()
        {
            // try
            TestSequence trySeq = new TestSequence();
            trySeq.Activities.Add(new TestWriteLine("Try", "Try"));
            trySeq.Activities.Add(new TestThrow<ArgumentException>("TestThrow1"));

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<Exception>(),
                                                    new TestCatch<ArgumentException>()};
            catches[1].HintHandleException = true;

            // create and run
            TestActivity act = CreateTryCatchFinally(trySeq, catches, null, WFType.SEQ, true);
            TestRuntime.RunAndValidateWorkflow(act);
        }

        #endregion

        #region Uncaught Exception

        /// <summary>
        /// Exception is thrown in sequence WF and not caught.
        /// </summary>        
        [Fact]
        public void UncaughtExceptionSequence()
        {
            // exception which is raised
            ArithmeticException exc = new ArithmeticException();

            // try
            TestThrow<ArithmeticException> tt = new TestThrow<ArithmeticException>("Test Throw")
            {
                ExceptionExpression = (context => new ArithmeticException())
            };

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<FileNotFoundException>(),
                                                    new TestCatch<ArgumentException>(),
                                                    new TestCatch<ArgumentOutOfRangeException>()};

            // finally
            TestSequence finallySeq = new TestSequence("Finally");

            // create and run
            TestActivity act = CreateTryCatchFinally(tt, catches, finallySeq, WFType.SEQ, true);
            TestRuntime.RunAndValidateAbortedException(act, exc.GetType(), null);
        }

        /// <summary>
        /// Exception is thrown in Flowchart WF and not caught.
        /// </summary>        
        [Fact]
        public void UncaughtExceptionFlowchart()
        {
            // exception which is raised
            ArithmeticException exc = new ArithmeticException();

            // try
            TestThrow<ArithmeticException> tt = new TestThrow<ArithmeticException>("Test Throw")
            {
                ExceptionExpression = (context => new ArithmeticException())
            };

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<FileNotFoundException>(),
                                                    new TestCatch<ArgumentException>(),
                                                    new TestCatch<ArgumentOutOfRangeException>()};

            // finally
            TestSequence finallySeq = new TestSequence("Finally");

            // create and run
            TestActivity act = CreateTryCatchFinally(tt, catches, finallySeq, WFType.FLOW, true);
            TestRuntime.RunAndValidateAbortedException(act, exc.GetType(), null);
        }

        #endregion

        #region TryCatchFinallyWtihExceptionInUncatchingCatch

        /// <summary>
        /// TryCatchfinally that throws from the catch that doesn’t catch
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void TryCatchFinallyWithExceptionInUncatchingCatch()
        {
            // try
            TestSequence trySeq = new TestSequence("Try");
            trySeq.Activities.Add(new TestThrow<ArgumentException>("ThrowFromInner"));

            // catch
            TestCatch[] catches = new TestCatch[] {
                new TestCatch<ArgumentException>()
                {
                    HintHandleException = true,
                    Body = new TestWriteLine("Catch", "Catch")
                },
                new TestCatch<UnauthorizedAccessException>()
                {
                    HintHandleException = false,
                    Body = new TestThrow<UnauthorizedAccessException>("Throw from uncalled catch")
                }
            };

            // finally
            TestWriteLine finalWrite = new TestWriteLine("Final", "Final");

            // Run test
            TestRuntime.RunAndValidateWorkflow
                    (CreateTryCatchFinally(trySeq, catches, finalWrite, WFType.SEQ, false));
        }


        #endregion

        #region TryCatchFinallyWithExceptionInFinally

        /// <summary>
        /// TryCatchFinally that throws from the finally
        /// </summary>        
        [Fact]
        public void TryCatchFinallyWithExceptionInFinally()
        {
            // Uncaught Exception
            ArithmeticException exc = new ArithmeticException();

            // finally
            TestThrow<ArithmeticException> tt = new TestThrow<ArithmeticException>("Test Throw")
            {
                ExceptionExpression = (context => new ArithmeticException())
            };

            // Run test
            TestActivity act = CreateTryCatchFinally(null, null, tt, WFType.SEQ, false);
            TestRuntime.RunAndValidateAbortedException(act, exc.GetType(), null);
        }

        #endregion

        #region TryCatchFinallyWithCaughtExceptionInFinally

        /// <summary>
        /// TryCatchFinally that throws from the finally, can catches it in a catch in an outer tcf
        /// </summary>        
        [Fact]
        public void TryCatchFinallyWithCaughtExceptionInFinally()
        {
            TestTryCatch outer = new TestTryCatch("Outer TCF")
            {
                Try = new TestTryCatch("Inner TCF")
                {
                    Finally = new TestSequence("Finally")
                    {
                        Activities =
                        {
                            new TestThrow<Exception>()
                            {
                                ExpectedOutcome = Outcome.CaughtException()
                            },
                            new TestWriteLine("Not called", "Not printed"),
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<Exception>()
                    {
                        Body = new TestWriteLine("Catch", "Catch"),
                    }
                }
            };

            // run the workflow
            TestRuntime.RunAndValidateWorkflow(outer);
        }

        #endregion

        #region CatchBothUnhandledExceptionHandlerAndInCatch

        public static string CustomMessage = "This is a custom Message.";

        /// <summary>
        /// Throw exception from one activity within try, catch it in the unhandled exception handler
        /// Throw exception from one activity within try, catch it in the unhandled exception handler then continue execution and throw another exception in the next activity and catch it in the catchlist
        /// </summary>        
        [Fact]
        public void CatchBothUnhandledExceptionHandlerAndInCatch()
        {
            // try
            // Throws a handled exception, then a caught exception
            TestSequence trySeq = new TestSequence("Try")
            {
                Activities =
                {
                    new TestThrow<FormatException>("ThrowFormat")
                    {
                        ExceptionExpression = (context => new FormatException(CustomUtility.CustomMessage)),
                        ExpectedOutcome = Outcome.HandledException(),
                    },
                },

                ExpectedOutcome = Outcome.Canceled
            };

            // catch
            // Should not catch anything
            TestCatch[] catches = new TestCatch[]
            {
                new TestCatch<ArgumentException>()
                {
                }
            };

            // finally
            // Just confirm it executed
            TestWriteLine finalSeq = new TestWriteLine("Final", "Final");

            // Run test
            TestActivity act = CreateTryCatchFinally(trySeq, catches, finalSeq, WFType.SEQ, false);


            // Run and validate trace
            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(act))
            {
                // Add the unhandled handler
                runtime.WorkflowRuntimeAdapterType = typeof(AddHandleExceptionRuntimeAdapter);
                runtime.ExecuteWorkflow();
                runtime.GetWatcher().WaitForWorkflowCanceled();
            }
        }

        /// <summary>
        /// This allows us to modify the unhandled exception handler
        /// </summary>
        public class AddHandleExceptionRuntimeAdapter : IWorkflowRuntimeAdapter
        {
            public void OnInstanceCreate(WorkflowApplication workflowInstance)
            {
                workflowInstance.OnUnhandledException =
                    delegate (WorkflowApplicationUnhandledExceptionEventArgs e)
                    {
                        if (e.UnhandledException.GetType() == typeof(FormatException) &&
                            e.UnhandledException.Message.Equals(CustomUtility.CustomMessage))
                        {
                            return UnhandledExceptionAction.Cancel;
                        }

                        return UnhandledExceptionAction.Abort;
                    };
            }

            public void OnInstanceLoad(WorkflowApplication workflowInstance) { }
        }


        #endregion

        #region Helper

        /// <summary>
        /// Different types of workflows.
        /// </summary>
        public enum WFType { FLOW, SEQ };

        /// <summary>
        /// Setup a t/c/f statement.
        /// </summary>
        /// <param name="tryBody">Try activity.</param>
        /// <param name="tc">Array of Catches.</param>
        /// <param name="finallyBody">Finally activity.</param>
        /// <param name="type">Type of workflow to create.</param>
        /// <param name="otherActivities">Flag indicating whether extra activities should be added.</param>
        public static TestActivity CreateTryCatchFinally(TestActivity tryBody, TestCatch[] tc,
                                            TestActivity finallyBody, WFType type, bool otherActivities)
        {
            // create the try/catch/finally
            TestTryCatch tcf = new TestTryCatch("TestTcf");
            if (tryBody != null)
            {
                tcf.Try = tryBody;
            }
            if (tc != null)
            {
                foreach (TestCatch testCatch in tc)
                {
                    tcf.Catches.Add(testCatch);
                }
            }
            if (finallyBody != null)
            {
                tcf.Finally = finallyBody;
            }

            // extra activities to add around activity if otherActivities is true
            TestWriteLine before = new TestWriteLine("BeforeTry", "BeforeTry");
            TestWriteLine after = new TestWriteLine("AfterTry", "AfterTry");

            // sequence
            if (type == WFType.SEQ)
            {
                TestSequence seq = new TestSequence("SequenceOfActivitiesContainingTCF");
                if (otherActivities)
                {
                    seq.Activities.Add(before);
                }
                seq.Activities.Add(tcf);
                if (otherActivities)
                {
                    seq.Activities.Add(after);
                }
                return seq;
            }

            // otherwise do flowchart
            else // type == wfType.FLOW
            {
                TestFlowchart flowchart = new TestFlowchart("FlowchartContainingTCF");
                if (otherActivities)
                {
                    flowchart.AddStartLink(before);
                    flowchart.AddLink(before, tcf);
                    flowchart.AddLink(tcf, after);
                }
                else
                {
                    flowchart.AddStartLink(tcf);
                }
                return flowchart;
            }
        }

        #endregion helpers

        /// <summary>
        /// Simple tcf scenario in a parallel. Where the branches have mismatching exceptions
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void TryCatchInParallelWithMissMatchingExceptions()
        {
            TestParallel parallel = new TestParallel("ParallelCatch")
            {
                CompletionCondition = true,
                Branches =
                {
                    new TestTryCatch("TryCatchBranch1")
                    {
                        Try = new TestThrow<ArgumentException>("ThrowingArgumentException")
                        {
                            ExpectedOutcome = Outcome.UncaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<ArithmeticException>()
                            {
                                Body = new TestWriteLine("CatchingArithmeticException", "Catching ArithmeticException")
                            }
                        },
                    },
                    new TestTryCatch("TryCatchBranch2")
                    {
                        Try = new TestThrow<ArithmeticException>("ThrowingArithmeticException")
                        {
                            ExpectedOutcome = Outcome.None,
                        },
                        Catches =
                        {
                            new TestCatch<ArgumentException>()
                            {
                                ExpectedOutcome = Outcome.None,
                                Body = new TestWriteLine("CatchingArgumentException", "Catching ArgumentException")
                                {
                                    ExpectedOutcome = Outcome.None,
                                }
                            }
                        },
                    }
                },
            };

            TestRuntime.RunAndValidateAbortedException(parallel, typeof(ArgumentException), null);
        }

        /// <summary>
        /// Simple tcf scenario in a parallel.
        /// Try catch in parlallel branch handles exception
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void TryCatchInParallel()
        {
            TestParallel parallel = new TestParallel("ParallelCatch")
            {
                Branches =
                {
                    new TestTryCatch("TryCatchBranch1")
                    {
                        Try = new TestThrow<ArgumentException>("Throwing ArgumentException")
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<ArgumentException>()
                            {
                                Body = new TestWriteLine("Catching TestCaseException", "Catching TestCaseException")
                            }
                        },
                    },
                    new TestTryCatch("TryCatchBranch2")
                    {
                        Try = new TestThrow<ArithmeticException>("Throwing ArithmeticException")
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        Catches =
                        {
                            new TestCatch<ArithmeticException>()
                            {
                                Body = new TestWriteLine("Catching ArithmeticException", "Catching ArithmeticException")
                            }
                        },
                    }
                },
            };

            TestRuntime.RunAndValidateWorkflow(parallel);
        }

        /// <summary>
        /// Parallel in TryCatch and handled by trycatch. Any executing branch will be cancelled. ( parallel within parallel and all branches are blocking at the same time so that you see the cancel behavior)
        /// Parallel in TryCatch and handled by trycatch . Any executing branch will be cancelled. ( parallel within parallel and all branches are blocking at the same time so that you see the cancel behavior)
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void ParallelInTryCatch()
        {
            TestTryCatch tryCatch = new TestTryCatch("TryCatch")
            {
                Try = new TestParallel("TryCatchParallel")
                {
                    Branches =
                    {
                        new TestWriteLine("WritingFirst", "WritingFirst"),
                        new TestThrow<ArgumentException>("Throwing")
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                        new TestWriteLine("AfterThrow", "AfterThrow")
                        {
                            ExpectedOutcome = Outcome.Canceled
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("Catching ArgumentException", "Catching ArgumentException")
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(tryCatch);
        }

        /// <summary>
        /// Trycatch that also throws from the catch that catches exception
        /// Trycatch finally that also throws from the catfch that catches exception
        /// </summary>        
        [Fact]
        public void TryCatchFinallyWithExceptionInCatchingCatch()
        {
            TestTryCatch tryCatch = new TestTryCatch("TryCatchTest")
            {
                Try = new TestSequence("TrySeq")
                {
                    Activities =
                    {
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                    },
                },

                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestThrow<ArithmeticException>("Throw from catch")
                        {
                            ExceptionExpression = (context => new ArithmeticException())
                        }
                    },
                },
                Finally = new TestWriteLine("Finally", "Finally")
            };

            // Run test
            TestRuntime.RunAndValidateAbortedException(tryCatch, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Throw exception from one catch and catch it in the other catch
        /// </summary>        
        [Fact]
        public void ThrowExceptionInOneCatchAndCatchItInOtherCatch()
        {
            TestTryCatch tryCatch = new TestTryCatch("TryCatchTestParent")
            {
                Try = new TestTryCatch("TryCatchTestChild")
                {
                    Try = new TestSequence("TrySeq")
                    {
                        Activities =
                        {
                            new TestThrow<ArgumentException>("TryException")
                            {
                                ExpectedOutcome = Outcome.CaughtException()
                            },
                        },
                    },

                    Catches =
                    {
                        new TestCatch<ArgumentException>()
                        {
                            Body = new TestThrow<ArithmeticException>("Throw from catch")
                            {
                                ExceptionExpression = (context => new ArithmeticException()),
                                ExpectedOutcome = Outcome.CaughtException()
                            }
                        },
                    },
                    Finally = new TestWriteLine("Finally", "Finally")
                },
                Catches =
                {
                    new TestCatch<ArithmeticException>()
                    {
                        Body = new TestWriteLine("CaughtIt", "I caught you")
                    },
                }
            };

            // Run test
            TestRuntime.RunAndValidateWorkflow(tryCatch);
        }

        /// <summary>
        /// Nest trycatchfinallies 5 levels deep and catch uncaught exception in outer try catch
        /// Nest trycatchfinallies 5 levels deep and catch uncatched exception in outer try catch…etc
        /// </summary>        
        /// Disabled and failed in desktop        
        //[Fact]
        private void TryCatchFinallyNested()
        {
            TestTryCatch NestedTryCatch = new TestTryCatch("Level1Try")
            {
                Try = new TestTryCatch("Level2Try")
                {
                    Try = new TestTryCatch("Level3Try")
                    {
                        Try = new TestTryCatch("Level4Try")
                        {
                            Try = new TestTryCatch("Level5Try")
                            {
                                Try = new TestThrow<ArithmeticException>("Level5Throw") { ExpectedOutcome = Outcome.CaughtException() },
                                Catches =
                                {
                                    new TestCatch<ArgumentOutOfRangeException>()
                                    {
                                        Body = new TestWriteLine("Not Catching"),
                                    }
                                }
                            },
                            Catches =
                            {
                                new TestCatch<ArithmeticException>()
                                {
                                    Body = new TestThrow<MemberAccessException>("Level4Throw"){ ExpectedOutcome = Outcome.CaughtException()}
                                }
                            }
                        },
                        Catches =
                        {
                            new TestCatch<MemberAccessException>()
                            {
                                Body = new TestThrow<FileNotFoundException>("Level3Throw"){ ExpectedOutcome = Outcome.CaughtException()}
                            }
                        }
                    },
                    Catches =
                    {
                        new TestCatch<FileNotFoundException>()
                        {
                            Body = new TestThrow<ArgumentException>("Level2Throw"){ ExpectedOutcome = Outcome.CaughtException()}
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("No More Catching!", "No More Catching!")
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(NestedTryCatch);
        }

        /// <summary>
        /// TryCatchFinally catching custom exceptions
        /// </summary>        
        [Fact]
        public void TryCatchFinallyCustomException()
        {
            // try
            TestThrow<CustomException> custromTry = new TestThrow<CustomException>("custom exception")
            {
                ExceptionExpression = (context => new CustomException("Invalid department")),
                ExpectedOutcome = Outcome.CaughtException()
            };

            // catch
            TestCatch[] catches = new TestCatch[] { new TestCatch<CustomException>() };

            // finally
            TestWriteLine finallyWrite = new TestWriteLine("FinallyCatchingCustomException", "FinallyCatchingCustomException");

            // create and run
            TestActivity act = CreateTryCatchFinally(custromTry, catches, finallyWrite, WFType.SEQ, true);

            TestRuntime.RunAndValidateWorkflow(act);
        }

        /// <summary>
        /// Invoke a TryCatch activity with the WorkflowInvoker
        /// </summary>        
        [Fact]
        public void TryCatchWithWorkflowInvoker()
        {
            TestTryCatch tcf = new TestTryCatch
            {

                // try
                Try = new TestThrow<ArgumentException>()
                {
                    ExpectedOutcome = Outcome.CaughtException()
                }
            };

            // catch
            TestCatch<ArgumentException> tc = new TestCatch<ArgumentException>
            {
                Body = new TestWriteLine("Hello world!", "Hello world!")
            };
            tcf.Catches.Add(tc);

            // finally
            tcf.Finally = new TestWriteLine("Finally", "Finally");

            // Run test
            TestRuntime.RunAndValidateUsingWorkflowInvoker(tcf, null, null, null);
        }

        /// <summary>
        /// Persist in the catch of the trycatchfinally
        /// </summary>        
        [Fact]
        public void PersistInCatch()
        {
            TestBlockingActivity blocking = new TestBlockingActivity("Bookmark");
            TestTryCatch tcf = new TestTryCatch
            {
                Try = new TestThrow<Exception>() { ExpectedOutcome = Outcome.CaughtException() },
                Catches = { { new TestCatch<Exception> { Body = blocking } } }
            };

            WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tcf, null, jsonStore, PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.PersistWorkflow();

                testWorkflowRuntime.ResumeBookMark("Bookmark", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Persist in the try block of the trycatchfinally
        /// Persist in the try of the trycatchfinally
        /// </summary>        
        [Fact]
        public void PersistInTry()
        {
            TestBlockingActivity blocking = new TestBlockingActivity("Bookmark");

            TestTryCatch tryCatch = new TestTryCatch("TryCatchTest")
            {
                Try = new TestSequence("TrySeq")
                {
                    Activities =
                    {
                        blocking,
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.CaughtException()
                        },
                    },
                },

                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("Caught", "Caught")
                    }
                },
                Finally = new TestWriteLine("Finally", "Finally")
            };

            WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tryCatch, null, jsonStore, PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.PersistWorkflow();

                testWorkflowRuntime.ResumeBookMark("Bookmark", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Persist in the finally of the trycatchfinally
        /// </summary>        
        [Fact]
        public void PersistInFinally()
        {
            TestBlockingActivity blocking = new TestBlockingActivity("Bookmark");
            TestTryCatch tcf = new TestTryCatch
            {
                Try = new TestThrow<ArgumentException>() { ExpectedOutcome = Outcome.CaughtException() },
                Catches = { { new TestCatch<ArgumentException>() { Body = new TestWriteLine("Caught", "Caught") } } },
                Finally = new TestSequence { Activities = { blocking, new TestWriteLine("Finally", "Finally") } }
            };

            WorkflowApplicationTestExtensions.Persistence.FileInstanceStore jsonStore = new WorkflowApplicationTestExtensions.Persistence.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tcf, null, jsonStore, PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.PersistWorkflow();

                testWorkflowRuntime.ResumeBookMark("Bookmark", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Put TryCatchFinally in loops and at every iteration throw and catch.
        /// Put TryCatchFinally in loops and at every iteration throw and catch a different exception.
        /// </summary>        
        [Fact]
        public void TryCatchFinallyInLoops()
        {
            Variable<int> count = new Variable<int>("Counter", 0);
            TestWhile whileAct = new TestWhile
            {
                Variables = { count },
                ConditionExpression = e => count.Get(e) < 5,
                Body = new TestSequence
                {
                    Activities =
                    {
                        new TestTryCatch
                        {
                            Try = new TestThrow<ArgumentException>() { ExpectedOutcome = Outcome.CaughtException() },
                            Catches = { { new TestCatch<ArgumentException>() { Body = new TestWriteLine("Caught", "Caught") } } },
                            Finally = new TestSequence { Activities = { new TestWriteLine("Finally", "Finally") } }
                        },
                        new TestIncrement
                        {
                            CounterVariable = count,
                            IncrementCount = 1
                        }
                    }
                },
                HintIterationCount = 5
            };

            TestRuntime.RunAndValidateWorkflow(whileAct);
        }
        /// <summary>
        /// Cancel tryCatchFinally while executing try block.
        /// </summary>        
        [Fact]
        public void CancelInTryBlock()
        {
            TestBlockingActivity blocking = new TestBlockingActivity("Bookmark") { ExpectedOutcome = Outcome.Canceled };

            TestTryCatch tryCatch = new TestTryCatch("TryCatchTest")
            {
                Try = new TestSequence("TrySeq")
                {
                    Activities =
                    {
                        blocking,
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.None
                        },
                    },
                },

                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("Caught", "Caught"),
                        ExpectedOutcome = Outcome.None
                    }
                }
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tryCatch))
            {
                testWorkflowRuntime.ExecuteWorkflow();

                testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);

                testWorkflowRuntime.CancelWorkflow();

                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Catch has empty handler.
        /// </summary>        
        [Fact]
        public void CatchWithEmptyHandler()
        {
            TestTryCatch tcf = new TestTryCatch
            {

                // try
                Try = new TestThrow<IOException>()
                {
                    ExpectedOutcome = Outcome.CaughtException()
                }
            };

            // catch
            TestCatch<IOException> tc = new TestCatch<IOException>();
            // do not add to tc.Body, want empty Action.Handler
            tcf.Catches.Add(tc);

            // Run test
            TestRuntime.RunAndValidateWorkflow(tcf);
        }
    }

    internal class CustomException : Exception
    {
        public CustomException()
            : base()
        {
        }

        public CustomException(string message)
            : base(message)
        {
        }

        public string Name
        {
            get;
            set;
        }
    }
}

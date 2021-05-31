// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Threading;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Tracking;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common;
using Xunit;
using TAC = TestCases.Activities.Common;

namespace TestCases.Activities
{
    public class TerminateWorkflowTests
    {
        private static Type s_exceptionType;
        private static string s_exceptionMsg;
        private static string s_terminationReason;

        #region Helper
        private static void workflowInstance_Completed(object sender, TestWorkflowCompletedEventArgs e)
        {
            Exception innerException = e.EventArgs.TerminationException.InnerException;

            // Checking for Inner Exception
            if (innerException != null)
            {
                if (innerException.GetType() != s_exceptionType)
                {
                    throw new TestCaseFailedException("Exception is Incorrect");
                }

                string exceptionMessage = innerException.Message;
                if (!exceptionMessage.Equals(s_exceptionMsg))
                {
                    throw new TestCaseFailedException("Exception Message is Incorrect");
                }
            }

            // Checking for Reason
            string reason = e.EventArgs.TerminationException.Message;
            if (!reason.Equals(s_terminationReason))
            {
                throw new TestCaseFailedException("Reason Message is Incorrect");
            }
        }

        private static void workflowCompletedNoExceptions(object sender, TestWorkflowCompletedEventArgs e)
        {
            if (e.EventArgs.TerminationException != null)
            {
                throw new TestCaseFailedException("Workflow was not terminated by TerminateWorkflow, Exception should not exist.");
            }
        }

        private static void VerifyUnsetValue(object sender, TestWorkflowCompletedEventArgs e)
        {
            Exception terminationException = e.EventArgs.TerminationException;
            string reason = terminationException.Message;

            // Exception Not Set & Reason Set - We Expect:
            // 1. TerminationException = WorkflowTerminatedException
            // 2. TerminationException.InnerException = null
            // 3. TerminationException.Message = TerminationReason.
            if ((s_exceptionType == null) && (s_exceptionMsg == null))
            {
                if (terminationException.GetType() != typeof(WorkflowTerminatedException))
                {
                    throw new TestCaseFailedException("Reason is not set, TerminationException should match ExceptionType");
                }
                if (!terminationException.Message.Equals(s_terminationReason))
                {
                    throw new TestCaseFailedException("Exception is not set, InnerException should be null!");
                }
            }
            // Exception Set & Reason Not Set - We Expect:
            // 1. TerminationException = ExceptionType
            // 2. TerminationException.InnerException = null
            // 3. TerminationException.Message = ExceptionMsg.
            else if (s_terminationReason == null)
            {
                if (terminationException.GetType() != s_exceptionType)
                {
                    throw new TestCaseFailedException("Reason is not set, TerminationException should match ExceptionType");
                }
                if (terminationException.InnerException != null)
                {
                    throw new TestCaseFailedException("Reason is not set, InnerException should be null!");
                }
                if (!reason.Equals(s_exceptionMsg))
                {
                    throw new TestCaseFailedException("Reason is not set, Message should match ExceptionType's Message!");
                }
            }
        }

        private void RunTestWithWorkflowRuntime(TestActivity activity)
        {
            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(activity))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(workflowInstance_Completed);
                testWorkflowRuntime.ExecuteWorkflow();

                WorkflowTrackingWatcher watcher = testWorkflowRuntime.GetWatcher();
                OrderedTraces orderedExpectedTrace = new OrderedTraces
                {
                    Steps =
                    {
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Started),
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Terminated),
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Deleted)
                        {
                            Optional = true
                        }
                    }
                };

                ExpectedTrace expectedWorkflowInstacneTrace = new ExpectedTrace(orderedExpectedTrace);

                Exception exp = new Exception();
                testWorkflowRuntime.WaitForTerminated(1, out exp, watcher.ExpectedTraces, expectedWorkflowInstacneTrace);
            }
        }

        private void WaitForTerminationHelper(TestWorkflowRuntime testWorkflowRuntime)
        {
            WorkflowTrackingWatcher watcher = testWorkflowRuntime.GetWatcher();
            OrderedTraces orderedExpectedTrace = new OrderedTraces
            {
                Steps =
                    {
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Started),
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Terminated),
                        new WorkflowInstanceTrace(testWorkflowRuntime.CurrentWorkflowInstanceId, WorkflowInstanceState.Deleted)
                        {
                            Optional = true
                        }
                    }
            };

            ExpectedTrace expectedWorkflowInstacneTrace = new ExpectedTrace(orderedExpectedTrace);

            Exception exp = new Exception();
            testWorkflowRuntime.WaitForTerminated(1, out exp, watcher.ExpectedTraces, expectedWorkflowInstacneTrace);
        }

        #endregion Helper

        #region Sample
        private static void runTerminate()
        {
            AutoResetEvent waitForWorkflow = new AutoResetEvent(false);

            System.Activities.Statements.Sequence seq = new System.Activities.Statements.Sequence()
            {
                Activities =
                {
                    new System.Activities.Statements.TerminateWorkflow
                    {
                        Exception = new InArgument<Exception>(context => new TAC.ApplicationException()),
                        Reason = new InArgument<string>("just because"),
                    },
                    new System.Activities.Statements.WriteLine()
                    {
                        Text = "Hello"
                    },
                }
            };

            try
            {
                WorkflowApplication instance = new WorkflowApplication(seq)
                {
                    Completed = delegate (WorkflowApplicationCompletedEventArgs args)
                    {
                        Console.WriteLine("Completed workflow status: " + args.CompletionState);
                        if (args.CompletionState == ActivityInstanceState.Faulted)
                        {
                            Console.WriteLine("Termination Inner exception: " + args.TerminationException.InnerException.GetType());
                            Console.WriteLine("Termination exception Reason is: " + args.TerminationException.Message);
                            Console.WriteLine("Termination exception: " + args.TerminationException);
                        }
                        waitForWorkflow.Set();
                    }
                };
                Console.WriteLine("Starting");
                instance.Run();
                waitForWorkflow.WaitOne(); // Complete        
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception is here!");
                Console.WriteLine("Type = {0} , Message = {1} , RealException is: {2}", e.GetType(), e.Message, e.InnerException.GetType());
            }

            Console.ReadLine();
        }

        private class MyTracking : TrackingParticipant
        {
            protected override void Track(TrackingRecord record, TimeSpan timeout)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;

                if (record is ActivityScheduledRecord)
                {
                    ActivityScheduledRecord actSchedule = record as ActivityScheduledRecord;
                    Console.WriteLine("Name: {0}, Scheduled", actSchedule.Child.Name);
                }
                else if (record is ActivityStateRecord)
                {
                    ActivityStateRecord actState = record as ActivityStateRecord;
                    Console.WriteLine("Name: {0}, {1}", actState.Activity.Name, actState.State);
                }
                else if (record is WorkflowInstanceRecord)
                {
                    WorkflowInstanceRecord wInstanceRecord = record as WorkflowInstanceRecord;
                    Console.WriteLine("WorkFlow, {0}", wInstanceRecord.State);
                }
                else
                {
                    Console.WriteLine(record.ToString());
                }

                Console.ResetColor();
            }
        }
        #endregion Sample

        /// <summary>
        /// Use Terminate activity in a sequence. Instance should terminate after execution of terminate activity.
        /// </summary>        
        [Fact]
        public void TerminateInstanceWithUsingTerminateActivity()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            TestSequence seq = new TestSequence("TerminateSeq")
            {
                Activities =
                {
                    new TestSequence("Second")
                    {
                        Activities =
                        {
                            new TestTerminateWorkflow("Terminating")
                            {
                                ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                                Reason = s_terminationReason,
                            }
                        }
                    }
                },
            };

            RunTestWithWorkflowRuntime(seq);
        }

        /// <summary>
        /// Invoke a standalone TerminateWorkflow Activity
        /// </summary>        
        [Fact]
        public void StandaloneTerminateWorkflow()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            TestTerminateWorkflow terminateWf = new TestTerminateWorkflow("Terminating")
            {
                ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                Reason = s_terminationReason,
            };

            RunTestWithWorkflowRuntime(terminateWf);
        }

        /// <summary>
        /// Specify reason property in terminate activity without specifying exception. Make sure TerminateException is thrown with message containing reason.
        /// </summary>        
        [Fact]
        public void TerminateActivityWithReasonWithoutException()
        {
            s_exceptionType = null;
            s_exceptionMsg = null;
            s_terminationReason = "Just cus!";

            TestSequence seq = new TestSequence("TerminateSeq")
            {
                Activities =
                {
                    new TestSequence("Second")
                    {
                        Activities =
                        {
                            new TestTerminateWorkflow("Terminating")
                            {
                                Reason = s_terminationReason,
                            }
                        }
                    }
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(seq))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(VerifyUnsetValue);
                testWorkflowRuntime.ExecuteWorkflow();

                WaitForTerminationHelper(testWorkflowRuntime);
            }
        }

        /// <summary>
        /// Specify exception in terminate activity without specifying reason. Verify that Exception specified is created.
        /// </summary>        
        [Fact]
        public void TerminateActivityWithExceptionWithoutReason()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = null;

            TestSequence seq = new TestSequence("TerminateSeq")
            {
                Activities =
                {
                    new TestTerminateWorkflow("Terminating")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception"))
                    }
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(seq))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(VerifyUnsetValue);
                testWorkflowRuntime.ExecuteWorkflow();

                WaitForTerminationHelper(testWorkflowRuntime);
            }
        }

        /// <summary>
        /// Verify that if No Reason and No Exception is Set, A Validation Exception is thrown instead of a WorkflowTerminatedException
        /// </summary>        
        [Fact]
        public void VerifyValidationException()
        {
            string wfName = "TerminatingWorkflow";

            TestTerminateWorkflow terminate = new TestTerminateWorkflow(wfName)
            {
                // Not setting Reason and Exception
            };

            string exceptionMessage = string.Format(ErrorStrings.OneOfTwoPropertiesMustBeSet, "Reason", "Exception", "TerminateWorkflow", wfName);
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(exceptionMessage, terminate.ProductActivity, false));

            TestRuntime.ValidateWorkflowErrors(terminate, constraints, exceptionMessage);
        }

        /// <summary>
        /// Set Exception and Reason to Null
        /// </summary>        
        [Fact]
        public void SetParametersToNull()
        {
            string wfName = "TerminatingWorkflow";
            string exceptionMessage = string.Format(ErrorStrings.OneOfTwoPropertiesMustBeSet, "Reason", "Exception", "TerminateWorkflow", wfName);

            try
            {
                TerminateWorkflow terminate = new TerminateWorkflow()
                {
                    DisplayName = wfName,
                    Exception = null,
                    Reason = null
                };


                WorkflowApplication instance = new WorkflowApplication(terminate);
                instance.Run();
            }
            catch (Exception e)
            {
                if (e.GetType() != typeof(InvalidWorkflowException))
                {
                    throw new TestCaseFailedException("Exception is incorrect");
                }
            }
        }

        /// <summary>
        /// Add terminate activity after A throw in Try of TryCatch. Ensure TerminateWorkflow is not executed
        /// </summary>        
        [Fact]
        public void TryTerminatingActivityAfterThrowInTryCatch()
        {
            s_exceptionType = typeof(InvalidCastException);
            s_terminationReason = "I like home!";

            TestTryCatch tryCatch = new TestTryCatch("TryCatch")
            {
                Try = new TestSequence("TryingSeq")
                {
                    Activities =
                    {
                        new TestWriteLine()
                        {
                            Message = "I'm Trying here",
                            HintMessage = "I'm Trying here"
                        },
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.CaughtException(),
                        },
                        new TestTerminateWorkflow()
                        {
                            ExceptionExpression = context => new InvalidCastException("I want to go home now!"),
                            Reason = s_terminationReason
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("CaughtException")
                        {
                            Message = "aha I caught you!",
                            HintMessage = "aha I caught you!"
                        }
                    }
                },
                Finally = new TestWriteLine("Finally")
                {
                    Message = "Ha! Now you have to stay",
                    HintMessage = "Ha! Now you have to stay"
                }
            };


            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(tryCatch))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(workflowCompletedNoExceptions);
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// Add terminate activity in Try of TryCatch, ensure Workflow is terminated
        /// </summary>        
        [Fact]
        public void TerminateActivityInTry()
        {
            s_exceptionType = typeof(InvalidCastException);
            s_exceptionMsg = "I want to go home now!";
            s_terminationReason = "I like home!";

            TestTryCatch tryCatch = new TestTryCatch("TryCatch")
            {
                Try = new TestSequence("TryingSeq")
                {
                    Activities =
                    {
                        new TestWriteLine()
                        {
                            Message = "I'm Trying here",
                            HintMessage = "I'm Trying here"
                        },
                        new TestTerminateWorkflow()
                        {
                            ExceptionExpression = ((env) => new InvalidCastException("I want to go home now!")),
                            Reason = s_terminationReason
                        }
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestWriteLine("CaughtException")
                        {
                            Message = "aha I caught you!",
                            HintMessage = "aha I caught you!"
                        }
                    }
                },
            };

            RunTestWithWorkflowRuntime(tryCatch);
        }

        /// <summary>
        /// Add terminate activity in Try of TryCatch, ensure Workflow is terminated
        /// </summary>        
        [Fact]
        public void TerminateActivityInCatch()
        {
            s_exceptionType = typeof(InvalidCastException);
            s_exceptionMsg = "I want to go home now!";
            s_terminationReason = "I like home!";

            TestTryCatch tryCatch = new TestTryCatch("TryCatch")
            {
                Try = new TestSequence("TryingSeq")
                {
                    Activities =
                    {
                        new TestWriteLine()
                        {
                            Message = "I'm Trying here",
                            HintMessage = "I'm Trying here"
                        },
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.CaughtException(),
                        },
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestSequence("CatchingSeq")
                        {
                            Activities =
                            {
                                new TestTerminateWorkflow()
                                {
                                    ExceptionExpression = ((env) => new InvalidCastException("I want to go home now!")),
                                    Reason = s_terminationReason
                                },
                                new TestWriteLine("CaughtException")
                                {
                                    Message = "Should not execute",
                                    HintMessage = "A Bug if executed",
                                    ExpectedOutcome = Outcome.None
                                }
                            }
                        }
                    }
                },
            };

            RunTestWithWorkflowRuntime(tryCatch);
        }

        /// <summary>
        /// Add terminate activity in Try of TryCatch, ensure Workflow is terminated
        /// </summary>        
        [Fact]
        public void TerminateActivityInFinally()
        {
            s_exceptionType = typeof(InvalidCastException);
            s_exceptionMsg = "I want to go home now!";
            s_terminationReason = "I like home!";

            TestTryCatch tryCatch = new TestTryCatch("TryCatch")
            {
                Try = new TestSequence("TryingSeq")
                {
                    Activities =
                    {
                        new TestWriteLine()
                        {
                            Message = "I'm Trying here",
                            HintMessage = "I'm Trying here"
                        },
                        new TestThrow<ArgumentException>("TryException")
                        {
                            ExpectedOutcome = Outcome.CaughtException(),
                        },
                    }
                },
                Catches =
                {
                    new TestCatch<ArgumentException>()
                    {
                        Body = new TestSequence("CatchingSeq")
                        {
                            Activities =
                            {
                                new TestWriteLine("CaughtException")
                                {
                                    Message = "aha I caught you!",
                                    HintMessage = "aha I caught you!",
                                }
                            }
                        }
                    }
                },
                Finally = new TestSequence("FinallySeq")
                {
                    Activities =
                    {
                        new TestTerminateWorkflow()
                        {
                            ExceptionExpression = ((env) => new InvalidCastException("I want to go home now!")),
                            Reason = s_terminationReason
                        },
                        new TestWriteLine("InFinally")
                        {
                            Message = "Should Not Execute!",
                            HintMessage = "A Bug if executed",
                            ExpectedOutcome = Outcome.None
                        }
                    }
                }
            };

            RunTestWithWorkflowRuntime(tryCatch);
        }

        /// <summary>
        /// Use terminate activity in flowchart
        /// </summary>        
        [Fact]
        public void TerminateActivityInFlowchart()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "Flowchart terminating";
            s_terminationReason = "Bad Flowchart!";

            TestSequence baseSeq = new TestSequence("BaseSequence")
            {
                Activities =
                {
                    new TestWriteLine("Base")
                    {
                        Message = "Boring Base",
                        HintMessage = "Boring Base",
                    }
                }
            };

            TestSequence trueBranch = new TestSequence("TrueSequence")
            {
                Activities =
                {
                    new TestWriteLine("TrueWriteLine")
                    {
                        Message = "I only know the truth!",
                        HintMessage = "I only know the truth!",
                    },
                    new TestTerminateWorkflow()
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("Flowchart terminating")),
                        Reason = s_terminationReason
                    }
                }
            };

            TestSequence falseBranch = new TestSequence("FalseSequence")
            {
                Activities =
                {
                    new TestWriteLine("FalseWriteLine")
                    {
                        Message = "I'm a liar",
                        HintMessage = "Bug if gets here",
                    }
                }
            };

            TestFlowchart flowchart = new TestFlowchart("FlowChart");
            TestFlowConditional flowDecision = new TestFlowConditional
            {
                Condition = true
            };
            flowchart.AddConditionalLink(baseSeq, flowDecision, trueBranch, falseBranch);
            RunTestWithWorkflowRuntime(flowchart);
        }

        /// <summary>
        /// Use Terminate activity in branch of parallel activity. Make sure all the other branches haven’t completed before terminate executes. The rests of the branches should cancel before instance terminates.
        /// </summary>        
        [Fact]
        public void TerminateInParallelBranch()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
            Variable<int> counter1 = VariableHelper.CreateInitialized<int>("counter", 0);

            TestParallel parallel = new TestParallel("TerminatePar")
            {
                Branches =
                {
                    new TestSequence("ActiveBranch1")
                    {
                        Activities =
                        {
                            new TestWriteLine("ActiveBranch1WL")
                            {
                                Message = "I should be Executed",
                                HintMessage = "I should be Executed"
                            },
                            new TestDelay()
                            {
                                Duration = new TimeSpan(0, 0, 5),
                                ExpectedOutcome = Outcome.Faulted
                            }
                        }
                    },

                    new TestSequence("ActiveBranch2")
                    {
                        Activities =
                        {
                            new TestWriteLine("ActiveBranch2WL")
                            {
                                Message = "I should also be Executed",
                                HintMessage = "I should also be Executed"
                            },
                            new TestDelay()
                            {
                                Duration = new TimeSpan(0, 0, 5),
                                ExpectedOutcome = Outcome.Faulted
                            }
                        }
                    },

                    new TestTerminateWorkflow("TerminatingBranch")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                        Reason = s_terminationReason,
                    },

                    new TestWriteLine("TerminatedBranch!!") // branch is not executed
                    {
                        Message = "I should be Terminated",
                        HintMessage = "I should be Terminated",

                        ExpectedOutcome = Outcome.Faulted
                    }
},
            };

            RunTestWithWorkflowRuntime(parallel);
        }

        /// <summary>
        /// Use Terminate activity in a sequence. Invoke it using WorkflowInvoker. Instance should terminate after execution of terminate activity.
        /// </summary>        
        [Fact]
        public void TerminateInstanceWithInvokerUsingTerminateActivity()
        {
            s_exceptionType = typeof(TestCaseFailedException);
            s_exceptionMsg = "Name is Not Found";
            s_terminationReason = "Name is not here";

            TestSequence seq = new TestSequence("TerminateSeq")
            {
                Activities =
                {
                    new TestTerminateWorkflow("Terminating")
                    {
                        ExceptionExpression = ((env) => new TestCaseFailedException("Name is Not Found")),
                        Reason = s_terminationReason
                    }
                },
            };

            try
            {
                TestRuntime.RunAndValidateUsingWorkflowInvoker(seq, null, null, null);
                throw new TestCaseFailedException("An exception should have been thrown");
            }
            catch (WorkflowTerminatedException exception)
            {
                if (exception.InnerException.GetType() != s_exceptionType)
                {
                    throw new TestCaseFailedException("Exception is Incorrect");
                }
                if (!exception.InnerException.Message.Equals(s_exceptionMsg))
                {
                    throw new TestCaseFailedException("Exception Message is Incorrect");
                }
                if (!exception.Message.Equals(s_terminationReason))
                {
                    throw new TestCaseFailedException("Reason Message is Incorrect");
                }
            }
        }


        /// <summary>
        /// Use terminate activity with no persistence added to the workflow.
        /// </summary>        
        [Fact]
        public void TerminateWithoutPersistenceConfiguration()
        {
            //TestParameters.SetParameter("PersistenceProviderFactoryType", "NoPP");

            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            TestSequence seq = new TestSequence("TerminateSeq")
            {
                Activities =
                {
                    new TestSequence("Second")
                    {
                        Activities =
                        {
                            new TestTerminateWorkflow("Terminating")
                            {
                                ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                                Reason = s_terminationReason,
                            }
                        }
                    }
                },
            };

            RunTestWithWorkflowRuntime(seq);

            //TestParameters.SetParameter("PersistenceProviderFactoryType", "SQLPP");
        }

        ///// <summary>
        ///// Block while evaluating expression and cancel the terminate activity.
        ///// </summary>        
        //[Fact]
        //public void CancelTerminateActivity()
        //{
        //    VisualBasicValue<ICollection<string>> vbv = new VisualBasicValue<ICollection<string>>("New SleepCollection(Of String) From {\"item1\", \"item2\"}");
        //    Variable<ICollection<string>> values = new Variable<ICollection<string>>
        //    {
        //        Default = vbv
        //    };

        //    TestSequence sequence = new TestSequence("Do while")
        //    {
        //        Variables = 
        //       {
        //           values
        //       },
        //        Activities = 
        //       {
        //          new TestAddToCollection<string>("AddingStuff")
        //          {
        //              CollectionVariable = values,
        //              Item = "item3"
        //          },
        //          new TestTerminateWorkflow("Terminating")
        //          {
        //              ExceptionExpression = context => new TAC.ApplicationException(),
        //              Reason = "Not Much", 
        //              ExpectedOutcome = Outcome.None
        //          }
        //        },
        //        ExpectedOutcome = Outcome.Canceled
        //    };

        //    VisualBasicUtility.AttachVisualBasicSettingsProperty(sequence.ProductActivity, new List<Type>() { typeof(SleepCollection<string>) });

        //    using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(sequence))
        //    {
        //        testWorkflowRuntime.ExecuteWorkflow();
        //        testWorkflowRuntime.WaitForActivityStatusChange("AddingStuff", TestActivityInstanceState.Executing);

        //        Thread.Sleep(500);

        //        testWorkflowRuntime.CancelWorkflow();
        //        testWorkflowRuntime.WaitForCompletion();
        //    }
        //}

        /// <summary>
        /// Verify that existing bookmarks are closed/cancelled when terminate executes.
        /// </summary>        
        [Fact]
        public void ResumeBookmarkWhileTerminating()
        {
            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            TestParallel parallel = new TestParallel("ParallelTest")
            {
                Branches =
                {
                    new TestSequence("BlockingBranch")
                    {
                        Activities =
                        {
                            new TestWriteLine("BeforeBlocking")
                            {
                                Message = "I should be Executed",
                                HintMessage = "I should be Executed"
                            },
                            new TestBlockingActivity("Blocking", "BlockBranch")
                            {
                                ExpectedOutcome = Outcome.Faulted
                            }
                        }
                    },
                    new TestTerminateWorkflow("TerminatingBranch")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                        Reason = s_terminationReason,
                    },
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parallel))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(workflowInstance_Completed);
                testWorkflowRuntime.ExecuteWorkflow();

                WaitForTerminationHelper(testWorkflowRuntime);

                // Try resuming the bookmark
                BookmarkResumptionResult result = testWorkflowRuntime.ResumeBookMark("Blocking", null);
                if (result != BookmarkResumptionResult.NotFound)
                {
                    throw new TestCaseFailedException("Bookmark should be cancelled");
                }
            }
        }

        ///// <summary>
        ///// Terminate instance inside transaction scope. TransactionScope should be faulted
        ///// </summary>        
        //[Fact]
        //public void TerminateInTransactionScope()
        //{
        //    ExceptionType = typeof(TAC.ApplicationException);
        //    ExceptionMsg = "I want to terminate!";
        //    TerminationReason = "In TransactionScope";

        //    TestTransactionScopeActivity transactionScope = new TestTransactionScopeActivity("TransactionScoping")
        //    {
        //        AbortInstanceOnTransactionFailure = false,
        //        Body = new TestSequence()
        //        {
        //            Activities = 
        //            {
        //                new TestWriteLine("BeforeTerminating")
        //                {
        //                    Message = "Terminating shortly",
        //                    HintMessage = "Terminating shortly"
        //                },
        //                new TestTerminateWorkflow("Terminating")
        //                {
        //                    ExceptionExpression = (env) => new TAC.ApplicationException("I want to terminate!"),
        //                    Reason = TerminationReason
        //                },
        //                new TestWriteLine("AfterTerminating")
        //                {
        //                    Message = "Should Not execute",
        //                    HintMessage = "Error if executed",
        //                    ExpectedOutcome = Outcome.None
        //                }
        //            }
        //        },
        //        ExpectedOutcome = Outcome.Faulted
        //    };


        //    RunTestWithWorkflowRuntime(transactionScope);
        //}

        /// <summary>
        /// Invoke async method in a parallel branch and on the other terminate. Workflow should be terminated, Async method can still be running
        /// </summary>        
        [Fact]
        public void InvokeAsyncAndTerminate()
        {
            //TestParameters.SetParameter("DisableXamlRoundTrip", "True");

            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Just cus!";

            TestParallel par = new TestParallel("ParallelTest")
            {
                Branches =
                {
                    new TestInvokeMethod(typeof(MethodHelper).GetMethod("MyMethod"))
                    {
                        TargetObject = new TestArgument<MethodHelper>(Direction.In, "TargetObject", (context => new MethodHelper())),
                        RunAsynchronously = true,
                        ExpectedOutcome = Outcome.Faulted
                    },
                    new TestTerminateWorkflow("TerminatingParallel")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                        Reason = s_terminationReason
                    }
                },
            };

            RunTestWithWorkflowRuntime(par);

            //TestParameters.SetParameter("DisableXamlRoundTrip", "False");
        }
        class MethodHelper
        {
            public void MyMethod()
            {
                for (int i = 0; i < 100; i++)
                {
                    Console.WriteLine(i.ToString());
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// Try to resume terminated instance with Persistence Enabled. Shouldn’t be possible.
        /// </summary>        
        [Fact]
        public void TryResumingTerminatedInstanceWithPersistence()
        {
            //TestParameters.SetParameter("PersistenceProviderFactoryType", "SQLPP");

            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Terminating Now!";

            TestSequence seq = new TestSequence("Executing")
            {
                Activities =
                {
                    new TestWriteLine("AboutToTerminate")
                    {
                        Message = "Terminating Soon!",
                        HintMessage = "Terminating Soon!"
                    },
                    new TestTerminateWorkflow("Terminating")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                        Reason = s_terminationReason
                    }
                },
            };


            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(seq))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(workflowInstance_Completed);
                testWorkflowRuntime.ExecuteWorkflow();

                WaitForTerminationHelper(testWorkflowRuntime);

                try
                {
                    testWorkflowRuntime.ResumeWorkflow();
                    throw new TestCaseFailedException("Should not be able to resume the instance");
                }
                catch (Exception e)
                {
                    ExceptionHelpers.ValidateException(e, (typeof(WorkflowApplicationTerminatedException)), null);
                }
            }
        }

        /// <summary>
        /// Try to resume terminated instance without Persistence Enabled. Shouldn’t be possible.
        /// </summary>        
        [Fact]
        public void TryResumingTerminatedInstanceWithoutPersistence()
        {
            //TestParameters.SetParameter("PersistenceProviderFactoryType", "NoPP");

            s_exceptionType = typeof(TAC.ApplicationException);
            s_exceptionMsg = "I am throwing this Exception";
            s_terminationReason = "Terminating Now!";

            TestSequence seq = new TestSequence("Executing")
            {
                Activities =
                {
                    new TestWriteLine("AboutToTerminate")
                    {
                        Message = "Terminating Soon!",
                        HintMessage = "Terminating Soon!"
                    },
                    new TestTerminateWorkflow("Terminating")
                    {
                        ExceptionExpression = ((env) => new TAC.ApplicationException("I am throwing this Exception")),
                        Reason = s_terminationReason
                    }
                },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(seq))
            {
                testWorkflowRuntime.OnWorkflowCompleted += new EventHandler<TestWorkflowCompletedEventArgs>(workflowInstance_Completed);
                testWorkflowRuntime.ExecuteWorkflow();
                WaitForTerminationHelper(testWorkflowRuntime);

                try
                {
                    testWorkflowRuntime.ResumeWorkflow();
                    throw new TestCaseFailedException("Should not be able to resume the instance");
                }
                catch (Exception e)
                {
                    ExceptionHelpers.ValidateException(e, (typeof(WorkflowApplicationTerminatedException)), null);
                }
            }

            //TestParameters.SetParameter("PersistenceProviderFactoryType", "SQLPP");
        }
    }
}

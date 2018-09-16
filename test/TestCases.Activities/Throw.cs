// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using TestCases.Activities.Common;
using Xunit;

namespace TestCases.Activities
{
    /// <summary>
    /// Made for temporary use
    /// </summary>
    public class Throw
    {
        /// <summary>
        /// Throw Exception using throw activity and verify we get the correct exception
        /// </summary>        
        [Fact]
        public void ThrowException()
        {
            TestThrow<InvalidDataException> throwAct = new TestThrow<InvalidDataException>("Throw Invalid data");

            TestRuntime.RunAndValidateAbortedException(throwAct, typeof(InvalidDataException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Throw Custom Exception
        /// </summary>        
        [Fact]
        public void ThrowCustomException()
        {
            TestThrow<CustomException> th = new TestThrow<CustomException>("custom exception")
            {
                ExceptionExpression = (context => new CustomException("Invalid department"))
            };

            TestRuntime.RunAndValidateAbortedException(th, typeof(CustomException), new Dictionary<string, string>());
        }

        /// <summary>
        /// We will catch the outer exception and rethrow the exception object's inner exception 
        /// and verify we get the inner exception what we passes to throw activity.
        /// </summary>
	    /// <summary>
	    /// Throw Exception and set the inner exception property of throw activity.
	    /// Throw Exception and set the inner exception property of throw activity.Verify setting inner exception property overwrites the inner exception of exception thrown
	    /// </summary>        
        [Fact]
        public void ThrowWithInnerException()
        {
            // Initializing variable which we will use to catch the exception object
            DelegateInArgument<MemberAccessException> accExc = new DelegateInArgument<MemberAccessException>();
            //TestParameters.DisableXamlRoundTrip = true;
            TestSequence seq = new TestSequence("Outer Seq")
            {
                Activities =
                {
                    new TestTryCatch("Try catch finally")
                    {
                        Try = new TestSequence("Try Activity")
                        {
                            Activities =
                            {
                                new TestThrow<MemberAccessException>("Throw Operation exception")
                                {
                                    ExceptionExpression = (context => new MemberAccessException("Throw Exception", new IndexOutOfRangeException())),
                                    //InnerException = new IndexOutOfRangeException(),
                                    ExpectedOutcome = Outcome.CaughtException(),
                                }
                            }
                        },

                        Catches =
                        {
                            new TestCatch<MemberAccessException>()
                            {
                                ExceptionVariable = accExc,
                                Body = new TestSequence("Body of Catch")
                                {
                                    Activities =
                                    {
                                        // Rethrowing inner exception so we can verify correct exception is thrown
                                        new TestThrow<IndexOutOfRangeException>("Throw inner exception")
                                        {
                                            ExceptionExpression = (env) => (IndexOutOfRangeException) accExc.Get(env).InnerException,
                                            ExpectedOutcome = Outcome.UncaughtException(typeof(IndexOutOfRangeException))
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(IndexOutOfRangeException), new Dictionary<string, string>());
        }

        /// <summary>
        /// We are using throw activity in try catch block so that we can catch exception 
        /// and use writeline to verify exception object message.
        /// </summary>
	    /// <summary>
	    /// Throw Exception and set the message property of throw activity.
	    /// Throw Exception and set the message property of throw activity.Verify message provided to property overwrites message thrown by exception
	    /// </summary>        
        [Fact]
        public void ThrowExceptionWithMessage()
        {
            // Initializing variable which we will use to catch the exception object
            DelegateInArgument<OperationCanceledException> op = new DelegateInArgument<OperationCanceledException>();
            //TestParameters.DisableXamlRoundTrip = true;
            TestSequence seq = new TestSequence("Outer Seq")
            {
                Activities =
                {
                    new TestTryCatch("Try catch finally")
                    {
                        Try = new TestSequence("Try Activity")
                        {
                            Activities =
                            {
                                new TestThrow<OperationCanceledException>("Throw Operation exception")
                                {
                                    ExceptionExpression = (context => new OperationCanceledException("We have set the message to overwrite exception message")),
                                    ExpectedOutcome  = Outcome.CaughtException(),
                                }
                            }
                        },
                        Catches =
                        {
                            new TestCatch<OperationCanceledException>()
                            {
                                ExceptionVariable = op,
                                Body = new TestSequence("Body of Catch")
                                {
                                    Activities =
                                    {
                                        new TestWriteLine("Writeline for exception message")
                                        {
                                            MessageExpression = (env) => op.Get(env).Message,
                                            HintMessage =  "We have set the message to overwrite exception message"
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Null Message In Exception
        /// </summary>        
        [Fact]
        public void NullMessageInException()
        {
            // Initializing variable which we will use to catch the exception object
            DelegateInArgument<DataMisalignedException> objDisp = new DelegateInArgument<DataMisalignedException> { Name = "objDisp" };
            DataMisalignedException ex = new DataMisalignedException(null);

            TestSequence outerSeq = new TestSequence()
            {
                Activities =
                {
                    new TestTryCatch("Try Catch finally")
                    {
                        Try = new TestSequence()
                        {
                            Activities =
                            {
                                new TestThrow<DataMisalignedException>("throw1")
                                {
                                    ExceptionExpression = (context => new DataMisalignedException(null)),
                                    ExpectedOutcome = Outcome.CaughtException(),
                                }
                            }
                        },
                        Catches =
                        {
                            new TestCatch<DataMisalignedException>()
                            {
                                ExceptionVariable = objDisp,
                                Body = new TestSequence()
                                {
                                    Activities =
                                    {
                                        new TestWriteLine()
                                        {
                                            MessageExpression = (env) => (string)objDisp.Get(env).Message,
                                            HintMessage = ex.Message,
                                        }
                                    }
                                },
                            }
                        }
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(outerSeq);
        }

        /// <summary>
        /// Add any activity after throw activity and verify this activity never gets executed.
        /// </summary>        
        [Fact]
        public void UnreachableActivityAfterThrow()
        {
            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestThrow<MissingMemberException>(),

                   new TestWriteLine("WriteLine"){Message = "Unreachable activity"},
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(MissingMemberException), new Dictionary<string, string>());
        }

        /// <summary>
        /// ThrowWithExceptionSet
        /// </summary>        
        [Fact]
        public void ThrowInSeq()
        {
            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestWriteLine()
                    {
                        Message = "Hi"
                    },

                    new TestThrow<InvalidCastException>()
                },
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(InvalidCastException), new Dictionary<string, string>());
        }

        /// <summary>
        /// ThrowWithExceptionSet
        /// </summary>        
        [Fact]
        public void ThrowWithExceptionSet()
        {
            //TestParameters.DisableXamlRoundTrip = true;
            DelegateInArgument<ArgumentNullException> ex = new DelegateInArgument<ArgumentNullException>();
            TestSequence seq = new TestSequence("Seq")
            {
                Activities =
                {
                    new TestTryCatch("Try catch finally")
                    {
                        Try = new TestSequence("Try")
                        {
                            Activities =
                            {
                                new TestThrow<ArgumentNullException>()
                                {
                                    ExpectedOutcome = new CaughtExceptionOutcome(typeof(ArgumentNullException)),
                                    ExceptionExpression = (context => new ArgumentNullException("Value cannot be null.", new InvalidCastException())),
                                }
                            }
                        },

                        Catches =
                        {
                            new TestCatch<InvalidCastException>(){},
                            new TestCatch<ArgumentNullException>
                            {
                                ExceptionVariable = ex,
                                Body = new TestSequence()
                                {
                                    Activities =
                                    {
                                        new TestWriteLine()
                                        {
                                            HintMessage = "Value cannot be null.",
                                            MessageExpression = (env) => (string) ex.Get(env).Message,
                                        },

                                        new TestThrow<InvalidCastException>()
                                        {
                                            ExceptionExpression = (env) => (InvalidCastException) ex.Get(env).InnerException,
                                        }
                                    }
                                },
}
                        }
                    }
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(InvalidCastException), new Dictionary<string, string>());
        }

        //[Fact]
        //public void DifferentArguments()
        //{
        //    //Testing Different argument types for Throw.Exception
        //    // DelegateInArgument
        //    // DelegateOutArgument
        //    // Activity<T>
        //    // Variable<T> and Expression is already implemented.

        //    DelegateInArgument<Exception> delegateInArgument = new DelegateInArgument<Exception>("Input");
        //    DelegateOutArgument<Exception> delegateOutArgument = new DelegateOutArgument<Exception>("Output");

        //    TestCustomActivity<InvokeFunc<Exception, Exception>> invokeFunc = TestCustomActivity<InvokeFunc<Exception, Exception>>.CreateFromProduct(
        //       new InvokeFunc<Exception, Exception>
        //       {
        //           Argument = new VisualBasicValue<Exception>("New Exception(\"TestException1\")"),
        //           Func = new ActivityFunc<Exception, Exception>
        //           {
        //               Argument = delegateInArgument,
        //               Result = delegateOutArgument,
        //               Handler = new CoreWf.Statements.TryCatch
        //               {
        //                   DisplayName = "TryCatch2",
        //                   Try = new CoreWf.Statements.Sequence
        //                   {
        //                       DisplayName = "Sequence1",
        //                       Activities =
        //                         {
        //                             new CoreWf.Statements.TryCatch
        //                             {
        //                                 DisplayName = "TryCatch1",
        //                                 Try = new CoreWf.Statements.Throw{ DisplayName = "Throw1",Exception = delegateInArgument},
        //                                 Catches = 
        //                                 {
        //                                     new CoreWf.Statements.Catch<Exception>
        //                                     { 
        //                                         Action = new ActivityAction<Exception>
        //                                         {
        //                                             Argument = new DelegateInArgument<Exception>("arg1"),
        //                                             Handler = new CoreWf.Statements.Assign<Exception>
        //                                                     {
        //                                                         DisplayName = "Assign1",
        //                                                         Value = new VisualBasicValue<Exception>("New ApplicationException(\"TestException2\")"),
        //                                                         To = delegateOutArgument,
        //                                                     },
        //                                         }
        //                                     }
        //                                 }
        //                             },
        //                             new CoreWf.Statements.Throw{ DisplayName = "Throw2", Exception = delegateOutArgument},
        //                         }
        //                   },
        //                   Catches = 
        //                   {
        //                        new CoreWf.Statements.Catch<ApplicationException>()
        //                   }
        //               }
        //           }
        //       }
        //    );

        //       TestTryCatch actForTraces = new TestTryCatch
        //            {
        //                DisplayName = "TryCatch2",
        //                Try =   new TestSequence
        //                {
        //                    DisplayName = "Sequence1",
        //                    Activities =
        //                    {
        //                        new TestTryCatch
        //                        {
        //                            DisplayName = "TryCatch1",
        //                            Try = new TestThrow<Exception>("Throw1"),
        //                            Catches = 
        //                            {
        //                                new TestCatch<Exception>
        //                                {
        //                                    HintHandleException = true,
        //                                    Body = new TestAssign<Exception>
        //                                    {
        //                                        DisplayName = "Assign1",
        //                                    }
        //                                }
        //                            }
        //                        },
        //                        new TestThrow<ApplicationException>("Throw2"){ ExpectedOutcome = Outcome.CaughtException()}
        //                    }
        //                },
        //                Catches =
        //                {
        //                    new TestCatch<ApplicationException>
        //                    {
        //                        HintHandleException = true,
        //                    }
        //                }
        //    };

        //    invokeFunc.CustomActivityTraces.Add(actForTraces.GetExpectedTrace().Trace);

        //    TestRuntime.RunAndValidateWorkflow(invokeFunc);
        //}

        [Fact(Skip = @"This test was commented out for framework, it may be showing issues with test infrastructure")]
        public void ThrowActivityInInnerMostLoop()
        {
            List<string> values = new List<string>() { "HI", "THere" };
            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestForEach<string>()
                    {
                        Values = values,
                        Body = new TestForEach<string>
                        {
                            Values = values,
                            Body = new TestForEach<string>
                            {
                                Values = values,
                                Body = new TestForEach<string>()
                                {
                                    Values = values,
                                    Body = new TestThrow<TestCaseException>()
                                }
                            }
                        }
                    }
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(TestCaseException), new Dictionary<string, string>());
        }
    }

    /// <summary>
    ///  Custom exception with no default constructor
    /// </summary>
    public class NameNotFoundException : Exception
    {
        public NameNotFoundException(string name) : base(name)
        {
        }
    }
}

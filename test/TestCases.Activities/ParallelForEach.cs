// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common;
using Xunit;

namespace TestCases.Activities
{
    public class ParallelForEach
    {
        /// <summary>
        /// Basic Parallel ForEach
        /// </summary>        
        [Fact]
        public void BasicParallelForEachTest()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerSeq");
            DelegateInArgument<string> i = new DelegateInArgument<string>() { Name = "i" };

            string[] strArray = new string[] { "var1", "var2", "var3" };

            TestParallelForEach<string> foreachAct = new TestParallelForEach<string>("foreach")
            {
                HintValues = new string[] { "var1", "var2", "var3" },
                ValuesExpression = (context => new string[] { "var1", "var2", "var3" }),
                CurrentVariable = i,
                HintIterationCount = 3
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                MessageExpression = ((env) => string.Format("WriteLine Argument: {0}", i.Get(env)))
            };

            for (int counter = strArray.Length - 1; counter > -1; counter--)
            {
                writeLine.HintMessageList.Add("WriteLine Argument: " + strArray[counter]);
            }

            foreachAct.Body = innerSequence;

            innerSequence.Activities.Add(writeLine);
            outerSequence.Activities.Add(foreachAct);

            ExpectedTrace tr = outerSequence.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(outerSequence, tr);
        }

        /// <summary>
        /// This is to make sure a Delay in ParallelForEach doesnt block the execution of another branch.
        /// DelayDoesNotBlockOtherExecution
        /// </summary>        
        [Fact]
        //[HostWorkflowAsWebService]
        public void DelayDoesNotBlockOtherExecution()
        {
            // In this scenario, I use "delayed" variable to make sure the Delay activity was not a blocking one 
            // This is verified, by setting the "delayed" variable after Delay activity is done.

            DelegateInArgument<string> currentVariable = new DelegateInArgument<string>() { Name = "currentVariable" };
            Variable<bool> delayed = VariableHelper.CreateInitialized<bool>("delayed", false);
            TimeSpan sec = new TimeSpan(0, 0, 0, 0, 1);


            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("PFE")
            {
                CurrentVariable = currentVariable,
                Body = new TestSequence("BodyOfParallel")
                {
                    Variables = { delayed },
                    Activities =
                    {
                        new TestIf("If condition", HintThenOrElse.Then, HintThenOrElse.Then)
                        {
                            ConditionExpression = ((env) => !delayed.Get(env)),
                            ThenActivity = new TestSequence("Body of If")
                            {
                                Activities =
                                {
                                    new TestDelay()
                                    {
                                         Duration = sec
                                    },
                                    new TestAssign<bool>
                                    {
                                        Value = true,
                                        ToVariable = delayed
                                    }
                                },
                            },
                        },
                    }
                },

                HintValues = new string[] { "a", "b" },
                ValuesExpression = (context => new string[] { "a", "b" }),
                HintIterationCount = 2
            };

            ExpectedTrace trace = parallelForEach.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelForEach, trace);
        }

        /// <summary>
        /// ParallelForEach activity in the body of parallel for each.
        /// </summary>        
        [Fact]
        public void NestedParallelForEach()
        {
            DelegateInArgument<string> _currentVariable_1 = new DelegateInArgument<string>() { Name = "_currentVariable_1" };
            DelegateInArgument<string> _currentVariable_2 = new DelegateInArgument<string>() { Name = "_currentVariable_2" };

            TestSequence sequ = new TestSequence("Sequence")
            {
                Activities =
                {
                    new TestParallelForEach<string>("Outer Parallel")
                    {
                        CurrentVariable = _currentVariable_1,
                        Body = new TestParallelForEach<string>("Inner parallel")
                        {
                            CurrentVariable = _currentVariable_2,
                            Body = new TestWriteLine("Writeline")
                            {
                                MessageExpression = (env) => (string) _currentVariable_2.Get(env),
                                HintMessageList = {"iuu","M", "iuu", "M"},
                            },

                            HintValues = new string[] { "M", "iuu" },
                            ValuesExpression = (context => new string[] { "M", "iuu" }),
                        },

                        HintValues = new string[] { "M", "iuu" },
                        ValuesExpression = (context => new string[] { "M", "iuu" }),
                    }
                }
            };

            // Using user traces to validate this test as by validating against expected trace 
            // test is going into infinite loop during tracing.
            ExpectedTrace expected = sequ.GetExpectedTrace();
            expected.AddVerifyTypes(typeof(UserTrace));
            TestRuntime.RunAndValidateWorkflow(sequ, expected);
        }

        /// <summary>
        /// Parallel for each activity in the for each loop activity.
        /// </summary>        
        [Fact]
        public void ParallelForEachInLoop()
        {
            //TestParameters.DisableXamlRoundTrip = true;
            List<string> list1 = new List<string>() { "Item11", "Item12", "Item13" };
            List<string> list2 = new List<string>() { "Item21", "Item22", "Item23" };
            List<List<string>> lists = new List<List<string>>();
            lists.Add(list1);
            lists.Add(list2);

            DelegateInArgument<List<string>> listVar = new DelegateInArgument<List<string>>() { Name = "listVar" };
            DelegateInArgument<string> _currentVariable = new DelegateInArgument<string>() { Name = "_currentVariable" };

            TestSequence seq = new TestSequence("Outer Seq")
            {
                Activities =
                 {
                     new TestForEach<List<string>>("For Each in Outer Seq")
                     {
                         ValuesExpression = (context => lists),
                         CurrentVariable = listVar,

                         Body = new TestParallelForEach<string>("Parallel For Each")
                         {
                             CurrentVariable = _currentVariable,
                             Body = new TestWriteLine("Writeline in for each")
                             {
                                 MessageExpression = (env) => (string) _currentVariable.Get(env),
                                 HintMessageList = { "Item13", "Item12", "Item11","Item23", "Item22", "Item21" }
                             },
                             HintValues = list1, // This is a hack to let the tracing know about the number of values inside ValuesExpression.
                             ValuesExpression= context => listVar.Get(context),
                             HintIterationCount = 3
                         },

                         HintIterationCount = 2
                     }
                 }
            };

            ExpectedTrace tr = seq.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(seq, tr);
        }

        /// <summary>
        /// Parallel for each in try catch finally
        /// </summary>        
        [Fact]
        public void ParallelForEachInTryCatchFinally()
        {
            TestTryCatch tcf = new TestTryCatch("TCF")
            {
                Try = new TestParallelForEach<string>("Parallel for each")
                {
                    Body = new TestThrow<InvalidCastException>
                    {
                        ExpectedOutcome = Outcome.CaughtException(typeof(InvalidCastException))
                    },
                    HintValues = new List<string>() { "str1", "STR2", "str3" },
                    ValuesExpression = (context => new List<string>() { "str1", "STR2", "str3" }),
                    CompletionCondition = true,
                    HintIterationCount = 1,
                },
                Catches =
                {
                    new TestCatch<InvalidCastException>()
                    {
                        HintHandleException = true
                    }
                }
            };

            ExpectedTrace tr = tcf.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(tcf, tr);
        }

        /// <summary>
        /// Pass null to the value of list of item and verify we do not get any exception.
        /// </summary>        
        [Fact]
        public void NullAsValueOfElements()
        {
            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel For Each")
            {
                Body = new TestWriteLine("Writeline") { Message = "I will  be displayed" },
                HintValues = new List<string>() { null },
                ValuesExpression = (context => new List<string>() { null }),
                HintIterationCount = 1,
            };

            ExpectedTrace tr = parallelForEach.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelForEach, tr);
        }

        /// <summary>
        /// Change Element Value In Body
        /// </summary>        
        [Fact]
        public void ChangeElementValueInBody()
        {
            DelegateInArgument<string> _currentVariable = new DelegateInArgument<string>() { Name = "_currentVariable" };

            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel for each")
            {
                Body = new TestSequence()
                {
                    Activities =
                    {
                        new TestAssign<string>("Assign activity")
                        {
                            ToExpression = context => _currentVariable.Get(context),
                            Value = "New changed variable"
                        },

                        new TestWriteLine("Writeline")
                        {
                            MessageExpression = context => _currentVariable.Get(context),
                            HintMessage = "New changed variable"
                        }
                    }
                },

                HintValues = new List<string>() { "str1", "str2", "str3" },
                ValuesExpression = (context => new List<string>() { "str1", "str2", "str3" }),
                HintIterationCount = 3,
                CurrentVariable = _currentVariable
            };

            ExpectedTrace tr = parallelForEach.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelForEach, tr);
        }

        /// <summary>
        /// Cancel Child Of ParallelForEach
        /// </summary>        
        [Fact]
        public void CancelChildOfParallelForEach()
        {
            DelegateInArgument<string> _currentVariable = new DelegateInArgument<string>() { Name = "_currentVariable" };

            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel For Each Activity")
            {
                HintValues = new List<string>() { "Hi", "There" },
                ValuesExpression = (context => new List<string>() { "Hi", "There" }),
                CurrentVariable = _currentVariable,
                Body = new TestSequence("Body of parallel for each")
                {
                    Activities =
                    {
                        new TestWriteLine("Writeline in parallel for each")
                        {
                            MessageExpression = (env) => (string)_currentVariable.Get(env),
                            HintMessageList = {"There", "Hi"}
                        },

                        new TestIf("Test If", HintThenOrElse.Else, HintThenOrElse.Then)
                        {
                            ConditionExpression = (env) => (bool)(_currentVariable.Get(env).Equals("Hi")),

                            ThenActivity =  new TestBlockingActivityUnique("BlockingActivity", "Bookmark")
                            {
                                ExpectedOutcome = Outcome.Canceled
                            },

                            ElseActivity = new TestWriteLine("Writeline in Else")
                            {
                                MessageExpression = (env) => (string)_currentVariable.Get(env),
                                HintMessage = "There"
                            }
                        },

                        new TestWriteLine()
                        {
                            Message = "Hello"
                        }
                    }
                },

                HintIterationCount = 2
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parallelForEach))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// CancelParallelForEach
        /// </summary>        
        [Fact]
        public void CancelParallelForEach()
        {
            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>()
            {
                HintValues = new List<string>() { "Hi", "There" },
                ValuesExpression = (context => new List<string>() { "Hi", "There" }),
                Body = new TestBlockingActivityUnique("Blocking activity")
                {
                    ExpectedOutcome = Outcome.Canceled
                }
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parallelForEach))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("Blocking activity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// PersistParallelForEach
        /// </summary>        
        [Fact]
        public void PersistParallelForEach()
        {
            DelegateInArgument<string> _currentVariable = new DelegateInArgument<string>() { Name = "_currentVariable" };

            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel for each")
            {
                HintValues = new string[] { "Hi", "There" },
                ValuesExpression = (context => new string[] { "Hi", "There" }),
                CurrentVariable = _currentVariable,
                Body = new TestSequence("Sequence")
                {
                    Activities =
                    {
                        new TestWriteLine("Writeline")
                        {
                            Message = "Hi"
                        },

                        new TestSequence("inner seq")
                        {
                            Activities =
                            {
                                new TestIf(HintThenOrElse.Else, HintThenOrElse.Then)
                                {
                                    DisplayName = "Test if",
                                    ConditionExpression = (env) => (bool)(_currentVariable.Get(env).Equals("Hi")),
                                    ThenActivity = new TestBlockingActivity("Block Hi"),
                                    ElseActivity = new TestBlockingActivity("Block There")
                                }
                            }
                        },

                        new TestWriteLine("Writeline act")
                        {
                            Message = "After blocking activty"
                        },
                   }
                }
            };

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(parallelForEach, null, jsonStore, PersistableIdleAction.None))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("Block Hi", TestActivityInstanceState.Executing);
                testWorkflowRuntime.PersistWorkflow();
                System.Threading.Thread.Sleep(2000);
                testWorkflowRuntime.ResumeBookMark("Block Hi", null);


                testWorkflowRuntime.WaitForActivityStatusChange("Block There", TestActivityInstanceState.Executing);
                testWorkflowRuntime.PersistWorkflow();
                System.Threading.Thread.Sleep(2000);
                testWorkflowRuntime.ResumeBookMark("Block There", null);

                testWorkflowRuntime.WaitForCompletion();
            }
        }

        /// <summary>
        /// CompletionConditionTrue
        /// </summary>        
        [Fact]
        public void CompletionConditionTrue()
        {
            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel For Each")
            {
                HintValues = new string[] { "Hi", "There", "Why" },
                ValuesExpression = (context => new string[] { "Hi", "There", "Why" }),
                Body = new TestSequence("Sequence")
                {
                    Activities =
                    {
                        new TestWriteLine("Writeline", "Hello")
                    },
                },

                CompletionCondition = true,
                HintIterationCount = 1
            };

            ExpectedTrace tr = parallelForEach.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelForEach, tr);
        }

        /// <summary>
        /// Set body not null and completion condition to null
        /// Set body not null and completion condition to null and verify we get vlidation error.
        /// </summary>        
        [Fact]
        public void BodyNullAndCompletionConditionNotNull()
        {
            Variable<bool> temp = new Variable<bool>("temp");
            Variable<string> strVar = new Variable<string>("strVar", "HI");

            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("Parallel For Each")
            {
                HintIterationCount = 3,
                ProductParallelForEach =
                {
                    Body = null,
                    CompletionCondition = true,
                    Values = new InArgument<IEnumerable<string>>(context => new string[] { "Hi", "There", "Why" }),
                }
            };

            TestRuntime.RunAndValidateWorkflow(parallelForEach);
        }

        /// <summary>
        /// Verify we can use current variable of outer ParallelForEach in the inner parallelFor each.
        /// </summary>        
        [Fact]
        public void VariableScopingTest()
        {
            DelegateInArgument<string> _currentVariable = new DelegateInArgument<string>() { Name = "_currentVariable" };

            TestParallelForEach<string> parallelForEach = new TestParallelForEach<string>("ParallelForEach")
            {
                CurrentVariable = _currentVariable,
                HintValues = new string[] { "Hello", "How", "Are", "You" },
                ValuesExpression = (context => new string[] { "Hello", "How", "Are", "You" }),
                Body = new TestParallelForEach<string>("Inner Parallel For Each")
                {
                    HintValues = new string[] { "Take" },
                    ValuesExpression = (context => new string[] { "Take" }),
                    Body = new TestWriteLine("Writeline")
                    {
                        MessageExpression = (env) => (string)_currentVariable.Get(env),
                        HintMessageList = { "You", "Are", "How", "Hello" }
                    }
                }
            };

            ExpectedTrace tr = parallelForEach.GetExpectedTrace();
            TestRuntime.RunAndValidateWorkflow(parallelForEach, tr);
        }

        /// <summary>
        /// ParallelForEach with WorkFlowInvoker
        /// </summary>        
        [Fact]
        public void ParallelForEachWithWorkflowInvoker()
        {
            TestSequence innerSequence = new TestSequence("innerSeq");
            DelegateInArgument<string> i = new DelegateInArgument<string>() { Name = "i" };

            string[] strArray = new string[] { "var1", "var2", "var3" };

            TestParallelForEach<string> foreachAct = new TestParallelForEach<string>("foreach")
            {
                HintValues = strArray,
                ValuesExpression = (context => new string[] { "var1", "var2", "var3" }),
                CurrentVariable = i,
                HintIterationCount = 3
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                MessageExpression = ((env) => string.Format("WriteLine Argument: {0}", i.Get(env)))
            };

            for (int counter = strArray.Length - 1; counter > -1; counter--)
            {
                writeLine.HintMessageList.Add("WriteLine Argument: " + strArray[counter]);
            }

            foreachAct.Body = innerSequence;

            innerSequence.Activities.Add(writeLine);

            TestRuntime.RunAndValidateUsingWorkflowInvoker(foreachAct, null, null, null);
        }

        /// <summary>
        /// In parallelForeach with 6 branches, the CompletionCondition evaluates to true after completing the third branch and so cancels the rest.
        /// </summary>        
        [Fact]
        public void CompletionConditionCancelsRestOfBranches()
        {
            Variable<bool> cancelIt = new Variable<bool> { Name = "cancelIt", Default = false };
            DelegateInArgument<int> i = new DelegateInArgument<int>() { Name = "i" };

            TestWriteLine w1 = new TestWriteLine("w1", "write1")
            {
                HintMessageList = { "write1", "write1", "write1" }
            };
            TestAssign<bool> a1 = new TestAssign<bool>
            {
                Value = true,
                ToVariable = cancelIt,
            };

            TestIf decide = new TestIf(HintThenOrElse.Else, HintThenOrElse.Else, HintThenOrElse.Then)
            {
                ConditionExpression = ((ctx) => i.Get(ctx) < 5),
                ThenActivity = a1,
                ElseActivity = w1,
            };

            TestParallelForEach<int> foreachAct = new TestParallelForEach<int>("foreach")
            {
                HintValues = new int[] { 1, 2, 3, 4, 5, 6 },
                ValuesExpression = (context => new int[] { 1, 2, 3, 4, 5, 6 }),
                CurrentVariable = i,
                HintIterationCount = 3,
                Body = decide,
                CompletionConditionVariable = cancelIt,
            };

            TestSequence sequence = new TestSequence
            {
                Activities = { foreachAct },
                Variables = { cancelIt },
            };

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Don't set Body.Handler like the following and it should not throw an exception.
        /// Don't set Body.Handler like the following and it should not throw an exception.
        /// ParallelForEach<string> act = new ParallelForEach<string>
        ///             {
        ///                 Values = new string[] { "a", "b" },
        ///                 Body = new ActivityAction<string> { }
        ///             };
        /// </summary>        
        [Fact]
        public void BodyHandlerIsNull()
        {
            TestParallelForEach<int> foreachAct = new TestParallelForEach<int>("foreach")
            {
                ValuesExpression = context => new int[] { 1, 2, 3 },
                HintIterationCount = 3
            };
            foreachAct.ProductParallelForEach.Body = new ActivityAction<int>();
            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        //CheckForDispose
        /// <summary>
        ///        static void DisposeTest()
        ///         {
        ///             ParallelForEach<int> f = new ParallelForEach<int>();
        ///             f.IndexVariable = new Variable<int>("x");
        ///             // f.IndexVariable = "x";
        ///             // f.Values = new InArgument<IEnumerable<int>>(Enumerable.Range(0, 100));
        ///             f.Values = new InArgument<IEnumerable<int>>(new MyRange(10));
        ///             // f.Values = new int[] { 1, 2, 3, 4 };
        ///             f.Body = new WriteLine<int>()
        ///                      {
        ///                          Input = new InArgument<int>(new Variable<int>("x"))
        ///                      };
        ///             f.Invoke();
        ///             Console.WriteLine("Done with invoke");
        ///             // the below code will dispose it
        ///             int c = Enumerable.Count(new MyRange(5));
        ///             Console.WriteLine("got " + c);
        ///         }
        ///         class MyRange : IEnumerable<int>, IEnumerator<int>
        ///         {
        ///             public IEnumerator<int> GetEnumerator()
        ///             {
        ///                 return this;
        ///             }
        ///             System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        ///             {
        ///                 throw new Exception("The method or operation is not implemented.");
        ///             }
        ///             readonly int max;
        ///             int x;
        ///             public MyRange(int max)
        ///             {
        ///                 this.max = max;
        ///                 x = -1;
        ///             }
        ///             public int Current
        ///             {
        ///                 get
        ///                 {
        ///                     return x;
        ///                 }
        ///             }
        ///             public void Dispose()
        ///             {
        ///                 Console.WriteLine("dispose called");
        ///             }
        ///             object System.Collections.IEnumerator.Current
        ///             {
        ///                 get
        ///                 {
        ///                     throw new Exception("The method or operation is not implemented.");
        ///                 }
        ///             }
        ///             public bool MoveNext()
        ///             {
        ///                 return (++x < max);
        ///             }
        ///             public void Reset()
        ///             {
        ///                 throw new Exception("The method or operation is not implemented.");
        ///             }
        ///         }
        /// </summary>        
        [Fact]
        public void CheckForDispose()
        {
            //TestCase.Current.Parameters.Add("DisableXamlRoundTrip", "true");
            MyRange range = new MyRange(5);
            DelegateInArgument<int> x = new DelegateInArgument<int>("x");
            TestParallelForEach<int> foreachAct = new TestParallelForEach<int>
            {
                CurrentVariable = x,
                ValuesExpression = context => range,
                HintValues = new int[] { 0, 1, 2, 3, 4 },
                Body = new TestWriteLine()
                {
                    MessageExpression = (env) => x.Get(env).ToString(),
                    HintMessageList = { "0", "1", "2", "3", "4" }
                },
                HintIterationCount = 5
            };
            TestRuntime.RunAndValidateWorkflow(foreachAct);

            // the below code will dispose it
            int c = Enumerable.Count(new MyRange(2));
            if (!range.disposeIsCalled)
                throw new TestCaseException("dispose is not called on Range!");
        }

        /// <summary>
        /// Implement IEnumerable and throw exception in this class
        /// </summary>        
        [Fact]
        public void ThrowExceptionInValues()
        {
            UnorderedTraces ordered = new UnorderedTraces()
            {
                Steps =
                {
                     new OrderedTraces()
                    {
                        Steps =
                        {
                            new ActivityTrace("w1", ActivityInstanceState.Executing),
                            new ActivityTrace("w1", ActivityInstanceState.Faulted),
                        }
                    },
                     new OrderedTraces()
                    {
                        Steps =
                        {
                            new ActivityTrace("w1", ActivityInstanceState.Executing),
                            new ActivityTrace("w1", ActivityInstanceState.Faulted),
                        }
                    },
                    new OrderedTraces()
                    {
                        Steps =
                        {
                            new ActivityTrace("w1", ActivityInstanceState.Executing),
                            new ActivityTrace("w1", ActivityInstanceState.Faulted),
                        }
                    }
                }
            };

            TestParallelForEach<int> foreachAct = new TestParallelForEach<int>("foreach")
            {
                ValuesExpression = context => new IEnumerableWithException { NumberOfIterations = 3 },
                Body = new TestWriteLine("w1") { Message = "w1" },
                ExpectedOutcome = Outcome.Faulted,
                ActivitySpecificTraces =
                {
                    ordered,
                }
            };

            TestRuntime.RunAndValidateAbortedException(foreachAct, typeof(TestCaseException), new Dictionary<string, string>());
        }

        /// <summary>
        /// ParallelForEach.CompletionCondition evaluates to true, when a child of Parallel overrides Cancel but does not call base.Cancel(context)
        /// </summary>        
        [Fact]
        public void ParallelForEachWithAChildThatOverridesCancelAndCompletionConditionIsTrue()
        {
            Variable<bool> cancelIt = new Variable<bool> { Name = "cancelIt", Default = false };
            DelegateInArgument<bool> arg = new DelegateInArgument<bool>("arg");

            TestParallelForEach<bool> pfeAct = new TestParallelForEach<bool>
            {
                DisplayName = "ParallelForEach1",
                HintIterationCount = 2,
                HintValues = new bool[] { true, false },
                ValuesExpression = (e => new bool[] { true, false }),
                CurrentVariable = arg,
                CompletionConditionVariable = cancelIt,
                Body = new TestIf(HintThenOrElse.Then, HintThenOrElse.Else)
                {
                    DisplayName = "If1",
                    ConditionExpression = e => arg.Get(e),
                    ThenActivity = new TestBlockingActivityWithWriteLineInCancel("writeLineInCancel", OutcomeState.Completed)
                    {
                        ExpectedOutcome = new Outcome(OutcomeState.Completed, OutcomeState.Canceled),
                    },
                    ElseActivity = new TestSequence
                    {
                        DisplayName = "Sequence2",
                        Activities =
                        {
                            new TestDelay("d1", new TimeSpan(1)),
                            new TestAssign<bool>{ DisplayName = "Assign1", Value = true, ToVariable = cancelIt}
                        }
                    }
                },
            };

            TestSequence root = new TestSequence
            {
                DisplayName = "Sequence1",
                Activities = { pfeAct },
                Variables = { cancelIt },
            };

            OrderedTraces ordered = new OrderedTraces()
            {
                Steps =
                {
                    new ActivityTrace("Sequence1", ActivityInstanceState.Executing),
                    new ActivityTrace("ParallelForEach1", ActivityInstanceState.Executing),
                    new UnorderedTraces
                    {
                        Steps =
                        {
                             new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("If1", ActivityInstanceState.Executing),
                                    new ActivityTrace("writeLineInCancel", ActivityInstanceState.Executing),
                                    new ActivityTrace("w1", ActivityInstanceState.Executing),
                                    new ActivityTrace("w1", ActivityInstanceState.Closed),
                                    new ActivityTrace("writeLineInCancel", ActivityInstanceState.Closed),
                                    new ActivityTrace("If1", ActivityInstanceState.Closed),
                                }
                            },
                             new OrderedTraces()
                            {
                                Steps =
                                {
                                    new ActivityTrace("If1", ActivityInstanceState.Executing),
                                    new ActivityTrace("Sequence2", ActivityInstanceState.Executing),
                                    new ActivityTrace("d1", ActivityInstanceState.Executing),
                                    new ActivityTrace("d1", ActivityInstanceState.Closed),
                                    new ActivityTrace("Assign1", ActivityInstanceState.Executing),
                                    new ActivityTrace("Assign1", ActivityInstanceState.Closed),
                                    new ActivityTrace("Sequence2", ActivityInstanceState.Closed),
                                    new ActivityTrace("If1", ActivityInstanceState.Closed),
                                    new ActivityTrace("VariableValue<Boolean>", ActivityInstanceState.Executing),
                                    new ActivityTrace("VariableValue<Boolean>", ActivityInstanceState.Closed),
                                }
                            }
                        }
                     },
                    new ActivityTrace("ParallelForEach1", ActivityInstanceState.Closed),
                    new ActivityTrace("Sequence1", ActivityInstanceState.Closed),
}
            };

            ExpectedTrace trace = new ExpectedTrace(ordered);
            trace.AddIgnoreTypes(typeof(SynchronizeTrace));
            trace.AddIgnoreTypes(typeof(BookmarkResumptionTrace));
            TestRuntime.RunAndValidateWorkflow(root, trace);
        }

        /// <summary>
        /// ParallelForEach.CompletionCondition evaluates to true, when a child of Parallel overrides Cancel but does not call base.Cancel(context)
        /// </summary>        
        [Fact(Skip = "Test cases not executed as part of suites and don't seem to pass on desktop. #72 - https://github.com/dotnet/wf/issues/72 - The aborted reason is NOT a TestCaseException, but it looks like the test framework is creating the exception")]
        public void ParallelForEachWithAChildThatThrowsInCancelWhileCompletionConditionIsTrue()
        {
            Variable<bool> cancelIt = new Variable<bool> { Name = "cancelIt", Default = false };
            DelegateInArgument<bool> arg = new DelegateInArgument<bool>("arg");

            TestParallelForEach<bool> pfeAct = new TestParallelForEach<bool>
            {
                HintIterationCount = 2,
                HintValues = new bool[] { true, false },
                ValuesExpression = (e => new bool[] { true, false }),
                CurrentVariable = arg,
                CompletionConditionVariable = cancelIt,
                Body = new TestIf(HintThenOrElse.Then, HintThenOrElse.Else)
                {
                    ConditionExpression = e => arg.Get(e),
                    ThenActivity = new TestBlockingActivityWithWriteLineInCancel("writeLineInCancel", OutcomeState.Faulted)
                    {
                        ExpectedOutcome = Outcome.UncaughtException(typeof(TestCaseException)),
                    },
                    ElseActivity = new TestSequence
                    {
                        Activities =
                        {
                            new TestDelay("d1", new TimeSpan(1)),
                            new TestAssign<bool>{ Value = true, ToVariable = cancelIt}
                        }
                    }
                }
            };

            TestSequence root = new TestSequence
            {
                Activities = { pfeAct },
                Variables = { cancelIt },
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(root))
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
    }

    public class IEnumerableWithException : IEnumerable<int>, IEnumerator<int>
    {
        public int NumberOfIterations { get; set; }
        public IEnumerator<int> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        public bool MoveNext()
        {
            if (NumberOfIterations < 1)
                throw new TestCaseException();
            else
                NumberOfIterations--;
            return true;
        }

        public int Current
        {
            get { return NumberOfIterations; }
        }

        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    public class MyRange : IEnumerable<int>, IEnumerator<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool disposeIsCalled = false;
        private readonly int _max;
        private int _x;
        public MyRange(int max)
        {
            _max = max;
            _x = -1;
        }

        public int Current
        {
            get
            {
                return _x;
            }
        }

        public void Dispose()
        {
            this.disposeIsCalled = true;
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public bool MoveNext()
        {
            _x++;
            return (_x < _max);
        }

        public void Reset()
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}


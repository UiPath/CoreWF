// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using Xunit;

namespace TestCases.Activities
{
    public class ForEach
    {
        /// <summary>
        /// Basic testing of DoWhile, While, ForEach, If
        /// ForEach with a few elements.
        /// </summary>        
        [Fact]
        public void BasicForEachTest()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerSeq");

            string[] strArray = new string[] { "var1", "var2", "var3" };
            TestForEach<string> foreachAct = new TestForEach<string>
            {
                Values = strArray,

                HintIterationCount = 3
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };

            foreachAct.Body = innerSequence;

            innerSequence.Activities.Add(writeLine);
            outerSequence.Activities.Add(foreachAct);

            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Foreach with the Values trying to be changed ( add, remove item) in the body of foreach - fail
        /// </summary>        
        [Fact]
        public void ForeachChangeTheValuesList()
        {
            List<string> strArray = new List<string>() { "var1", "var2", "var3" };

            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Activities =
                {
                    new TestForEach<string>()
                    {
                        ExpectedOutcome = Outcome.UncaughtException(typeof(Exception)),
                        Values = strArray,
                        HintIterationCount = 1,
                        Body = new TestSequence("innerseq")
                        {
                            Activities =
                            {
                                new TestInvokeMethod("add item", typeof(List<string>).GetMethod("Add"))
                                {
                                    TargetObject = new TestArgument<List<string>>(Direction.In, "TargetObject", context => strArray),
                                    Arguments =
                                    {
                                        new TestArgument<string>(Direction.In,"item", "Add Item"),
                                    }
                                },
                                new TestWriteLine("trace print")
                                {
                                    Message = "Can and only can print once"
                                }
                            }
                        }
                    }
                }
            };
            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(InvalidOperationException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Simple foreach test (set Values to a list to iterate) without anything in the body
        /// </summary>        
        [Fact]
        public void SimpleEmptyForeach()
        {
            //  Test case description:
            //  Simple foreach test (set Values to a list to iterate) without anything in the body
            string[] strArray = new string[] { "var1", "var2", "var3" };
            TestForEach<string> foreachAct = new TestForEach<string>
            {
                Values = strArray
            };

            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// Foreach with Values tryig to be set to a different list in the body of foreach
        /// </summary>        
        [Fact]
        public void ForeachSetTheValuesListToDifferentList()
        {
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innerseq");

            List<string> stringList = new List<string>() { "var1", "var2", "var3" };

            TestWriteLine writeLine = new TestWriteLine("Write Line")
            {
                Message = "Here we are!"
            };

            TestForEach<string> foreachAct = new TestForEach<string>()
            {
                Body = innerSequence,
                Values = stringList,
                HintIterationCount = 1,
                ExpectedOutcome = Outcome.UncaughtException()
            };

            Variable<List<string>> foreachList = new Variable<List<string>>("foreachList", context => stringList);

            BindingFlags flags = /*BindingFlags.InvokeMethod |*/ BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            TestInvokeMethod methodInvokeAct = new TestInvokeMethod("methodinvoke", this.GetType().GetMethod("ChangeListToNewList", flags))
            {
                TargetObject = new TestArgument<ForEach>(Direction.In, "TargetObject", context => new ForEach())
            };
            methodInvokeAct.Arguments.Add(new TestArgument<List<string>>(Direction.InOut, "listOfStrings", foreachList));

            innerSequence.Activities.Add(methodInvokeAct);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(foreachList);
            outerSequence.Activities.Add(foreachAct);

            string error = GetCollectionCanNotBeChangedErrorString();

            Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
            exceptionProperty.Add("Message", error);

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(System.InvalidOperationException), exceptionProperty);
        }

        private string GetCollectionCanNotBeChangedErrorString()
        {
            string message = "";

            try
            {
                List<string> list = new List<string> { "s" };

                foreach (string l in list)
                {
                    list.Add("a");
                }
            }
            catch (Exception e)
            {
                message = e.Message;
            }

            return message;
        }

        /// <summary>
        /// Foreach with Values tryig to be set to a different list in the body of foreach
        /// </summary>        
        [Fact]
        public void ForeachSetTheValuesListToNull()
        {
            //  Test case description:
            //  Foreach with Values trying to be set to null

            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("innersequence");

            List<string> strArray = new List<string>() { "var1", "var2", "var3" };

            TestWriteLine writeLine = new TestWriteLine("Write Line");

            TestForEach<string> foreachAct = new TestForEach<string>()
            {
                Body = innerSequence,
                Values = strArray,
                HintIterationCount = 1,
                ExpectedOutcome = Outcome.UncaughtException()
            };

            Variable<List<string>> foreachList = new Variable<List<string>>("foreachList", context => strArray);

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.InvokeMethod*/;
            TestInvokeMethod methodInvokeAct = new TestInvokeMethod("methodinv", this.GetType().GetMethod("ChangeListToNull", flags));
            methodInvokeAct.Arguments.Add(new TestArgument<List<string>>(Direction.In, "listOfStrings", foreachList));
            methodInvokeAct.TargetObject = new TestArgument<ForEach>(Direction.In, "TargetObject", context => new ForEach());

            writeLine.Message = "Here we are!";

            innerSequence.Activities.Add(methodInvokeAct);
            innerSequence.Activities.Add(writeLine);
            outerSequence.Variables.Add(foreachList);
            outerSequence.Activities.Add(foreachAct);

            string error = GetCollectionCanNotBeChangedErrorString();

            Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
            exceptionProperty.Add("Message", error);

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(System.InvalidOperationException), exceptionProperty);
        }

        /// <summary>
        /// List of values null in the beginning
        /// </summary>        
        [Fact]
        public void NullListOfValuesForEach()
        {
            //  Test case description:
            //  List of values null in the beginning

            TestSequence outerSequence = new TestSequence("sequence1");
            TestWriteLine innerWriteLine = new TestWriteLine("BodyWriteLine", "BodyWriteLine");

            List<string> strArray = null;

            TestForEach<string> foreachAct = new TestForEach<string>()
            {
                ExpectedOutcome = Outcome.UncaughtException(typeof(System.InvalidOperationException)),
                Body = innerWriteLine,
                Values = strArray,
                HintIterationCount = -1,
            };
            outerSequence.Activities.Add(foreachAct);

            Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
            exceptionProperty.Add("Message", string.Format(ErrorStrings.ForEachRequiresNonNullValues, foreachAct.DisplayName));

            TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(System.InvalidOperationException), exceptionProperty);
        }

        /// <summary>
        /// List of values empty in the beginning
        /// </summary>        
        [Fact]
        public void EmptyListOfValuesForEach()
        {
            //  Test case description:
            //  List of values empty in the beginning
            List<string> strArray = new List<string>();
            TestForEach<string> foreachAct = new TestForEach<string>()
            {
                Body = new TestWriteLine("foreachSeq", "I'm a writeLine activity"),
                Values = strArray,
                HintIterationCount = -1,
            };
            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// Linked list iteration
        /// List of values empty in the beginning
        /// </summary>        
        [Fact]
        public void LinkedListIterationForEach()
        {
            //  Test case description:
            //  linked list iteration
            List<string> strArray = new List<string>();
            List<string> strArray2 = new List<string>();
            List<string> strArray3 = new List<string>();
            List<string> strArray4 = new List<string>();
            List<string> strArray5 = new List<string>();
            LinkedList<List<string>> linkedList = new LinkedList<List<string>>();
            linkedList.AddFirst(strArray);
            linkedList.AddAfter(linkedList.Find(strArray), strArray2);
            linkedList.AddAfter(linkedList.Find(strArray2), strArray3);
            linkedList.AddAfter(linkedList.Find(strArray3), strArray4);
            linkedList.AddAfter(linkedList.Find(strArray4), strArray5);

            TestForEach<List<string>> foreachAct = new TestForEach<List<string>>
            {
                Body = new TestWriteLine("writeLine in foreach", "I'm a writeLine activity"),
                Values = linkedList,
                HintIterationCount = linkedList.Count,
            };
            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// stack iteration
        /// </summary>        
        [Fact]
        public void StackIterationForEach()
        {
            //  Test case description:
            //  stack iteration

            Stack<List<string>> stackOfLists = new Stack<List<string>>();
            List<string> strArray = new List<string>();
            List<string> strArray2 = new List<string>();
            List<string> strArray3 = new List<string>();
            List<string> strArray4 = new List<string>();
            List<string> strArray5 = new List<string>();
            stackOfLists.Push(strArray);
            stackOfLists.Push(strArray2);
            stackOfLists.Push(strArray3);
            stackOfLists.Push(strArray4);
            stackOfLists.Push(strArray5);

            TestForEach<List<string>> foreachAct = new TestForEach<List<string>>
            {
                Body = new TestWriteLine("WriteLine in foreach", "Funny") { HintMessageList = { "Funny", "Funny", "Funny", "Funny", "Funny" } },
                Values = stackOfLists,
                HintIterationCount = stackOfLists.Count,
            };
            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// queue iteration
        /// </summary>        
        [Fact]
        public void QueueIterationForEach()
        {
            //  Test case description:
            //  queue iteration

            Queue<List<string>> queueOfLists = new Queue<List<string>>();
            List<string> strArray = new List<string>();
            List<string> strArray2 = new List<string>();
            List<string> strArray3 = new List<string>();
            List<string> strArray4 = new List<string>();
            List<string> strArray5 = new List<string>();
            queueOfLists.Enqueue(strArray);
            queueOfLists.Enqueue(strArray2);
            queueOfLists.Enqueue(strArray3);
            queueOfLists.Enqueue(strArray4);
            queueOfLists.Enqueue(strArray5);

            TestForEach<List<string>> foreachAct = new TestForEach<List<string>>
            {
                Body = new TestWriteLine("WriteLine in foreach", "Funny") { HintMessageList = { "Funny", "Funny", "Funny", "Funny", "Funny" } },
                Values = queueOfLists,
                HintIterationCount = queueOfLists.Count,
            };
            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// sort the list in the foreach values
        /// </summary>        
        [Fact]
        public void SortValuesListWhileExecutingForEach()
        {
            //  Test case description:
            //  custom list with custom enumeration
            List<string> strArray = new List<string>() { "zoo", "boo", "awuu" };

            Type t = typeof(ForEach);

            Variable<List<string>> values = new Variable<List<string>>("values", context => strArray);

            Variable<string[]> valuesArray = new Variable<string[]>("valuesArray", context => new string[3]);

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public /*| BindingFlags.InvokeMethod*/;
            TestInvokeMethod toArray = new TestInvokeMethod("invoke act", t.GetMethod("SortList", flags));
            toArray.Arguments.Add(new TestArgument<List<string>>(Direction.In, "listOfStrings", values));
            toArray.SetResultVariable<string[]>(valuesArray);
            toArray.TargetObject = new TestArgument<ForEach>(Direction.In, "TargetObject", context => new ForEach());

            TestSequence rootSequence = new TestSequence("rootSequence")
            {
                Variables =
                {
                    values,
                    valuesArray
                },
                Activities =
                {
                    new TestForEach<string>
                    {
                        ExpectedOutcome = Outcome.UncaughtException(typeof(InvalidOperationException)),
                        HintIterationCount = 1,
                        Values = strArray,
                        Body =  new TestSequence("foreach sequence act")
                        {
                                Activities =
                                {
                                    toArray,
                                    new TestIf("ifAct" ,HintThenOrElse.Then)
                                    {
                                        ConditionExpression = ((env) => ((string[])valuesArray.Get(env))[0] == "awuu"),
                                        ThenActivity = new TestSequence("if seq"),
                                    }
                                },
                        },
                    }
                }
            };

            TestRuntime.RunAndValidateAbortedException(rootSequence, typeof(InvalidOperationException), new Dictionary<string, string>());
        }

        // These tests use VB expressions
        ///// <summary>
        ///// custom list with custom enumeration
        ///// </summary>        
        //[Fact]
        //public void CustomEnumerableCollectionWithCustomEnumeration()
        //{
        //    //  Test case description:
        //    //  custom list with custom enumeration
        //    Type t = typeof(ForEach);


        //    Variable<CustomEnumerable<int>> values = new Variable<CustomEnumerable<int>>
        //    {
        //        Name = "values",
        //        Default = new VisualBasicValue<CustomEnumerable<int>>("New CustomEnumerable(Of Integer) From {100, 90, 0, -10}"),
        //    };
        //    Variable<int[]> valuesArray = new Variable<int[]>
        //    {
        //        Name = "valuesArray",
        //        Default = new VisualBasicValue<int[]>("New Integer() {1, 2, 3}")
        //    };
        //    System.Activities.Statements.ForEach<int> a = new System.Activities.Statements.ForEach<int>
        //    {
        //        Values = new VisualBasicValue<IEnumerable<int>>("values"),
        //    };

        //    BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod;
        //    TestInvokeMethod toArray = new TestInvokeMethod("invokeAct", t.GetMethod("ChangeListToArray", flags));
        //    toArray.TargetObject = new TestArgument<ForEach>(Direction.In, "TargetObject", context => new ForEach());
        //    toArray.Arguments.Add(new TestArgument<CustomEnumerable<int>>(Direction.In, "listOfInts", values));
        //    toArray.SetResultVariable<int[]>(valuesArray);

        //    TestSequence rootSequence = new TestSequence("rootSequence")
        //    {
        //        Variables =
        //        {
        //            values,
        //            valuesArray
        //        },
        //        Activities =
        //        {
        //            new TestForEach<int>
        //            {
        //                HintIterationCount = 4,
        //                ValuesActivity = new TestVisualBasicValue<IEnumerable<int>>("values"),
        //                Body =  new TestSequence("foreach sequence act")
        //                {
        //                        Activities =
        //                        {
        //                            toArray,
        //                            new TestIf("ifAct", HintThenOrElse.Then)
        //                            {
        //                                ConditionExpression = ((env) => ((int[])valuesArray.Get(env))[0] < ((int[])valuesArray.Get(env))[1]),
        //                                ThenActivity = new TestWriteLine("if writeLine", "I'm a writeLine activity"),
        //                            }
        //                        }
        //                }
        //            }
        //        }
        //    };

        //    VisualBasicUtility.AttachVisualBasicSettingsProperty(rootSequence.ProductActivity, new List<Type> { typeof(CustomEnumerable<int>) });
        //    TestRuntime.RunAndValidateWorkflow(rootSequence);
        //}

        ///// <summary>
        ///// Foreach in other loop activities.
        ///// </summary>        
        //[Fact]
        //public void ForeachNestedWithinOtherLoops()
        //{
        //    //  Test case description:
        //    //  Foreach in other loop activities. 

        //    TestSequence outerSequence = new TestSequence("sequence1");
        //    TestSequence innerSequence = new TestSequence("innerSeq");
        //    TestAssign<int> increment = new TestAssign<int>("Increment Counter");

        //    Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);
        //    Variable<IEnumerable<string>> strArray = new Variable<IEnumerable<string>>
        //    {
        //        Name = "strArray",
        //        Default = new VisualBasicValue<IEnumerable<string>>("New List(Of String) From {\"var1\",\"var2\", \"var3\"}")
        //    };
        //    VariableHelper.CreateInitialized<int>("counter", 0);

        //    increment.ToVariable = counter;
        //    increment.ValueExpression = ((env) => ((int)counter.Get(env)) + 1);

        //    TestForEach<string> foreachAct = new TestForEach<string>
        //    {
        //        Body = innerSequence,
        //        ValuesVariableT = strArray,
        //        HintIterationCount = 3,
        //    };

        //    TestDoWhile doWhile = new TestDoWhile("dowhile")
        //    {
        //        ConditionExpression = ((env) => ((int)counter.Get(env)) < 9),
        //        Body = foreachAct,
        //        HintIterationCount = 3,
        //    };

        //    innerSequence.Activities.Add(increment);
        //    outerSequence.Activities.Add(doWhile);
        //    outerSequence.Variables.Add(counter);
        //    outerSequence.Variables.Add(strArray);
        //    TestRuntime.RunAndValidateWorkflow(outerSequence);
        //}

        /// <summary>
        /// Foreach in other loop activities.
        /// </summary>        
        [Fact]
        public void ForeachNestedWithinOtherLoops2()
        {
            //  Test case description:
            //  Foreach in other loop activities. 
            TestSequence outerSequence = new TestSequence("sequence1");
            TestSequence innerSequence = new TestSequence("inner sequence");
            TestAssign<int> increment = new TestAssign<int>("Increment counter");
            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            increment.ToVariable = counter;
            increment.ValueExpression = ((env) => ((int)counter.Get(env)) + 1);

            List<string> strArray = new List<string>() { "var1", "var2", "var3" };

            TestForEach<string> foreachAct = new TestForEach<string>
            {
                Body = innerSequence,
                Values = strArray,
                HintIterationCount = strArray.Count,
            };

            TestWhile whileAct = new TestWhile("while act")
            {
                ConditionExpression = ((env) => ((int)counter.Get(env)) < 9),
                Body = foreachAct,
                HintIterationCount = 3,
            };

            innerSequence.Activities.Add(increment);
            outerSequence.Activities.Add(whileAct);
            outerSequence.Variables.Add(counter);
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            TestForEach<string> foreachAct = new TestForEach<string>
            {
                Body = new TestWriteLine("write hello A")
                {
                    Message = "Its a big world after all",
                }
            };

            WorkflowInspectionServices.GetActivities(foreachAct.ProductActivity);

            foreachAct.Values = new string[] { "var1", "var2", "var3" };
            // Reset Body property, not only Body.Handler
            foreachAct.Body = null;
            foreachAct.HintIterationCount = 3;


            foreachAct.Body = new TestWriteLine("write hello B")
            {
                Message = "Its a small world after all",
            };

            // Now that we've changed the tree we'll explicitly recache
            WorkflowInspectionServices.CacheMetadata(foreachAct.ProductActivity);

            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        //[Fact]
        //public void ThrowExceptionInForeach()
        //{
        //    //  Test case description:
        //    //  Throw exception in foreach

        //    List<string> strArray = new List<string>() { "var1", "var2", "var3" };

        //    TestSequence outerSequence = new TestSequence("sequence1")
        //    {
        //        Activities =
        //        {
        //            new TestForEach<string>
        //            {
        //                Values = strArray,
        //                HintIterationCount = 1,
        //                Body = new TestSequence("innerseq")
        //                {
        //                        Activities =
        //                        {
        //                            new TestWriteLine("will execute")
        //                            {
        //                                Message = "i will survive"
        //                            },
        //                            new TestThrow<CustomAttributeFormatException>
        //                            {
        //                                ExceptionExpression = context => new CustomAttributeFormatException("i am a context marhsal exception")
        //                            },
        //                            new TestWriteLine("wont execute")
        //                            {
        //                                Message = "i wont survive"
        //                            }
        //                        }
        //                }
        //            }
        //        }
        //    };
        //    TestRuntime.RunAndValidateAbortedException(outerSequence, typeof(CustomAttributeFormatException), new Dictionary<string, string>());

        //}

        [Fact]
        public void SimpleForeach()
        {
            //  Test case description:
            //  Simple foreach test (set Values to a list to iterate) with other activities in the body
            LinkedList<string> linkedList = new LinkedList<string>();
            linkedList.AddFirst("first");
            linkedList.AddAfter(linkedList.First, new LinkedListNode<string>("second"));
            linkedList.AddLast("last");

            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Activities =
                {
                    new TestForEach<string>
                    {
                        Values = linkedList,
                        HintIterationCount = 3,
                        Body = new TestSequence("innerseq")
                        {
                                Activities =
                                {
                                    new TestWriteLine("will execute")
                                    {
                                        Message = "i will survive"
                                    },
                                    new TestWriteLine("second writeline")
                                    {
                                        Message = "i am a survivor as well"
                                    }
                                }
                        }
                    }
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        [Fact]
        public void ForEachWithSortedDictionary()
        {
            //  Test case description:
            //  Simple foreach test (set Values to a list to iterate) with other activities in the body
            SortedDictionary<int, string> sortedDict = new SortedDictionary<int, string>();
            sortedDict.Add(12, "1212");
            sortedDict.Add(1212, "12121212");
            sortedDict.Add(-123, "sdfsdf");


            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Activities =
                {
                    new TestForEach<KeyValuePair<int, string>>
                    {
                        Values = sortedDict,
                        HintIterationCount = 3,
                        Body = new TestSequence("innerseq")
                        {
                                Activities =
                                {
                                    new TestWriteLine("will execute")
                                    {
                                        Message = "i will survive"
                                    },
                                    new TestWriteLine("second writeline")
                                    {
                                        Message = "i am a survivor as well"
                                    }
                                }
                        }
                    }
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        [Fact]
        public void ForEachWithSynchCollection()
        {
            //  Test case description:
            //  Simple foreach test (set Values to a list to iterate) with other activities in the body
            ConcurrentBag<Exception> synchcoll = new ConcurrentBag<Exception>();
            synchcoll.Add(new RankException("rank exception"));
            synchcoll.Add(new Exception("base exception"));
            synchcoll.Add(new OperationCanceledException("operation cancelled"));

            DelegateInArgument<Exception> currentVar = new DelegateInArgument<Exception>() { Name = "currentVar" };
            Variable<int> counter = VariableHelper.Create<int>("counter");
            counter.Default = 0;
            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Variables =
                {
                    counter
                },
                Activities =
                {
                    new TestForEach<Exception>
                    {
                        Values = synchcoll,
                        CurrentVariable = currentVar,
                        HintIterationCount = 3,
                        Body = new TestSequence("innerseq")
                        {
                                Activities =
                                {
                                    new TestWriteLine("will execute")
                                    {
                                        Message = "i will survive"
                                    },
                                    new TestIncrement()
                                    {
                                        CounterVariable = counter,
                                        IncrementCount= 1,
                                    },
                                    new TestIf(HintThenOrElse.Then,HintThenOrElse.Neither,HintThenOrElse.Neither)
                                    {
                                        ConditionExpression = ((env) => ((int)counter.Get(env)) == 1),
                                        ThenActivity = new TestWriteLine()
                                        {
                                            Message = "first iteration"
                                        },
                                    },
                                    new TestIf(HintThenOrElse.Neither,HintThenOrElse.Then,HintThenOrElse.Neither)
                                    {
                                        ConditionExpression = ((env) => ((int)counter.Get(env)) == 2),
                                        ThenActivity = new TestWriteLine()
                                        {
                                            Message = "second iteration"
                                        },
                                    },
                                    new TestIf(HintThenOrElse.Neither,HintThenOrElse.Neither,HintThenOrElse.Then)
                                    {
                                        ConditionExpression = ((env) => ((int)counter.Get(env)) == 3),
                                        ThenActivity = new TestWriteLine()
                                        {
                                            Message = "third iteration"
                                        },
                                    },
                                    new TestWriteLine("second writeline")
                                    {
                                        Message = "i am a survivor as well"
                                    }
                                }
                        }
                    },
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        [Fact]
        public void ForEachWithDictionary()
        {
            //  Test case description:
            //  Simple foreach test (set Values to a list to iterate) with other activities in the body
            Dictionary<double, object> dict = new Dictionary<double, object>();
            dict.Add(12.2, "first");
            dict.Add(12.12, "second");
            dict.Add(-1.23, "last");


            TestSequence outerSequence = new TestSequence("sequence1")
            {
                Activities =
                {
                    new TestForEach<KeyValuePair<double, object>>
                    {
                        Values = dict,
                        HintIterationCount = 3,
                        Body = new TestSequence("innerseq")
                        {
                                Activities =
                                {
                                    new TestWriteLine("will execute")
                                    {
                                        Message = "i will survive"
                                    },
                                    new TestWriteLine("second writeline")
                                    {
                                        Message = "i am a survivor as well"
                                    }
                                }
                        }
                    }
                }
            };
            TestRuntime.RunAndValidateWorkflow(outerSequence);
        }

        public void ChangeListToNewList(ref List<string> listOfStrings)
        {
            listOfStrings.Add("var4");
        }

        public void ChangeListToNull(List<string> listOfStrings)
        {
            listOfStrings.Clear();
        }

        public string[] SortList(List<string> listOfStrings)
        {
            listOfStrings.Sort();
            return listOfStrings.ToArray();
        }

        public int[] ChangeListToArray(CustomEnumerable<int> listOfInts)
        {
            return listOfInts.ToArray();
        }

        /// <summary>
        /// ForeachContainingOtherLoops
        /// </summary>        
        [Fact]
        public void ForeachContainingOtherLoops()
        {
            List<string> strArray = new List<string>() { "var1", "var2", "var3" };
            DelegateInArgument<string> temp = new DelegateInArgument<string>() { Name = "temp" };

            Variable<int> counter = VariableHelper.CreateInitialized<int>("counter", 0);

            TestForEach<string> forEachSeq = new TestForEach<string>()
            {
                Values = strArray,
                CurrentVariable = temp,

                Body = new TestSequence("Body of For Each")
                {
                    Activities =
                    {
                        new TestWhile("While in body of for each")
                        {
                            Variables =
                            {
                                counter
                            },

                            ConditionExpression = (env) => (int) counter.Get(env) < 1,

                            Body = new TestSequence("Body of While Activity")
                            {
                                Activities =
                                {
                                    new TestWriteLine("Writeline in While")
                                    {
                                        MessageExpression = (env) => temp.Get(env).ToString(),
                                        HintMessageList = { "var1", "var2", "var3" }
                                    },

                                    new TestAssign<int>("Assign in While")
                                    {
                                        ToVariable = counter,
                                        ValueExpression = (env) => (int) counter.Get(env) + 1
                                    }
                                }
                            },

                            HintIterationCount = 1
                        },
}
                },

                HintIterationCount = 3
            };

            TestRuntime.RunAndValidateWorkflow(forEachSeq);
        }

        /// <summary>
        /// Cancel foreach
        /// </summary>        
        [Fact]
        public void ForeachCancelled()
        {
            List<string> vars = new List<string>() { "Hi", "There" };
            DelegateInArgument<string> temp = new DelegateInArgument<string>() { Name = "temp" };

            TestForEach<string> forEachAct = new TestForEach<string>()
            {
                Values = vars,
                CurrentVariable = temp,
                Body = new TestSequence("Body of for each")
                {
                    Activities =
                    {
                        new TestWriteLine("Writeline in for each")
                        {
                            MessageExpression = (env) => temp.Get(env).ToString(),
                            HintMessageList = {"Hi", "There"}
                        },

                        new TestIf("Test If", HintThenOrElse.Else, HintThenOrElse.Then)
                        {
                            ConditionExpression = (env) => (bool)(temp.Get(env).Equals("There")),

                            ThenActivity =  new TestBlockingActivity("BlockingActivity", "Bookmark")
                            {
                                ExpectedOutcome = Outcome.Canceled
                            },

                            ElseActivity = new TestWriteLine("Writeline in Else")
                            {
                                MessageExpression = (env) => temp.Get(env).ToString(),
                                HintMessage = "Hi"
                            }
                        }
                    }
                },

                HintIterationCount = 2
            };

            using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(forEachAct))
            {
                testWorkflowRuntime.ExecuteWorkflow();
                testWorkflowRuntime.WaitForActivityStatusChange("BlockingActivity", TestActivityInstanceState.Executing);
                testWorkflowRuntime.CancelWorkflow();
                testWorkflowRuntime.WaitForCanceled();
            }
        }

        /// <summary>
        /// Try to change the value of current item (IndexVariable) in the foreach body
        /// Try to change the value of "current item" (IndexVariable) in the foreach body
        /// </summary>        
        [Fact]
        public void ChangeTheCurrentItemValueInForeach()
        {
            List<string> vars = new List<string>() { "Hello", "How", "How", "How" };

            DelegateInArgument<string> assign = new DelegateInArgument<string>() { Name = "assign" };

            TestForEach<string> foreachAct = new TestForEach<string>()
            {
                HintIterationCount = 4,
                CurrentVariable = assign,
                Values = vars,
                Body = new TestSequence("Sequence in For Each")
                {
                    Activities =
                    {
                        new TestIf("If Activity", HintThenOrElse.Then, HintThenOrElse.Else, HintThenOrElse.Else, HintThenOrElse.Else)
                        {
                            ConditionExpression = (env) => ((bool)assign.Get(env).Equals("Hello")),

                            ThenActivity =  new TestSequence("Then Activity")
                            {
                                Activities =
                                {
                                    new TestAssign<string>("assign in then")
                                    {
                                        ToExpression = context => assign.Get(context),
                                        Value = "Hi"
                                    },

                                    new TestWriteLine("Writeline in then")
                                    {
                                        MessageExpression = (env) => assign.Get(env).ToString(),
                                        HintMessage = "Hi"
                                    },
                                }
                            },

                            ElseActivity =  new TestWriteLine("Write line in else")
                            {
                                MessageExpression = (env) => (string) assign.Get(env).ToString(),
                                HintMessage = "How"
                            }
                        },
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(foreachAct);
        }

        /// <summary>
        /// Run ForEachNG using WorkflowInvoker
        /// </summary>        
        [Fact]
        public void ForEachWithWorkFlowInvoker()
        {
            string[] strArray = new string[] { "var1", "var2", "var3" };
            TestForEach<string> foreachAct = new TestForEach<string>
            {
                HintIterationCount = 3
            };

            TestWriteLine writeLine = new TestWriteLine("write hello")
            {
                Message = "Its a small world after all"
            };

            foreachAct.Body = writeLine;

            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("Values", strArray);
            TestRuntime.RunAndValidateUsingWorkflowInvoker(foreachAct, dic, null, null);
        }

        /// <summary>
        /// Foreach test with persistence. After persistence happens the loop should continue from the last point it left and number of iterations should not change.
        /// ForeachWithPersistence
        /// </summary>        
        [Fact]
        public void ForeachWithPersistence()
        {
            int[] intArray = new int[] { 1, 2, 3 };
            DelegateInArgument<int> arg = new DelegateInArgument<int>("arg");

            TestSwitch<int> switchAct = new TestSwitch<int>
            {
                ExpressionExpression = (env) => arg.Get(env),
                Hints = { 0, 1, 2 }
            };

            switchAct.AddCase(1, new TestWriteLine("W1") { Message = "case 1" });
            switchAct.AddCase(2, new TestBlockingActivity("BookMark1"));
            switchAct.AddCase(3, new TestWriteLine("W3") { Message = "case 3" });

            TestForEach<int> foreachAct = new TestForEach<int>
            {
                HintIterationCount = 3,
                Body = switchAct,
                CurrentVariable = arg,
                Values = intArray
            };

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(foreachAct, null, jsonStore, PersistableIdleAction.None))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForIdle();
                runtime.PersistWorkflow();
                runtime.ResumeBookMark("BookMark1", null);
                runtime.WaitForCompletion();
            }
        }

        /// <summary>
        /// ForEach with Values property evaluated to null, expects runtime error 
        /// ForEachValuesEvaluatedToNull
        /// </summary>        
        [Fact]
        public void ForEachValuesEvaluatedToNull()
        {
            Variable<IEnumerable<string>> vals = new Variable<IEnumerable<string>> { Name = "vals", Default = null };
            TestForEach<string> foreachAct = new TestForEach<string>
            {
                Body = new TestWriteLine("W1") { Message = "W1" },
                ValuesVariableT = vals,
                HintIterationCount = -1,
                ExpectedOutcome = Outcome.UncaughtException(typeof(InvalidOperationException))
            };

            TestSequence seq = new TestSequence("RootSequence")
            {
                Variables = { vals },
                Activities =
                {
                    foreachAct
                }
            };

            Dictionary<string, string> exceptionProperty = new Dictionary<string, string>();
            exceptionProperty.Add("Message", string.Format(ErrorStrings.ForEachRequiresNonNullValues, foreachAct.DisplayName));
            TestRuntime.RunAndValidateAbortedException(seq, typeof(System.InvalidOperationException), exceptionProperty);
        }

        // Test uses VB expressions
        //[Fact]
        //public void DifferentArguments()
        //{
        //    //Testing Different argument types for ForEach.Values
        //    // DelegateInArgument
        //    // DelegateOutArgument
        //    // Activity<T>
        //    // Activity<T>
        //    // Variable<T> , Expression is already implemented.

        //    DelegateInArgument<IEnumerable<int>> delegateInArgument = new DelegateInArgument<IEnumerable<int>>("Input");
        //    DelegateOutArgument<IEnumerable<int>> delegateOutArgument = new DelegateOutArgument<IEnumerable<int>>("Output");

        //    TestCustomActivity<InvokeFunc<IEnumerable<int>, IEnumerable<int>>> invokeFunc = TestCustomActivity<InvokeFunc<IEnumerable<int>, IEnumerable<int>>>.CreateFromProduct(
        //           new InvokeFunc<IEnumerable<int>, IEnumerable<int>>
        //           {
        //               Argument = new VisualBasicValue<IEnumerable<int>>("New Integer() {1, 2}"),
        //               Func = new ActivityFunc<IEnumerable<int>, IEnumerable<int>>
        //               {
        //                   Argument = delegateInArgument,
        //                   Result = delegateOutArgument,
        //                   Handler = new System.Activities.Statements.Sequence
        //                   {
        //                       DisplayName = "Sequence1",
        //                       Activities =
        //                        {
        //                            new System.Activities.Statements.ForEach<int>
        //                            {
        //                                DisplayName = "ForEach1",
        //                                Values = delegateInArgument,
        //                                Body = new ActivityAction<int>
        //                                {
        //                                    Argument = new DelegateInArgument<int>("arg1"),
        //                                    Handler =  new System.Activities.Statements.WriteLine{DisplayName = "W1", Text = new InArgument<string>( new VisualBasicValue<string>("arg1 & \"\" ") ) },
        //                                }
        //                            },
        //                            new System.Activities.Statements.Assign<IEnumerable<int>>
        //                            {
        //                                DisplayName = "Assign1",
        //                                Value = delegateInArgument,
        //                                To = delegateOutArgument,
        //                            },
        //                            new System.Activities.Statements.ForEach<int>
        //                            {
        //                                DisplayName = "ForEach2",
        //                                Values = delegateOutArgument,
        //                                Body = new ActivityAction<int>
        //                                {
        //                                    Argument = new DelegateInArgument<int>("arg2"),
        //                                    Handler =  new System.Activities.Statements.WriteLine{ DisplayName = "W2", Text = new InArgument<string>( new VisualBasicValue<string>("arg2 & \"\" ") ) },
        //                                }
        //                            }
        //                        }
        //                   }
        //               }
        //           }
        //       );

        //    TestSequence sequenceForTracing = new TestSequence
        //    {
        //        DisplayName = "Sequence1",
        //        Activities =
        //        {
        //            new TestForEach<int>
        //            {
        //                DisplayName = "ForEach1",
        //                HintIterationCount = 2,
        //                Body = new TestSequence("W1"),
        //                Values = new int[] {1, 2},
        //            },
        //            new TestAssign<IEnumerable<int>>
        //            {
        //                DisplayName="Assign1"
        //            },
        //            new TestForEach<int>
        //            {
        //                DisplayName = "ForEach2",
        //                HintIterationCount = 2,
        //                Body = new TestSequence("W2"),
        //                Values = new int[] {1, 2},
        //            }
        //        }
        //    };
        //    invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);


        //    TestForEach<int> root = new TestForEach<int>
        //    {
        //        CurrentVariable = new DelegateInArgument<int>("arg3"),
        //        Body = new TestWriteLine
        //        {
        //            MessageActivity = new TestVisualBasicValue<string>("arg3 & \"\" "),
        //            HintMessageList = { "1", "2" }
        //        },
        //        ValuesActivity = invokeFunc,
        //        HintIterationCount = 2,
        //    };

        //    TestRuntime.RunAndValidateWorkflow(root);
        //}
    }

    public class CustomEnumerable<T> : List<T>, IEnumerable<T>
    {
        #region IEnumerable<int> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            this.Sort();
            return base.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            this.Sort();
            return this.GetEnumerator();
        }
        #endregion
    }
}

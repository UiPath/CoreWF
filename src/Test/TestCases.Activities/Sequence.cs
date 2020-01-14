// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities
{
    public class Sequence
    {
        /// <summary>
        /// BasicSequenceNoVariables
        /// </summary>        
        [Fact]
        public void BasicSequenceNoVariables()
        {
            //  Test case description:
            //  Sequence activity without any variables

            TestSequence sequence = new TestSequence("Sequence1");

            TestSequence sequence2 = new TestSequence("InnerSequence1");
            TestSequence sequence3 = new TestSequence("InnerSequence2");
            TestSequence sequence4 = new TestSequence("InnerSequence3");
            TestSequence sequence5 = new TestSequence("InnerSequence4");

            TestSequence sequence6 = new TestSequence("seq5");
            TestSequence sequence7 = new TestSequence("seq6");
            TestSequence sequence8 = new TestSequence("seq7");
            TestSequence sequence9 = new TestSequence("seq8");

            TestWriteLine writeLine1 = new TestWriteLine("Hello One")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence2.DisplayName, sequence6.DisplayName)
            };
            TestWriteLine writeLine2 = new TestWriteLine("Hello Two")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence3.DisplayName, sequence7.DisplayName)
            };
            TestWriteLine writeLine3 = new TestWriteLine("Hello Three")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence4.DisplayName, sequence8.DisplayName)
            };
            TestWriteLine writeLine4 = new TestWriteLine("Hello Four")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence5.DisplayName, sequence9.DisplayName)
            };

            sequence6.Activities.Add(writeLine1);
            sequence7.Activities.Add(writeLine2);
            sequence8.Activities.Add(writeLine3);
            sequence9.Activities.Add(writeLine4);
            sequence2.Activities.Add(sequence6);
            sequence3.Activities.Add(sequence7);
            sequence4.Activities.Add(sequence8);
            sequence5.Activities.Add(sequence9);
            sequence.Activities.Add(sequence2);
            sequence.Activities.Add(sequence3);
            sequence.Activities.Add(sequence4);
            sequence.Activities.Add(sequence5);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// TestSequenceProperties
        /// </summary>        
        [Fact]
        public void DisplayNameNullOrEmpty()
        {
            //  Test case description:
            //  (General test case information that is valid for all the following activities)Test the meta-data
            //  properties for all activities that may contain meta-data properties:   1. if activity has a DisplayName
            //  , test when the name is   too long( if there is a limit on the name? if not robustness test),   have
            //  weird characters using random string generators and several punctuations,   have dots in it, multiple
            //  dots following each other,   set to null,   set to empty  add multiple spaces.     2. if activty has an
            //  integer property, test the max value, min value, max+1,    3. if activity has a list, have an empty
            //  list, null list, perform other list operations on it   ��?will be added more cases when meta-data
            //  properties will be finalized to see if there are

            TestSequence sequence = new TestSequence("seq");

            try
            {
                sequence.DisplayName = null;
            }
            catch (ArgumentException exc)
            {
                if (!exc.StackTrace.Contains("DisplayName"))
                    throw exc;
            }

            try
            {
                sequence.DisplayName = String.Empty;
            }
            catch (ArgumentException exc)
            {
                if (!exc.StackTrace.Contains("DisplayName"))
                    throw exc;
            }
        }

        [Fact]
        public void DisplayNameLong()
        {
            // display name more than 256 characters... 
            TestSequence sequence = new TestSequence("seq")
            {
                DisplayName =
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    " +
                "123456789 abcdefghijklmnopqrstuvwxyz    123456789 abcdefghijklmnopqrstuvwxyz    "
            };
            sequence.Activities.Add(new TestWriteLine("w1", "I'm a funny writeLine") { HintMessage = "I'm a funny writeLine" });
            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        [Fact]
        public void DisplayNameWithPunctuations()
        {
            TestSequence sequence = new TestSequence("seq")
            {
                DisplayName =
                "!@#$%^&*()_+-={}|[]\\:\";\'.../////\\\\\\\\<>?,./~` !@#$%^&*()_+-={}|[]\\:\";\'<>?,./~`"
            };
            sequence.Activities.Add(new TestWriteLine("w1", "I'm a funny writeLine") { HintMessage = "I'm a funny writeLine" });
            TestRuntime.RunAndValidateWorkflow(sequence);
        }



        /// <summary>
        /// Execute empty sequence activity - expected to pass
        /// EmptySequence
        /// </summary>        
        [Fact]
        public void EmptySequence()
        {
            //  Test case description:
            //  Execute empty sequence activity - expected to pass

            TestSequence sequence = new TestSequence("Seq");

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Add same activity multiple times to same sequence
        /// SameActivityMultipleTimesInSameSequence
        /// </summary>        
        [Fact]
        public void SameActivityMultipleTimesInSameSequence()
        {
            //  Test case description:
            //  Add same activity multiple times to same sequence

            TestSequence sequence = new TestSequence("ContainerSequence");

            TestSequence sequence2 = new TestSequence("Seq");

            TestWriteLine writeLine1 = new TestWriteLine("Hello One")
            {
                Message = "Hello world!"
            };

            TestWriteLine writeLine2 = new TestWriteLine("Hello Two")
            {
                Message = "Hello world!"
            };

            //begin:same sequence object with default name in a sequence
            sequence.Activities.Add(sequence2);
            sequence.Activities.Add(sequence2);
            //end:same sequence object with default name in a sequence

            TestRuntime.ValidateInstantiationException(sequence,
                string.Format(ErrorStrings.ActivityCannotBeReferencedWithoutTarget, sequence2.DisplayName, sequence.DisplayName, sequence.DisplayName));
        }

        /// <summary>
        /// Add same activity multiple times to different sequences
        /// SameActivityMultipleTimesInDifferentSequences
        /// </summary>        
        [Fact]
        public void SameActivityMultipleTimesInDifferentSequences()
        {
            //  Test case description:
            //  Add same activity multiple times to different sequences

            TestWriteLine writeLine1 = new TestWriteLine("Hello One")
            {
                Message = "Hello world!"
            };

            //this test case has the same writeline activity in different levels of the sequence chains
            //  sequence 2
            //         sequence3   
            //        sequence4
            //            sequence 5
            //                sequence 6
            //                    WriteLine1
            //sequence 7
            //        sequence8   
            //            sequence9
            //                sequence10
            //                    sequence11
            //                        WriteLine1

            TestSequence sequence = new TestSequence("seq")
            {
                Activities =
                {
                    new TestSequence("seq")
                    {
                        Activities =
                        {
                            new TestSequence("seq")
                            {
                                Activities=
                                {
                                    new TestSequence("seq")
                                    {
                                        Activities=
                                        {
                                            new TestSequence("seq")
                                            {
                                                Activities=
                                                {
                                                    new TestSequence("seq")
                                                    {
                                                        Activities =
                                                        {
                                                            writeLine1,
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },

                    new TestSequence("seq")
                    {
                        Activities =
                        {
                            new TestSequence("seq")
                            {
                                Activities=
                                {
                                    new TestSequence("seq")
                                    {
                                        Activities=
                                        {
                                            new TestSequence("seq")
                                            {
                                                Activities=
                                                {
                                                    new TestSequence("seq")
                                                    {
                                                        Activities =
                                                        {
                                                            writeLine1,
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            TestRuntime.ValidateInstantiationException(sequence,
                string.Format(ErrorStrings.ActivityCannotBeReferencedWithoutTarget,
                                writeLine1.DisplayName,
                                "seq",
                                "seq"));
        }

        /// <summary>
        /// two variables that have the same name roundtrip serialize
        /// </summary>        
        [Fact]
        public void TwoVariablesSameNameRoundTripSerialize()
        {
            TestSequence sequence = new TestSequence("Sequence");
            Variable<int> intVar = VariableHelper.Create<int>("some name");
            sequence.Variables.Add(intVar);

            TestSequence sequence2 = new TestSequence("Sequence");
            Variable<double> doubleVar = VariableHelper.Create<double>("some name");
            sequence2.Variables.Add(doubleVar);
            sequence2.Activities.Add(new TestWriteLine("w1", "I'm a funny writeLine") { HintMessage = "I'm a funny writeLine" });

            sequence.Activities.Add(sequence2);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// two variables that have the same name roundtrip serialize
        /// </summary>        
        [Fact]
        public void VariableStringDefaultValueRoundTripSerialize()
        {
            TestSequence sequence = new TestSequence("Sequence");

            Variable<string> stringVar = VariableHelper.Create<string>("some name");
            sequence.Variables.Add(stringVar);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// two variables that have the same name roundtrip serialize
        /// </summary>        
        [Fact]
        public void TwoActivitiesSameNameRoundTripSerialize()
        {
            TestSequence sequence = new TestSequence("Sequence");
            TestSequence sequence2 = new TestSequence("Sequence");

            sequence2.Activities.Add(new TestWriteLine("w1", "I'm a funny writeLine") { HintMessage = "I'm a funny writeLine" });
            sequence.Activities.Add(sequence2);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Sequence activity with  variables of different data types (i.e. enums, enum with flags, nullables, Type, collections, generic collections, structs, UDTs)
        /// BasicSeqeunceDifferentTypesOfVariables
        /// </summary>        
        [Fact]
        public void BasicSeqeunceDifferentTypesOfVariables()
        {
            //  Test case description:
            //  Sequence activity with  variables of different data types (i.e. enums, enum with flags, nullables,
            //  Type, collections, generic collections, structs, UDTs)

            Dictionary<double, Guid> dict = new Dictionary<double, Guid>();
            dict.Add(12.123, Guid.Empty);
            dict.Add(23.44, new Guid("03998230918103948213098130981309"));
            dict.Add(-0.233, new Guid("99999999-9999-9999-0000-123456789102"));

            HashSet<ulong> hashSet = new HashSet<ulong>();
            hashSet.Add(90923023);
            hashSet.Add(232300000);
            hashSet.Add(0);
            hashSet.Add(333);
            hashSet.Add(ulong.MaxValue);
            hashSet.Add(ulong.MinValue);

            UriBuilder anotherweirdtype = new UriBuilder("http://www.live.com");

            Stream stream = new MemoryStream();

            Variable<Dictionary<double, Guid>> dictionary = VariableHelper.CreateInitialized<Dictionary<double, Guid>>("dictionary", context => dict);
            Variable<HashSet<ulong>> hash = VariableHelper.CreateInitialized<HashSet<ulong>>("hash", hashSet);
            Variable<UriBuilder> uribuild = VariableHelper.CreateInitialized<UriBuilder>("uribuild", anotherweirdtype);
            Variable<Stream> streaminUSA = VariableHelper.CreateInitialized<Stream>("streaminUSA", stream);

            Guid outguid = Guid.Empty;
            TestSequence sequence = new TestSequence("Sequence1")
            {
                Variables =
                {
                    dictionary,
                    hash,
                    uribuild,
                    streaminUSA
                },
                Activities =
                {
                    new TestIf("if activity")
                    {
                        ConditionExpression = ((env) => ((Dictionary<double, Guid>)dictionary.Get(env)).TryGetValue(-0.233, out outguid)),
                        ThenActivity = new TestWriteLine("In if 1", "In if 1"),
                    },
                    new TestIf("if activity")
                    {
                        ConditionExpression = ((env) => ((HashSet<ulong>)hash.Get(env)).SetEquals(hashSet)),
                        ThenActivity = new TestWriteLine("In if 2", "In if 2"),
                    },
                    new TestIf("if activity")
                    {
                        ConditionExpression = ((env) => ((UriBuilder)uribuild.Get(env)).Equals(new Uri("http://www.live.com"))),
                        ThenActivity = new TestWriteLine("In if 3", "In if 3"),
                    },
                    new TestIf("if activity")
                    {
                        ConditionExpression = ((env) => ((Stream)streaminUSA.Get(env)).Equals(stream)),
                        ThenActivity = new TestWriteLine("In if 4", "In if 4"),
                    },
                }
            };

            TestRuntime.RunAndValidateWorkflow(sequence);

            if (outguid.CompareTo(new Guid("99999999-9999-9999-0000-123456789102")) != 0)
            {
                throw new Exception("guid value is wrong");
            }
        }

        /// <summary>
        /// Execute sequence activity that has multiple different children in the Activities list and expect the execution result be sequential as in the order they were added to the Activities list
        /// BasicSequenceWithMultipleChildren
        /// </summary>        
        [Fact]
        public void BasicSequenceWithMultipleChildren()
        {
            //  Test case description:
            //  Execute sequence activity that has multiple different children in the Activities list and expect the
            //  execution result be sequential as in the order they were added to the Activities list

            Stack<Guid> stackOfGuids = new Stack<Guid>();
            stackOfGuids.Push(new Guid("11111111-1111-1111-1111-111111111111"));
            TestSequence sequence = new TestSequence("Sequence1");

            TestSequence sequence2 = new TestSequence("InnerSequence1");
            TestSequence sequence3 = new TestSequence("InnerSequence2");
            TestSequence sequence4 = new TestSequence("InnerSequence3");
            TestSequence sequence5 = new TestSequence("InnerSequence4");

            Variable<Stack<Guid>> stack = VariableHelper.Create<Stack<Guid>>("keyed_collection");

            TestInvokeMethod invokeact = new TestInvokeMethod("method invoke act", typeof(Sequence).GetMethod("CheckValue"))
            {
                TargetObject = new TestArgument<Sequence>(Direction.In, "TargetObject", (context => new Sequence()))
            };
            invokeact.Arguments.Add(new TestArgument<Stack<Guid>>(Direction.In, "stack", stack));
            TestSequence sequence6 = new TestSequence("seq5")
            {
                Variables =
                {
                    stack
                },
                Activities =
                {
                    new TestWriteLine("hello writeline", "Hello from Mars"),
                    new TestAssign<Stack<Guid>>("assign activity ")
                    {
                        ToVariable = stack,
                        ValueExpression = context => stackOfGuids,
                    },
                    new TestIf("ifact")
                    {
                        Condition =  true,
                        ThenActivity = invokeact
                    }
                }
            };
            TestSequence sequence7 = new TestSequence("seq6");
            TestSequence sequence8 = new TestSequence("seq7");
            TestSequence sequence9 = new TestSequence("seq8");


            TestWriteLine writeLine2 = new TestWriteLine("Hello Two")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence3.DisplayName, sequence7.DisplayName)
            };
            TestWriteLine writeLine3 = new TestWriteLine("Hello Three")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence4.DisplayName, sequence8.DisplayName)
            };
            TestWriteLine writeLine4 = new TestWriteLine("Hello Four")
            {
                Message = string.Format("Hello world in {0} , {1} , {2}!", sequence.DisplayName, sequence5.DisplayName, sequence9.DisplayName)
            };

            sequence7.Activities.Add(writeLine2);
            sequence8.Activities.Add(writeLine3);
            sequence9.Activities.Add(writeLine4);
            sequence2.Activities.Add(sequence6);
            sequence3.Activities.Add(sequence7);
            sequence4.Activities.Add(sequence8);
            sequence5.Activities.Add(sequence9);
            sequence.Activities.Add(sequence2);
            sequence.Activities.Add(sequence3);
            sequence.Activities.Add(sequence4);
            sequence.Activities.Add(sequence5);

            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// same variable added twice to sequence
        /// </summary>        
        [Fact]
        public void SameVariableAddedTwiceOnSameSequence()
        {
            TestSequence sequence = new TestSequence("Sequence");
            Variable<int> intVariable = VariableHelper.Create<int>("some integer");

            sequence.Variables.Add(intVariable);
            sequence.Variables.Add(intVariable);

            TestRuntime.ValidateInstantiationException(sequence, string.Format(ErrorStrings.VariableAlreadyInUseOnActivity, intVariable.Name, sequence.DisplayName, sequence.DisplayName));
        }

        /// <summary>
        /// SequenceWithMethodInvoke
        /// </summary>        
        [Fact]
        public void SequenceWithMethodInvoke()
        {
            //  Test case description:
            //  Execute empty sequence activity - expected to pass
            TestSequence sequence = new TestSequence("Seq");

            TestInvokeMethod methodInvokeAct = new TestInvokeMethod("methodinvoke", this.GetType().GetMethod("DummyMethod"))
            {
                TargetObject = new TestArgument<Sequence>(Direction.In, "TargetObject", (context => this))
            };
            sequence.Activities.Add(methodInvokeAct);
            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        /// <summary>
        /// Call GetChildren, modify children activities, and then execute activity
        /// </summary>        
        [Fact]
        public void GetChildrenModifyChildrenExecute()
        {
            TestSequence sequence = new TestSequence("Test Sequence")
            {
                Activities =
                {
                    new TestWriteLine("WriteLine A")
                    {
                        Message = "message a",
                    },
                },
            };

            WorkflowInspectionServices.GetActivities(sequence.ProductActivity);

            sequence.Activities.Add(
                new TestWriteLine("WriteLine B")
                {
                    Message = "message b",
                }
            );

            // Now that we've changed the tree we explicitly recache
            WorkflowInspectionServices.CacheMetadata(sequence.ProductActivity);
            TestRuntime.RunAndValidateWorkflow(sequence);
        }

        public void DummyMethod()
        {
            // This is a dummy method
        }

        public void CheckValue(Stack<Guid> stack)
        {
            if (stack.Pop().CompareTo(new Guid("11111111-1111-1111-1111-111111111111")) != 0)
            {
                throw new Exception("disappointed that stack doesnt compare me");
            }
        }

        /// <summary>
        /// SequenceWithWorkFlowInvoker
        /// </summary>        
        [Fact]
        public void SequenceWithWorkFlowInvoker()
        {
            Variable<string> note = VariableHelper.CreateInitialized<string>("note", "It's funny");
            TestSequence sequence = new TestSequence("Seq");
            sequence.Activities.Add(new TestWriteLine("w1", note, "It's funny"));
            sequence.Variables.Add(note);

            TestRuntime.RunAndValidateUsingWorkflowInvoker(sequence, null, null, null);
        }

        /// <summary>
        /// Cancel in the first activity - Sequence should mark first activity cancelled, all other activities as completed/cancelled
        /// CancelSequenceInTheMiddle
        /// </summary>        
        [Fact]
        public void CancelSequenceInTheMiddle()
        {
            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    new TestWriteLine("W1", "I should be printed"),
                    new TestBlockingActivity("BookMark1"){ExpectedOutcome = Outcome.Canceled},
                    new TestWriteLine("W2", "I should not be printed"){ ExpectedOutcome = Outcome.Canceled},
                    new TestWriteLine("W3", "I should not be printed"){ ExpectedOutcome = Outcome.Canceled},
                }
            };

            JsonFileInstanceStore.FileInstanceStore jsonStore = new JsonFileInstanceStore.FileInstanceStore(".\\~");

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(seq, null, jsonStore, PersistableIdleAction.None))
            {
                runtime.ExecuteWorkflow();

                runtime.WaitForIdle();

                runtime.PersistWorkflow();

                runtime.CancelWorkflow();

                runtime.WaitForCanceled();
            }
        }
    }
}

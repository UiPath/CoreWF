// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Microsoft.CoreWf.Expressions;
using Xunit;
using Xunit.Abstractions;

namespace TestCases.Activities
{
    public class Assignment : IDisposable
    {
        public enum NestedColors
        {
            Red = 1,
            Green = 2,
            Blue = 3
        }

        public void Dispose()
        {
        }
        #region Assign<T> cases
        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void BasicAssignment()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssign<string>
                    {
                        Value = hello,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }
        /// <summary>
        /// Assign to collections
        /// Assign to data types like lists, dictionaries…etc
        /// </summary>        
        [Fact]
        public void AssignToCollections()
        {
            Dictionary<Exception, NestedColors> concurrentColl = new Dictionary<Exception, NestedColors>();
            concurrentColl.Add(new NullReferenceException(), NestedColors.Blue);
            concurrentColl.Add(new ArgumentNullException(), NestedColors.Red);
            concurrentColl.Add(new ArrayTypeMismatchException(), NestedColors.Green);

            Variable<Dictionary<Exception, NestedColors>> word = new Variable<Dictionary<Exception, NestedColors>>();

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssign<Dictionary<Exception, NestedColors>>
                    {
                        ValueExpression = (context => concurrentColl),
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageExpression = Environment => (word.Get(Environment) as Dictionary<Exception, NestedColors>).Count.ToString(),
                        HintMessage = "3",
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }
        /// <summary>
        /// Simple assignment
        /// Assign to a null object
        /// </summary>        
        [Fact]
        public void AssignToANullObject()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();
            word.Default = null;

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssign<string>
                    {
                        Value = hello,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }
        /// <summary>
        /// Simple assignment
        /// Assignment activity to and from is not set
        /// </summary>        
        [Fact]
        public void EmptyAssignment()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();
            word.Default = null;
            TestAssign<string> assign = new TestAssign<string>();
            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    assign,
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            List<TestConstraintViolation> errors = new List<TestConstraintViolation> { new TestConstraintViolation(string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"), assign.ProductActivity, false), new TestConstraintViolation(string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "To"), assign.ProductActivity, false) };
            TestRuntime.ValidateWorkflowErrors(seq, errors, string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"));
        }

        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void EmptyAssignmentStandAlone()
        {
            TestAssign<string> assign = new TestAssign<string>();
            TestRuntime.ValidateInstantiationException(assign, typeof(System.ArgumentException), string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"));
        }
        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void AssignWithWorkflowInvoker()
        {
            Type hello = typeof(Exception);
            TestAssign<Type> assign = new TestAssign<Type>();
            Dictionary<string, object> inputs = new Dictionary<string, object>();
            inputs.Add("Value", hello);
            Dictionary<string, object> outputs = new Dictionary<string, object>();
            outputs.Add("To", hello);
            TestRuntime.RunAndValidateUsingWorkflowInvoker(assign, inputs, outputs, null);
        }
        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void BasicAssignNullValue()
        {
            string hello = null;
            Variable<string> word = new Variable<string>();

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssign<string>()
                    {
                        Value = hello,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }
        #endregion

        #region Assign
        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void BasicAssignNonGeneric()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssignNG(typeof(string))
                    {
                        Value = hello,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Assign to collections
        /// </summary>        
        [Fact]
        public void AssignToCollectionsNonGeneric()
        {
            Dictionary<Exception, NestedColors> concurrentColl = new Dictionary<Exception, NestedColors>();
            concurrentColl.Add(new NullReferenceException(), NestedColors.Blue);
            concurrentColl.Add(new ArgumentNullException(), NestedColors.Red);
            concurrentColl.Add(new ArrayTypeMismatchException(), NestedColors.Green);
            Variable<Dictionary<Exception, NestedColors>> value = new Variable<Dictionary<Exception, NestedColors>>("word", e => concurrentColl);


            Variable<Dictionary<Exception, NestedColors>> word = new Variable<Dictionary<Exception, NestedColors>>();

            TestSequence seq = new TestSequence
            {
                Variables = { word, value },
                Activities =
                {
                    new TestAssignNG(typeof(Dictionary<Exception, NestedColors>))
                    {
                        Value = value,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageExpression = Environment => (word.Get(Environment) as Dictionary<Exception, NestedColors>).Count.ToString(),
                        HintMessage = "3",
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void AssignToANullObjectNonGeneric()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();
            word.Default = null;

            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    new TestAssignNG(typeof(string))
                    {
                        Value = hello,
                        ToVariable = word,
                    },
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            TestRuntime.RunAndValidateWorkflow(seq);
        }
        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void EmptyAssignmentNonGeneric()
        {
            string hello = "Hello World";
            Variable<string> word = new Variable<string>();
            word.Default = null;
            TestAssignNG assign = new TestAssignNG(typeof(string));
            TestSequence seq = new TestSequence
            {
                Variables = { word },
                Activities =
                {
                    assign,
                    new TestWriteLine
                    {
                        MessageVariable = word,
                        HintMessage = hello,
                    }
                },
            };
            List<TestConstraintViolation> errors = new List<TestConstraintViolation> { new TestConstraintViolation(string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"), assign.ProductActivity, false), new TestConstraintViolation(string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "To"), assign.ProductActivity, false) };
            TestRuntime.ValidateWorkflowErrors(seq, errors, string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"));
        }

        /// <summary>
        /// Simple assignment
        /// </summary>        
        [Fact]
        public void EmptyAssignmentStandAloneNonGeneric()
        {
            TestAssignNG assign = new TestAssignNG(typeof(string));
            TestRuntime.ValidateInstantiationException(assign, typeof(System.ArgumentException), string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Value"));
        }

        //[Fact]
        //public void DifferentArguments()
        //{
        //    //Testing Different argument types for Assign.To and Assign.Value:
        //    // DelegateInArgument
        //    // DelegateOutArgument
        //    // Variable<T> , Activity<T> and Expression is already implemented.

        //    DelegateInArgument<string> delegateInArgument = new DelegateInArgument<string>("Input");
        //    DelegateOutArgument<string> delegateOutArgument = new DelegateOutArgument<string>("Output");

        //    TestCustomActivity<InvokeFunc<string, string>> invokeFunc = TestCustomActivity<InvokeFunc<string, string>>.CreateFromProduct(
        //            new InvokeFunc<string, string>
        //            {
        //                Argument = "PassedInValue",
        //                Func = new ActivityFunc<string, string>
        //                {
        //                    Argument = delegateInArgument,
        //                    Result = delegateOutArgument,
        //                    Handler = new Microsoft.CoreWf.Statements.Sequence
        //                    {
        //                        DisplayName = "sequence1",
        //                        Activities =
        //                        {
        //                            new Microsoft.CoreWf.Statements.WriteLine { Text = ExpressionServices.Convert<string>(ctx=>delegateInArgument.Get(ctx)) , DisplayName = "W1"},
        //                            new Microsoft.CoreWf.Statements.Assign<string>
        //                            {
        //                                DisplayName = "Assign1",
        //                                To = delegateInArgument,
        //                                Value = new VisualBasicValue<string>("Input & \"_VB\"")  ,
        //                            },
        //                            new Microsoft.CoreWf.Statements.WriteLine { Text = ExpressionServices.Convert<string>(ctx=>delegateInArgument.Get(ctx)) , DisplayName = "W2"},
        //                            new Microsoft.CoreWf.Statements.Assign<string>
        //                            {
        //                                DisplayName = "Assign2",
        //                                To = delegateOutArgument,
        //                                Value = delegateInArgument,
        //                            },
        //                        },
        //                    }
        //                }
        //            }
        //        );

        //    TestSequence sequenceForTracing = new TestSequence
        //    {
        //        DisplayName = "sequence1",
        //        Activities =
        //        {
        //            new TestSequence{ DisplayName = "W1"},
        //            new TestAssign<string>("Assign1"),
        //            new TestSequence{ DisplayName = "W2"},
        //            new TestAssign<string>("Assign2"),
        //        }
        //    };
        //    invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);

        //    TestWriteLine root = new TestWriteLine
        //    {
        //        HintMessage = "PassedInValue_VB",
        //        MessageActivity = invokeFunc
        //    };

        //    TestRuntime.RunAndValidateWorkflow(root);
        //}

        #endregion
    }
}

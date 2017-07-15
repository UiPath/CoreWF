// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class ArrayItemValue : IDisposable
    {
        /// <summary>
        /// Access value of an item in one dimensional array.
        /// </summary>        
        [Fact]
        public void AccessValueOfAnItemInOneDimensionalArray()
        {
            Variable<int[]> arrayVariable = new Variable<int[]>("arrayVariable");

            TestArrayItemValue<int> arrayItemValue = new TestArrayItemValue<int> { ArrayVariable = arrayVariable, Index = 0 };

            Variable<int> result = new Variable<int>() { Name = "Result" };
            arrayItemValue.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { arrayVariable, result },
                Activities =
                {
                    new TestAssign<int[]> { ToVariable = arrayVariable, ValueExpression = (context => new int[] { 1, 2, 3 }) },
                    arrayItemValue,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = "1" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestArrayItemValue<int>(),
                                                           new Dictionary<string, object> { { "Array", new int[3] { 1, 2, 3 } }, { "Index", 1 } },
                                                           new Dictionary<string, object> { { "Result", 2 } },
                                                           null);
        }

        /// <summary>
        /// Try accessing an item from an uninitialized array. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryAccessingItemFromAnUninitializedArray()
        {
            Variable<int[]> intArrayVar = new Variable<int[]>() { Name = "IntVar" };

            TestArrayItemValue<int> arrayVal = new TestArrayItemValue<int>
            {
                DisplayName = "GetValue",
                ArrayVariable = intArrayVar,
                Index = 4,
                ExpectedOutcome = Outcome.UncaughtException()
            };

            TestSequence seq = new TestSequence
            {
                Variables = { intArrayVar },
                Activities =
                {
                    arrayVal
                }
            };

            TestRuntime.RunAndValidateAbortedException(
                seq,
                typeof(InvalidOperationException),
                new Dictionary<string, string>
                {
                    { "Message", string.Format(ErrorStrings.MemberCannotBeNull, "Array", arrayVal.ProductActivity.GetType().Name, arrayVal.DisplayName) }
                });
        }

        /// <summary>
        /// Try accessing negative indexed item from array. Validation exception expected.
        /// </summary>        
        [Fact]
        public void SetIndexNegative()
        {
            Variable<string[]> array = new Variable<string[]>("array", context => new string[] { "Ola" });
            TestArrayItemValue<string> itemValue = new TestArrayItemValue<string>
            {
                ArrayVariable = array,
                Index = -1,
                ExpectedOutcome = Outcome.UncaughtException()
            };
            TestSequence testSequence = new TestSequence
            {
                Variables = { array },
                Activities = { itemValue }
            };

            TestRuntime.RunAndValidateAbortedException(testSequence, typeof(IndexOutOfRangeException), null);
        }

        /// <summary>
        /// Access the last item of an array.
        /// </summary>        
        [Fact]
        public void AccessLastItemOfAnArray()
        {
            Variable<Complex[]> complexArrayVar = new Variable<Complex[]> { Name = "ComplexArrVar" };

            Variable<Complex> result = new Variable<Complex>() { Name = "Result" };

            TestSequence seq = new TestSequence
            {
                Variables = { complexArrayVar, result },
                Activities =
                {
                    new TestAssign<Complex[]> { ToVariable = complexArrayVar, ValueExpression = (context => new Complex[] { new Complex(0, 0), new Complex(1, 1) }) },
                    new TestArrayItemValue<Complex>
                    {
                        DisplayName = "GetValue",
                        ArrayVariable = complexArrayVar,
                        Index = 1,
                        Result = result
                    },
                    new TestWriteLine
                    {
                        MessageExpression = e => result.Get(e).ToString(),
                        HintMessage = new Complex(1,1).ToString()
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try accessing item from a null array. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryAccessingItemFromNullArray()
        {
            Variable<string> result = new Variable<string>() { Name = "Result" };

            TestArrayItemValue<string> arrayItem = new TestArrayItemValue<string> { Index = 0, Result = result };

            TestSequence seq = new TestSequence { Variables = { result }, Activities = { arrayItem } };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Array");

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>() { new TestConstraintViolation(errorMessage, arrayItem.ProductActivity, false) };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, errorMessage);
        }

        /// <summary>
        /// Try executing ArrayValue activity by setting index null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void SetIndexNull()
        {
            TestArrayItemValue<string> testArray = new TestArrayItemValue<string>
            {
                ArrayExpression = context => new string[] { "x" }
            };
            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Index");

            TestRuntime.ValidateWorkflowErrors(testArray, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Access the first item of an array.
        /// </summary>        
        [Fact]
        public void AccessFirstItemOfAnArray()
        {
            Variable<Complex[]> complexArrayVar = new Variable<Complex[]> { Name = "ComplexArrVar" };

            Variable<Complex> result = new Variable<Complex>() { Name = "Result" };

            TestSequence seq = new TestSequence
            {
                Variables = { complexArrayVar, result },
                Activities =
                {
                    new TestAssign<Complex[]> { ToVariable = complexArrayVar, ValueExpression = (context => new Complex[] { new Complex(0, 0), new Complex(1, 1) }) },
                    new TestArrayItemValue<Complex>
                    {
                        DisplayName = "GetValue",
                        ArrayVariable = complexArrayVar,
                        Index = 0,
                        Result = result
                    },
                    new TestWriteLine
                    {
                        MessageExpression = e => result.Get(e).ToString(),
                        HintMessage = new Complex(0,0).ToString()
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        public void Dispose()
        {
        }
    }
}

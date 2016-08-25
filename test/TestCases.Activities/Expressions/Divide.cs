// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects;
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
    public class Divide : IDisposable
    {
        /// <summary>
        /// Multiply two positive integers.
        /// </summary>        
        [Fact]
        public void DivideTwoPositiveIntegers()
        {
            TestDivide<int, int, int> divide = new TestDivide<int, int, int>(12, 4);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, int>(divide, "3");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Divide an integer and a double. Validation exception expected.
        /// </summary>        
        [Fact]
        public void SubtractTwoIncompatibleTypes()
        {
            TestDivide<int, double, double> divide = new TestDivide<int, double, double>(4, 4.2);

            TestRuntime.ValidateInstantiationException(divide, TestExpressionTracer.GetExceptionMessage<int, double, double>(System.Linq.Expressions.ExpressionType.Divide));
        }

        /// <summary>
        /// Both operands of custom type which overloads division operator.
        /// Both operands of custom type which overloads division operator
        /// </summary>        
        [Fact]
        public void CustomTypeOverloadedDivideOperatorAsOperands()
        {
            TestDivide<Complex, Complex, Complex> divide = new TestDivide<Complex, Complex, Complex>
            {
                LeftExpression = context => new Complex(2, 6),
                RightExpression = context => new Complex(1, 2),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, Complex>(divide, "2 3");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestDivide<int, int, int>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 2 } },
                                                           new Dictionary<string, object> { { "Result", 2 } },
                                                           null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestDivide<int, string, string> div = new TestDivide<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Divide);

            TestExpressionTracer.Validate(div, new List<string> { errorMessage });
        }

        /// <summary>
        /// Set the right operand to 0. DivideByZero exception expected at runtime.
        /// </summary>        
        [Fact]
        public void DivideByZero()
        {
            TestDivide<int, int, int> divide = new TestDivide<int, int, int>(12, 0) { ExpectedOutcome = Outcome.UncaughtException() };

            TestRuntime.RunAndValidateAbortedException(divide, typeof(DivideByZeroException), null);
        }

        /// <summary>
        /// Try dividing null with valid right operand. Validation exception expected.
        /// </summary>        
        [Fact]
        public void LeftOperandNull()
        {
            TestDivide<int, int, int> div = new TestDivide<int, int, int>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(div, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try dividing valid left operand with null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void RightOperandNull()
        {
            TestDivide<int, int, int> div = new TestDivide<int, int, int>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(div, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestDivide<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType> div = new TestDivide<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(13),
                RightExpression = context => new OverLoadOperatorThrowingType(14),
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            div.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>(div, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }
        public void Dispose()
        {
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    public class GreaterThan : IDisposable
    {
        /// <summary>
        /// Evaluate 2 > 3.
        /// </summary>        
        [Fact]
        public void CompareTwoIntegers()
        {
            TestGreaterThan<int, int, bool> greaterThan = new TestGreaterThan<int, int, bool>(2, 3);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, bool>(greaterThan, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has GreaterThan overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestGreaterThan<Complex, Complex, bool> greaterThan = new TestGreaterThan<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(2, 3),
                RightExpression = context => new Complex(1, 4),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(greaterThan, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestGreaterThan<int, int, bool>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", false } },
                                                           null);
        }

        /// <summary>
        /// Try evaluating GreaterThan of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryGreaterThanOnIncompatibleTypes()
        {
            TestGreaterThan<int, string, bool> greaterThan = new TestGreaterThan<int, string, bool> { Left = 1, Right = "1" };

            string error = TestExpressionTracer.GetExceptionMessage<int, string, bool>(System.Linq.Expressions.ExpressionType.GreaterThan);

            TestRuntime.ValidateWorkflowErrors(greaterThan, new List<TestConstraintViolation>() { new TestConstraintViolation(error, greaterThan.ProductActivity) }, error);
        }

        /// <summary>
        /// Try executing GreaterThan activity by setting left operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestGreaterThan<int, int, bool> greaterThan = new TestGreaterThan<int, int, bool> { Right = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(greaterThan, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try executing GreaterThan activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestGreaterThan<int, int, bool> greaterThan = new TestGreaterThan<int, int, bool> { Left = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(greaterThan, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestGreaterThan<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> greaterThan = new TestGreaterThan<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(2),
                RightExpression = context => new OverLoadOperatorThrowingType(1),
                ExpectedOutcome = Outcome.UncaughtException()
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(greaterThan, true.ToString());

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestGreaterThan<PublicType, string, bool> greaterThan = new TestGreaterThan<PublicType, string, bool>
            {
                LeftExpression = context => new PublicType(),
                Right = "1"
            };

            string error = TestExpressionTracer.GetExceptionMessage<PublicType, string, bool>(System.Linq.Expressions.ExpressionType.GreaterThan);

            TestExpressionTracer.Validate(greaterThan, new List<string> { error });
        }

        public void Dispose()
        {
        }
    }
}

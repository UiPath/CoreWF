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
    public class LessThan : IDisposable
    {
        /// <summary>
        /// Evaluate 2 < 3.
        /// </summary>        
        [Fact]
        public void CompareTwoIntegers()
        {
            TestLessThan<int, int, bool> lessThan = new TestLessThan<int, int, bool>(2, 3);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, bool>(lessThan, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestLessThan<int, int, bool>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", false } },
                                                           null);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has LessThan overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestLessThan<Complex, Complex, bool> lessThan = new TestLessThan<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(2, 3),
                RightExpression = context => new Complex(1, 4),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(lessThan, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try evaluating LessThan of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryLessThanOnIncompatibleTypes()
        {
            TestLessThan<int, string, bool> lessThan = new TestLessThan<int, string, bool> { Left = 1, Right = "1" };

            string error = TestExpressionTracer.GetExceptionMessage<int, string, bool>(System.Linq.Expressions.ExpressionType.LessThan);

            TestRuntime.ValidateWorkflowErrors(lessThan, new List<TestConstraintViolation>() { new TestConstraintViolation(error, lessThan.ProductActivity) }, error);
        }

        /// <summary>
        /// Try executing LessThan activity by setting left operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestLessThan<int, int, bool> lessThan = new TestLessThan<int, int, bool> { Right = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(lessThan, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try executing LessThan activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestLessThan<int, int, bool> lessThan = new TestLessThan<int, int, bool> { Left = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(lessThan, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestLessThan<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> lessThan = new TestLessThan<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(2),
                RightExpression = context => new OverLoadOperatorThrowingType(1),
                ExpectedOutcome = Outcome.UncaughtException()
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(lessThan, true.ToString());

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestLessThan<PublicType, string, bool> lessThan = new TestLessThan<PublicType, string, bool>
            {
                LeftExpression = context => new PublicType(),
                Right = "1"
            };

            string error = TestExpressionTracer.GetExceptionMessage<PublicType, string, bool>(System.Linq.Expressions.ExpressionType.LessThan);

            TestExpressionTracer.Validate(lessThan, new List<string> { error });
        }

        public void Dispose()
        {
        }
    }
}

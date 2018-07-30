// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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
    public class LessThanOrEqual : IDisposable
    {
        /// <summary>
        /// Evaluate 2 <= 2.
        /// </summary>        
        [Fact]
        public void CompareTwoIntegers()
        {
            TestLessThanOrEqual<int, int, bool> lessThanOrEqual = new TestLessThanOrEqual<int, int, bool>(2, 2);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, bool>(lessThanOrEqual, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestLessThanOrEqual<int, int, bool>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", true } },
                                                           null);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has LessThanOrEqual overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestLessThanOrEqual<Complex, Complex, bool> lessThanOrEqual = new TestLessThanOrEqual<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(2, 3),
                RightExpression = context => new Complex(2, 4),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(lessThanOrEqual, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try evaluating LessThanOrEqual of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryLessThanOrEqualOnIncompatibleTypes()
        {
            TestLessThanOrEqual<int, string, bool> lessThanOrEqual = new TestLessThanOrEqual<int, string, bool> { Left = 1, Right = "1" };

            string error = TestExpressionTracer.GetExceptionMessage<int, string, bool>(System.Linq.Expressions.ExpressionType.LessThanOrEqual);

            TestRuntime.ValidateWorkflowErrors(lessThanOrEqual, new List<TestConstraintViolation>() { new TestConstraintViolation(error, lessThanOrEqual.ProductActivity) }, error);
        }

        /// <summary>
        /// Try executing LessThanOrEqual activity by setting left operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestLessThanOrEqual<int, int, bool> lessThanOrEqual = new TestLessThanOrEqual<int, int, bool> { Right = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(lessThanOrEqual, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try executing LessThanOrEqual activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestLessThanOrEqual<int, int, bool> lessThanOrEqual = new TestLessThanOrEqual<int, int, bool> { Left = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(lessThanOrEqual, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestLessThanOrEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> lessThanOrEqual = new TestLessThanOrEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(2),
                RightExpression = context => new OverLoadOperatorThrowingType(1),
                ExpectedOutcome = Outcome.UncaughtException()
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(lessThanOrEqual, true.ToString());

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestLessThanOrEqual<PublicType, string, bool> lessThanOrEqual = new TestLessThanOrEqual<PublicType, string, bool>
            {
                LeftExpression = context => new PublicType(),
                Right = "1"
            };

            string error = TestExpressionTracer.GetExceptionMessage<PublicType, string, bool>(System.Linq.Expressions.ExpressionType.LessThanOrEqual);

            TestExpressionTracer.Validate(lessThanOrEqual, new List<string> { error });
        }

        public void Dispose()
        {
        }
    }
}

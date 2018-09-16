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
    public class GreaterThanOrEqual : IDisposable
    {
        /// <summary>
        /// Evaluate 2 >= 3.
        /// </summary>        
        [Fact]
        public void CompareTwoIntegers()
        {
            TestGreaterThanOrEqual<int, int, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<int, int, bool>(2, 3);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, bool>(greaterThanOrEqual, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestGreaterThanOrEqual<int, int, bool>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", true } },
                                                           null);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has GreaterThanOrEqual overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestGreaterThanOrEqual<Complex, Complex, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(2, 3),
                RightExpression = context => new Complex(2, 4),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(greaterThanOrEqual, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try evaluating GreaterThanOrEqual of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryGreaterThanOrEqualOnIncompatibleTypes()
        {
            TestGreaterThanOrEqual<int, string, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<int, string, bool> { Left = 1, Right = "1" };

            string error = TestExpressionTracer.GetExceptionMessage<int, string, bool>(System.Linq.Expressions.ExpressionType.GreaterThanOrEqual);

            TestRuntime.ValidateWorkflowErrors(greaterThanOrEqual, new List<TestConstraintViolation>() { new TestConstraintViolation(error, greaterThanOrEqual.ProductActivity) }, error);
        }

        /// <summary>
        /// Try executing GreaterThanOrEqual activity by setting left operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestGreaterThanOrEqual<int, int, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<int, int, bool> { Right = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(greaterThanOrEqual, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try executing GreaterThanOrEqual activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestGreaterThanOrEqual<int, int, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<int, int, bool> { Left = 10 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(greaterThanOrEqual, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestGreaterThanOrEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(2),
                RightExpression = context => new OverLoadOperatorThrowingType(1),
                ExpectedOutcome = Outcome.UncaughtException()
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(greaterThanOrEqual, true.ToString());

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestGreaterThanOrEqual<PublicType, string, bool> greaterThanOrEqual = new TestGreaterThanOrEqual<PublicType, string, bool>
            {
                LeftExpression = context => new PublicType(),
                Right = "1"
            };

            string error = TestExpressionTracer.GetExceptionMessage<PublicType, string, bool>(System.Linq.Expressions.ExpressionType.GreaterThanOrEqual);

            TestExpressionTracer.Validate(greaterThanOrEqual, new List<string> { error });
        }

        public void Dispose()
        {
        }
    }
}

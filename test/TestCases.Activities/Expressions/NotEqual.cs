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
    public class NotEqual : IDisposable
    {
        /// <summary>
        /// Evaluate true == false.
        /// </summary>        
        [Fact]
        public void CompareTwoBooleans()
        {
            TestNotEqual<bool, bool, bool> notEqual = new TestNotEqual<bool, bool, bool>(true, false);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<bool, bool, bool>(notEqual, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestNotEqual<Complex, Complex, bool>(),
                                                           new Dictionary<string, object> { { "Left", new Complex(1, 2) }, { "Right", new Complex(1, 2) } },
                                                           new Dictionary<string, object> { { "Result", false } },
                                                           null);
        }

        /// <summary>
        /// Executing NotEqual activity by setting left operand null. Should execute successfully.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestNotEqual<int, int, bool> notEq = new TestNotEqual<int, int, bool>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(notEq, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Execute NotEqual activity by setting right operand null. Should execute sucessfully.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestNotEqual<int, int, bool> notEq = new TestNotEqual<int, int, bool>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(notEq, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestNotEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> notEq = new TestNotEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(13),
                RightExpression = context => new OverLoadOperatorThrowingType(14),
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            notEq.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(notEq, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has NotEquals overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestNotEqual<Complex, Complex, bool> notEq = new TestNotEqual<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(1, 1),
                RightExpression = context => new Complex(2, 1),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(notEq, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestNotEqual<int, string, string> notEq = new TestNotEqual<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.NotEqual);

            TestExpressionTracer.Validate(notEq, new List<string> { errorMessage });
        }

        /// <summary>
        /// Try evaluating NotEquals of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryNotEqualsOnIncompatibleTypes()
        {
            TestNotEqual<int, string, int> notEq = new TestNotEqual<int, string, int> { Right = "3" };

            TestRuntime.ValidateInstantiationException(notEq, TestExpressionTracer.GetExceptionMessage<int, string, int>(System.Linq.Expressions.ExpressionType.NotEqual));
        }

        public void Dispose()
        {
        }
    }
}

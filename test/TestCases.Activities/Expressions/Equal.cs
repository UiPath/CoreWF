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
    public class Equal : IDisposable
    {
        /// <summary>
        /// Evaluate true == false.
        /// </summary>        
        [Fact]
        public void CompareTwoBooleans()
        {
            TestEqual<bool, bool, bool> equal = new TestEqual<bool, bool, bool>(true, false);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<bool, bool, bool>(equal, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Evaluate comparing two objects of custom type which has Equals overloaded.
        /// Evaluate comparing two objects of custom type which has Equals overloaded..
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestEqual<Complex, Complex, bool> equal = new TestEqual<Complex, Complex, bool>
            {
                LeftExpression = context => new Complex(2, 3),
                RightExpression = context => new Complex(2, 3),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(equal, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestEqual<int, int, bool>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", true } },
                                                           null);
        }

        /// <summary>
        /// Execute Equal activity by setting left operand null. Successful execution.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestEqual<int, int, bool> eq = new TestEqual<int, int, bool>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(eq, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Execute Equal activity by setting right operand null. Successful execution..
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestEqual<int, int, bool> eq = new TestEqual<int, int, bool>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(eq, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool> eq = new TestEqual<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(13),
                RightExpression = context => new OverLoadOperatorThrowingType(14),
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            eq.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, bool>(eq, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Have both left and right operands null. The comparision should evaluate to true.
        /// </summary>
        //[Fact]
        //public void CompareNullWithNull()
        //{
        //    TestEqual<Complex, Complex, bool> equal = new TestEqual<Complex, Complex, bool>
        //    {
        //        Left = null,
        //        Right = null
        //    };

        //    TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, bool>(equal, true.ToString());

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        [Fact]
        public void ConstraintViolationIncompatibleTypes()
        {
            TestEqual<int, string, string> eq = new TestEqual<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Equal);

            TestExpressionTracer.Validate(eq, new List<string> { errorMessage });
        }

        /// <summary>
        /// Try evaluating Equals of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryEqualsOnIncompatibleTypes()
        {
            TestEqual<int, string, string> eq = new TestEqual<int, string, string>
            {
                Left = 12,
                Right = "12"
            };

            TestRuntime.ValidateInstantiationException(eq, TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Equal));
        }

        public void Dispose()
        {
        }
    }
}

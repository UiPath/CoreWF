// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    public class Or : IDisposable
    {
        /// <summary>
        /// Evaluate true OR false.
        /// </summary>        
        [Fact]
        public void TwoBooleansInOr()
        {
            TestOr<bool, bool, bool> or = new TestOr<bool, bool, bool>(false, true);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<bool, bool, bool>(or, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestOr<int, int, int>(),
                                                           new Dictionary<string, object> { { "Left", 1 }, { "Right", 0 } },
                                                           new Dictionary<string, object> { { "Result", 1 } },
                                                           null);
        }

        /// <summary>
        /// AND two operands of custom type which overloads OR operator.
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestOr<Complex, Complex, Complex> complexOr = new TestOr<Complex, Complex, Complex>()
            {
                LeftExpression = context => new Complex(1, 2),
                RightExpression = context => new Complex(2, 3)
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, Complex>(complexOr, new Complex(1 | 2, 2 | 3).ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try executing OR activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestOr<int, int, int> intOr = new TestOr<int, int, int> { Left = 3 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(intOr, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try evaluating null OR with bool. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestOr<int, int, int> intOr = new TestOr<int, int, int> { Right = 3 };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(intOr, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try evaluating OR of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryOROnIncompatibleTypes()
        {
            TestOr<int, string, int> intOr = new TestOr<int, string, int> { Right = "3" };

            TestRuntime.ValidateInstantiationException(intOr, TestExpressionTracer.GetExceptionMessage<int, string, int>(System.Linq.Expressions.ExpressionType.Or));
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestOr<int, string, string> or = new TestOr<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Or);

            TestExpressionTracer.Validate(or, new List<string> { errorMessage });
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestOr<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, int> or = new TestOr<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, int>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(13),
                RightExpression = context => new OverLoadOperatorThrowingType(14),
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            or.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, int>(or, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        public void Dispose()
        {
        }
    }
}

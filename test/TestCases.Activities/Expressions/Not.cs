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
    public class Not : IDisposable
    {
        /// <summary>
        /// Evaluate NOT with true as operand.
        /// </summary>        
        [Fact]
        public void EvaluateNotOfBoolean()
        {
            TestNot<bool, bool> not = new TestNot<bool, bool>
            {
                Operand = true
            };

            TestSequence seq = TestExpressionTracer.GetTraceableUnaryExpressionActivity<bool, bool>(not, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestNot<bool, bool>(),
                                                           new Dictionary<string, object> { { "Operand", true } },
                                                           new Dictionary<string, object> { { "Result", false } },
                                                           null);
        }

        /// <summary>
        /// NOT an operand of custom type which overloads NOT operator.
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestNot<Complex, Complex> complexNot = new TestNot<Complex, Complex>()
            {
                OperandExpression = context => new Complex(1, 0)
            };

            TestSequence seq = TestExpressionTracer.GetTraceableUnaryExpressionActivity<Complex, Complex>(complexNot, new Complex(0, 1).ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try evaluating NOT of null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetOperandNull()
        {
            TestNot<int, int> not = new TestNot<int, int>();

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Operand");

            TestRuntime.ValidateWorkflowErrors(not, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestNot<OverLoadOperatorThrowingType, bool> not = new TestNot<OverLoadOperatorThrowingType, bool>
            {
                OperandExpression = context => new OverLoadOperatorThrowingType(13)
            };

            OverLoadOperatorThrowingType.ThrowException = true;

            not.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableUnaryExpressionActivity<OverLoadOperatorThrowingType, bool>(not, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestNot<string, Complex> not = new TestNot<string, Complex>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<string, Complex>(System.Linq.Expressions.ExpressionType.Not);

            TestExpressionTracer.Validate(not, new List<string> { errorMessage });
        }

        public void Dispose()
        {
        }
    }
}

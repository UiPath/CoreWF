// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using exp = System.Linq.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class Subtract : IDisposable
    {
        /// <summary>
        /// Subtract two positive integers.
        /// </summary>        
        [Fact]
        public void SubtractTwoPositiveIntegers()
        {
            TestSubtract<int, int, int> sub = new TestSubtract<int, int, int>(4, 4);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, int>(sub, "0");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestSubtract<int, int, int>(),
                                                           new Dictionary<string, object> { { "Left", 27 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", 22 } },
                                                           null);
        }

        [Fact]
        public void DefaultCheckedSubtract()
        {
            //  
            //  Test case description:
            //  Checked should be defaulted to true.
            Subtract<int, int, int> sub = new Subtract<int, int, int>();

            if (sub.Checked != true)
                throw new Exception("Checked is not defaulted to true");
        }

        [Fact]
        public void CheckedSubOverflow()
        {
            //  
            //  Test case description:
            //  subtract two integers which result in overflow of integer. OverflowException is expected.

            TestSubtract<int, int, int> sub = new TestSubtract<int, int, int>()
            {
                Checked = true,
                Right = 1,
                Left = int.MinValue,
                HintExceptionThrown = typeof(OverflowException)
            };

            TestRuntime.RunAndValidateAbortedException(sub, typeof(OverflowException), null);
        }

        //[Fact]
        //public void UnCheckedSubOverflow()
        //{
        //    //  
        //    //  Test case description:
        //    //  unchecked subtract two integers which result in overflow of integer.

        //    Variable<int> result = new Variable<int>("result");
        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables =
        //        {
        //            result
        //        },
        //        Activities =
        //        {
        //            new TestSubtract<int, int, int>()
        //            {
        //                Checked = false,
        //                Right = 1,
        //                Left = int.MinValue,
        //                Result = result
        //            },
        //            new TestWriteLine() { MessageActivity = new TestVisualBasicValue<string>("result.ToString()"), HintMessage = unchecked(int.MinValue - 1).ToString() }
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        /// <summary>
        /// Subtract an integer and double. Validation exception expected.
        /// Subtract  an integer and a double. Validation exception expected.
        /// </summary>        
        [Fact]
        public void SubtractTwoIncompatibleTypes()
        {
            TestSubtract<int, string, string> sub = new TestSubtract<int, string, string>
            {
                Left = 12,
                Right = "12"
            };

            TestRuntime.ValidateInstantiationException(sub, TestExpressionTracer.GetExceptionMessage<int, string, string>(exp.ExpressionType.Subtract));
        }

        /// <summary>
        /// Subtract DateTime type with TimeSpan.
        /// </summary>        
        [Fact]
        public void SubtractTwoCompatibleDifferentTypes()
        {
            TestSubtract<DateTime, TimeSpan, DateTime> sub = new TestSubtract<DateTime, TimeSpan, DateTime>
            {
                Left = new DateTime(2009, 2, 12),
                Right = new TimeSpan(17, 58, 59)
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<DateTime, TimeSpan, DateTime>(sub, @"2/11/2009 6:01:01 AM");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Add two operands of custom type which overloads sub operator.
        /// Both operands of custom type which overloads subtract operator
        /// </summary>        
        [Fact]
        public void CustomTypeOverloadedSubOperatorAsOperands()
        {
            TestSubtract<Complex, Complex, Complex> subComplex = new TestSubtract<Complex, Complex, Complex>
            {
                LeftExpression = context => new Complex(1, 2),
                RightExpression = context => new Complex(2, 3),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, Complex>(subComplex, "-1 -1");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try adding null with valid right operand. Validation exception expected.
        /// Try subtracting null with valid right operand. Validation exception expected.
        /// </summary>        
        [Fact]
        public void LeftOperandNull()
        {
            TestSubtract<int, int, int> sub = new TestSubtract<int, int, int>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(sub, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try adding null with valid left operand. Validation exception expected.
        /// Try subtracting valid left operand with null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void RightOperandNull()
        {
            TestSubtract<int, int, int> sub = new TestSubtract<int, int, int>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");


            TestRuntime.ValidateWorkflowErrors(sub, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestSubtract<int, string, string> sub = new TestSubtract<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Subtract);

            TestExpressionTracer.Validate(sub, new List<string> { errorMessage });
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestSubtract<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType> sub = new TestSubtract<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>
            {
                LeftExpression = context => new OverLoadOperatorThrowingType(13),
                RightExpression = context => new OverLoadOperatorThrowingType(14),
            };
            OverLoadOperatorThrowingType.ThrowException = true;

            sub.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>(sub, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(ArithmeticException), null);
        }

        public void Dispose()
        {
        }
    }
}

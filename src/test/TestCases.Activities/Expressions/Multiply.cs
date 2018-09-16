// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using exp = System.Linq.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class Multiply : IDisposable
    {
        /// <summary>
        /// Multiply two positive integers.
        /// </summary>        
        [Fact]
        public void MultiplyTwoPositiveIntegers()
        {
            TestMultiply<int, int, int> multiply = new TestMultiply<int, int, int>(3, 4);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, int>(multiply, "12");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestMultiply<int, int, int>(),
                                                           new Dictionary<string, object> { { "Left", 5 }, { "Right", 5 } },
                                                           new Dictionary<string, object> { { "Result", 25 } },
                                                           null);
        }

        /// <summary>
        /// Multiply two operands of custom type which overloads multiply operator.
        /// Both operands of custom type which overloads multiply operator
        /// </summary>        
        [Fact]
        public void CustomTypeOverloadedMultiplyOperatorAsOperands()
        {
            TestMultiply<Complex, Complex, Complex> mulComplex = new TestMultiply<Complex, Complex, Complex>
            {
                LeftExpression = context => new Complex(1, 2),
                RightExpression = context => new Complex(2, 3),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, Complex>(mulComplex, "2 6");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Multiply an integer and double. Validation exception expected.
        /// Multiply an integer and a double. Validation exception expected.
        /// </summary>        
        [Fact]
        public void MultiplyTwoIncompatibleTypes()
        {
            TestMultiply<int, string, string> multiply = new TestMultiply<int, string, string>
            {
                Left = 12,
                Right = "12"
            };

            TestRuntime.ValidateInstantiationException(multiply, TestExpressionTracer.GetExceptionMessage<int, string, string>(exp.ExpressionType.Multiply));
        }

        /// <summary>
        /// Try multiplying null with valid right operand. Validation exception expected.
        /// </summary>        
        [Fact]
        public void LeftOperandNull()
        {
            TestMultiply<int, int, int> multiply = new TestMultiply<int, int, int>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(multiply, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try multiplying null with valid left operand. Validation exception expected.
        /// Try multiplying valid left operand with null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void RightOperandNull()
        {
            TestMultiply<int, int, int> multiply = new TestMultiply<int, int, int>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(multiply, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        [Fact]
        public void DefaultCheckedMultiply()
        {
            //  
            //  Test case description:
            //  Checked should be defaulted to true.
            Multiply<int, int, int> multiply = new Multiply<int, int, int>();

            if (multiply.Checked != true)
                throw new Exception("Checked is not defaulted to true");
        }

        [Fact]
        public void CheckedMultiplyOverflow()
        {
            //  
            //  Test case description:
            //  multiply two integers which result in overflow of integer. OverflowException is expected.

            TestMultiply<int, int, int> multiply = new TestMultiply<int, int, int>()
            {
                Checked = true,
                Right = 2,
                Left = int.MaxValue,
                HintExceptionThrown = typeof(OverflowException)
            };

            TestRuntime.RunAndValidateAbortedException(multiply, typeof(OverflowException), null);
        }

        //[Fact]
        //public void UnCheckedMultiplyOverflow()
        //{
        //    //  
        //    //  Test case description:
        //    //  unchecked multiply two integers which result in overflow of integer.

        //    Variable<int> result = new Variable<int>("result");
        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables =
        //        {
        //            result
        //        },
        //        Activities =
        //        {
        //            new TestMultiply<int, int, int>()
        //            {
        //                Checked = false,
        //                Right = 2,
        //                Left = int.MaxValue,
        //                Result = result
        //            },
        //            new TestWriteLine() { MessageActivity = new TestVisualBasicValue<string>("result.ToString()"), HintMessage = unchecked(int.MaxValue * 2).ToString() }
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        [Fact]
        public void ConstraintViolationIncompatibleTypes()
        {
            TestMultiply<int, string, string> mul = new TestMultiply<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Multiply);

            TestExpressionTracer.Validate(mul, new List<string> { errorMessage });
        }

        public void Dispose()
        {
        }
    }
}

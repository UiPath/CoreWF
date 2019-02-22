// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using TestCases.Activities.Common.Expressions;
using Test.Common.TestObjects.Utilities;
using exp = System.Linq.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class Add : IDisposable
    {
        /// <summary>
        /// Add two positive integers.
        /// </summary>        
        [Fact]
        public void AddTwoPositiveIntegers()
        {
            TestAdd<int, int, int> add = new TestAdd<int, int, int>(3, 4);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, int>(add, "7");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Add two operands of custom type which overloads add operator.
        /// </summary>        
        [Fact]
        public void CustomTypeOverloadedAddOperatorAsOperands()
        {
            TestAdd<Complex, Complex, Complex> addComplex = new TestAdd<Complex, Complex, Complex>()
            {
                LeftExpression = context => new Complex(1, 2),
                RightExpression = context => new Complex(2, 3),
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<Complex, Complex, Complex>(addComplex, "3 5");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Invoke with WorkflowInvoker
        /// </summary>        
        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestAdd<int, int, int>(),
                                                    new Dictionary<string, object> { { "Left", 1 }, { "Right", 2 } },
                                                    new Dictionary<string, object> { { "Result", 3 } },
                                                    new List<object>());
        }

        /// <summary>
        /// Add DateTime type with TimeSpan.
        /// </summary>        
        [Fact]
        public void AddTwoCompatibleDifferentTypes()
        {
            TestAdd<DateTime, TimeSpan, DateTime> add = new TestAdd<DateTime, TimeSpan, DateTime>
            {
                Left = new DateTime(2009, 2, 12),
                Right = new TimeSpan(17, 58, 59)
            };

            DateTime expectedDateTime = new DateTime(2009, 2, 12, 17, 58, 59);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<DateTime, TimeSpan, DateTime>(add, expectedDateTime.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Add an integer and double. Validation exception expected.
        /// </summary>        
        [Fact]
        public void AddTwoIncompatibleTypes()
        {
            TestAdd<int, string, string> add = new TestAdd<int, string, string>
            {
                Left = 12,
                Right = "12"
            };

            TestRuntime.ValidateInstantiationException(add, TestExpressionTracer.GetExceptionMessage<int, string, string>(exp.ExpressionType.Add));
        }

        /// <summary>
        /// Try adding null with valid right operand. Validation exception expected.
        /// </summary>        
        [Fact]
        public void LeftOperandNull()
        {
            TestAdd<int, int, int> add = new TestAdd<int, int, int>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(add, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try adding null with valid left operand. Validation exception expected.
        /// Try adding valid left operand with null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void RightOperandNull()
        {
            TestAdd<int, int, int> add = new TestAdd<int, int, int>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");


            TestRuntime.ValidateWorkflowErrors(add, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        [Fact]
        public void DefaultCheckedAdd()
        {
            //  
            //  Test case description:
            //  Checked should be defaulted to true.
            Add<int, int, int> add = new Add<int, int, int>();

            if (add.Checked != true)
                throw new Exception("Checked is not defaulted to true");
        }

        [Fact]
        public void CheckedAddOverflow()
        {
            //  
            //  Test case description:
            //  add two integers which result in overflow of integer. OverflowException is expected.

            TestAdd<int, int, int> add = new TestAdd<int, int, int>()
            {
                Checked = true,
                Right = int.MaxValue,
                Left = 1,
                HintExceptionThrown = typeof(OverflowException)
            };

            TestRuntime.RunAndValidateAbortedException(add, typeof(OverflowException), null);
        }

        [Fact]
        public void UnCheckedAddOverflow()
        {
            //  
            //  Test case description:
            //  unchecked add two integers which result in overflow of integer.

            Variable<int> result = new Variable<int>("result");
            TestSequence seq = new TestSequence()
            {
                Variables =
                {
                    result
                },
                Activities =
                {
                    new TestAdd<int, int, int>()
                    {
                        Checked = false,
                        Right = int.MaxValue,
                        Left = 1,
                        Result = result
                    },
                    new TestWriteLine() { MessageExpression = e => result.Get(e).ToString(), HintMessage = unchecked(int.MaxValue + 1).ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void ConstraintViolationIncompatibleTypes()
        {
            TestAdd<int, string, string> add = new TestAdd<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.Add);

            TestExpressionTracer.Validate(add, new List<string> { errorMessage });
        }

        public void Dispose()
        {
        }
    }
}

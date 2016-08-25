// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
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
    public class And : IDisposable
    {
        /// <summary>
        /// Evaluate true AND false.
        /// Evaluate true AND false
        /// </summary>        
        [Fact]
        public void TwoBooleansInAnd()
        {
            TestAnd<bool, bool, bool> and = new TestAnd<bool, bool, bool>(true, false);

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<bool, bool, bool>(and, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Compute AND of two integral types.
        /// </summary>        
        [Fact]
        public void ComputeIntegralAnd()
        {
            TestAnd<int, int, int> and = new TestAnd<int, int, int>(2, 2); //010 & 010 = 010

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<int, int, int>(and, "2");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// AND two operands of custom type which overloads AND operator.
        /// </summary>        
        [Fact]
        public void CustomTypeOperandWithOperatorOverloaded()
        {
            TestAnd<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType> testAnd = new TestAnd<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>();
            testAnd.ProductAnd.Left = new InArgument<OverLoadOperatorThrowingType>(context => new OverLoadOperatorThrowingType(13));
            testAnd.ProductAnd.Right = new InArgument<OverLoadOperatorThrowingType>(context => new OverLoadOperatorThrowingType(14));
            OverLoadOperatorThrowingType.ThrowException = false;

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>(testAnd, "12"); //1101 & 1110 = 1100

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Throw from overloaded operator.
        /// </summary>        
        [Fact]
        public void ThrowFromOverloadedOperator()
        {
            TestAnd<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType> testAnd = new TestAnd<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>();
            testAnd.ProductAnd.Left = new InArgument<OverLoadOperatorThrowingType>(context => new OverLoadOperatorThrowingType(13));
            testAnd.ProductAnd.Right = new InArgument<OverLoadOperatorThrowingType>(context => new OverLoadOperatorThrowingType(14));
            OverLoadOperatorThrowingType.ThrowException = true;

            testAnd.ExpectedOutcome = Outcome.UncaughtException();

            TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<OverLoadOperatorThrowingType, OverLoadOperatorThrowingType, OverLoadOperatorThrowingType>(testAnd, "12");

            TestRuntime.RunAndValidateAbortedException(seq, typeof(DivideByZeroException), null);
        }

        /// <summary>
        /// Try adding null with valid right operand. Validation exception expected.
        /// Try evaluating null AND with bool. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestAnd<int, int, int> and = new TestAnd<int, int, int>
            {
                Right = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Left");

            TestRuntime.ValidateWorkflowErrors(and, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try adding null with valid left operand. Validation exception expected.
        /// Try executing AND activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestAnd<int, int, int> and = new TestAnd<int, int, int>
            {
                Left = 12
            };

            string errorMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Right");

            TestRuntime.ValidateWorkflowErrors(and, new List<TestConstraintViolation>(), typeof(ArgumentException), errorMessage);
        }

        /// <summary>
        /// Try evaluating AND of Boolean and string. Validation exception.
        /// </summary>        
        [Fact]
        public void TryANDOnIncompatibleTypes()
        {
            TestAnd<bool, string, bool> and = new TestAnd<bool, string, bool>
            {
                Left = true,
                Right = "true"
            };

            TestRuntime.ValidateInstantiationException(and, TestExpressionTracer.GetExceptionMessage<bool, string, bool>(exp.ExpressionType.And));
        }

        /// <summary>
        /// Persist And activity which blocked on left activity execution.
        /// </summary>        
        //[Fact]
        //public void PersistWhileBlockedInLeft()
        //{
        //    TestBlockingActivity blocking = new TestBlockingActivity("BlockingActivity");

        //    TestAnd<bool, bool, bool> and = new TestAnd<bool, bool, bool>()
        //    {
        //        LeftActivity = new TestExpressionEvaluatorWithBody<bool>(true)
        //        {
        //            Body = blocking
        //        },
        //        Right = false
        //    };

        //    TestSequence seq = TestExpressionTracer.GetTraceableBinaryExpressionActivity<bool, bool, bool>(and, false.ToString());

        //    using (TestWorkflowRuntime testWorkflowRuntime = TestRuntime.CreateTestWorkflowRuntime(seq))
        //    {
        //        testWorkflowRuntime.ExecuteWorkflow();
        //        testWorkflowRuntime.WaitForActivityStatusChange(blocking.DisplayName, TestActivityInstanceState.Executing);
        //        testWorkflowRuntime.PersistWorkflow();
        //        testWorkflowRuntime.ResumeBookMark(blocking.DisplayName, null);
        //        testWorkflowRuntime.WaitForCompletion();
        //    }
        //}

        /// <summary>
        /// Invoke with workflowInvoker.Invoke
        /// </summary>        
        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestAnd<bool, bool, bool> and = new TestAnd<bool, bool, bool>();

            TestRuntime.RunAndValidateUsingWorkflowInvoker(and,
                                                            new Dictionary<string, object> { { "Left", true }, { "Right", false } },
                                                            new Dictionary<string, object> { { "Result", false } },
                                                            new List<object>());
        }

        //This is disabled in desktop and failing too.
        //[Fact]
        public void ThrowWhileEvaluatingLeft()
        {
            TestAnd<bool, bool, bool> and = new TestAnd<bool, bool, bool>
            {
                LeftActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<ArithmeticException>()
                },
                Right = true
            };

            TestRuntime.RunAndValidateAbortedException(and, typeof(ArithmeticException), null);
        }

        [Fact]
        public void ConstraintViolationIncompatibleTypes()
        {
            TestAnd<int, string, string> and = new TestAnd<int, string, string>();

            string errorMessage = TestExpressionTracer.GetExceptionMessage<int, string, string>(System.Linq.Expressions.ExpressionType.And);

            TestExpressionTracer.Validate(and, new List<string> { errorMessage });
        }

        public void Dispose()
        {
        }
    }
}

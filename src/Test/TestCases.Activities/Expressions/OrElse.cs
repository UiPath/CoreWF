// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class OrElse : IDisposable
    {
        /// <summary>
        /// Evaluate true || false.
        /// </summary>        
        [Fact]
        public void TwoBooleansInOrElse()
        {
            TestOrElse orElse = new TestOrElse(false, true);

            TestSequence seq = TestExpressionTracer.GetTraceableBoolResultActivity(orElse, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestOrElse(false, true),
                                                           null,
                                                           new Dictionary<string, object> { { "Result", true } },
                                                           null);
        }

        /// <summary>
        /// Execute OrElse with left returning True. Make sure right activity is not executed.
        /// </summary>        
        [Fact]
        public void ShortCircuitEvaluation()
        {
            TestOrElse orElse = new TestOrElse
            {
                LeftActivity = new TestExpressionEvaluator<bool>
                {
                    ExpressionResult = true
                },
                RightActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<Exception>()
                },
                HintShortCircuit = true
            };

            TestSequence seq = TestExpressionTracer.GetTraceableBoolResultActivity(orElse, true.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Throw exception while evaluating right activity.
        /// </summary>
        /// This test is disabled in desktop and failing too.         
        //[Fact]
        public void ThrowExceptionInRightActivity()
        {
            TestOrElse orElse = new TestOrElse
            {
                LeftActivity = new TestExpressionEvaluator<bool>
                {
                    ExpressionResult = false
                },
                RightActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<ArithmeticException>()
                },
                ExceptionInRight = true
            };

            TestRuntime.RunAndValidateAbortedException(orElse, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Throw exception in left activity.
        /// </summary>
        /// This test is disabled in desktop and failing too.         
        //[Fact]
        public void ThrowExceptionWhileEvaluatingLeft()
        {
            TestOrElse orElse = new TestOrElse
            {
                LeftActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<ArithmeticException>()
                },
                RightActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<ArithmeticException>()
                },
                ExceptionInLeft = true
            };

            TestRuntime.RunAndValidateAbortedException(orElse, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Try executing OrElse activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            Variable<bool> var = new Variable<bool> { Default = true };
            TestOrElse orElse = new TestOrElse { Left = var };

            TestSequence seq = new TestSequence
            {
                Variables = { var },
                Activities = { orElse }
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument,
                "Right",
                "OrElse",
                orElse.DisplayName),
                orElse.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(
                seq,
                constraints,
                string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Right", "OrElse", orElse.DisplayName));
        }

        /// <summary>
        /// Try evaluating null OrElse with bool. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            Variable<bool> var = new Variable<bool> { Default = true };
            TestOrElse orElse = new TestOrElse { Right = var };

            TestSequence seq = new TestSequence
            {
                Variables = { var },
                Activities = { orElse }
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument,
                "Left",
                "OrElse",
                orElse.DisplayName),
                orElse.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(
                seq,
                constraints,
                string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Left", "OrElse", orElse.DisplayName));
        }

        public void Dispose()
        {
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
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
    public class AndAlso : IDisposable
    {
        /// <summary>
        /// Evaluate true && false.
        /// </summary>        
        [Fact]
        public void TwoBooleansInAndAlso()
        {
            TestAndAlso andAlso = new TestAndAlso(true, false);

            TestSequence seq = TestExpressionTracer.GetTraceableBoolResultActivity(andAlso, false.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Execute AndAlso with left returning False. Make sure right activity is not executed.
        /// </summary>        

        [Fact]
        public void ShortCircuitEvaluation()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                Left = false,
                RightActivity = new TestExpressionEvaluatorWithBody<bool> { Body = new TestThrow<Exception>() },
                HintShortCircuit = true
            };

            TestRuntime.RunAndValidateWorkflow(andAlso);
        }

        /// <summary>
        /// Set right operand null.
        /// Try executing AndAlso activity by setting right operand null. Validation exception.
        /// </summary>        
        [Fact]
        public void SetRightOperandNull()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                DisplayName = "Somename",
                Left = false
            };

            andAlso.ProductAndAlso.Right = null;

            string error = string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Right", "AndAlso", andAlso.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, andAlso.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(andAlso, constraints, typeof(InvalidWorkflowException), error);
        }

        /// <summary>
        /// Throw exception in left activity.
        /// </summary>  
        /// This is disabled in desktop and failing too.      
        //[Fact]
        public void ThrowExceptionWhileEvaluatingLeft()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                LeftActivity = new TestExpressionEvaluatorWithBody<bool>
                {
                    Body = new TestThrow<ArithmeticException>()
                },
                Right = false,
                ExceptionInLeft = true
            };

            TestRuntime.RunAndValidateAbortedException(andAlso, typeof(ArithmeticException), null);
        }

        /// <summary>
        /// Invoke with workflow invoker
        /// </summary>        
        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestAndAlso andAlso = new TestAndAlso { Left = true, Right = false };
            TestRuntime.RunAndValidateUsingWorkflowInvoker(andAlso,
                                                           null,
                                                           new Dictionary<string, object> { { "Result", false } },
                                                           null);
        }

        [Fact]
        public void ConstraintViolationRightNull()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                DisplayName = "Somename",
                Left = false
            };

            andAlso.ProductAndAlso.Right = null;

            string error = string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Right", "AndAlso", andAlso.DisplayName);

            TestExpressionTracer.Validate(andAlso, new List<string> { error });
        }

        [Fact]
        public void ConstraintViolationLeftNull()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                DisplayName = "Somename",
                Right = false
            };

            andAlso.ProductAndAlso.Left = null;

            string error = string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Left", "AndAlso", andAlso.DisplayName);

            TestExpressionTracer.Validate(andAlso, new List<string> { error });
        }

        /// <summary>
        /// Set right operand null.
        /// Try evaluating null AndAlso with bool. Validation exception.
        /// </summary>        
        [Fact]
        public void SetLeftOperandNull()
        {
            TestAndAlso andAlso = new TestAndAlso
            {
                DisplayName = "Somename",
                Right = false
            };

            andAlso.ProductAndAlso.Left = null;

            string error = string.Format(ErrorStrings.BinaryExpressionActivityRequiresArgument, "Left", "AndAlso", andAlso.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, andAlso.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(andAlso, constraints, typeof(InvalidWorkflowException), error);
        }

        public void Dispose()
        {
        }
    }
}

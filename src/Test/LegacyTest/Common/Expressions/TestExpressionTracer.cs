// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq.Expressions;
using exp = System.Linq.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities;
using LegacyTest.Test.Common.TestObjects.Activities.Expressions;
using LegacyTest.Test.Common.TestObjects.Runtime;
using LegacyTest.Test.Common.TestObjects.Runtime.ConstraintValidation;

namespace LegacyLegacyTest.Test.Common.Expressions
{
    public static class TestExpressionTracer
    {
        public static TestSequence GetTraceableBinaryExpressionActivity<TLeft, TRight, TResult>(ITestBinaryExpression<TLeft, TRight, TResult> binaryActivity, string expectedResult)
        {
            Variable<TResult> result = new Variable<TResult>() { Name = "Result" };

            binaryActivity.Result = result;

            return new TestSequence()
            {
                Variables = { result },
                Activities =
                {
                    (TestActivity)binaryActivity,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = expectedResult }
                }
            };
        }

        public static TestSequence GetTraceableUnaryExpressionActivity<TOperand, TResult>(ITestUnaryExpression<TOperand, TResult> unaryActivity, string expectedResult)
        {
            Variable<TResult> result = new Variable<TResult>() { Name = "Result" };

            unaryActivity.Result = result;

            return new TestSequence()
            {
                Variables = { result },
                Activities =
                {
                    (TestActivity)unaryActivity,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = expectedResult }
                }
            };
        }

        public static TestSequence GetTraceableBoolResultActivity(ITestBoolReturningActivity activity, string expectedResult)
        {
            Variable<bool> result = new Variable<bool>() { Name = "Result" };

            activity.Result = result;

            return new TestSequence()
            {
                Variables = { result },
                Activities =
                {
                    (TestActivity)activity,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = expectedResult }
                }
            };
        }

        public static TestSequence GetTraceablePropertyValue<TOperand, TResult>(TestPropertyValue<TOperand, TResult> propertyValue, string expectedResult)
        {
            Variable<TResult> result = new Variable<TResult>() { Name = "Result" };
            propertyValue.Result = result;

            return new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    propertyValue,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = expectedResult }
                }
            };
        }

        public static string GetExceptionMessage<TLeft, TRight, TResult>(exp.ExpressionType operatorType)
        {
            ParameterExpression leftParameter = Expression.Parameter(typeof(TLeft), "left");
            ParameterExpression rightParameter = Expression.Parameter(typeof(TRight), "right");
            try
            {
                Expression.MakeBinary(operatorType, leftParameter, rightParameter);
                throw new Exception();
            }
            catch (InvalidOperationException e)
            {
                return e.Message;
            }
        }

        public static string GetExceptionMessage<TOperand, TResult>(exp.ExpressionType operatorType)
        {
            ParameterExpression operand = Expression.Parameter(typeof(TOperand), "operand");

            try
            {
                Expression.MakeUnary(operatorType, operand, typeof(TResult));
                throw new Exception(
                    string.Format("Expression.MakeUnary was expected to throw exception for: TOperand of type {0} and TResult of type {1} for ExpressionType {2}",
                    typeof(TOperand).Name,
                    typeof(TResult).Name,
                    operatorType));
            }
            catch (InvalidOperationException e)
            {
                return e.Message;
            }
        }


        public static void Validate(TestActivity activity, List<string> errors)
        {
            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();

            if (errors != null)
            {
                foreach (string error in errors)
                {
                    constraints.Add(new TestConstraintViolation(error, activity.ProductActivity));
                }
            }

            TestRuntime.ValidateConstraints(activity, constraints, null);
        }
    }
}

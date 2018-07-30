// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class NewExpression
    {
        /// <summary>
        /// New without argument and validate the conversion result
        /// </summary>        
        [Fact]
        public void NewWithoutArgument()
        {
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = new New<int>(),
                ExpressionTree = Expression.New(typeof(int))
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// New with arguments and validate the conversion result
        /// </summary>        
        [Fact]
        public void NewWithArgument()
        {
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(String),
                ExpectedNode = new New<String>()
                {
                    Arguments =
                    {
                        new InArgument<char>('a')
                        {
                            EvaluationOrder = 0
                        },
                        new InArgument<int>(1)
                        {
                            EvaluationOrder = 1,
                        },
                    }
                },
                ExpressionTree = Expression.New(
                    typeof(String).GetConstructor(new Type[] { typeof(char), typeof(int) }),
                    Expression.Constant('a'),
                    Expression.Constant(1))
            };

            ExpressionTestRuntime.ValidateExpressionXaml<String>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// New with initializer syntax. This is not supported, and error is expected
        /// </summary>        
        [Fact]
        public void NewWithInitializer()
        {
            NotSupportedException expectedException = new NotSupportedException(
                string.Format(ErrorStrings.UnsupportedExpressionType, ExpressionType.MemberInit));

            ExpressionTestRuntime.Convert((env) => new DummyHelper() { StringVar = null }, expectedException);
        }

        /// <summary>
        /// Convert a new array expression, and validate the conversion result
        /// </summary>        
        [Fact]
        public void NewArray()
        {
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int[]),
                ExpectedNode = new NewArray<int[]>()
                {
                    Bounds =
                    {
                        new InArgument<int>(10)
                        {
                            EvaluationOrder = 0
                        }
                    }
                },
                ExpressionTree = Expression.NewArrayBounds(
                    typeof(int), Expression.Constant(10))
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int[]>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// New array with initialize syntax. This is not supported, and It should throw error.
        /// </summary>        
        [Fact]
        public void NewArrayInit()
        {
            NotSupportedException expectedException = new NotSupportedException(
                string.Format(ErrorStrings.UnsupportedExpressionType, ExpressionType.NewArrayInit));

            Activity we = ExpressionTestRuntime.Convert((env) => new int[] { 1, 2, 3 }, expectedException);
        }
    }
}

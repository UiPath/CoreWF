// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class UnaryExpression
    {
        private List<NodeExpressionPair> _leafNode;
        private List<UnaryOperator> _operators;

        [Fact]
        //  Convert cast expression
        public void ConvertCast()
        {
            NodeExpressionPair node = ExpressionLeafs.StaticIntField;

            Expression linq = Expression.Convert(node.LeafExpression, typeof(double));

            Activity<double> expectedActivity = new Cast<int, double>()
            {
                Checked = false,
                Operand = new InArgument<int>(
                    (Activity<int>)node.GetLeafNode())
            };

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(double),
                ExpectedNode = expectedActivity,
                ExpressionTree = linq
            };

            ExpressionTestRuntime.ValidateExpressionXaml<double>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Cast type that has operator overloading
        /// </summary>        
        [Fact]
        //  Not operator with overloading
        public void NotOperatorWithOverloading()
        {
            Expression<Func<ActivityContext, DummyHelper>> expression = (env) => !DummyHelper.Instance;
            Activity<DummyHelper> expectedActivity = new InvokeMethod<DummyHelper>()
            {
                MethodName = "op_LogicalNot",
                TargetType = typeof(DummyHelper),
                Parameters =
                {
                    new InArgument<DummyHelper>(
                        new FieldValue<DummyHelper, DummyHelper>()
                        {
                            FieldName = "Instance"
                        })
                }
            };

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(DummyHelper),
                ExpectedNode = expectedActivity,
                ExpressionTree = expression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<DummyHelper>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null, typeof(DummyHelper));
        }

        /// <summary>
        /// Convert an unsupported unary operator and validate the error .
        /// </summary>        
        [Fact]
        // Unsupported unary operator
        public void UnsupportedUnaryOperator()
        {
            NotSupportedException expectedException = new NotSupportedException(
                string.Format(ErrorStrings.UnsupportedExpressionType, ExpressionType.NegateChecked));

            Expression<Func<ActivityContext, int>> expression =
                Expression.Lambda<Func<ActivityContext, int>>(
                    Expression.MakeUnary(ExpressionType.NegateChecked, Expression.Constant(100), typeof(int)),
                    Expression.Parameter(typeof(ActivityContext), "context"));

            ExpressionTestRuntime.Convert(expression, expectedException);
        }

        /// <summary>
        /// Permute the expression tree of different expression that returns value type. Convert the expression tree. Compare the activity tree and validate the execution result.
        /// </summary>        
        [Fact]
        //  Test value type unary expression
        public void ValueTypeUnaryExpressionTest()
        {
            _operators = new List<UnaryOperator>()
            {
                UnaryOperator.Not,
                UnaryOperator.Cast,
                UnaryOperator.CheckedCast
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstIntValue,
                ExpressionLeafs.StaticIntField,
                ExpressionLeafs.StaticIntProperty,
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<int>()
            };

            int numberOfTests = 0;
            foreach (TestUnaryExpression expr in EnumerateTest<int>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());
                ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
                ExpressionTestRuntime.ValidateExecutionResult(expr, variables);
            }
        }

        /// <summary>
        /// Permute the expression tree of TypeAs and Cast with various expressions as operands. Convert the expression tree. Compare the activity tree and validate the execution result.
        /// </summary>        
        [Fact]
        //  Test Cast TypeAs unary expression
        public void CastTypeAsTest()
        {
            _operators = new List<UnaryOperator>()
            {
                UnaryOperator.Cast,
                UnaryOperator.CheckedCast,
                UnaryOperator.TypeAs
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstStringValue,
                ExpressionLeafs.StaticStringField,
                ExpressionLeafs.StaticStringProperty,
            };

            Variable<String> stringVar = LeafHelper.GetVariable<string>();
            stringVar.Default = "String Variable";

            List<Variable> variables = new List<Variable>()
            {
                stringVar
            };

            int numberOfTests = 0;
            foreach (TestUnaryExpression expr in EnumerateTest<String>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());
                ExpressionTestRuntime.ValidateExpressionXaml<string>(expr);
                ExpressionTestRuntime.ValidateExecutionResult(expr, variables);
            }
        }

        private IEnumerable EnumerateTest<T>(int level, int maxLevel)
        {
            // for non-leaf node, it could be a unary expression or one of the pre-defined node
            if (level < maxLevel)
            {
                foreach (UnaryOperator op in _operators)
                {
                    TestUnaryExpression expression = new TestUnaryExpression
                    {
                        ResultType = typeof(T)
                    };
                    expression.ResultType = typeof(T);
                    expression.Operator = op;
                    foreach (TestExpression operand in EnumerateTest<T>(level + 1, maxLevel))
                    {
                        operand.ResultType = typeof(T);
                        expression.Operand = operand;
                        yield return expression;
                    }
                }
            }

            // leaf node return const node only
            if (level == maxLevel)
            {
                NodeExpressionPair leaf = LeafHelper.GetConstNode(typeof(T));
                yield return new TestExpression()
                {
                    ExpectedNode = leaf.GetLeafNode(),
                    ExpressionTree = leaf.LeafExpression
                };
            }
            // root can't be leaf node
            else if (level != 0)
            {
                foreach (NodeExpressionPair leaf in _leafNode)
                {
                    TestExpression te = new TestExpression()
                    {
                        ExpectedNode = leaf.GetLeafNode(),
                        ExpressionTree = leaf.LeafExpression
                    };
                    yield return te;
                }
            }
        }
    }
}

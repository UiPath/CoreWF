// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class BinaryExpression
    {
        private List<NodeExpressionPair> _leafNode;
        private List<BinaryOperator> _operators;

        /// <summary>
        /// Add String and String
        /// </summary>        
        [Fact]
        //  Add two strings
        public void AddTwoStrings()
        {
            Expression<Func<ActivityContext, string>> expression =
                (env) => DummyHelper.StaticStringField1 + DummyHelper.StaticStringField2 + DummyHelper.StaticStringField3;

            Activity<string> expectedActivity = new InvokeMethod<string>()
            {
                MethodName = "Concat",
                TargetType = typeof(string),
                Parameters =
                {
                    new InArgument<string>()
                    {
                        EvaluationOrder = 0,
                        Expression = new InvokeMethod<string>()
                        {
                            MethodName = "Concat",
                            TargetType = typeof(string),
                            Parameters =
                            {
                                new InArgument<string>()
                                {
                                    EvaluationOrder = 0,
                                    Expression = new FieldValue<DummyHelper, string>()
                                    {
                                        FieldName = "StaticStringField1"
                                    },
                                },
                                new InArgument<string>()
                                {
                                    EvaluationOrder = 1,
                                    Expression = new FieldValue<DummyHelper, string>()
                                    {
                                        FieldName = "StaticStringField2"
                                    },
                                }
                            }
                        },
                    },
                    new InArgument<string>()
                    {
                        EvaluationOrder = 1,
                        Expression = new FieldValue<DummyHelper, string>()
                        {
                            FieldName = "StaticStringField3"
                        },
                    }
                }
            };

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(string),
                ExpectedNode = expectedActivity,
                ExpressionTree = expression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<string>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Add DateTime and TimeSpan
        /// </summary>        
        [Fact]
        //  Add different types that has operator overloading
        public void AddDifferentTypes()
        {
            Expression<Func<ActivityContext, DateTime>> expression =
                (env) => DummyHelper.StaticDate + DummyHelper.StaticTimeSpan;

            Activity<DateTime> expectedActivity = new InvokeMethod<DateTime>()
            {
                MethodName = "op_Addition",
                TargetType = typeof(DateTime),
                Parameters =
                {
                    new InArgument<DateTime>()
                    {
                        EvaluationOrder = 0,
                        Expression = new FieldValue<DummyHelper, DateTime>()
                        {
                            FieldName = "StaticDate",
                        }
                    },
                    new InArgument<TimeSpan>()
                    {
                        EvaluationOrder = 1,
                        Expression = new FieldValue<DummyHelper, TimeSpan>()
                        {
                            FieldName = "StaticTimeSpan",
                        }
                    },
                }
            };

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(DateTime),
                ExpectedNode = expectedActivity,
                ExpressionTree = expression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<DateTime>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Add with generic types as operands, and validate the result
        /// </summary>        
        [Fact]
        //  Add with generic type as operands
        public void AddGenericTypes()
        {
            Add<Nullable<int>, Nullable<int>, Nullable<int>> expectedActivity = new Add<Nullable<int>, Nullable<int>, Nullable<int>>()
            {
                Left = new InArgument<int?>()
                {
                    EvaluationOrder = 0,
                    Expression = new New<Nullable<int>>()
                    {
                        Arguments =
                        {
                            new InArgument<int>(10)
                            {
                                EvaluationOrder = 0
                            }
                        }
                    },
                },
                Right = new InArgument<int?>()
                {
                    EvaluationOrder = 1,
                    Expression = new New<Nullable<int>>()
                    {
                        Arguments =
                            {
                                new InArgument<int>(20)
                                {
                                    EvaluationOrder = 0
                                }
                            }
                    }
                },
                Checked = false
            };

            ConstructorInfo ctorHandler = typeof(Nullable<int>).GetConstructors()[0];

            Expression expectedExpression = Expression.Add(
                Expression.New(ctorHandler, Expression.Constant(10)),
                Expression.New(ctorHandler, Expression.Constant(20)));

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(Nullable<int>),
                ExpectedNode = expectedActivity,
                ExpressionTree = expectedExpression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<Nullable<int>>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Permute the expression tree of different arithmetic & bitwise operators. Convert the expression tree. Compare the activity tree and validate the execution result
        /// </summary>        
        [Fact]
        //  Arithmetic operator test
        public void ArithmeticOperatorTest()
        {
            _operators = new List<BinaryOperator>()
            {
                BinaryOperator.Add,
                BinaryOperator.Subtract,
                BinaryOperator.Multiply,
                BinaryOperator.Divide
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstIntValue,
                ExpressionLeafs.StaticIntField,
                ExpressionLeafs.StaticIntProperty,
                ExpressionLeafs.UnsupportedBinaryOperator
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<int>()
            };

            int numberOfTests = 0;
            foreach (TestBinaryExpression binExpr in EnumerateTest<int>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());

                ExpressionTestRuntime.ValidateExpressionXaml<int>(binExpr);
                if (binExpr.ExpectedConversionException == null)
                {
                    ExpressionTestRuntime.ValidateExecutionResult(binExpr, variables);
                }
            }
        }

        [Fact]
        //  Checked arithmetic operator test
        public void CheckedArithmeticOperatorTest()
        {
            _operators = new List<BinaryOperator>()
            {
                BinaryOperator.CheckedAdd,
                BinaryOperator.CheckedSubtract,
                BinaryOperator.CheckedMultiply,
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstIntValue,
                ExpressionLeafs.StaticIntField,
                ExpressionLeafs.StaticIntProperty,
                ExpressionLeafs.UnsupportedBinaryOperator
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<int>()
            };

            int numberOfTests = 0;
            foreach (TestBinaryExpression binExpr in EnumerateTest<int>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());

                ExpressionTestRuntime.ValidateExpressionXaml<int>(binExpr);
                if (binExpr.ExpectedConversionException == null)
                {
                    ExpressionTestRuntime.ValidateExecutionResult(binExpr, variables);
                }
            }
        }

        [Fact]
        //  Bitwise operator test
        public void BitwiseOperatorTest()
        {
            _operators = new List<BinaryOperator>()
            {
                BinaryOperator.And,
                BinaryOperator.Or
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstIntValue,
                ExpressionLeafs.StaticIntField,
                ExpressionLeafs.StaticIntProperty,
                ExpressionLeafs.UnsupportedBinaryOperator
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<int>()
            };

            int numberOfTests = 0;
            foreach (TestBinaryExpression binExpr in EnumerateTest<int>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());

                ExpressionTestRuntime.ValidateExpressionXaml<int>(binExpr);
                if (binExpr.ExpectedConversionException == null)
                {
                    ExpressionTestRuntime.ValidateExecutionResult(binExpr, variables);
                }
            }
        }

        /// <summary>
        /// Permute the expression tree of different comparison operators. Convert the expression tree. Compare the activity tree and validate the execution result
        /// </summary>        
        [Fact]
        //  Comparison operator test
        public void ComparisonOperatorTest()
        {
            _operators = new List<BinaryOperator>()
            {
                BinaryOperator.Add,
                BinaryOperator.CheckedAdd,
                BinaryOperator.Subtract,
                BinaryOperator.CheckedSubtract,
                BinaryOperator.Multiply,
                BinaryOperator.CheckedMultiply,
                BinaryOperator.Divide,
                BinaryOperator.And,
                BinaryOperator.Or
            };

            List<BinaryOperator> comparisonOperators = new List<BinaryOperator>()
            {
                BinaryOperator.Equal,
                BinaryOperator.GreaterThan,
                BinaryOperator.GreaterThanOrEqual,
                BinaryOperator.LessThan,
                BinaryOperator.LessThanOrEqual
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstIntValue,
                ExpressionLeafs.StaticIntField,
                ExpressionLeafs.StaticIntProperty,
                ExpressionLeafs.VariableInWrapper
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<int>()
            };

            int numberOfTests = 0;

            foreach (BinaryOperator op in comparisonOperators)
            {
                TestBinaryExpression binExpr = new TestBinaryExpression()
                {
                    Operator = op,
                    ResultType = typeof(Boolean),
                };

                foreach (TestExpression left in EnumerateTest<int>(0, 1))
                {
                    numberOfTests++;
                    //Log.Info(numberOfTests.ToString());

                    binExpr.Left = left;

                    foreach (TestExpression right in EnumerateTest<int>(0, 1))
                    {
                        binExpr.Right = right;
                    }
                    ExpressionTestRuntime.ValidateExpressionXaml<bool>(binExpr);
                    if (binExpr.ExpectedConversionException == null)
                    {
                        ExpressionTestRuntime.ValidateExecutionResult(binExpr, variables);
                    }
                }
            }
        }

        /// <summary>
        /// Create a logical operator as root node of the expression tree. Create different expressions as the operands. Convert the expression tree. Compare the activity tree and validate the execution result
        /// </summary>        
        [Fact]
        //  Logical operator test
        public void LogicalOperatorTest()
        {
            _operators = new List<BinaryOperator>()
            {
                BinaryOperator.And,
                BinaryOperator.OrElse,
                BinaryOperator.Or,
                BinaryOperator.AndAlso
            };

            _leafNode = new List<NodeExpressionPair>()
            {
                ExpressionLeafs.ConstBooleanTrue,
                ExpressionLeafs.ConstBooleanFalse,
                ExpressionLeafs.StaticBooleanField,
                ExpressionLeafs.StaticBooleanProperty,
            };

            List<Variable> variables = new List<Variable>()
            {
                LeafHelper.GetVariable<Boolean>()
            };

            int numberOfTests = 0;
            foreach (TestBinaryExpression binExpr in EnumerateTest<Boolean>(0, 2))
            {
                numberOfTests++;
                //Log.Info(numberOfTests.ToString());
                ExpressionTestRuntime.ValidateExpressionXaml<Boolean>(binExpr);
                ExpressionTestRuntime.ValidateExecutionResult(binExpr, variables);
            }
        }

        /// <summary>
        /// Unsupported binary operator. Error is expected.
        /// </summary>        
        [Fact]
        //  Unsupported binary operator
        public void UnsupportedBinaryOperator()
        {
            NodeExpressionPair node = ExpressionLeafs.UnsupportedBinaryOperator;
            Expression<Func<ActivityContext, int>> lambdaExpression = Expression.Lambda<Func<ActivityContext, int>>((Expression)node.LeafExpression, Expression.Parameter(typeof(ActivityContext), "context"));

            NotSupportedException expectedException = new NotSupportedException(
                string.Format(ErrorStrings.UnsupportedExpressionType, ExpressionType.Coalesce));

            ExpressionTestRuntime.Convert(lambdaExpression, expectedException);
        }

        #region EnumerationHelper
        private IEnumerable EnumerateTest<T>(int level, int maxLevel)
        {
            // for non-leaf node, it could be a binary expression or one of the pre-defined node
            if (level < maxLevel)
            {
                foreach (BinaryOperator op in _operators)
                {
                    TestBinaryExpression binaryExpression = new TestBinaryExpression();
                    binaryExpression.ResultType = typeof(T);
                    binaryExpression.Operator = op;
                    foreach (TestExpression left in EnumerateTest<T>(level + 1, maxLevel))
                    {
                        left.ResultType = typeof(T);
                        binaryExpression.Left = left;
                        foreach (TestExpression right in EnumerateTest<T>(level + 1, maxLevel))
                        {
                            right.ResultType = typeof(T);
                            binaryExpression.Right = right;
                            yield return binaryExpression;
                        }
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
                    Exception expectedException = null;
                    object expectedNode = null;
                    try
                    {
                        expectedNode = leaf.GetLeafNode();
                    }
                    catch (Exception ex)
                    {
                        expectedException = ex;
                    }

                    TestExpression te = new TestExpression()
                    {
                        ExpectedConversionException = expectedException,
                        ExpectedNode = expectedNode,
                        ExpressionTree = leaf.LeafExpression
                    };
                    yield return te;
                }
            }
        }
        #endregion
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class MethodCallExpression
    {
        /// <summary>
        /// Convert a method call expression that has no parameter. Validate that the method call is converted properly
        /// </summary>        
        [Fact]
        //  Method call without argument
        public void MethodCallWithoutArgument()
        {
            NodeExpressionPair node = ExpressionLeafs.MethodCallNoParam;

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = node.GetLeafNode(),
                ExpressionTree = node.LeafExpression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Convert a method call expression that has parameter. Validate that the method call is converted properly.
        /// </summary>        
        [Fact]
        //  Method call with argument
        public void MethodCallWithArgument()
        {
            NodeExpressionPair node = ExpressionLeafs.MethodCallNoParam;

            string methodName = "MethodCallWithArgument";
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = new InvokeMethod<int>()
                {
                    MethodName = methodName,
                    TargetType = typeof(DummyHelper),
                    Parameters =
                    {
                        new InArgument<int>(
                            (InvokeMethod<int>)node.GetLeafNode())
                            {
                                EvaluationOrder = 1
                            }
                    }
                },
                ExpressionTree = Expression.Call(
                    typeof(DummyHelper).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static),
                    node.LeafExpression)
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Convert a method call with ref argument. Exception is expected.
        /// </summary>        
        [Fact]
        //  Method call with ref argument
        public void MethodCallWithRefArgument()
        {
            Expression<Func<ActivityContext, int>> expression = (env) => DummyHelper.MethodCallWithRefArgument(ref DummyHelper.StaticIntField);

            string methodName = "MethodCallWithRefArgument";
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = new InvokeMethod<int>()
                {
                    MethodName = methodName,
                    TargetType = typeof(DummyHelper),
                    Parameters =
                    {
                        new InOutArgument<int>()
                            {
                                Expression = new FieldReference<DummyHelper, int>
                                {
                                    FieldName = "StaticIntField"
                                },
                                EvaluationOrder = 1
                            }
                    }
                },
                ExpressionTree = expression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// TryConvert a method call with ref argument. TryConvert should return false and get a null output.
        /// </summary>        
        [Fact]
        //  TryConvert method call with ref argument
        public void TryConvertMethodCallWithRefArgument()
        {
            int i = 100;

            ExpressionTestRuntime.TryConvert((env) => DummyHelper.MethodCallWithRefArgument(ref i), true);
        }

        /// <summary>
        /// Convert a method call expression that has argument array, and validate the result.
        /// </summary>        
        [Fact]
        //  Method call with VarArg
        public void MethodCallWithVarArg()
        {
            TargetInvocationException expectedException = new TargetInvocationException(null);

            ExpressionTestRuntime.Convert((env) => DummyHelper.MethodCallWithVarArg(1, 2, 3), expectedException);
        }

        [Fact]
        //  Method call with VarArg
        public void TryMethodCallWithVarArg()
        {
            ExpressionTestRuntime.TryConvert((env) => DummyHelper.MethodCallWithVarArg(1, 2, 3), false);
        }

        ///// <summary>
        ///// Convert a method call on an instance, and validate the result.
        ///// </summary>        
        //[Fact]
        ////  Method call of instance
        //public void InstanceMethodCall()
        //{
        //    VisualBasicValue<DummyHelper> vbv = new VisualBasicValue<DummyHelper>("New DummyHelper()");
        //    Variable<DummyHelper> dummyVar = new Variable<DummyHelper>("dummyVar") { Default = vbv };

        //    Activity<int> expectedActivity = new InvokeMethod<int>()
        //    {
        //        MethodName = "InstanceFuncReturnInt",
        //        TargetObject = new InArgument<DummyHelper>()
        //        {
        //            Expression = dummyVar,
        //            EvaluationOrder = 0
        //        }
        //    };

        //    Expression<Func<ActivityContext, int>> expectedExpression = (env) => dummyVar.Get(env).InstanceFuncReturnInt();

        //    TestExpression expr = new TestExpression()
        //    {
        //        ResultType = typeof(int),
        //        ExpectedNode = expectedActivity,
        //        ExpressionTree = expectedExpression
        //    };

        //    List<Variable> vars = new List<Variable>()
        //    {
        //        dummyVar
        //    };

        //    ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
        //    ExpressionTestRuntime.ValidateExecutionResult(expr, vars, typeof(DummyHelper));
        //}

        /// <summary>
        /// Convert a delegate call that is a static field, and validate the result.
        /// </summary>        
        [Fact]
        //  Static delegate call
        public void StaticDelegateCall()
        {
            NodeExpressionPair node = ExpressionLeafs.MethodCallNoParam;

            string delegateName = "StaticDelegate";
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = new InvokeMethod<int>()
                {
                    MethodName = "Invoke",
                    Parameters =
                    {
                        new InArgument<int>()
                        {
                            Expression = (Activity<int>)node.GetLeafNode(),
                            EvaluationOrder = 1,
                        }
                    },
                    TargetObject = new InArgument<Func<int, int>>()
                    {
                        Expression = new FieldValue<DummyHelper, Func<int, int>>()
                        {
                            FieldName = delegateName,
                            Operand = null,
                        },
                        EvaluationOrder = 0
                    }
                },
                ExpressionTree = Expression.Invoke(
                    Expression.Field(
                        null, typeof(DummyHelper).GetField(delegateName, BindingFlags.Public | BindingFlags.Static)),
                        node.LeafExpression)
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        ///// <summary>
        ///// Convert a delegate call that is an instance value, and validate the result.
        ///// </summary>        
        //[Fact]
        ////  Instance delegate call
        //public void InstanceDelegateCall()
        //{
        //    string delegateName = "InstanceDelegate";
        //    VisualBasicValue<DummyHelper> vbv = new VisualBasicValue<DummyHelper>("New DummyHelper()");
        //    Variable<DummyHelper> dummyVar = new Variable<DummyHelper>()
        //    {
        //        Default = vbv
        //    };

        //    Activity<int> expectedActivity = new InvokeMethod<int>()
        //    {
        //        MethodName = "Invoke",
        //        TargetObject = new InArgument<Func<int>>()
        //        {

        //            Expression = new FieldValue<DummyHelper, Func<int>>()
        //            {
        //                FieldName = delegateName,
        //                Operand = dummyVar
        //            },
        //            EvaluationOrder = 0
        //        },
        //    };

        //    Expression<Func<ActivityContext, int>> expectedExpression = (env) => dummyVar.Get(env).InstanceDelegate();

        //    TestExpression expr = new TestExpression()
        //    {
        //        ResultType = typeof(int),
        //        ExpectedNode = expectedActivity,
        //        ExpressionTree = expectedExpression
        //    };

        //    List<Variable> vars = new List<Variable>()
        //    {
        //        dummyVar
        //    };

        //    ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
        //    ExpressionTestRuntime.ValidateExecutionResult(expr, vars, typeof(DummyHelper));
        //}

        /// <summary>
        /// Convert a generic method call expression, and validate the result.
        /// </summary>        
        [Fact]
        //  Call generic method
        public void GenericMethodCall()
        {
            NodeExpressionPair node = ExpressionLeafs.ConstStringValue;
            string methodName = "GenericMethod";

            MethodInfo genericMethodHanlder = typeof(DummyHelper).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            MethodInfo specializedGenericMethodHandler = genericMethodHanlder.MakeGenericMethod(typeof(string));

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(string),
                ExpectedNode = new InvokeMethod<string>()
                {
                    MethodName = methodName,
                    TargetType = typeof(DummyHelper),
                    GenericTypeArguments =
                    {
                        typeof(string)
                    },
                    Parameters =
                    {
                        new InArgument<string>()
                        {
                            Expression = (Activity<string>)node.GetLeafNode(),
                            EvaluationOrder = 1
                        }
                    }
                },
                ExpressionTree = Expression.Call(
                    specializedGenericMethodHandler, node.LeafExpression)
            };

            ExpressionTestRuntime.ValidateExpressionXaml<string>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        /// <summary>
        /// Convert a method call with different expressions as argument, and validate the result.
        /// </summary>        
        [Fact]
        //  Method call with different expressions as argument
        public void MethodCallWithVariousParameters()
        {
            Activity<int> expectedActivity = new InvokeMethod<int>()
            {
                MethodName = "MethodCallWithVariousArgs",
                TargetType = typeof(DummyHelper),
                Parameters =
                {
                    new InArgument<int>(-1)
                    {
                        EvaluationOrder = 1
                    },
                    new InArgument<string>("hello")
                    {
                        EvaluationOrder = 2,
                    },
                    new InOutArgument<int>()
                    {
                        EvaluationOrder = 3,
                        Expression = new FieldReference<DummyHelper, int>()
                        {
                            FieldName = "StaticIntField"
                        },
                    },
                    new InArgument<Variable<int?>>()
                    {
                        EvaluationOrder = 4,
                        Expression = new FieldValue<DummyHelper, Variable<int?>>()
                        {
                            FieldName = "StaticNullableIntVar"
                        }
                    },
                    new InArgument<Func<int, int>>()
                    {
                        EvaluationOrder = 5,
                        Expression = new FieldValue<DummyHelper, Func<int, int>>()
                        {
                            FieldName = "StaticDelegate"
                        }
                    },
                    new InArgument<DummyHelper>()
                    {
                        EvaluationOrder = 6,
                        Expression = new New<DummyHelper>()
                    }
                },
            };

            Expression<Func<ActivityContext, int>> expectedExpression =
                (env) => DummyHelper.MethodCallWithVariousArgs(-1, "hello", ref DummyHelper.StaticIntField, DummyHelper.StaticNullableIntVar, DummyHelper.StaticDelegate, new DummyHelper());

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = expectedActivity,
                ExpressionTree = expectedExpression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }
    }
}

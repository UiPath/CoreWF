// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class MemberExpression
    {
        /// <summary>
        /// Convert constant value and validate the result.
        /// </summary>        
        [Fact]
        //  Constant value
        public void ConstantValue()
        {
            NodeExpressionPair node = ExpressionLeafs.ConstIntValue;
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
        /// Convert static field and validate the result.
        /// </summary>        
        [Fact]
        //  Static field
        public void StaticField()
        {
            NodeExpressionPair node = ExpressionLeafs.StaticIntField;
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
        /// Convert static property and validate the result
        /// </summary>        
        [Fact]
        //  Static property
        public void StaticProperty()
        {
            NodeExpressionPair node = ExpressionLeafs.StaticIntProperty;
            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = node.GetLeafNode(),
                ExpressionTree = node.LeafExpression
            };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        ///// <summary>
        ///// Convert instance field and validate the result
        ///// </summary>        
        //[Fact]
        ////  Instance  field
        //public void InstanceField()
        //{
        //    VisualBasicValue<DummyHelper> vbv = new VisualBasicValue<DummyHelper>("New DummyHelper()");
        //    Variable<DummyHelper> dummyVar = new Variable<DummyHelper>()
        //    {
        //        Default = vbv
        //    };

        //    Activity<int> expectedActivity =
        //        new FieldValue<DummyHelper, int>()
        //        {
        //            Operand = dummyVar,
        //            FieldName = "IntField",
        //        };

        //    Expression<Func<ActivityContext, int>> expectedExpression = (env) => dummyVar.Get(env).IntField;

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

        ///// <summary>
        ///// Convert instance property and validate the result
        ///// </summary>        
        //[Fact]
        ////  Instance property
        //public void InstanceProperty()
        //{
        //    VisualBasicValue<DummyHelper> vbv = new VisualBasicValue<DummyHelper>("New DummyHelper()");
        //    Variable<DummyHelper> dummyVar = new Variable<DummyHelper>()
        //    {
        //        Default = vbv
        //    };

        //    Activity<int> expectedActivity =
        //        new PropertyValue<DummyHelper, int>()
        //        {
        //            Operand = dummyVar,
        //            PropertyName = "IntProperty"
        //        };

        //    Expression<Func<ActivityContext, int>> expectedExpression = (env) => dummyVar.Get(env).IntProperty;

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
        /// Convert the variable.Get(env) that is generated by Linq, and validate the result.
        /// </summary>        
        [Fact]
        //  Workflow variable by Linq
        public void VariableByLinq()
        {
            // Linq treats local variable with special syntax.
            // See comment in LeafHelper.GetMemberAccessVariableExpression for more info.
            // The purpose test is to use real compiler generated lambda expression.
            Variable<string> var = new Variable<string>()
            {
                Name = "var",
                Default = "Linq var test"
            };

            Expression<Func<ActivityContext, string>> expression = (env) => var.Get(env);

            System.Activities.Statements.Sequence expectedSequence = new System.Activities.Statements.Sequence()
            {
                Variables =
                {
                    var
                },
                Activities =
                {
                    new WriteLine()
                    {
                        Text = var
                    }
                }
            };

            System.Activities.Statements.Sequence actualSequence = new System.Activities.Statements.Sequence()
            {
                Variables =
                {
                    var
                },
                Activities =
                {
                    new WriteLine()
                    {
                        Text = ExpressionServices.Convert(expression)
                    }
                }
            };

            ExpressionTestRuntime.ValidateActivity(expectedSequence, actualSequence);
        }

        /// <summary>
        /// Convert the none-generic Variable.Get, and validate the result
        /// </summary>        
        [Fact]
        //  None generic variable get
        public void NoneGenericVariableGet()
        {
            Variable<int> var = new Variable<int>()
            {
                Name = "NoneGenericVariable"
            };

            Activity<int> expectedActivity = new Cast<object, int>()
            {
                Operand = var,
                Checked = false
            };

            Expression<Func<ActivityContext, int>> lambda = (env) => (int)DummyHelper.NoneGenericVariable.Get(env);

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(int),
                ExpectedNode = expectedActivity,
                ExpressionTree = lambda
            };

            List<Variable> varsActual = new List<Variable>()
                {
                    DummyHelper.NoneGenericVariable
                };

            List<Variable> varsExpected = new List<Variable>()
                {
                    var
                };

            ExpressionTestRuntime.ValidateExpressionXaml<int>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, varsExpected, varsActual);
        }

        /// <summary>
        /// Convert indexer access and validate the result.
        /// </summary>        
        [Fact]
        //  Indexer
        public void RValueIndexer()
        {
            Expression<Func<ActivityContext, string>> lambda = (env) => DummyHelper.StaticDictionary[1];

            Activity<string> expectedActivity = new InvokeMethod<string>()
            {
                MethodName = "get_Item",
                Parameters =
                {
                    new InArgument<int>(1)
                    {
                        EvaluationOrder = 1
                    }
                },
                TargetObject = new InArgument<Dictionary<int, string>>()
                {
                    EvaluationOrder = 0,
                    Expression = new FieldValue<DummyHelper, Dictionary<int, string>>()
                    {
                        FieldName = "StaticDictionary"
                    }
                }
            };

            TestExpression expr = new TestExpression()
            {
                ResultType = typeof(string),
                ExpectedNode = expectedActivity,
                ExpressionTree = lambda
            };

            ExpressionTestRuntime.ValidateExpressionXaml<string>(expr);
            ExpressionTestRuntime.ValidateExecutionResult(expr, null);
        }

        ///// <summary>
        ///// RValue array item
        ///// </summary>        
        //[Fact]
        ////  RValue array item
        //public void ArrayItemValue()
        //{
        //    Variable<int> result = new Variable<int>("result");
        //    Variable<int[]> var = new Variable<int[]>("var");
        //    Expression<Func<ActivityContext, int>> expression = (ctx) => var.Get(ctx)[1];

        //    Sequence expectedSequence = new Sequence()
        //    {
        //        Variables =
        //        {
        //            var,
        //            result
        //        },
        //        Activities = 
        //        {
        //            new Assign<int[]>() 
        //            {
        //                To = var,
        //                Value = new VisualBasicValue<int[]>("new Integer() {1, 2, 3}")
        //            },
        //            new Assign<int>
        //            {
        //                Value = new ArrayItemValue<int>()
        //                {
        //                    Index = new InArgument<int>(1)
        //                    {
        //                        EvaluationOrder = 1,
        //                    },
        //                    Array = new InArgument<int[]>(var)
        //                    {
        //                        EvaluationOrder = 0,
        //                    },
        //                },
        //                To = result
        //            }
        //        }
        //    };

        //    Sequence actualSequence = new Sequence()
        //    {
        //        Variables =
        //        {
        //            var,
        //            result
        //        },
        //        Activities =
        //        {
        //            new Assign<int[]>() 
        //            {
        //                To = var,
        //                Value = new VisualBasicValue<int[]>("new Integer() {1, 2, 3}")
        //            },
        //            new Assign<int>
        //            {
        //                Value = ExpressionTestRuntime.Convert(expression, null),
        //                To = result
        //            },
        //            new WriteLine()
        //            {
        //                Text = new VisualBasicValue<string> { ExpressionText = "result.ToString()" }
        //            }
        //        }
        //    };

        //    ExpressionTestRuntime.ModifyValidateAndExecuteActivity(expectedSequence, actualSequence, "2");
        //}
    }
}

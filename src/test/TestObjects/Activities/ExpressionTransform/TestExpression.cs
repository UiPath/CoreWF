// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.ExpressionTransform
{
    public class TestExpression
    {
        public Expression ExpressionTree
        {
            get;
            set;
        }

        public Type ResultType
        {
            get;
            set;
        }

        private object _expectedProductNode;
        public object ExpectedNode
        {
            get
            {
                if (_expectedProductNode == null)
                    throw new Exception("ExpectedLeafNode is null!");

                return _expectedProductNode;
            }
            set
            {
                _expectedProductNode = value;
            }
        }

        public Exception ExpectedConversionException
        {
            get;
            set;
        }

        // The type[] array for the parameter list used in Expression factory method
        public Type[] FactoryMethodParamTypes
        {
            get;
            set;
        }

        public virtual Expression CreateLinqExpression()
        {
            return ExpressionTree;
        }

        public LambdaExpression CreateLambdaExpresson<T>()
        {
            Expression expr = CreateLinqExpression();

            if (expr is LambdaExpression)
            {
                return (LambdaExpression)expr;
            }
            else
            {
                Expression<Func<ActivityContext, T>> lambdaExpression =
                    Expression.Lambda<Func<ActivityContext, T>>(this.CreateLinqExpression(), TestExpression.EnvParameter);

                return lambdaExpression;
            }
        }

        public virtual object CreateExpectedActivity()
        {
            return ExpectedNode;
        }

        public virtual Sequence CreateExpectedWorkflow<TResult>()
        {
            Variable<TResult> result = new Variable<TResult>()
            {
                Name = "result"
            };

            Sequence sequence = new Sequence()
            {
                Variables =
                {
                    result
                },
                Activities =
                {
                    new Assign<TResult>()
                    {
                        Value = (Activity<TResult>)CreateExpectedActivity(),
                        To = result
                    },
                    new WriteLine()
                    {
                        Text = "result.ToString()"
                        //VisualBasicValue<string>("result.ToString()")
                    },
                }
            };

            return sequence;
        }

        public virtual Sequence CreateActualWorkflow<TResult>()
        {
            Expression<Func<ActivityContext, TResult>> lambdaExpression = (Expression<Func<ActivityContext, TResult>>)this.CreateLambdaExpresson<TResult>();
            Activity<TResult> we = ExpressionServices.Convert(lambdaExpression);

            Variable<TResult> result = new Variable<TResult>()
            {
                Name = "result"
            };

            Sequence sequence = new Sequence()
            {
                Variables =
                {
                    result
                },
                Activities =
                {
                    new Assign<TResult>()
                    {
                        Value = new InArgument<TResult>(we),
                        To = result
                    },
                    new WriteLine()
                    {
                        Text = "result.ToString()"
                        //new VisualBasicValue<string>("result.ToString()")
                    }
                }
            };

            return sequence;
        }


        private static ParameterExpression s_paramEnv;

        public static ParameterExpression EnvParameter
        {
            get
            {
                if (s_paramEnv == null)
                    s_paramEnv = Expression.Parameter(typeof(ActivityContext), "context");

                return s_paramEnv;
            }
        }

        public static InArgument<T> GetInArgumentFromExpectedNode<T>(TestExpression te)
        {
            InArgument<T> retInArg = null;

            // leaf node:
            if (te.GetType() == typeof(TestExpression))
            {
                if (te.ExpectedNode is Activity<T>)
                    retInArg = (Activity<T>)te.ExpectedNode;
                else if (te.ExpectedNode is Variable<T>)
                    retInArg = (Variable<T>)te.ExpectedNode;
                else
                {
                    //Log.TraceInternal("Expect a generic type with type parameter: {0}", typeof(T).ToString());
                    //Log.TraceInternal("Expected node type is: {0}", te.ExpectedNode.GetType().ToString());
                    throw new NotSupportedException("Not supported expected node type");
                }
            }
            // non-leaf node:
            else if (te is TestBinaryExpression || te is TestUnaryExpression)
            {
                retInArg = (Activity<T>)te.CreateExpectedActivity();
            }

            return retInArg;
        }

        static protected Activity<T> GetWorkflowElementFromExpectedNode<T>(TestExpression te)
        {
            Activity<T> we = null;
            if (te.ExpectedConversionException != null)
                return null;

            // leaf node:
            if (te.GetType() == typeof(TestExpression))
            {
                if (te.ExpectedNode is Activity<T>)
                {
                    we = (Activity<T>)te.ExpectedNode;
                }
                else if (te.ExpectedNode is Variable<T>)
                {
                    we = new VariableValue<T>()
                    {
                        Variable = (Variable<T>)te.ExpectedNode
                    };
                }
                else
                {
                    throw new NotSupportedException("Not supported expected node type");
                }
            }
            // non-leaf node:
            else if (te is TestBinaryExpression || te is TestUnaryExpression)
            {
                we = (Activity<T>)te.CreateExpectedActivity();
            }

            return we;
        }
    }
}

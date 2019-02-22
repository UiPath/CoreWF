// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.ExpressionTransform
{
    public class TestLValueExpression : TestExpression
    {
        /// <summary>
        /// The source value to assign
        /// </summary>
        public object FromValue
        {
            get;
            set;
        }

        /// <summary>
        /// The Linq representation for the To value
        /// </summary>
        public Expression<Func<ActivityContext, string>> ToExpression
        {
            get;
            set;
        }

        public override Sequence CreateExpectedWorkflow<TResult>()
        {
            Activity<Location<TResult>> result = (Activity<Location<TResult>>)CreateExpectedActivity();

            Sequence sequence = new Sequence()
            {
                Activities =
                {
                    new Assign<TResult>()
                    {
                        Value = (TResult)FromValue,
                        To = result
                    },
                    new WriteLine()
                    {
                        Text = new InArgument<string>(ToExpression)
                    },
                }
            };

            return sequence;
        }

        public override Sequence CreateActualWorkflow<TResult>()
        {
            Expression<Func<ActivityContext, TResult>> lambdaExpression = Expression.Lambda<Func<ActivityContext, TResult>>(this.CreateLinqExpression(), TestExpression.EnvParameter);
            Activity<Location<TResult>> we = ExpressionServices.ConvertReference(lambdaExpression);

            Sequence sequence = new Sequence()
            {
                Activities =
                {
                    new Assign<TResult>()
                    {
                        Value = (TResult)FromValue,
                        To = we
                    },
                    new WriteLine()
                    {
                        Text = new InArgument<string>(ToExpression)
                    }
                }
            };

            return sequence;
        }
    }
}

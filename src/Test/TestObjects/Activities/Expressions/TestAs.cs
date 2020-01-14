// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestAs<TOperand, TResult> : TestActivity, ITestUnaryExpression<TOperand, TResult>
    {
        public TestAs()
        {
            this.ProductActivity = new As<TOperand, TResult>();
        }

        public TOperand Operand
        {
            set
            {
                ((As<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((As<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }


        public Variable<TResult> Result
        {
            set
            {
                ((As<TOperand, TResult>)this.ProductActivity).Result = new OutArgument<TResult>(value);
            }
        }
    }
}

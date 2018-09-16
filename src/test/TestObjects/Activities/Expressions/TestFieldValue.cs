// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestFieldValue<TOperand, TResult> : TestActivity
    {
        public TestFieldValue()
        {
            this.ProductActivity = new FieldValue<TOperand, TResult>();
        }

        public TOperand Operand
        {
            set
            {
                ((FieldValue<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Variable<TOperand> OperandVariable
        {
            set
            {
                ((FieldValue<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((FieldValue<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }

        public string FieldName
        {
            set
            {
                ((FieldValue<TOperand, TResult>)this.ProductActivity).FieldName = value;
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((FieldValue<TOperand, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}

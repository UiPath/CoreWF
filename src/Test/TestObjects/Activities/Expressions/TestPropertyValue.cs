// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestPropertyValue<TOperand, TResult> : TestActivity
    {
        public TestPropertyValue()
        {
            this.ProductActivity = new PropertyValue<TOperand, TResult>();
        }

        public TestPropertyValue(TOperand operand, string propertyName)
            : this()
        {
            Operand = operand;
            PropertyName = propertyName;
        }

        public TOperand Operand
        {
            set
            {
                ((PropertyValue<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((PropertyValue<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }

        public Variable<TOperand> OperandVariable
        {
            set
            {
                ((PropertyValue<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public string PropertyName
        {
            set
            {
                ((PropertyValue<TOperand, TResult>)this.ProductActivity).PropertyName = value;
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ((PropertyValue<TOperand, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}

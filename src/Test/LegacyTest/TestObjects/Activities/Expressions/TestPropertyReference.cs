// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestPropertyReference<TOperand, TResult> : TestActivity
    {
        public TestPropertyReference()
        {
            this.ProductActivity = new PropertyReference<TOperand, TResult>();
        }

        public TestPropertyReference(TOperand operand, string propertyName)
            : this()
        {
            Operand = operand;
            PropertyName = propertyName;
        }

        public TOperand Operand
        {
            set
            {
                ((PropertyReference<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((PropertyReference<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }

        public Variable<TOperand> OperandVariable
        {
            set
            {
                ((PropertyReference<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public string PropertyName
        {
            set
            {
                ((PropertyReference<TOperand, TResult>)this.ProductActivity).PropertyName = value;
            }
        }

        public Variable<Location<TResult>> Result
        {
            set
            {
                ((PropertyReference<TOperand, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}

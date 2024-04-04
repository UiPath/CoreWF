// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestFieldReference<TOperand, TResult> : TestActivity
    {
        public TestFieldReference()
        {
            this.ProductActivity = new FieldReference<TOperand, TResult>();
        }

        public TestFieldReference(TOperand operand, string fieldName)
            : this()
        {
            Operand = operand;
            FieldName = fieldName;
        }

        public TOperand Operand
        {
            set
            {
                ((FieldReference<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandExpression
        {
            set
            {
                ((FieldReference<TOperand, TResult>)this.ProductActivity).Operand = new InArgument<TOperand>(value);
            }
        }

        public Variable<TOperand> OperandVariable
        {
            set
            {
                ((FieldReference<TOperand, TResult>)this.ProductActivity).Operand = value;
            }
        }

        public string FieldName
        {
            set
            {
                ((FieldReference<TOperand, TResult>)this.ProductActivity).FieldName = value;
            }
        }

        public Variable<Location<TResult>> Result
        {
            set
            {
                ((FieldReference<TOperand, TResult>)this.ProductActivity).Result = value;
            }
        }
    }
}

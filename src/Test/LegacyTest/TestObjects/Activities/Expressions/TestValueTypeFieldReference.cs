// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestValueTypeFieldReference<TOperand, TResult> : TestActivity
    {
        private TestActivity _operandLocationActivity;
        public TestValueTypeFieldReference()
        {
            this.ProductActivity = new ValueTypeFieldReference<TOperand, TResult>();
        }

        public TestActivity OperandLocation
        {
            set
            {

                if (!(value.ProductActivity is Activity<Location<TOperand>> we))
                {
                    throw new Exception("TestActivity should be for Activity<Location<T>> for conversion");
                }

                this.ProductValueTypeFieldReference.OperandLocation = we;
                _operandLocationActivity = value;
            }
        }

        public Variable<TOperand> OperandLocationVariable
        {
            set
            {
                this.ProductValueTypeFieldReference.OperandLocation = value;
                _operandLocationActivity = null;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandLocationExpression
        {
            set
            {
                this.ProductValueTypeFieldReference.OperandLocation = new InOutArgument<TOperand>(value);
                _operandLocationActivity = null;
            }
        }

        public string FieldName
        {
            set
            {
                this.ProductValueTypeFieldReference.FieldName = value;
            }
        }

        public Variable<Location<TResult>> Result
        {
            set
            {
                this.ProductValueTypeFieldReference.Result = value;
            }
        }

        private ValueTypeFieldReference<TOperand, TResult> ProductValueTypeFieldReference
        {
            get
            {
                return (ValueTypeFieldReference<TOperand, TResult>)this.ProductActivity;
            }
        }
    }
}

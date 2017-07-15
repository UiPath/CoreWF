// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestValueTypePropertyReference<TOperand, TResult> : TestActivity
    {
        private TestActivity _operandLocationActivity;
        public TestValueTypePropertyReference()
        {
            this.ProductActivity = new ValueTypePropertyReference<TOperand, TResult>();
        }

        public TestActivity OperandLocation
        {
            set
            {
                Activity<Location<TOperand>> we = value.ProductActivity as Activity<Location<TOperand>>;

                if (we == null)
                {
                    throw new Exception("TestActivity should be for Activity<Location<T>> for conversion");
                }

                this.ProductValueTypePropertyReference.OperandLocation = we;
                _operandLocationActivity = value;
            }
        }

        public Variable<TOperand> OperandLocationVariable
        {
            set
            {
                this.ProductValueTypePropertyReference.OperandLocation = value;
                _operandLocationActivity = null;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandLocationExpression
        {
            set
            {
                this.ProductValueTypePropertyReference.OperandLocation = new InOutArgument<TOperand>(value);
                _operandLocationActivity = null;
            }
        }

        public string PropertyName
        {
            set
            {
                this.ProductValueTypePropertyReference.PropertyName = value;
            }
        }

        public Variable<Location<TResult>> Result
        {
            set
            {
                this.ProductValueTypePropertyReference.Result = value;
            }
        }

        private ValueTypePropertyReference<TOperand, TResult> ProductValueTypePropertyReference
        {
            get
            {
                return (ValueTypePropertyReference<TOperand, TResult>)this.ProductActivity;
            }
        }
    }
}

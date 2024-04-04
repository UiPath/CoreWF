// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Collections;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestValueTypeIndexerReference<TOperand, TResult> : TestActivity
    {
        private TestActivity _operandLocationActivity;
        private readonly MemberCollection<TestArgument> _indices;

        public TestValueTypeIndexerReference()
        {
            this.ProductActivity = new ValueTypeIndexerReference<TOperand, TResult>();
            _indices = new MemberCollection<TestArgument>(AddArgument);
        }

        public TestActivity OperandLocation
        {
            set
            {

                if (!(value.ProductActivity is Activity<Location<TOperand>> we))
                {
                    throw new Exception("TestActivity should be for Activity<Location<T>> for conversion");
                }

                this.ProductValueTypeIndexerReference.OperandLocation = we;
                _operandLocationActivity = value;
            }
        }

        public Expression<Func<ActivityContext, TOperand>> OperandLocationExpression
        {
            set
            {
                this.ProductValueTypeIndexerReference.OperandLocation = new InOutArgument<TOperand>(value);
                _operandLocationActivity = null;
            }
        }


        public Variable<TOperand> OperandLocationVariable
        {
            set
            {
                this.ProductValueTypeIndexerReference.OperandLocation = value;
                _operandLocationActivity = null;
            }
        }

        public MemberCollection<TestArgument> Indices
        {
            get
            {
                return _indices;
            }
        }

        public Variable<Location<TResult>> Result
        {
            set
            {
                this.ProductValueTypeIndexerReference.Result = value;
            }
        }

        private ValueTypeIndexerReference<TOperand, TResult> ProductValueTypeIndexerReference
        {
            get
            {
                return (ValueTypeIndexerReference<TOperand, TResult>)this.ProductActivity;
            }
        }

        private void AddArgument(TestArgument item)
        {
            this.ProductValueTypeIndexerReference.Indices.Add((InArgument)item.ProductArgument);
        }
    }
}

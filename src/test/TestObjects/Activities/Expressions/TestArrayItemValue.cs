// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestArrayItemValue<TItem> : TestActivity
    {
        public TestArrayItemValue()
        {
            this.ProductActivity = new ArrayItemValue<TItem>();
        }

        public Variable<TItem[]> ArrayVariable
        {
            set
            {
                ((ArrayItemValue<TItem>)this.ProductActivity).Array = value;
            }
        }

        public Expression<Func<ActivityContext, TItem[]>> ArrayExpression
        {
            set
            {
                ((ArrayItemValue<TItem>)this.ProductActivity).Array = new InArgument<TItem[]>(value);
            }
        }

        public int Index
        {
            set
            {
                ((ArrayItemValue<TItem>)this.ProductActivity).Index = value;
            }
        }

        public Variable<TItem> Result
        {
            set
            {
                ((ArrayItemValue<TItem>)this.ProductActivity).Result = value;
            }
        }
    }
}

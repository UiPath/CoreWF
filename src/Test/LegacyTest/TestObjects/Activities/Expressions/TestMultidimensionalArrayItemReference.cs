// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Linq.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Collections;

namespace LegacyTest.Test.Common.TestObjects.Activities.Expressions
{
    public class TestMultidimensionalArrayItemReference<TItem> : TestActivity
    {
        private readonly MemberCollection<TestArgument<int>> _indices;

        public TestMultidimensionalArrayItemReference()
        {
            this.ProductActivity = new MultidimensionalArrayItemReference<TItem>();
            _indices = new MemberCollection<TestArgument<int>>(AddArgument);
        }

        public Array Array
        {
            set
            {
                this.ProductMultidimensionalArrayItemReference.Array = value;
            }
        }

        public Variable ArrayVariable
        {
            set
            {
                this.ProductMultidimensionalArrayItemReference.Array = value;
            }
        }

        public Expression<Func<ActivityContext, Array>> ArrayExpression
        {
            set
            {
                this.ProductMultidimensionalArrayItemReference.Array = new InArgument<Array>(value);
            }
        }

        public MemberCollection<TestArgument<int>> Indices
        {
            get
            {
                return _indices;
            }
        }

        public Variable<Location<TItem>> Result
        {
            set
            {
                this.ProductMultidimensionalArrayItemReference.Result = value;
            }
        }

        private MultidimensionalArrayItemReference<TItem> ProductMultidimensionalArrayItemReference
        {
            get
            {
                return (MultidimensionalArrayItemReference<TItem>)this.ProductActivity;
            }
        }

        private void AddArgument(TestArgument<int> item)
        {
            this.ProductMultidimensionalArrayItemReference.Indices.Add((InArgument<int>)item.ProductArgument);
        }
    }
}

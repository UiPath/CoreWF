// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestArrayItemReference<TItem> : TestActivity
    {
        public TestArrayItemReference()
        {
            this.ProductActivity = new ArrayItemReference<TItem>();
        }

        public TestArrayItemReference(Variable<TItem[]> array, int index)
            : this()
        {
            Array = array;
            Index = index;
        }

        public Variable<TItem[]> Array
        {
            set
            {
                ((ArrayItemReference<TItem>)this.ProductActivity).Array = value;
            }
        }

        public int Index
        {
            set
            {
                ((ArrayItemReference<TItem>)this.ProductActivity).Index = value;
            }
        }

        public Variable<Location<TItem>> Result
        {
            set
            {
                ((ArrayItemReference<TItem>)this.ProductActivity).Result = value;
            }
        }
    }
}

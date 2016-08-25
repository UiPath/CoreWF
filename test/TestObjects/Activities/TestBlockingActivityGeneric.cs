// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Test.Common.TestObjects.CustomActivities;

namespace Test.Common.TestObjects.Activities
{
    public class TestBlockingActivity<T> : TestActivity
    {
        private TestActivity _child;

        public TestBlockingActivity(string displayName)
        {
            this.ProductActivity = new BlockingActivity<T>();
            this.DisplayName = displayName;
        }

        public TestBlockingActivity(string displayName, string namedBookmark)
        {
            this.ProductActivity = new BlockingActivity<T>(namedBookmark);
            this.DisplayName = displayName;
        }

        public T ExpectedResult
        {
            set { ProductBlockingActivity.ExpectedResult = value; }
        }

        public TestActivity ChildActivity
        {
            set
            {
                if (value != null)
                {
                    ProductBlockingActivity.Child = value.ProductActivity;
                }
                else
                {
                    ProductBlockingActivity.Child = null;
                }

                _child = value;
            }
            get
            {
                return _child;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            yield return this.ChildActivity;
        }

        private BlockingActivity<T> ProductBlockingActivity
        {
            get { return (BlockingActivity<T>)this.ProductActivity; }
        }
    }
}

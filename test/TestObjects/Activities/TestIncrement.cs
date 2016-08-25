// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Test.Common.TestObjects.CustomActivities;

namespace Test.Common.TestObjects.Activities
{
    public class TestIncrement : TestActivity
    {
        public TestIncrement()
        {
            this.ProductActivity = new Increment();
        }

        public TestIncrement(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestIncrement(string displayName, int incrementCount)
        {
            this.ProductActivity = new Increment(incrementCount);
            this.DisplayName = displayName;
        }

        public int IncrementCount
        {
            set
            {
                this.ProductIncrement.IncrementCount = new InArgument<int>(value);
            }
        }

        public Variable<int> CounterVariable
        {
            set { this.ProductIncrement.Counter = new InOutArgument<int>(value); }
        }

        private Increment ProductIncrement
        {
            get { return (Increment)this.ProductActivity; }
        }
    }
}

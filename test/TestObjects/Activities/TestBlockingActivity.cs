// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Test.Common.TestObjects.CustomActivities;

namespace Test.Common.TestObjects.Activities
{
    public class TestBlockingActivity : TestActivity
    {
        public TestBlockingActivity(string displayName)
        {
            this.ProductActivity = new BlockingActivity();
            this.DisplayName = displayName;
        }

        public TestBlockingActivity(string displayName, string namedBookmark)
        {
            this.ProductActivity = new BlockingActivity(namedBookmark);
            this.DisplayName = displayName;
        }
    }
}

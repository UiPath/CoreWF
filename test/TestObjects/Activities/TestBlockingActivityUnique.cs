// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Test.Common.TestObjects.CustomActivities;

namespace Test.Common.TestObjects.Activities
{
    // This class implements a blocking activity that creates a bookmark with
    // a unique name by appending new Guid().ToString() to the DisplayName.
    // Note that this bookmark name is not exposed, so is NEVER expected to be
    // resumed.
    //
    // This is the blocking activity that should be used inside the Body activity of ParallelForEach
    // activities. It is necessary to duplicate bookmark names.
    public class TestBlockingActivityUnique : TestActivity
    {
        public TestBlockingActivityUnique(string displayName)
        {
            this.ProductActivity = new BlockingActivityUnique();
            this.DisplayName = displayName;
        }

        public TestBlockingActivityUnique(string displayName, string namedBookmark)
        {
            this.ProductActivity = new BlockingActivityUnique(namedBookmark);
            this.DisplayName = displayName;
        }
    }
}

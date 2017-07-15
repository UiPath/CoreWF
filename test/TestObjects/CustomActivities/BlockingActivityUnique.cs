// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;

namespace Test.Common.TestObjects.CustomActivities
{
    // This class implements a blocking activity that creates a bookmark with
    // a unique name by appending new Guid().ToString() to the DisplayName.
    // Note that this bookmark name is not exposed, so is NEVER expected to be
    // resumed.
    //
    // This is the blocking activity that should be used inside the Body activity of ParallelForEach
    // activities. It is necessary to duplicate bookmark names.
    public class BlockingActivityUnique : NativeActivity
    {
        public BlockingActivityUnique()
        {
        }

        public BlockingActivityUnique(string displayName)
        {
            this.DisplayName = displayName;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // nothing to do
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(this.DisplayName + Guid.NewGuid().ToString(), new BookmarkCallback(OnBookmarkResumed));
        }

        private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
            // No-op
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;

namespace Test.Common.TestObjects.CustomActivities
{
    public class BlockingActivity : NativeActivity
    {
        public BlockingActivity()
        {
        }

        public BlockingActivity(string displayName)
        {
            this.DisplayName = displayName;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // nothing to do
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
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

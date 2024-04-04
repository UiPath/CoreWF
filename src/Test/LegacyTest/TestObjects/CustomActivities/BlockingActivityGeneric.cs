// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    public class BlockingActivity<TResult> : NativeActivity<TResult>
    {
        public BlockingActivity()
        {
        }

        public BlockingActivity(string displayName)
        {
            this.DisplayName = displayName;
        }

        public TResult ExpectedResult { get; set; }
        public Activity Child { get; set; }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));

            base.Result.Set(context, ExpectedResult);
        }

        private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
            if (this.Child != null)
                context.ScheduleActivity(this.Child);
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (this.Child != null)
            {
                metadata.AddChild(this.Child);
            }
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}
    }
}

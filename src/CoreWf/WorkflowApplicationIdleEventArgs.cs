// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Hosting;
    using CoreWf.Runtime;
    using System.Collections.ObjectModel;

    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationIdleEventArgs : WorkflowApplicationEventArgs
    {
        private ReadOnlyCollection<BookmarkInfo> bookmarks;

        internal WorkflowApplicationIdleEventArgs(WorkflowApplication application)
            : base(application)
        {
        }

        public ReadOnlyCollection<BookmarkInfo> Bookmarks
        {
            get
            {
                if (this.bookmarks == null)
                {
                    this.bookmarks = this.Owner.GetBookmarksForIdle();
                }

                return this.bookmarks;
            }
        }
    }
}

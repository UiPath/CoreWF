// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Hosting;
using CoreWf.Runtime;
using System.Collections.ObjectModel;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationIdleEventArgs : WorkflowApplicationEventArgs
    {
        private ReadOnlyCollection<BookmarkInfo> _bookmarks;

        internal WorkflowApplicationIdleEventArgs(WorkflowApplication application)
            : base(application)
        {
        }

        public ReadOnlyCollection<BookmarkInfo> Bookmarks
        {
            get
            {
                if (_bookmarks == null)
                {
                    _bookmarks = this.Owner.GetBookmarksForIdle();
                }

                return _bookmarks;
            }
        }
    }
}

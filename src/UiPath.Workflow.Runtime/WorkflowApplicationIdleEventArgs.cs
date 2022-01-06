// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;
using Hosting;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public class WorkflowApplicationIdleEventArgs : WorkflowApplicationEventArgs
{
    private ReadOnlyCollection<BookmarkInfo> bookmarks;

    internal WorkflowApplicationIdleEventArgs(WorkflowApplication application)
        : base(application) { }

    public ReadOnlyCollection<BookmarkInfo> Bookmarks
    {
        get
        {
            bookmarks ??= Owner.GetBookmarksForIdle();
            return bookmarks;
        }
    }
}

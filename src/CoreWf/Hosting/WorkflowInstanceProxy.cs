// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Hosting;

public sealed class WorkflowInstanceProxy
{
    private readonly WorkflowInstance _instance;

    internal WorkflowInstanceProxy(WorkflowInstance instance)
    {
        _instance = instance;
    }

    public Guid Id => _instance.Id;

    public Activity WorkflowDefinition => _instance.WorkflowDefinition;

    public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        => BeginResumeBookmark(bookmark, value, TimeSpan.MaxValue, callback, state);

    public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
    {
        TimeoutHelper.ThrowIfNegativeArgument(timeout);

        return _instance.OnBeginResumeBookmark(bookmark, value, timeout, callback, state);
    }

    public BookmarkResumptionResult EndResumeBookmark(IAsyncResult result)
        => _instance.OnEndResumeBookmark(result);
}

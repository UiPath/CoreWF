// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
internal class BookmarkWorkItem : ActivityExecutionWorkItem
{
    private BookmarkCallbackWrapper _callbackWrapper;
    private Bookmark _bookmark;
    private object _state;

    public BookmarkWorkItem(ActivityExecutor executor, bool isExternal, BookmarkCallbackWrapper callbackWrapper, Bookmark bookmark, object value)
        : this(callbackWrapper, bookmark, value)
    {
        if (isExternal)
        {
            executor.EnterNoPersist();
            ExitNoPersistRequired = true;
        }
    }

    // This ctor is only used by subclasses which make their own determination about no persist or not
    protected BookmarkWorkItem(BookmarkCallbackWrapper callbackWrapper, Bookmark bookmark, object value)
        : base(callbackWrapper.ActivityInstance)
    {
        _callbackWrapper = callbackWrapper;
        _bookmark = bookmark;
        _state = value;
    }

    [DataMember(Name = "callbackWrapper")]
    internal BookmarkCallbackWrapper SerializedCallbackWrapper
    {
        get => _callbackWrapper;
        set => _callbackWrapper = value;
    }

    [DataMember(Name = "bookmark")]
    internal Bookmark SerializedBookmark
    {
        get => _bookmark;
        set => _bookmark = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "state")]
    internal object SerializedState
    {
        get => _state;
        set => _state = value;
    }

    public override void TraceCompleted()
    {
        if (TD.CompleteBookmarkWorkItemIsEnabled())
        {
            TD.CompleteBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
        }
    }

    public override void TraceScheduled()
    {
        if (TD.ScheduleBookmarkWorkItemIsEnabled())
        {
            TD.ScheduleBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
        }
    }

    public override void TraceStarting()
    {
        if (TD.StartBookmarkWorkItemIsEnabled())
        {
            TD.StartBookmarkWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
        }
    }

    public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        NativeActivityContext nativeContext = executor.NativeActivityContextPool.Acquire();

        try
        {
            nativeContext.Initialize(ActivityInstance, executor, bookmarkManager);
            _callbackWrapper.Invoke(nativeContext, _bookmark, _state);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            ExceptionToPropagate = e;
        }
        finally
        {
            nativeContext.Dispose();
            executor.NativeActivityContextPool.Release(nativeContext);
        }

        return true;
    }
}

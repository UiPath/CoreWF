// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;

namespace System.Activities.Runtime;

[DataContract(Name = XD.Runtime.BookmarkManager, Namespace = XD.Runtime.Namespace)]
public class BookmarkManager
{
    private long _nextId;
    private Dictionary<Bookmark, BookmarkCallbackWrapper> _bookmarks;
    private BookmarkScope _scope;
    private BookmarkScopeHandle _scopeHandle;

    public BookmarkManager()
    {
        _nextId = 1;
    }

    internal BookmarkManager(BookmarkScope scope, BookmarkScopeHandle scopeHandle)
        : this()
    {
        _scope = scope;
        _scopeHandle = scopeHandle;
    }

    public bool HasBookmarks => _bookmarks != null && _bookmarks.Count > 0;

    [DataMember(Name = "nextId")]
    internal long SerializedNextId
    {
        get => _nextId;
        set => _nextId = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "bookmarks")]
    internal Dictionary<Bookmark, BookmarkCallbackWrapper> SerializedBookmarks
    {
        get => _bookmarks;
        set => _bookmarks = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "scope")]
    internal BookmarkScope SerializedScope
    {
        get => _scope;
        set => _scope = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "scopeHandle")]
    internal BookmarkScopeHandle SerializedScopeHandle
    {
        get => _scopeHandle;
        set => _scopeHandle = value;
    }

    public Bookmark CreateBookmark(string name, BookmarkCallback callback, ActivityInstance owningInstance, BookmarkOptions options)
    {
        Bookmark toAdd = new(name);

        if (_bookmarks != null && _bookmarks.ContainsKey(toAdd))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkAlreadyExists(name)));
        }

        AddBookmark(toAdd, callback, owningInstance, options);
        //Regular bookmarks are never important
        UpdateAllExclusiveHandles(toAdd, owningInstance);

        return toAdd;
    }

    public Bookmark CreateBookmark(BookmarkCallback callback, ActivityInstance owningInstance, BookmarkOptions options)
    {
        Fx.Assert(_scope == null, "We only support named bookmarks within bookmark scopes right now.");

        Bookmark bookmark = Bookmark.Create(GetNextBookmarkId());
        AddBookmark(bookmark, callback, owningInstance, options);
        //Regular bookmarks are never important
        UpdateAllExclusiveHandles(bookmark, owningInstance);

        return bookmark;
    }

    private void UpdateAllExclusiveHandles(Bookmark bookmark, ActivityInstance owningInstance)
    {
        Fx.Assert(bookmark != null, "Invalid call to UpdateAllExclusiveHandles. Bookmark was null");
        Fx.Assert(owningInstance != null, "Invalid call to UpdateAllExclusiveHandles. ActivityInstance was null");

        if (owningInstance.PropertyManager == null)
        {
            return;
        }

        if (!owningInstance.PropertyManager.HasExclusiveHandlesInScope)
        {
            return;
        }

        List<ExclusiveHandle> handles = owningInstance.PropertyManager.FindAll<ExclusiveHandle>();

        if (handles == null)
        {
            return;
        }

        for (int i = 0; i < handles.Count; i++)
        {
            ExclusiveHandle handle = handles[i];
            if (handle != null)
            {
                if (_scopeHandle != null)
                {
                    bool found = false;
                    foreach (BookmarkScopeHandle bookmarkScopeHandle in handle.RegisteredBookmarkScopes)
                    {
                        if (bookmarkScopeHandle == _scopeHandle)
                        {
                            handle.AddToImportantBookmarks(bookmark);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        handle.AddToUnimportantBookmarks(bookmark);
                    }
                }
                else
                {
                    handle.AddToUnimportantBookmarks(bookmark);
                }
            }
        }
    }

    public Bookmark GenerateTempBookmark() => Bookmark.Create(GetNextBookmarkId());

    private void AddBookmark(Bookmark bookmark, BookmarkCallback callback, ActivityInstance owningInstance, BookmarkOptions options)
    {
        _bookmarks ??= new Dictionary<Bookmark, BookmarkCallbackWrapper>(Bookmark.Comparer);
        bookmark.Scope = _scope;

        BookmarkCallbackWrapper bookmarkCallbackWrapper = new(callback, owningInstance, options)
        {
            Bookmark = bookmark
        };
        _bookmarks.Add(bookmark, bookmarkCallbackWrapper);

        owningInstance.AddBookmark(bookmark, options);

        if (TD.CreateBookmarkIsEnabled())
        {
            TD.CreateBookmark(owningInstance.Activity.GetType().ToString(), owningInstance.Activity.DisplayName, owningInstance.Id, ActivityUtilities.GetTraceString(bookmark), ActivityUtilities.GetTraceString((BookmarkScope)bookmark.Scope));
        }
    }

    private long GetNextBookmarkId()
    {
        if (_nextId == long.MaxValue)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.OutOfInternalBookmarks));
        }

        long result = _nextId;
        _nextId++;
        return result;
    }

    // This method translates from a bookmark that we may have received from the outside world (IE - new Bookmark(someName))
    // to our internal Bookmark object.  This is necessary because we use bookmark objects as the key to our dictionary
    // (hence our ability to resolve an externally created one), but we keep a lot of important information on our internal
    // instance of that bookmark.  We must always perform this translation when doing exclusive handle housekeeping.
    public bool TryGetBookmarkFromInternalList(Bookmark bookmark, out Bookmark internalBookmark, out BookmarkCallbackWrapper callbackWrapper)
    {
        internalBookmark = null;
        callbackWrapper = null;
        if (_bookmarks == null)
        {
            return false;
        }

        if (_bookmarks.TryGetValue(bookmark, out BookmarkCallbackWrapper wrapper))
        {
            internalBookmark = wrapper.Bookmark;
            callbackWrapper = wrapper;
            return true;
        }

        return false;
    }

    public BookmarkResumptionResult TryGenerateWorkItem(ActivityExecutor executor, bool isExternal, ref Bookmark bookmark, object value, ActivityInstance isolationInstance, out ActivityExecutionWorkItem workItem)
    {
        if (!TryGetBookmarkFromInternalList(bookmark, out Bookmark internalBookmark, out BookmarkCallbackWrapper callbackWrapper))
        {
            workItem = null;
            return BookmarkResumptionResult.NotFound;
        }

        bookmark = internalBookmark;
        if (!ActivityUtilities.IsInScope(callbackWrapper.ActivityInstance, isolationInstance))
        {
            workItem = null;

            // We know about the bookmark, but we can't resume it yet
            return BookmarkResumptionResult.NotReady;
        }

        workItem = callbackWrapper.CreateWorkItem(executor, isExternal, bookmark, value);

        if (!BookmarkOptionsHelper.SupportsMultipleResumes(callbackWrapper.Options))
        {
            // cleanup bookmark on resumption unless the user opts into multi-resume
            Remove(bookmark, callbackWrapper);
        }

        return BookmarkResumptionResult.Success;
    }

    public void PopulateBookmarkInfo(List<BookmarkInfo> bookmarks)
    {
        Fx.Assert(HasBookmarks, "Should only be called if this actually has bookmarks.");

        foreach (KeyValuePair<Bookmark, BookmarkCallbackWrapper> bookmarkEntry in _bookmarks)
        {
            if (bookmarkEntry.Key.IsNamed)
            {
                bookmarks.Add(bookmarkEntry.Key.GenerateBookmarkInfo(bookmarkEntry.Value));
            }
        }
    }

    // No need to translate using TryGetBookmarkFromInternalList because we already have
    // internal instances since this call comes from bookmarks hanging off of the
    // ActivityInstance and not from an external source
    public void PurgeBookmarks(Bookmark singleBookmark, IList<Bookmark> multipleBookmarks)
    {
        if (singleBookmark != null)
        {
            PurgeSingleBookmark(singleBookmark);
        }
        else
        {
            Fx.Assert(multipleBookmarks != null, "caller should never pass null");
            for (int i = 0; i < multipleBookmarks.Count; i++)
            {
                Bookmark bookmark = multipleBookmarks[i];
                PurgeSingleBookmark(bookmark);
            }
        }
    }

    internal void PurgeSingleBookmark(Bookmark bookmark)
    {
        Fx.Assert(_bookmarks.ContainsKey(bookmark) && ReferenceEquals(bookmark, _bookmarks[bookmark].Bookmark), "Something went wrong with our housekeeping - it must exist and must be our intenral reference");
        UpdateExclusiveHandleList(bookmark);
        _bookmarks.Remove(bookmark);
    }

    public bool Remove(Bookmark bookmark, ActivityInstance instanceAttemptingRemove)
    {
        if (TryGetBookmarkFromInternalList(bookmark, out Bookmark internalBookmark, out BookmarkCallbackWrapper callbackWrapper))
        {
            if (callbackWrapper.ActivityInstance != instanceAttemptingRemove)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OnlyBookmarkOwnerCanRemove));
            }

            Remove(internalBookmark, callbackWrapper);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Remove(Bookmark bookmark, BookmarkCallbackWrapper callbackWrapper)
    {
        callbackWrapper.ActivityInstance.RemoveBookmark(bookmark, callbackWrapper.Options);
        UpdateExclusiveHandleList(bookmark);
        _bookmarks.Remove(bookmark);
    }

    private static void UpdateExclusiveHandleList(Bookmark bookmark)
    {
        if (bookmark.ExclusiveHandles != null)
        {
            for (int i = 0; i < bookmark.ExclusiveHandles.Count; i++)
            {
                ExclusiveHandle handle = bookmark.ExclusiveHandles[i];
                Fx.Assert(handle != null, "Internal error.. ExclusiveHandle was null");
                handle.RemoveBookmark(bookmark);
            }
        }
    }
}

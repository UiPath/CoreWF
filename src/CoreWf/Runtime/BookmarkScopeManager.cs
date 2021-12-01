// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Activities.Runtime;

[DataContract]
internal class BookmarkScopeManager
{
    private Dictionary<BookmarkScope, BookmarkManager> _bookmarkManagers;
    private List<BookmarkScope> _uninitializedScopes;
    private List<InstanceKey> _keysToAssociate;
    private List<InstanceKey> _keysToDisassociate;
    private BookmarkScope _defaultScope;
    private long _nextTemporaryId;

    public BookmarkScopeManager()
    {
        _nextTemporaryId = 1;
        _defaultScope = CreateAndRegisterScope(Guid.Empty);
    }

    public BookmarkScope Default => _defaultScope;

    public bool HasKeysToUpdate
    {
        get
        {
            if (_keysToAssociate != null && _keysToAssociate.Count > 0)
            {
                return true;
            }

            if (_keysToDisassociate != null && _keysToDisassociate.Count > 0)
            {
                return true;
            }

            return false;
        }
    }

    [DataMember(Name = "defaultScope")]
    internal BookmarkScope SerializedDefaultScope
    {
        get => _defaultScope;
        set => _defaultScope = value;
    }

    [DataMember(Name = "nextTemporaryId")]
    internal long SerializedNextTemporaryId
    {
        get => _nextTemporaryId;
        set => _nextTemporaryId = value;
    }

    [DataMember(EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
    internal Dictionary<BookmarkScope, BookmarkManager> SerializedBookmarkManagers
    {
        get
        {
            Fx.Assert(_bookmarkManagers != null && _bookmarkManagers.Count > 0, "We always have the default sub instance.");

            return _bookmarkManagers;
        }
        set
        {
            Fx.Assert(value != null, "We don't serialize null.");
            _bookmarkManagers = value;
        }
    }

    [DataMember(EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, Justification = "Called from Serialization")]
    internal List<BookmarkScope> SerializedUninitializedScopes
    {
        get
        {
            if (_uninitializedScopes == null || _uninitializedScopes.Count == 0)
            {
                return null;
            }
            else
            {
                return _uninitializedScopes;
            }
        }
        set
        {
            Fx.Assert(value != null, "We don't serialize null.");
            _uninitializedScopes = value;
        }
    }

    private long GetNextTemporaryId()
    {
        long temp = _nextTemporaryId;
        _nextTemporaryId++;

        return temp;
    }

    public Bookmark CreateBookmark(string name, BookmarkScope scope, BookmarkCallback callback, ActivityInstance owningInstance, BookmarkOptions options)
    {
        Fx.Assert(scope != null, "We should never have a null scope.");

        BookmarkScope lookupScope = scope;

        if (scope.IsDefault)
        {
            lookupScope = _defaultScope;
        }

        if (!_bookmarkManagers.TryGetValue(lookupScope, out BookmarkManager manager))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RegisteredBookmarkScopeRequired));
        }

        return manager.CreateBookmark(name, callback, owningInstance, options);
    }

    public bool RemoveBookmark(Bookmark bookmark, BookmarkScope scope, ActivityInstance instanceAttemptingRemove)
    {
        Fx.Assert(scope != null, "We should never have a null scope.");

        BookmarkScope lookupScope = scope;

        if (scope.IsDefault)
        {
            lookupScope = _defaultScope;
        }

        if (_bookmarkManagers.TryGetValue(lookupScope, out BookmarkManager manager))
        {
            return manager.Remove(bookmark, instanceAttemptingRemove);
        }
        else
        {
            return false;
        }
    }

    public BookmarkResumptionResult TryGenerateWorkItem(ActivityExecutor executor, ref Bookmark bookmark, BookmarkScope scope, object value, ActivityInstance isolationInstance, bool nonScopedBookmarksExist, out ActivityExecutionWorkItem workItem)
    {
        Fx.Assert(scope != null, "We should never have a null sub instance.");

        workItem = null;
        BookmarkScope lookupScope = scope;

        if (scope.IsDefault)
        {
            lookupScope = _defaultScope;
        }

        // We don't really care about the return value since we'll
        // use null to know we should check uninitialized sub instances
        _bookmarkManagers.TryGetValue(lookupScope, out BookmarkManager manager);

        if (manager == null)
        {
            Fx.Assert(lookupScope != null, "The sub instance should not be default if we are here.");

            BookmarkResumptionResult finalResult = BookmarkResumptionResult.NotFound;

            // Check the uninitialized sub instances for a matching bookmark
            if (_uninitializedScopes != null)
            {
                for (int i = 0; i < _uninitializedScopes.Count; i++)
                {
                    BookmarkScope uninitializedScope = _uninitializedScopes[i];

                    Fx.Assert(_bookmarkManagers.ContainsKey(uninitializedScope), "We must always have the uninitialized sub instances.");
                    BookmarkResumptionResult resumptionResult;
                    if (!_bookmarkManagers[uninitializedScope].TryGetBookmarkFromInternalList(bookmark, out Bookmark internalBookmark, out _))
                    {
                        resumptionResult = BookmarkResumptionResult.NotFound;
                    }
                    else if (IsExclusiveScopeUnstable(internalBookmark))
                    {
                        resumptionResult = BookmarkResumptionResult.NotReady;
                    }
                    else 
                    {
                        resumptionResult = _bookmarkManagers[uninitializedScope].TryGenerateWorkItem(executor, true, ref bookmark, value, isolationInstance, out workItem);
                    }

                    if (resumptionResult == BookmarkResumptionResult.Success)
                    {
                        // We are using InitializeBookmarkScopeWithoutKeyAssociation because we know this is a new uninitialized scope and
                        // the key we would associate is already associated. And if we did the association here, the subsequent call to
                        // FlushBookmarkScopeKeys would try to flush it out, but it won't have the transaction correct so will hang waiting for
                        // the transaction that has the PersistenceContext locked to complete. But it won't complete successfully until
                        // we finish processing here.
                        InitializeBookmarkScopeWithoutKeyAssociation(uninitializedScope, scope.Id);

                        // We've found what we were looking for
                        return BookmarkResumptionResult.Success;
                    }
                    else if (resumptionResult == BookmarkResumptionResult.NotReady)
                    {
                        // This uninitialized sub-instance has a matching bookmark but
                        // it can't currently be resumed.  We won't return BookmarkNotFound
                        // because of this.
                        finalResult = BookmarkResumptionResult.NotReady;
                    }
                    else
                    {
                        if (finalResult == BookmarkResumptionResult.NotFound)
                        {
                            // If we still are planning on returning failure then
                            // we'll incur the cost of seeing if this scope is
                            // stable or not.

                            if (!IsStable(uninitializedScope, nonScopedBookmarksExist))
                            {
                                // There exists an uninitialized scope which is unstable.
                                // At the very least this means we'll return NotReady since
                                // this uninitialized scope might eventually contain this
                                // bookmark.
                                finalResult = BookmarkResumptionResult.NotReady;
                            }
                        }
                    }
                }
            }

            return finalResult;
        }
        else
        {
            BookmarkResumptionResult resumptionResult;
            if (!manager.TryGetBookmarkFromInternalList(bookmark, out Bookmark bookmarkFromList, out _))
            {
                resumptionResult = BookmarkResumptionResult.NotFound;
            }
            else
            {
                if (IsExclusiveScopeUnstable(bookmarkFromList))
                {
                    resumptionResult = BookmarkResumptionResult.NotReady;
                }
                else
                {
                    resumptionResult = manager.TryGenerateWorkItem(executor, true, ref bookmark, value, isolationInstance, out workItem);
                }
            }


            if (resumptionResult == BookmarkResumptionResult.NotFound)
            {
                if (!IsStable(lookupScope, nonScopedBookmarksExist))
                {
                    resumptionResult = BookmarkResumptionResult.NotReady;
                }
            }

            return resumptionResult;
        }
    }

    public void PopulateBookmarkInfo(ref List<BookmarkInfo> bookmarks)
    {
        foreach (BookmarkManager manager in _bookmarkManagers.Values)
        {
            if (manager.HasBookmarks)
            {
                bookmarks ??= new List<BookmarkInfo>();
                manager.PopulateBookmarkInfo(bookmarks);
            }
        }
    }

    public ReadOnlyCollection<BookmarkInfo> GetBookmarks(BookmarkScope scope)
    {
        Fx.Assert(scope != null, "We should never be passed null here.");

        BookmarkScope lookupScope = scope;

        if (scope.IsDefault)
        {
            lookupScope = _defaultScope;
        }

        if (_bookmarkManagers.TryGetValue(lookupScope, out BookmarkManager manager))
        {
            if (!manager.HasBookmarks)
            {
                manager = null;
            }
        }


        if (manager != null)
        {
            List<BookmarkInfo> bookmarks = new();

            manager.PopulateBookmarkInfo(bookmarks);

            return new ReadOnlyCollection<BookmarkInfo>(bookmarks);
        }
        else
        {
            return null;
        }
    }

    public ICollection<InstanceKey> GetKeysToAssociate()
    {
        if (_keysToAssociate == null || _keysToAssociate.Count == 0)
        {
            return null;
        }

        ICollection<InstanceKey> result = _keysToAssociate;
        _keysToAssociate = null;
        return result;
    }

    public ICollection<InstanceKey> GetKeysToDisassociate()
    {
        if (_keysToDisassociate == null || _keysToDisassociate.Count == 0)
        {
            return null;
        }

        ICollection<InstanceKey> result = _keysToDisassociate;
        _keysToDisassociate = null;
        return result;
    }

    public void InitializeScope(BookmarkScope scope, Guid id)
    {
        Fx.Assert(!scope.IsInitialized, "This should have been checked by the caller.");

        BookmarkScope lookupScope = InitializeBookmarkScopeWithoutKeyAssociation(scope, id);
        CreateAssociatedKey(lookupScope);
    }

    public BookmarkScope InitializeBookmarkScopeWithoutKeyAssociation(BookmarkScope scope, Guid id)
    {
        Fx.Assert(!scope.IsInitialized, "This should have been checked by the caller.");

        BookmarkScope lookupScope = scope;

        if (scope.IsDefault)
        {
            lookupScope = _defaultScope;
        }

        if (_uninitializedScopes == null || !_uninitializedScopes.Contains(lookupScope))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeNotRegisteredForInitialize));
        }

        Fx.Assert(_bookmarkManagers != null, "This is never null if uninitializedScopes is non-null.");

        if (_bookmarkManagers.ContainsKey(new BookmarkScope(id)))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeWithIdAlreadyExists(id)));
        }

        BookmarkManager bookmarks = _bookmarkManagers[lookupScope];
        _bookmarkManagers.Remove(lookupScope);
        _uninitializedScopes.Remove(lookupScope);

        long temporaryId = lookupScope.TemporaryId;
        // We initialize and re-add to our dictionary.  We have to
        // re-add because the hash has changed.
        lookupScope.Id = id;
        _bookmarkManagers.Add(lookupScope, bookmarks);

        if (TD.BookmarkScopeInitializedIsEnabled())
        {
            TD.BookmarkScopeInitialized(temporaryId.ToString(CultureInfo.InvariantCulture), lookupScope.Id.ToString());
        }

        return lookupScope;
    }

    public BookmarkScope CreateAndRegisterScope(Guid scopeId) => CreateAndRegisterScope(scopeId, null);

    internal BookmarkScope CreateAndRegisterScope(Guid scopeId, BookmarkScopeHandle scopeHandle)
    {
        _bookmarkManagers ??= new Dictionary<BookmarkScope, BookmarkManager>();
        BookmarkScope scope = null;
        if (scopeId == Guid.Empty)
        {
            //
            // This is the very first activity which started the sub-instance
            //
            scope = new BookmarkScope(GetNextTemporaryId());
            _bookmarkManagers.Add(scope, new BookmarkManager(scope, scopeHandle));

            if (TD.CreateBookmarkScopeIsEnabled())
            {
                TD.CreateBookmarkScope(ActivityUtilities.GetTraceString(scope));
            }

            _uninitializedScopes ??= new List<BookmarkScope>();
            _uninitializedScopes.Add(scope);
        }
        else
        {
            //
            // Try to find one in the existing sub-instances
            //
            foreach (BookmarkScope eachScope in _bookmarkManagers.Keys)
            {
                if (eachScope.Id.Equals(scopeId))
                {
                    scope = eachScope;
                    break;
                }
            }

            //
            // We did not find one, e.g. the first receive will get the correlation id from the 
            // correlation channel
            //
            if (scope == null)
            {
                scope = new BookmarkScope(scopeId);
                _bookmarkManagers.Add(scope, new BookmarkManager(scope, scopeHandle));

                if (TD.CreateBookmarkScopeIsEnabled())
                {
                    TD.CreateBookmarkScope(string.Format(CultureInfo.InvariantCulture, "Id: {0}", ActivityUtilities.GetTraceString(scope)));
                }
            }

            CreateAssociatedKey(scope);
        }

        return scope;
    }

    private void CreateAssociatedKey(BookmarkScope newScope)
    {
        _keysToAssociate ??= new List<InstanceKey>(2);
        _keysToAssociate.Add(new InstanceKey(newScope.Id));
    }

    public void UnregisterScope(BookmarkScope scope)
    {
        Fx.Assert(!scope.IsDefault, "Cannot unregister the default sub instance.");

        if (_bookmarkManagers == null || !_bookmarkManagers.ContainsKey(scope))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeNotRegisteredForUnregister));
        }

        if (_bookmarkManagers[scope].HasBookmarks)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeHasBookmarks));
        }

        _bookmarkManagers.Remove(scope);

        if (!scope.IsInitialized)
        {
            Fx.Assert(_uninitializedScopes != null && _uninitializedScopes.Contains(scope), "Something is wrong with our housekeeping.");

            _uninitializedScopes.Remove(scope);
        }
        else
        {
            _keysToDisassociate ??= new List<InstanceKey>(2);
            _keysToDisassociate.Add(new InstanceKey(scope.Id));
            Fx.Assert(_uninitializedScopes == null || !_uninitializedScopes.Contains(scope), "We shouldn't have this in the uninitialized list.");
        }
    }

    private bool IsStable(BookmarkScope scope, bool nonScopedBookmarksExist)
    {
        Fx.Assert(_bookmarkManagers.ContainsKey(scope), "The caller should have made sure this scope exists in the bookmark managers dictionary.");

        if (nonScopedBookmarksExist)
        {
            return false;
        }

        if (_bookmarkManagers != null)
        {
            foreach (KeyValuePair<BookmarkScope, BookmarkManager> scopeBookmarks in _bookmarkManagers)
            {
                IEquatable<BookmarkScope> comparison = scopeBookmarks.Key;
                if (!comparison.Equals(scope))
                {
                    if (scopeBookmarks.Value.HasBookmarks)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

#pragma warning disable CA1822 // Mark members as static
    public bool IsExclusiveScopeUnstable(Bookmark bookmark)
#pragma warning restore CA1822 // Mark members as static
    {
        if (bookmark.ExclusiveHandles != null)
        {
            for (int i = 0; i < bookmark.ExclusiveHandles.Count; i++)
            {
                ExclusiveHandle handle = bookmark.ExclusiveHandles[i];
                Fx.Assert(handle != null, "Internal error..ExclusiveHandle was null");
                if ((handle.ImportantBookmarks != null && handle.ImportantBookmarks.Contains(bookmark)) && (handle.UnimportantBookmarks != null && handle.UnimportantBookmarks.Count != 0))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void PurgeBookmarks(BookmarkManager nonScopedBookmarkManager, Bookmark singleBookmark, IList<Bookmark> multipleBookmarks)
    {
        if (singleBookmark != null)
        {
            PurgeBookmark(singleBookmark, nonScopedBookmarkManager);
        }
        else
        {
            Fx.Assert(multipleBookmarks != null, "caller should never pass null");
            for (int i = 0; i < multipleBookmarks.Count; i++)
            {
                Bookmark bookmark = multipleBookmarks[i];

                PurgeBookmark(bookmark, nonScopedBookmarkManager);
            }
        }
    }

    private void PurgeBookmark(Bookmark bookmark, BookmarkManager nonScopedBookmarkManager)
    {
        BookmarkManager manager;
        if (bookmark.Scope != null)
        {
            BookmarkScope lookupScope = bookmark.Scope;

            if (bookmark.Scope.IsDefault)
            {
                lookupScope = _defaultScope;
            }

            Fx.Assert(_bookmarkManagers.ContainsKey(lookupScope), "We should have the single bookmark's sub instance registered");
            manager = _bookmarkManagers[lookupScope];
        }
        else
        {
            manager = nonScopedBookmarkManager;
        }

        manager.PurgeSingleBookmark(bookmark);
    }
}

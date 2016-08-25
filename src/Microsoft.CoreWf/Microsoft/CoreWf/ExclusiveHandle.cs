// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
    [DataContract]
    public class ExclusiveHandle : Handle
    {
        private ReadOnlyCollection<BookmarkScopeHandle> _readOnlyBookmarkScopeCollection;

        private List<BookmarkScopeHandle> _bookmarkScopes;

        private ActivityInstance _owningInstance;

        private ActivityExecutor _executor;

        private ExclusiveHandleBookmarkList _importantBookmarks;

        private ExclusiveHandleBookmarkList _unimportantBookmarks;

        private bool _bookmarkScopesListIsDefault;

        public ExclusiveHandle()
        {
            this.CanBeRemovedWithExecutingChildren = true;
        }

        public ReadOnlyCollection<BookmarkScopeHandle> RegisteredBookmarkScopes
        {
            get
            {
                if (_bookmarkScopes == null)
                {
                    return new ReadOnlyCollection<BookmarkScopeHandle>(new List<BookmarkScopeHandle>());
                }

                if (_readOnlyBookmarkScopeCollection == null)
                {
                    _readOnlyBookmarkScopeCollection = new ReadOnlyCollection<BookmarkScopeHandle>(_bookmarkScopes);
                }
                return _readOnlyBookmarkScopeCollection;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "bookmarkScopes")]
        internal List<BookmarkScopeHandle> SerializedBookmarkScopes
        {
            get { return _bookmarkScopes; }
            set { _bookmarkScopes = value; }
        }

        [DataMember(Name = "owningInstance")]
        internal ActivityInstance SerializedOwningInstance
        {
            get { return _owningInstance; }
            set { _owningInstance = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "executor")]
        internal ActivityExecutor SerializedExecutor
        {
            get { return _executor; }
            set { _executor = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "importantBookmarks")]
        internal ExclusiveHandleBookmarkList SerializedImportantBookmarks
        {
            get { return _importantBookmarks; }
            set { _importantBookmarks = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "unimportantBookmarks")]
        internal ExclusiveHandleBookmarkList SerializedUnimportantBookmarks
        {
            get { return _unimportantBookmarks; }
            set { _unimportantBookmarks = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "bookmarkScopesListIsDefault")]
        internal bool SerializedBookmarkScopesListIsDefault
        {
            get { return _bookmarkScopesListIsDefault; }
            set { _bookmarkScopesListIsDefault = value; }
        }


        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "We are restricting the activities that can call this API.")]
        public void RegisterBookmarkScope(NativeActivityContext context, BookmarkScopeHandle bookmarkScopeHandle)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.ThrowIfDisposed();

            if (bookmarkScopeHandle == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("bookmarkScopeHandle");
            }

            if ((this.ImportantBookmarks != null && this.ImportantBookmarks.Count != 0) || (this.UnimportantBookmarks != null && this.UnimportantBookmarks.Count != 0))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ExclusiveHandleRegisterBookmarkScopeFailed));
            }

            if (_bookmarkScopesListIsDefault)
            {
                _bookmarkScopesListIsDefault = false;
                _bookmarkScopes.Clear();
            }

            _bookmarkScopes.Add(bookmarkScopeHandle);
            _readOnlyBookmarkScopeCollection = null;
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "We are restricting the activities that can call this API.")]
        public void Reinitialize(NativeActivityContext context)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.ThrowIfDisposed();

            if ((this.ImportantBookmarks != null && this.ImportantBookmarks.Count != 0) || (this.UnimportantBookmarks != null && this.UnimportantBookmarks.Count != 0))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ExclusiveHandleReinitializeFailed));
            }
            _bookmarkScopes.Clear();
            _readOnlyBookmarkScopeCollection = null;
            this.PerformDefaultRegistration();
        }

        protected override void OnInitialize(HandleInitializationContext context)
        {
            _owningInstance = context.OwningActivityInstance;
            _executor = context.Executor;
            PerformDefaultRegistration();
        }

        internal ExclusiveHandleBookmarkList ImportantBookmarks
        {
            get
            {
                return _importantBookmarks;
            }
            set
            {
                _importantBookmarks = value;
            }
        }

        internal ExclusiveHandleBookmarkList UnimportantBookmarks
        {
            get
            {
                return _unimportantBookmarks;
            }
            set
            {
                _unimportantBookmarks = value;
            }
        }

        internal void AddToImportantBookmarks(Bookmark bookmark)
        {
            if (this.ImportantBookmarks == null)
            {
                this.ImportantBookmarks = new ExclusiveHandleBookmarkList();
            }

            Fx.Assert(!this.ImportantBookmarks.Contains(bookmark), "We shouldnt be here. We attempted to add the same bookmark");
            this.ImportantBookmarks.Add(bookmark);

            if (bookmark.ExclusiveHandles == null)
            {
                bookmark.ExclusiveHandles = new ExclusiveHandleList();
            }

            Fx.Assert(!bookmark.ExclusiveHandles.Contains(this), "We shouldnt be here. We attempted to add the bookmark to this exclusive handle already");
            bookmark.ExclusiveHandles.Add(this);
        }

        internal void AddToUnimportantBookmarks(Bookmark bookmark)
        {
            if (this.UnimportantBookmarks == null)
            {
                this.UnimportantBookmarks = new ExclusiveHandleBookmarkList();
            }

            Fx.Assert(!this.UnimportantBookmarks.Contains(bookmark), "We shouldnt be here. We attempted to add the same bookmark");
            this.UnimportantBookmarks.Add(bookmark);

            if (bookmark.ExclusiveHandles == null)
            {
                bookmark.ExclusiveHandles = new ExclusiveHandleList();
            }

            Fx.Assert(!bookmark.ExclusiveHandles.Contains(this), "We shouldnt be here. We attempted to add the bookmark to this exclusive handle already");
            bookmark.ExclusiveHandles.Add(this);
        }

        internal void RemoveBookmark(Bookmark bookmark)
        {
            Fx.Assert((this.ImportantBookmarks != null && this.ImportantBookmarks.Contains(bookmark)) ||
                       (this.UnimportantBookmarks != null && this.UnimportantBookmarks.Contains(bookmark)), "Internal error");

            if (this.ImportantBookmarks != null)
            {
                if (this.ImportantBookmarks.Contains(bookmark))
                {
                    this.ImportantBookmarks.Remove(bookmark);
                    return;
                }
            }

            if (this.UnimportantBookmarks != null)
            {
                if (this.UnimportantBookmarks.Contains(bookmark))
                {
                    this.UnimportantBookmarks.Remove(bookmark);
                }
            }
        }

        private void PerformDefaultRegistration()
        {
            if (_bookmarkScopes == null)
            {
                _bookmarkScopes = new List<BookmarkScopeHandle>();
            }

            //First register the default subinstance
            _bookmarkScopes.Add(BookmarkScopeHandle.Default);

            // Note that we are starting the LocationEnvironment traversal from the current environment's Parent. We don't
            // want to include any BookmarkScopeHandles that are at the same scope level as the ExclusiveHandle. The ExclusiveHandle
            // should only be dependent on BookmarkScopeHandles that are higher in the scope tree.
            LocationEnvironment current = _owningInstance.Environment;
            if (current != null)
            {
                for (current = current.Parent; current != null; current = current.Parent)
                {
                    //don't bother continuing if at this level there are no handles
                    if (!current.HasHandles)
                    {
                        continue;
                    }

                    // Look at the contained handles for the environment.
                    List<Handle> handles = current.Handles;
                    if (handles != null)
                    {
                        int count = handles.Count;
                        for (int i = 0; i < count; i++)
                        {
                            BookmarkScopeHandle scopeHandle = handles[i] as BookmarkScopeHandle;
                            if (scopeHandle != null)
                            {
                                _bookmarkScopes.Add(scopeHandle);
                            }
                        }
                    }
                }
            }

            // Also need to look in the Executor for handles that may have been created without an environment.
            List<Handle> executorHandles = _executor.Handles;
            if (executorHandles != null)
            {
                int count = executorHandles.Count;
                for (int i = 0; i < count; i++)
                {
                    BookmarkScopeHandle scopeHandle = executorHandles[i] as BookmarkScopeHandle;
                    if (scopeHandle != null)
                    {
                        _bookmarkScopes.Add(scopeHandle);
                    }
                }
            }

            _bookmarkScopesListIsDefault = true;
        }

        // Exclusive handle needs to track bookmarks such that it can tell the difference between two bookmarks in
        // different bookmark scopes with the same name.  Since we always deal in terms of the internal bookmark
        // reference that we have, we can do an object.ReferenceEquals comparison to determine distinct bookmarks
        // without having to add some sort of "containing scope" property to Bookmark.
        [DataContract]
        internal class ExclusiveHandleBookmarkList
        {
            private List<Bookmark> _bookmarks;

            public ExclusiveHandleBookmarkList()
                : base()
            {
                _bookmarks = new List<Bookmark>();
            }

            public int Count
            {
                get { return _bookmarks.Count; }
            }

            [DataMember(Name = "bookmarks")]
            internal List<Bookmark> SerializedBookmarks
            {
                get { return _bookmarks; }
                set { _bookmarks = value; }
            }

            public void Add(Bookmark bookmark)
            {
                Fx.Assert(bookmark != null, "A valid bookmark is expected.");
                _bookmarks.Add(bookmark);
            }

            public void Remove(Bookmark bookmark)
            {
                Fx.Assert(bookmark != null, "A valid bookmark is expected.");

                for (int i = 0; i < _bookmarks.Count; i++)
                {
                    if (object.ReferenceEquals(_bookmarks[i], bookmark))
                    {
                        _bookmarks.RemoveAt(i);
                        return;
                    }
                }
            }

            public bool Contains(Bookmark bookmark)
            {
                Fx.Assert(bookmark != null, "A valid bookmark is expected.");

                for (int i = 0; i < _bookmarks.Count; i++)
                {
                    if (object.ReferenceEquals(_bookmarks[i], bookmark))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

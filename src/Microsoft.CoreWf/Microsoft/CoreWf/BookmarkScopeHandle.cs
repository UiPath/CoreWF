// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime.DurableInstancing;
using System;
using System.Runtime.Serialization;

namespace CoreWf
{
    [DataContract]
    public sealed class BookmarkScopeHandle : Handle
    {
        private BookmarkScope _bookmarkScope;

        private static BookmarkScopeHandle s_defaultBookmarkScopeHandle = new BookmarkScopeHandle(BookmarkScope.Default);

        public BookmarkScopeHandle()
        {
        }

        internal BookmarkScopeHandle(BookmarkScope bookmarkScope)
        {
            _bookmarkScope = bookmarkScope;
            if (bookmarkScope != null)
            {
                _bookmarkScope.IncrementHandleReferenceCount();
            }
        }

        public static BookmarkScopeHandle Default
        {
            get
            {
                return s_defaultBookmarkScopeHandle;
            }
        }

        public BookmarkScope BookmarkScope
        {
            get
            {
                return _bookmarkScope;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "bookmarkScope")]
        internal BookmarkScope SerializedBookmarkScope
        {
            get { return _bookmarkScope; }
            set
            {
                _bookmarkScope = value;
                if (_bookmarkScope != null)
                {
                    _bookmarkScope.IncrementHandleReferenceCount();
                }
            }
        }

        //To be called from public APIs that need to verify the passed in context
        private void ThrowIfContextIsNullOrDisposed(NativeActivityContext context)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.ThrowIfDisposed();
        }

        public void CreateBookmarkScope(NativeActivityContext context)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            if (_bookmarkScope != null)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
            }

            this.ThrowIfUninitialized();
            _bookmarkScope = context.CreateBookmarkScope(Guid.Empty, this);
            _bookmarkScope.IncrementHandleReferenceCount();
        }

        public void CreateBookmarkScope(NativeActivityContext context, Guid scopeId)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            if (_bookmarkScope != null)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
            }

            this.ThrowIfUninitialized();
            _bookmarkScope = context.CreateBookmarkScope(scopeId, this);
            _bookmarkScope.IncrementHandleReferenceCount();
        }

        public void Initialize(NativeActivityContext context, Guid scope)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            this.ThrowIfUninitialized();
            _bookmarkScope.Initialize(context, scope);
        }

        protected override void OnUninitialize(HandleInitializationContext context)
        {
            if (_bookmarkScope != null)
            {
                int scopeRefCount = _bookmarkScope.DecrementHandleReferenceCount();
                DisassociateInstanceKeysExtension extension = context.GetExtension<DisassociateInstanceKeysExtension>();
                // We only unregister the BookmarkScope if the extension exists and is enabled and if we had the last reference to it.
                if ((extension != null) && extension.AutomaticDisassociationEnabled && (scopeRefCount == 0))
                {
                    context.UnregisterBookmarkScope(_bookmarkScope);
                }
            }
            base.OnUninitialize(context);
        }
    }
}



// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System;
    using System.Runtime.Serialization;
    using CoreWf.Internals;
    using CoreWf.Runtime.DurableInstancing;

    [DataContract]
    public sealed class BookmarkScopeHandle : Handle
    {
        public BookmarkScopeHandle()
        {
        }

        internal BookmarkScopeHandle(BookmarkScope bookmarkScope)
        {
            this.BookmarkScope = bookmarkScope;
            if (bookmarkScope != null)
            {
                this.BookmarkScope.IncrementHandleReferenceCount();
            }
        }

        public static BookmarkScopeHandle Default { get; } = new BookmarkScopeHandle(BookmarkScope.Default);

        public BookmarkScope BookmarkScope { get; private set; }

        [DataMember(EmitDefaultValue = false, Name = "bookmarkScope")]
        internal BookmarkScope SerializedBookmarkScope
        {
            get { return this.BookmarkScope; }
            set
            {
                this.BookmarkScope = value;
                if (this.BookmarkScope != null)
                {
                    this.BookmarkScope.IncrementHandleReferenceCount();
                }
            }
        }

        //To be called from public APIs that need to verify the passed in context
        private void ThrowIfContextIsNullOrDisposed(NativeActivityContext context)
        {
            if (context == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(context));
            }

            context.ThrowIfDisposed();
        }

        public void CreateBookmarkScope(NativeActivityContext context)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            if (this.BookmarkScope != null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
            }

            this.ThrowIfUninitialized();
            this.BookmarkScope = context.CreateBookmarkScope(Guid.Empty, this);
            this.BookmarkScope.IncrementHandleReferenceCount();
        }

        public void CreateBookmarkScope(NativeActivityContext context, Guid scopeId)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            if (this.BookmarkScope != null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
            }

            this.ThrowIfUninitialized();
            this.BookmarkScope = context.CreateBookmarkScope(scopeId, this);
            this.BookmarkScope.IncrementHandleReferenceCount();
        }
        
        public void Initialize(NativeActivityContext context, Guid scope)
        {
            this.ThrowIfContextIsNullOrDisposed(context);
            this.ThrowIfUninitialized();
            this.BookmarkScope.Initialize(context, scope);
        }

        protected override void OnUninitialize(HandleInitializationContext context)
        {
            if (this.BookmarkScope != null)
            {
                int scopeRefCount = this.BookmarkScope.DecrementHandleReferenceCount();
                DisassociateInstanceKeysExtension extension = context.GetExtension<DisassociateInstanceKeysExtension>();
                // We only unregister the BookmarkScope if the extension exists and is enabled and if we had the last reference to it.
                if ((extension != null) && extension.AutomaticDisassociationEnabled && (scopeRefCount == 0))
                {
                    context.UnregisterBookmarkScope(this.BookmarkScope);
                }
            }
            base.OnUninitialize(context);
        }
    }
}



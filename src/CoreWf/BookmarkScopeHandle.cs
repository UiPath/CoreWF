// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime.DurableInstancing;

[DataContract]
public sealed class BookmarkScopeHandle : Handle
{
    public BookmarkScopeHandle() { }

    internal BookmarkScopeHandle(BookmarkScope bookmarkScope)
    {
        BookmarkScope = bookmarkScope;
        BookmarkScope?.IncrementHandleReferenceCount();
    }

    public static BookmarkScopeHandle Default { get; } = new BookmarkScopeHandle(BookmarkScope.Default);

    public BookmarkScope BookmarkScope { get; private set; }

    [DataMember(EmitDefaultValue = false, Name = "bookmarkScope")]
    internal BookmarkScope SerializedBookmarkScope
    {
        get => BookmarkScope;
        set
        {
            BookmarkScope = value;
            BookmarkScope?.IncrementHandleReferenceCount();
        }
    }

    //To be called from public APIs that need to verify the passed in context
    private static void ThrowIfContextIsNullOrDisposed(NativeActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.ThrowIfDisposed();
    }

    public void CreateBookmarkScope(NativeActivityContext context)
    {

        ThrowIfContextIsNullOrDisposed(context);
        if (BookmarkScope != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
        }

        ThrowIfUninitialized();
        BookmarkScope = context.CreateBookmarkScope(Guid.Empty, this);
        BookmarkScope.IncrementHandleReferenceCount();
    }

    public void CreateBookmarkScope(NativeActivityContext context, Guid scopeId)
    {

        ThrowIfContextIsNullOrDisposed(context);
        if (BookmarkScope != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CreateBookmarkScopeFailed));
        }

        ThrowIfUninitialized();
        BookmarkScope = context.CreateBookmarkScope(scopeId, this);
        BookmarkScope.IncrementHandleReferenceCount();
    }

    public void Initialize(NativeActivityContext context, Guid scope)
    {

        ThrowIfContextIsNullOrDisposed(context);
        ThrowIfUninitialized();
        BookmarkScope.Initialize(context, scope);
    }

    protected override void OnUninitialize(HandleInitializationContext context)
    {
        if (BookmarkScope != null)
        {
            int scopeRefCount = BookmarkScope.DecrementHandleReferenceCount();
            DisassociateInstanceKeysExtension extension = context.GetExtension<DisassociateInstanceKeysExtension>();
            // We only unregister the BookmarkScope if the extension exists and is enabled and if we had the last reference to it.
            if ((extension != null) && extension.AutomaticDisassociationEnabled && (scopeRefCount == 0))
            {
                context.UnregisterBookmarkScope(BookmarkScope);
            }
        }
        base.OnUninitialize(context);
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.Activities;
using Hosting;
using Internals;
using Runtime;

[DataContract]
[Fx.Tag.XamlVisible(false)]
public sealed class BookmarkScope : IEquatable<BookmarkScope>
{
    private static BookmarkScope defaultBookmarkScope;
    private long _temporaryId;
    private Guid _id;
    private int _handleReferenceCount;

    internal BookmarkScope(long temporaryId)
    {
        Fx.Assert(temporaryId != default, "Should never call this constructor with the default value.");
        _temporaryId = temporaryId;
    }

    public BookmarkScope(Guid id) => _id = id;

    private BookmarkScope()
    {
        // Only called for making the default sub instance
        // which has an Id of Guid.Empty
    }

    public bool IsInitialized => _temporaryId == default;

    public Guid Id
    {
        get => _id;
        internal set
        {
            Fx.Assert(value != Guid.Empty, "Cannot set this to Guid.Empty.");
            Fx.Assert(!IsInitialized, "Can only set this when uninitialized.");

            _id = value;
            _temporaryId = default;
        }
    }

    internal int IncrementHandleReferenceCount() => ++_handleReferenceCount;

    internal int DecrementHandleReferenceCount() => --_handleReferenceCount;

    [DataMember(EmitDefaultValue = false, Name = "temporaryId")]
    internal long SerializedTemporaryId
    {
        get => _temporaryId;
        set => _temporaryId = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "id")]
    internal Guid SerializedId
    {
        get => _id;
        set => _id = value;
    }

    internal long TemporaryId => _temporaryId;

    public static BookmarkScope Default
    {
        get
        {
            defaultBookmarkScope ??= new BookmarkScope();
            return defaultBookmarkScope;
        }
    }

    internal bool IsDefault
    {
        get
        {
            // In the strictest sense the default is not initiailized.
            // The Default BookmarkScope is really just a loose reference
            // to the instance specific default that you can get to
            // through NativeActivityContext.DefaultBookmarkScope.
            // We use a scope initialized to Guid.Empty to signify this
            // "loose reference".
            return IsInitialized && _id == Guid.Empty;
        }
    }

    public void Initialize(NativeActivityContext context, Guid id)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        if (id == Guid.Empty)
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(id));
        }

        if (IsInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkScopeAlreadyInitialized));
        }

        context.InitializeBookmarkScope(this, id);
    }

    public override int GetHashCode()
    {
        if (IsInitialized)
        {
            return _id.GetHashCode();
        }
        else
        {
            return _temporaryId.GetHashCode();
        }
    }

    internal BookmarkScopeInfo GenerateScopeInfo()
    {
        if (IsInitialized)
        {
            return new BookmarkScopeInfo(Id);
        }
        else
        {
            return new BookmarkScopeInfo(_temporaryId.ToString(CultureInfo.InvariantCulture));
        }
    }

    public bool Equals(BookmarkScope other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (IsDefault)
        {
            return other.IsDefault;
        }
        else if (IsInitialized)
        {
            Fx.Assert(_id != Guid.Empty, "If we're not the default but we're initialized then we must have a non-Empty Guid.");

            if (other._id == _id)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            Fx.Assert(_temporaryId != 0, "We should have a non-zero temp id if we're not the default and not initialized.");

            if (other._temporaryId == _temporaryId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public override bool Equals(object obj) => Equals(obj as BookmarkScope);
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.Activities;
using Hosting;
using Internals;
using Runtime;

public delegate void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object value);

[DataContract]
[Fx.Tag.XamlVisible(false)]
[TypeConverter(typeof(BookmarkConverter))]
public class Bookmark : IEquatable<Bookmark>
{
    private static readonly Bookmark asyncOperationCompletionBookmark = new(-1);
    private static IEqualityComparer<Bookmark> comparer;

    //Used only when exclusive scopes are involved
    private ExclusiveHandleList _exclusiveHandlesThatReferenceThis;
    private long _id;
    private string _externalName;

    internal Bookmark() { }

    private Bookmark(long id)
    {
        Fx.Assert(id != 0, "id should not be zero");
        _id = id;
    }

    public Bookmark(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        _externalName = name;
    }

    internal static Bookmark AsyncOperationCompletionBookmark => asyncOperationCompletionBookmark;

    internal static IEqualityComparer<Bookmark> Comparer
    {
        get
        {
            comparer ??= new BookmarkComparer();
            return comparer;
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "exclusiveHandlesThatReferenceThis", Order = 2)]
    internal ExclusiveHandleList SerializedExclusiveHandlesThatReferenceThis
    {
        get => _exclusiveHandlesThatReferenceThis;
        set => _exclusiveHandlesThatReferenceThis = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "id", Order = 0)]
    internal long SerializedId
    {
        get => _id;
        set => _id = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "externalName", Order = 1)]
    internal string SerializedExternalName
    {
        get => _externalName;
        set => _externalName = value;
    }

    [DataMember(EmitDefaultValue = false)]
    internal BookmarkScope Scope { get; set; }

    internal bool IsNamed => _id == 0;

    public string Name => IsNamed ? _externalName : string.Empty;

    internal long Id
    {
        get
        {
            Fx.Assert(!IsNamed, "We should only get the id for unnamed bookmarks.");

            return _id;
        }
    }

    internal ExclusiveHandleList ExclusiveHandles
    {
        get => _exclusiveHandlesThatReferenceThis;
        set => _exclusiveHandlesThatReferenceThis = value;
    }


    internal static Bookmark Create(long id) => new(id);

    internal BookmarkInfo GenerateBookmarkInfo(BookmarkCallbackWrapper bookmarkCallback)
    {
        Fx.Assert(IsNamed, "Can only generate BookmarkInfo for external bookmarks");

        BookmarkScopeInfo scopeInfo = null;

        if (Scope != null)
        {
            scopeInfo = Scope.GenerateScopeInfo();
        }

        return new BookmarkInfo(_externalName, bookmarkCallback.ActivityInstance.Activity.DisplayName, scopeInfo);
    }

    public bool Equals(Bookmark other)
    {
        if (other is null)
        {
            return false;
        }

        if (IsNamed)
        {
            return other.IsNamed && _externalName == other._externalName;
        }
        else
        {
            return _id == other._id;
        }
    }

    public override bool Equals(object obj) => Equals(obj as Bookmark);

    public override int GetHashCode() => IsNamed ? _externalName.GetHashCode() : _id.GetHashCode();

    public override string ToString() => IsNamed ? Name : Id.ToString(CultureInfo.InvariantCulture);

    [DataContract]
    internal class BookmarkComparer : IEqualityComparer<Bookmark>
    {
        public BookmarkComparer() { }

        public bool Equals(Bookmark x, Bookmark y)
        {
            if (x is null)
            {
                return y is null;
            }

            return x.Equals(y);
        }

        public int GetHashCode(Bookmark obj) => obj.GetHashCode();
    }
}

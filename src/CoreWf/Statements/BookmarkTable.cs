// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

[DataContract]
internal class BookmarkTable
{
    //Number of bookmarks used internally       
    private static readonly int tableSize = Enum.GetValues(typeof(CompensationBookmarkName)).Length;
    private Bookmark[] _bookmarkTable;

    public BookmarkTable()
    {
        _bookmarkTable = new Bookmark[tableSize];
    }

    public Bookmark this[CompensationBookmarkName bookmarkName]
    {
        get => _bookmarkTable[(int)bookmarkName];
        set => _bookmarkTable[(int)bookmarkName] = value;
    }

    [DataMember(Name = "bookmarkTable")]
    internal Bookmark[] SerializedBookmarkTable
    {
        get => _bookmarkTable;
        set => _bookmarkTable = value;
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Statements;

[Fx.Tag.XamlVisible(false)]
[DataContract]
internal class CompensationTokenData
{
    internal CompensationTokenData(long compensationId, long parentCompensationId)
    {
        CompensationId = compensationId;
        ParentCompensationId = parentCompensationId;
        BookmarkTable = new BookmarkTable();
        ExecutionTracker = new ExecutionTracker();
        CompensationState = CompensationState.Creating;
    }

    [DataMember(EmitDefaultValue = false)]
    internal long CompensationId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal long ParentCompensationId { get; set; }

    [DataMember]
    internal BookmarkTable BookmarkTable { get; set; }

    [DataMember]
    internal ExecutionTracker ExecutionTracker { get; set; }

    [DefaultValue(CompensationState.Active)]
    [DataMember(EmitDefaultValue = false)]
    internal CompensationState CompensationState { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string DisplayName { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal bool IsTokenValidInSecondaryRoot { get; set; }

    internal void RemoveBookmark(NativeActivityContext context, CompensationBookmarkName bookmarkName)
    {
        Bookmark bookmark = BookmarkTable[bookmarkName];

        if (bookmark != null)
        {
            context.RemoveBookmark(bookmark);
            BookmarkTable[bookmarkName] = null;
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Hosting;

[DataContract]
[Fx.Tag.XamlVisible(false)]
public sealed class BookmarkScopeInfo
{
    private Guid _id;
    private string _temporaryId;

    internal BookmarkScopeInfo(Guid id)
    {
        Id = id;
    }

    internal BookmarkScopeInfo(string temporaryId)
    {
        TemporaryId = temporaryId;
    }

    public bool IsInitialized => TemporaryId == null;

    public Guid Id
    {
        get => _id;
        private set => _id = value;
    }

    public string TemporaryId
    {
        get => _temporaryId;
        private set => _temporaryId = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "Id")]
    internal Guid SerializedId
    {
        get => Id;
        set => Id = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "TemporaryId")]
    internal string SerializedTemporaryId
    {
        get => TemporaryId;
        set => TemporaryId = value;
    }
}

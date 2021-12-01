// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class BookmarkResumptionRecord : TrackingRecord
{
    private Guid _bookmarkScope;
    private string _bookmarkName;
    private object _payload;
    private ActivityInfo _owner;

    internal BookmarkResumptionRecord(Guid instanceId, Bookmark bookmark, ActivityInstance ownerInstance, object payload)
        : base(instanceId)
    {
        if (bookmark.Scope != null)
        {
            BookmarkScope = bookmark.Scope.Id;
        }

        if (bookmark.IsNamed)
        {
            BookmarkName = bookmark.Name;
        }

        Owner = new ActivityInfo(ownerInstance);
        Payload = payload;
    }

    public BookmarkResumptionRecord(Guid instanceId, long recordNumber, Guid bookmarkScope, string bookmarkName, ActivityInfo owner)
        : base(instanceId, recordNumber)
    {
        BookmarkScope = bookmarkScope;
        BookmarkName = bookmarkName;
        Owner = owner ?? throw FxTrace.Exception.ArgumentNull(nameof(owner));
    }

    private BookmarkResumptionRecord(BookmarkResumptionRecord record)
        : base(record)
    {
        BookmarkScope = record.BookmarkScope;
        Owner = record.Owner;
        BookmarkName = record.BookmarkName;
        Payload = record.Payload;           
    }
        
    public Guid BookmarkScope
    {
        get => _bookmarkScope;
        private set => _bookmarkScope = value;
    }

    public string BookmarkName
    {
        get => _bookmarkName;
        private set => _bookmarkName = value;
    }

    public object Payload
    {
        get => _payload;
        internal set => _payload = value;
    }

    public ActivityInfo Owner
    {
        get => _owner;
        private set => _owner = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "BookmarkScope")]
    internal Guid SerializedBookmarkScope
    {
        get => BookmarkScope;
        set => BookmarkScope = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "BookmarkName")]
    internal string SerializedBookmarkName
    {
        get => BookmarkName;
        set => BookmarkName = value;
    }

    [DataMember(Name = "Payload")]
    internal object SerializedPayload
    {
        get => Payload;
        set => Payload = value;
    }

    [DataMember(Name = "Owner")]
    internal ActivityInfo SerializedOwner
    {
        get => Owner;
        set => Owner = value;
    }

    protected internal override TrackingRecord Clone() => new BookmarkResumptionRecord(this);

    public override string ToString()
        => string.Format(CultureInfo.CurrentCulture,
            "BookmarkResumptionRecord {{ {0}, BookmarkName = {1}, BookmarkScope = {2}, OwnerActivity {{ {3} }} }}",
            base.ToString(),
            BookmarkName ?? "<null>",
            BookmarkScope,
            Owner.ToString());
}

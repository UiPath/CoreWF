// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Tracking
{
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
                this.BookmarkScope = bookmark.Scope.Id;
            }

            if (bookmark.IsNamed)
            {
                this.BookmarkName = bookmark.Name;
            }

            this.Owner = new ActivityInfo(ownerInstance);
            this.Payload = payload;
        }

        public BookmarkResumptionRecord(Guid instanceId, long recordNumber, Guid bookmarkScope, string bookmarkName, ActivityInfo owner)
            : base(instanceId, recordNumber)
        {
            if (owner == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("owner");
            }

            this.BookmarkScope = bookmarkScope;
            this.BookmarkName = bookmarkName;
            this.Owner = owner;
        }

        private BookmarkResumptionRecord(BookmarkResumptionRecord record)
            : base(record)
        {
            this.BookmarkScope = record.BookmarkScope;
            this.Owner = record.Owner;
            this.BookmarkName = record.BookmarkName;
            this.Payload = record.Payload;
        }

        public Guid BookmarkScope
        {
            get
            {
                return _bookmarkScope;
            }
            private set
            {
                _bookmarkScope = value;
            }
        }

        public string BookmarkName
        {
            get
            {
                return _bookmarkName;
            }
            private set
            {
                _bookmarkName = value;
            }
        }

        public object Payload
        {
            get { return _payload; }
            internal set { _payload = value; }
        }

        public ActivityInfo Owner
        {
            get
            {
                return _owner;
            }
            private set
            {
                _owner = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "BookmarkScope")]
        internal Guid SerializedBookmarkScope
        {
            get { return this.BookmarkScope; }
            set { this.BookmarkScope = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "BookmarkName")]
        internal string SerializedBookmarkName
        {
            get { return this.BookmarkName; }
            set { this.BookmarkName = value; }
        }

        [DataMember(Name = "Payload")]
        internal object SerializedPayload
        {
            get { return this.Payload; }
            set { this.Payload = value; }
        }

        [DataMember(Name = "Owner")]
        internal ActivityInfo SerializedOwner
        {
            get { return this.Owner; }
            set { this.Owner = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new BookmarkResumptionRecord(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "BookmarkResumptionRecord {{ {0}, BookmarkName = {1}, BookmarkScope = {2}, OwnerActivity {{ {3} }} }}",
                base.ToString(),
                this.BookmarkName ?? "<null>",
                this.BookmarkScope,
                this.Owner.ToString());
        }
    }
}

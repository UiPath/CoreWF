// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Runtime.Serialization;

namespace CoreWf.Hosting
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class BookmarkScopeInfo
    {
        private Guid _id;
        private string _temporaryId;

        internal BookmarkScopeInfo(Guid id)
        {
            this.Id = id;
        }

        internal BookmarkScopeInfo(string temporaryId)
        {
            this.TemporaryId = temporaryId;
        }

        public bool IsInitialized
        {
            get
            {
                return this.TemporaryId == null;
            }
        }

        public Guid Id
        {
            get
            {
                return _id;
            }
            private set
            {
                _id = value;
            }
        }

        public string TemporaryId
        {
            get
            {
                return _temporaryId;
            }
            private set
            {
                _temporaryId = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "Id")]
        internal Guid SerializedId
        {
            get { return this.Id; }
            set { this.Id = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "TemporaryId")]
        internal string SerializedTemporaryId
        {
            get { return this.TemporaryId; }
            set { this.TemporaryId = value; }
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Runtime.Serialization;

namespace CoreWf.Hosting
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class BookmarkInfo
    {
        private string _bookmarkName;
        private BookmarkScopeInfo _scopeInfo;
        private string _ownerDisplayName;

        internal BookmarkInfo() { }

        internal BookmarkInfo(string bookmarkName, string ownerDisplayName, BookmarkScopeInfo scopeInfo)
        {
            this.BookmarkName = bookmarkName;
            this.OwnerDisplayName = ownerDisplayName;
            this.ScopeInfo = scopeInfo;
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

        public string OwnerDisplayName
        {
            get
            {
                return _ownerDisplayName;
            }
            private set
            {
                _ownerDisplayName = value;
            }
        }

        public BookmarkScopeInfo ScopeInfo
        {
            get
            {
                return _scopeInfo;
            }
            private set
            {
                _scopeInfo = value;
            }
        }

        [DataMember(Name = "BookmarkName")]
        internal string SerializedBookmarkName
        {
            get { return this.BookmarkName; }
            set { this.BookmarkName = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "OwnerDisplayName")]
        internal string SerializedOwnerDisplayName
        {
            get { return this.OwnerDisplayName; }
            set { this.OwnerDisplayName = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "ScopeInfo")]
        internal BookmarkScopeInfo SerializedScopeInfo
        {
            get { return this.ScopeInfo; }
            set { this.ScopeInfo = value; }
        }
    }
}

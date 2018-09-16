// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Hosting
{
    using System.Runtime.Serialization;
    using CoreWf.Runtime;

    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public sealed class BookmarkInfo
    {
        private string bookmarkName;
        private BookmarkScopeInfo scopeInfo;
        private string ownerDisplayName;

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
                return this.bookmarkName;
            }
            private set
            {
                this.bookmarkName = value;
            }
        }
        
        public string OwnerDisplayName
        {
            get
            {
                return this.ownerDisplayName;
            }
            private set
            {
                this.ownerDisplayName = value;
            }
        }
        
        public BookmarkScopeInfo ScopeInfo
        {
            get
            {
                return this.scopeInfo;
            }
            private set
            {
                this.scopeInfo = value;
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

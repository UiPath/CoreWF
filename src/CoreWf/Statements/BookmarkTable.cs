// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace CoreWf.Statements
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal class BookmarkTable
    {
        //Number of bookmarks used internally       
        private static int tableSize = Enum.GetValues(typeof(CompensationBookmarkName)).Length;
        private Bookmark[] bookmarkTable;

        public BookmarkTable()
        {
            this.bookmarkTable = new Bookmark[tableSize];
        }

        public Bookmark this[CompensationBookmarkName bookmarkName]
        {
            get 
            {
                return this.bookmarkTable[(int)bookmarkName];
            }
            set 
            {
                this.bookmarkTable[(int)bookmarkName] = value;
            }
        }

        [DataMember(Name = "bookmarkTable")]
        internal Bookmark[] SerializedBookmarkTable
        {
            get { return this.bookmarkTable; }
            set { this.bookmarkTable = value; }
        }
    }
}

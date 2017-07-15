// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace CoreWf.Statements
{
    [DataContract]
    internal class BookmarkTable
    {
        //Number of bookmarks used internally       
        private static int s_tableSize = Enum.GetValues(typeof(CompensationBookmarkName)).Length;

        private Bookmark[] _bookmarkTable;

        public BookmarkTable()
        {
            _bookmarkTable = new Bookmark[s_tableSize];
        }

        public Bookmark this[CompensationBookmarkName bookmarkName]
        {
            get
            {
                return _bookmarkTable[(int)bookmarkName];
            }
            set
            {
                _bookmarkTable[(int)bookmarkName] = value;
            }
        }

        [DataMember(Name = "bookmarkTable")]
        internal Bookmark[] SerializedBookmarkTable
        {
            get { return _bookmarkTable; }
            set { _bookmarkTable = value; }
        }
    }
}

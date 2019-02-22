// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    [DataContract]
    internal class BookmarkList : HybridCollection<Bookmark>
    {
        public BookmarkList()
            : base()
        {
        }

        internal bool Contains(Bookmark bookmark)
        {
            if (this.SingleItem != null)
            {
                if (this.SingleItem.Equals(bookmark))
                {
                    return true;
                }
            }
            else if (this.MultipleItems != null)
            {
                for (int i = 0; i < this.MultipleItems.Count; i++)
                {
                    if (bookmark.Equals(this.MultipleItems[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void TransferBookmarks(out Bookmark singleItem, out IList<Bookmark> multipleItems)
        {
            singleItem = base.SingleItem;
            multipleItems = base.MultipleItems;
        }

    }

}


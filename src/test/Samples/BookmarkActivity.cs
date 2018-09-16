// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Samples
{
    public class BookmarkActivity : NativeActivity
    {
        public InArgument<string> BookmarkName
        {
            get;
            set;
        }

        public InArgument<BookmarkOptions> Options
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddArgument(new RuntimeArgument("BookmarkName", typeof(string), ArgumentDirection.In));
            metadata.AddArgument(new RuntimeArgument("Options", typeof(BookmarkOptions), ArgumentDirection.In));
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(this.BookmarkName.Get(context), new BookmarkCallback(BookmarkCallback), this.Options.Get(context));
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
        {
            Console.WriteLine("Bookmark {0} resumed", bookmark.Name);
            string dataString = bookmarkData as string;
            if (dataString != null)
            {
                if (string.Compare(dataString, "stop", true) == 0)
                {
                    context.RemoveBookmark(bookmark.Name);
                }
            }
        }
    }
}

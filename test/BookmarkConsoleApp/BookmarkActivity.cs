// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;

namespace BookmarkConsoleApp
{
    public class BookmarkActivity : NativeActivity
    {
        private Variable<int> _iteration = new Variable<int>("iteration", 0);

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddImplementationVariable(_iteration);
        }

        protected override void Execute(NativeActivityContext context)
        {
            Guid wfInstanceId = context.WorkflowInstanceId;
            string bookmarkName = wfInstanceId.ToString();

            Console.WriteLine("BookmarkActivity.Execute - creating bookmark with name {0}", bookmarkName);
            context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
            Console.WriteLine("BookmarkActivity.Execute - successfull created bookmark with name {0}", bookmarkName);
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
            _iteration.Set(context, _iteration.Get(context) + 1);

            string dataString = bookmarkData as string;
            if (dataString != null)
            {
                Console.WriteLine("Interation: {0}; Bookmark {1} resumed with data: {2}", _iteration.Get(context).ToString(), bookmark.Name, dataString);
                if (string.Compare(dataString, "stop", true) == 0)
                {
                    context.RemoveBookmark(bookmark.Name);
                }
            }
            else
            {
                Console.WriteLine("Iteration: {0}; Bookmark {1} resumed with data that is not a string", _iteration.Get(context).ToString(), bookmark.Name);
            }
        }
    }
}

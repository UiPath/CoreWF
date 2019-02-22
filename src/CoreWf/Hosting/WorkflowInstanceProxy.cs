// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Hosting
{
    using System.Activities.Runtime;
    using System;

    public sealed class WorkflowInstanceProxy
    {
        private readonly WorkflowInstance instance;

        internal WorkflowInstanceProxy(WorkflowInstance instance)
        {
            this.instance = instance;
        }

        public Guid Id
        {
            get
            {
                return this.instance.Id;
            }
        }

        public Activity WorkflowDefinition
        {
            get
            {
                return this.instance.WorkflowDefinition;
            }
        }

        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        {
            return BeginResumeBookmark(bookmark, value, TimeSpan.MaxValue, callback, state);
        }

        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return this.instance.OnBeginResumeBookmark(bookmark, value, timeout, callback, state);
        }

        public BookmarkResumptionResult EndResumeBookmark(IAsyncResult result)
        {
            return this.instance.OnEndResumeBookmark(result);
        }
    }
}

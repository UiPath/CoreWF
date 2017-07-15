// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf.Hosting
{
    public sealed class WorkflowInstanceProxy
    {
        private WorkflowInstance _instance;

        internal WorkflowInstanceProxy(WorkflowInstance instance)
        {
            _instance = instance;
        }

        public Guid Id
        {
            get
            {
                return _instance.Id;
            }
        }

        public Activity WorkflowDefinition
        {
            get
            {
                return _instance.WorkflowDefinition;
            }
        }

        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, AsyncCallback callback, object state)
        {
            return BeginResumeBookmark(bookmark, value, TimeSpan.MaxValue, callback, state);
        }

        public IAsyncResult BeginResumeBookmark(Bookmark bookmark, object value, TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper.ThrowIfNegativeArgument(timeout);

            return _instance.OnBeginResumeBookmark(bookmark, value, timeout, callback, state);
        }

        public BookmarkResumptionResult EndResumeBookmark(IAsyncResult result)
        {
            return _instance.OnEndResumeBookmark(result);
        }
    }
}

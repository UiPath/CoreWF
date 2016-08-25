// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class BookmarkWorkItem : ActivityExecutionWorkItem
    {
        private BookmarkCallbackWrapper _callbackWrapper;
        private Bookmark _bookmark;
        private object _state;

        public BookmarkWorkItem(ActivityExecutor executor, bool isExternal, BookmarkCallbackWrapper callbackWrapper, Bookmark bookmark, object value)
            : this(callbackWrapper, bookmark, value)
        {
            if (isExternal)
            {
                executor.EnterNoPersist();
                this.ExitNoPersistRequired = true;
            }
        }

        // This ctor is only used by subclasses which make their own determination about no persist or not
        protected BookmarkWorkItem(BookmarkCallbackWrapper callbackWrapper, Bookmark bookmark, object value)
            : base(callbackWrapper.ActivityInstance)
        {
            _callbackWrapper = callbackWrapper;
            _bookmark = bookmark;
            _state = value;
        }

        [DataMember(Name = "callbackWrapper")]
        internal BookmarkCallbackWrapper SerializedCallbackWrapper
        {
            get { return _callbackWrapper; }
            set { _callbackWrapper = value; }
        }

        [DataMember(Name = "bookmark")]
        internal Bookmark SerializedBookmark
        {
            get { return _bookmark; }
            set { _bookmark = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "state")]
        internal object SerializedState
        {
            get { return _state; }
            set { _state = value; }
        }

        public override void TraceCompleted()
        {
            if (TD.CompleteBookmarkWorkItemIsEnabled())
            {
                TD.CompleteBookmarkWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
            }
        }

        public override void TraceScheduled()
        {
            if (TD.ScheduleBookmarkWorkItemIsEnabled())
            {
                TD.ScheduleBookmarkWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
            }
        }

        public override void TraceStarting()
        {
            if (TD.StartBookmarkWorkItemIsEnabled())
            {
                TD.StartBookmarkWorkItem(this.ActivityInstance.Activity.GetType().ToString(), this.ActivityInstance.Activity.DisplayName, this.ActivityInstance.Id, ActivityUtilities.GetTraceString(_bookmark), ActivityUtilities.GetTraceString(_bookmark.Scope));
            }
        }

        public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
        {
            NativeActivityContext nativeContext = executor.NativeActivityContextPool.Acquire();

            try
            {
                nativeContext.Initialize(this.ActivityInstance, executor, bookmarkManager);
                _callbackWrapper.Invoke(nativeContext, _bookmark, _state);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                this.ExceptionToPropagate = e;
            }
            finally
            {
                nativeContext.Dispose();
                executor.NativeActivityContextPool.Release(nativeContext);
            }

            return true;
        }
    }
}

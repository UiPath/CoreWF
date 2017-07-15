// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;

namespace CoreWf
{
    internal class AsyncOperationContext
    {
        private static AsyncCallback s_onResumeAsyncCodeActivityBookmark;

        private ActivityExecutor _executor;
        private ActivityInstance _owningActivityInstance;
        private bool _hasCanceled;
        private bool _hasCompleted;

        internal AsyncOperationContext(ActivityExecutor executor, ActivityInstance owningActivityInstance)
        {
            _executor = executor;
            _owningActivityInstance = owningActivityInstance;
        }

        internal bool IsStillActive
        {
            get
            {
                return !_hasCanceled && !_hasCompleted;
            }
        }

        public object UserState
        {
            get;
            set;
        }

        public bool HasCalledAsyncCodeActivityCancel
        {
            get;
            set;
        }

        public bool IsAborting
        {
            get;
            set;
        }

        private bool ShouldCancel()
        {
            return this.IsStillActive;
        }

        private bool ShouldComplete()
        {
            if (_hasCanceled)
            {
                return false;
            }

            if (_hasCompleted)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.OperationAlreadyCompleted));
            }

            return true;
        }

        internal void CancelOperation()
        {
            if (ShouldCancel())
            {
                _executor.CompleteOperation(_owningActivityInstance);
            }

            _hasCanceled = true;
        }

        public void CompleteOperation()
        {
            if (ShouldComplete())
            {
                _executor.CompleteOperation(_owningActivityInstance);

                _hasCompleted = true;
            }
        }

        // used by AsyncCodeActivity to efficiently complete a "true" async operation
        internal void CompleteAsyncCodeActivity(CompleteData completeData)
        {
            Fx.Assert(completeData != null, "caller must validate this is not null");

            if (!this.ShouldComplete())
            {
                // nothing to do here
                return;
            }

            if (s_onResumeAsyncCodeActivityBookmark == null)
            {
                s_onResumeAsyncCodeActivityBookmark = Fx.ThunkCallback(new AsyncCallback(OnResumeAsyncCodeActivityBookmark));
            }

            try
            {
                IAsyncResult result = _executor.BeginResumeBookmark(Bookmark.AsyncOperationCompletionBookmark,
                    completeData, TimeSpan.MaxValue, s_onResumeAsyncCodeActivityBookmark, _executor);
                if (result.CompletedSynchronously)
                {
                    _executor.EndResumeBookmark(result);
                }
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                _executor.AbortWorkflowInstance(e);
            }
        }

        private static void OnResumeAsyncCodeActivityBookmark(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            ActivityExecutor executor = (ActivityExecutor)result.AsyncState;

            try
            {
                executor.EndResumeBookmark(result);
            }
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                executor.AbortWorkflowInstance(e);
            }
        }

        internal abstract class CompleteData
        {
            private AsyncOperationContext _context;
            private bool _isCancel;

            protected CompleteData(AsyncOperationContext context, bool isCancel)
            {
                Fx.Assert(context != null, "Cannot have a null context.");
                _context = context;
                _isCancel = isCancel;
            }

            protected ActivityExecutor Executor
            {
                get
                {
                    return _context._executor;
                }
            }

            public ActivityInstance Instance
            {
                get
                {
                    return _context._owningActivityInstance;
                }
            }

            protected AsyncOperationContext AsyncContext
            {
                get
                {
                    return _context;
                }
            }

            // This method will throw if the complete/cancel is now invalid, it will return
            // true if the complete/cancel should proceed, and return false if the complete/cancel
            // should be ignored.
            private bool ShouldCallExecutor()
            {
                if (_isCancel)
                {
                    return _context.ShouldCancel();
                }
                else
                {
                    return _context.ShouldComplete();
                }
            }

            // This must be called from a workflow thread
            public void CompleteOperation()
            {
                if (ShouldCallExecutor())
                {
                    OnCallExecutor();

                    // We only update hasCompleted if we just did the completion work.
                    // Calling Cancel followed by Complete does not mean you've completed.
                    if (!_isCancel)
                    {
                        _context._hasCompleted = true;
                    }
                }

                // We update hasCanceled even if we skipped the actual work.
                // Calling Complete followed by Cancel does imply that you have canceled.
                if (_isCancel)
                {
                    _context._hasCanceled = true;
                }
            }

            protected abstract void OnCallExecutor();
        }
    }
}

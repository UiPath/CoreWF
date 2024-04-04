// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Diagnostics;

namespace LegacyTest.Test.Common.TestObjects.Utilities
{
    public abstract class AsyncResult : IAsyncResult
    {
        private static AsyncCallback s_asyncCompletionWrapperCallback;
        private readonly AsyncCallback _callback;
        private bool _completedSynchronously;
        private bool _endCalled;
        private Exception _exception;
        private bool _isCompleted;
        private ManualResetEvent _manualResetEvent;
        private AsyncCompletion _nextAsyncCompletion;
        private readonly object _state;
        private readonly object _thisLock;

#if DEBUG_EXPENSIVE
        StackTrace endStack;
        StackTrace completeStack;
#endif

        protected AsyncResult(AsyncCallback callback, object state)
        {
            _callback = callback;
            _state = state;
            _thisLock = new object();
        }

        public object AsyncState
        {
            get
            {
                return _state;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                if (_manualResetEvent != null)
                {
                    return _manualResetEvent;
                }

                lock (ThisLock)
                {
                    if (_manualResetEvent == null)
                    {
                        _manualResetEvent = new ManualResetEvent(_isCompleted);
                    }
                }

                return _manualResetEvent;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return _completedSynchronously;
            }
        }

        public bool HasCallback
        {
            get
            {
                return _callback != null;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
        }

        private object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }

        protected void Complete(bool completedSynchronously)
        {
            if (_isCompleted)
            {
                Debug.Assert(false, "AsyncResult complete called twice for the same operation.");
                throw new InvalidProgramException();
            }

#if DEBUG_EXPENSIVE
            if (completeStack == null)
                completeStack = new StackTrace();
#endif

            _completedSynchronously = completedSynchronously;

            if (completedSynchronously)
            {
                // If we completedSynchronously, then there's no chance that the manualResetEvent was created so
                // we don't need to worry about a race condition
                Debug.Assert(_manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
                _isCompleted = true;
            }
            else
            {
                lock (ThisLock)
                {
                    _isCompleted = true;
                    if (_manualResetEvent != null)
                    {
                        _manualResetEvent.Set();
                    }
                }
            }

            if (_callback != null)
            {
                try
                {
                    _callback(this);
                }
#pragma warning disable 1634
#pragma warning suppress 56500 // transferring exception to another thread
                catch (Exception e) // jasonv - how is the exception transferred to another thread?
                {
                    throw new InvalidProgramException("Async Callback threw an exception.", e);
                }
#pragma warning restore 1634
            }
        }

        protected void Complete(bool completedSynchronously, Exception exception)
        {
            _exception = exception;
            Complete(completedSynchronously);
        }

        private static void AsyncCompletionWrapperCallback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            AsyncResult thisPtr = (AsyncResult)result.AsyncState;
            AsyncCompletion callback = thisPtr.GetNextCompletion();

            bool completeSelf = false;
            Exception completionException = null;
            try
            {
                completeSelf = callback(result);
            }
            catch (Exception e) // jasonv - approved; wraps a callback, rethrows in End()
            {
                completeSelf = true;
                completionException = e;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completionException);
            }
        }

        protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
        {
            _nextAsyncCompletion = callback;
            if (AsyncResult.s_asyncCompletionWrapperCallback == null)
            {
                AsyncResult.s_asyncCompletionWrapperCallback = new AsyncCallback(AsyncCompletionWrapperCallback);
            }
            return AsyncResult.s_asyncCompletionWrapperCallback;
        }

        private AsyncCompletion GetNextCompletion()
        {
            AsyncCompletion result = _nextAsyncCompletion;
            Debug.Assert(result != null, "next async completion should be non-null");
            _nextAsyncCompletion = null;
            return result;
        }

        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncResult
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }


            if (!(result is TAsyncResult asyncResult))
            {
                throw new ArgumentException("result", "Invalid async result.");
            }

            if (asyncResult._endCalled)
            {
                throw new InvalidOperationException("End cannot be called twice on an AsyncResult.");
            }

#if DEBUG_EXPENSIVE
            if (asyncResult.endStack == null)
                asyncResult.endStack = new StackTrace();
#endif

            asyncResult._endCalled = true;

            if (!asyncResult._isCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }

            if (asyncResult._manualResetEvent != null)
            {
                asyncResult._manualResetEvent.Dispose();
            }

            if (asyncResult._exception != null)
            {
                throw asyncResult._exception;
            }

            return asyncResult;
        }

        // can be utilized by subclasses to write core completion code for both the sync and async paths
        // in one location, signalling chainable synchronous completion with the boolean result,
        // and leveraging PrepareAsyncCompletion for conversion to an AsyncCallback.
        // NOTE: requires that "this" is passed in as the state object to the asynchronous sub-call being used with a completion routine.
        protected delegate bool AsyncCompletion(IAsyncResult result);
    }
}

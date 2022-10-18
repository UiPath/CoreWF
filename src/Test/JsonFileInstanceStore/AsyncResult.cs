// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace JsonFileInstanceStore
{
    /// <summary>
    /// A generic base class for IAsyncResult implementations
    /// that wraps a ManualResetEvent.
    /// </summary>
    internal abstract class FileStoreAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback _callback;
        private readonly object _state;
        private bool _completedSynchronously;
        private bool _endCalled;
        private Exception _exception;
        private bool _isCompleted;
        private ManualResetEvent _manualResetEvent;
        private readonly object _thisLock;

        protected FileStoreAsyncResult(AsyncCallback callback, object state)
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

        // Call this version of complete when your asynchronous operation is complete.  This will update the state
        // of the operation and notify the callback.
        protected void Complete(bool completedSynchronously)
        {
            if (_isCompleted)
            {
                // It is incorrect to call Complete twice.
                // throw new InvalidOperationException(Resources.AsyncResultAlreadyCompleted);
                throw new InvalidOperationException("AsyncResultAlreadyCompleted");
            }

            _completedSynchronously = completedSynchronously;

            if (completedSynchronously)
            {
                // If we completedSynchronously, then there is no chance that the manualResetEvent was created so
                // we do not need to worry about a race condition.
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

            // If the callback throws, the callback implementation is incorrect
            if (_callback != null)
            {
                _callback(this);
            }
        }

        // Call this version of complete if you raise an exception during processing.  In addition to notifying
        // the callback, it will capture the exception and store it to be thrown during AsyncResult.End.
        protected void Complete(bool completedSynchronously, Exception exception)
        {
            _exception = exception;
            Complete(completedSynchronously);
        }

        // End should be called when the End function for the asynchronous operation is complete.  It
        // ensures the asynchronous operation is complete, and does some common validation.
        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : FileStoreAsyncResult
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }


            if (!(result is TAsyncResult asyncResult))
            {
                // throw new ArgumentException(Resources.InvalidAsyncResult);
                throw new ArgumentException("InvalidAsyncResult");
            }

            if (asyncResult._endCalled)
            {
                // throw new InvalidOperationException(Resources.AsyncResultAlreadyEnded);
                throw new InvalidOperationException("AsyncResultAlreadyEnded");
            }

            asyncResult._endCalled = true;

            if (!asyncResult._isCompleted)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
            }

            if (asyncResult._manualResetEvent != null)
            {
                // was manualResetEvent.Close();
                asyncResult._manualResetEvent.Dispose();
            }

            var exception = asyncResult._exception;
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            return asyncResult;
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities.Runtime;

// AsyncResult starts acquired; Complete releases.
[Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.ManualResetEvent, SupportsAsync = true, ReleaseMethod = "Complete")]
internal abstract class AsyncResult : IAsyncResult
{
    private static AsyncCallback s_asyncCompletionWrapperCallback;
    private readonly AsyncCallback _callback;
    private bool _completedSynchronously;
    private bool _endCalled;
    private Exception _exception;
    private bool _isCompleted;
    private AsyncCompletion _nextAsyncCompletion;
    private readonly object _state;
    private Action _beforePrepareAsyncCompletionAction;
    private Func<IAsyncResult, bool> _checkSyncValidationFunc;
    [Fx.Tag.SynchronizationObject]

    private ManualResetEvent _manualResetEvent;
    [Fx.Tag.SynchronizationObject(Blocking = false)]

    private readonly object _thisLock;

    //#if DEBUG
    //        StackTrace endStack;
    //        StackTrace completeStack;
    //        UncompletedAsyncResultMarker marker;
    //#endif

    protected AsyncResult(AsyncCallback callback, object state)
    {
        _callback = callback;
        _state = state;
        _thisLock = new object();

        //#if DEBUG
        //            this.marker = new UncompletedAsyncResultMarker(this);
        //#endif
    }

    public object AsyncState => _state;

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
                _manualResetEvent ??= new ManualResetEvent(_isCompleted);
            }

            return _manualResetEvent;
        }
    }

    public bool CompletedSynchronously => _completedSynchronously;

    public bool HasCallback => _callback != null;

    public bool IsCompleted => _isCompleted;

    // used in conjunction with PrepareAsyncCompletion to allow for finally blocks
    protected Action<AsyncResult, Exception> OnCompleting { get; set; }

    private object ThisLock => _thisLock;

    // subclasses like TraceAsyncResult can use this to wrap the callback functionality in a scope
    protected Action<AsyncCallback, IAsyncResult> VirtualCallback { get; set; }

    protected void Complete(bool completedSynchronously)
    {
        if (_isCompleted)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(SR.AsyncResultCompletedTwice(GetType())));
        }

        //#if DEBUG
        //            this.marker.AsyncResult = null;
        //            this.marker = null;
        //            if (!Fx.FastDebug && completeStack == null)
        //            {
        //                completeStack = new StackTrace();
        //            }
        //#endif

        _completedSynchronously = completedSynchronously;
        if (OnCompleting != null)
        {
            // Allow exception replacement, like a catch/throw pattern.
            try
            {
                OnCompleting(this, _exception);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }
                _exception = exception;
            }
        }

        if (completedSynchronously)
        {
            // If we completedSynchronously, then there's no chance that the manualResetEvent was created so
            // we don't need to worry about a race condition
            Fx.Assert(_manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
            _isCompleted = true;
        }
        else
        {
            lock (ThisLock)
            {
                _isCompleted = true;
                _manualResetEvent?.Set();
            }
        }

        if (_callback != null)
        {
            try
            {
                if (VirtualCallback != null)
                {
                    VirtualCallback(_callback, this);
                }
                else
                {
                    _callback(this);
                }
            }
#pragma warning disable 1634
#pragma warning suppress 56500 // transferring exception to another thread
            catch (Exception e)
            {
                if (Fx.IsFatal(e))
                {
                    throw;
                }

                throw Fx.Exception.AsError(new CallbackException(SR.AsyncCallbackThrewException, e));
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
        if (result == null)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(SR.InvalidNullAsyncResult));
        }
        if (result.CompletedSynchronously)
        {
            return;
        }

        AsyncResult thisPtr = (AsyncResult)result.AsyncState;
        if (!thisPtr.OnContinueAsyncCompletion(result))
        {
            return;
        }

        AsyncCompletion callback = thisPtr.GetNextCompletion();
        if (callback == null)
        {
            ThrowInvalidAsyncResult(result);
        }

        bool completeSelf = false;
        Exception completionException = null;
        try
        {
            completeSelf = callback(result);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }
            completeSelf = true;
            completionException = e;
        }

        if (completeSelf)
        {
            thisPtr.Complete(false, completionException);
        }
    }

    // Note: this should be only derived by the TransactedAsyncResult
    protected virtual bool OnContinueAsyncCompletion(IAsyncResult result) => true;

    // Note: this should be used only by the TransactedAsyncResult
    protected void SetBeforePrepareAsyncCompletionAction(Action beforePrepareAsyncCompletionAction) => _beforePrepareAsyncCompletionAction = beforePrepareAsyncCompletionAction;

    // Note: this should be used only by the TransactedAsyncResult
    protected void SetCheckSyncValidationFunc(Func<IAsyncResult, bool> checkSyncValidationFunc) => _checkSyncValidationFunc = checkSyncValidationFunc;

    protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
    {
        if (_beforePrepareAsyncCompletionAction != null)
        {
            _beforePrepareAsyncCompletionAction();
        }

        _nextAsyncCompletion = callback;
        s_asyncCompletionWrapperCallback ??= Fx.ThunkCallback(new AsyncCallback(AsyncCompletionWrapperCallback));
        return s_asyncCompletionWrapperCallback;
    }

    protected bool CheckSyncContinue(IAsyncResult result) => TryContinueHelper(result, out AsyncCompletion dummy);

    protected bool SyncContinue(IAsyncResult result)
    {
        if (TryContinueHelper(result, out AsyncCompletion callback))
        {
            return callback(result);
        }
        else
        {
            return false;
        }
    }

    private bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
    {
        if (result == null)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(SR.InvalidNullAsyncResult));
        }

        callback = null;
        if (_checkSyncValidationFunc != null)
        {
            if (!_checkSyncValidationFunc(result))
            {
                return false;
            }
        }
        else if (!result.CompletedSynchronously)
        {
            return false;
        }

        callback = GetNextCompletion();
        if (callback == null)
        {
            ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
        }
        return true;
    }

    private AsyncCompletion GetNextCompletion()
    {
        AsyncCompletion result = _nextAsyncCompletion;
        _nextAsyncCompletion = null;
        return result;
    }

    protected static void ThrowInvalidAsyncResult(IAsyncResult result)
        => throw Fx.Exception.AsError(new InvalidOperationException(SR.InvalidAsyncResultImplementation(result.GetType())));

    protected static void ThrowInvalidAsyncResult(string debugText)
    {
        string message = SR.InvalidAsyncResultImplementationGeneric;
        if (debugText != null)
        {
#if DEBUG
            message += " " + debugText;
#endif
        }
        throw Fx.Exception.AsError(new InvalidOperationException(message));
    }

    [Fx.Tag.Blocking(Conditional = "!asyncResult.isCompleted")]
    public static TAsyncResult End<TAsyncResult>(IAsyncResult result)
        where TAsyncResult : AsyncResult
    {
        if (result == null)
        {
            throw Fx.Exception.ArgumentNull(nameof(result));
        }

        if (result is not TAsyncResult asyncResult)
        {
            throw Fx.Exception.Argument(nameof(result), SR.InvalidAsyncResult);
        }

        if (asyncResult._endCalled)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(SR.AsyncResultAlreadyEnded));
        }

        //#if DEBUG
        //            if (!Fx.FastDebug && asyncResult.endStack == null)
        //            {
        //                asyncResult.endStack = new StackTrace();
        //            }
        //#endif

        asyncResult._endCalled = true;

        if (!asyncResult._isCompleted)
        {
            asyncResult.AsyncWaitHandle.WaitOne();
        }

        //asyncResult.manualResetEvent?.Close();
        asyncResult._manualResetEvent?.Dispose();

        if (asyncResult._exception != null)
        {
            throw Fx.Exception.AsError(asyncResult._exception);
        }

        return asyncResult;
    }

    // can be utilized by subclasses to write core completion code for both the sync and async paths
    // in one location, signalling chainable synchronous completion with the boolean result,
    // and leveraging PrepareAsyncCompletion for conversion to an AsyncCallback.
    // NOTE: requires that "this" is passed in as the state object to the asynchronous sub-call being used with a completion routine.
    protected delegate bool AsyncCompletion(IAsyncResult result);

#if DEBUG
    private class UncompletedAsyncResultMarker
    {
        public UncompletedAsyncResultMarker(AsyncResult result)
        {
            AsyncResult = result;
        }

        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode,
        //Justification = "Debug-only facility")]
        public AsyncResult AsyncResult { get; set; }
    }
#endif
}

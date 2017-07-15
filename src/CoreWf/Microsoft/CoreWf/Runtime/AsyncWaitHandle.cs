// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security;
using CoreWf.Internals;
using System.Threading;

namespace CoreWf.Runtime
{
    [Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.MonitorWait, SupportsAsync = true, ReleaseMethod = "Set")]
    internal class AsyncWaitHandle
    {
        private static Action<object> s_timerCompleteCallback;

        private List<AsyncWaiter> _asyncWaiters;
        private bool _isSignaled;
        private EventResetMode _resetMode;
        [Fx.Tag.SynchronizationObject(Kind = Fx.Tag.SynchronizationKind.MonitorWait)]

        private object _syncObject;

        private int _syncWaiterCount;

        public AsyncWaitHandle()
            : this(EventResetMode.AutoReset)
        {
        }

        public AsyncWaitHandle(EventResetMode resetMode)
        {
            _resetMode = resetMode;
            _syncObject = new object();
        }

        public bool WaitAsync(Action<object, TimeoutException> callback, object state, TimeSpan timeout)
        {
            if (!_isSignaled || (_isSignaled && _resetMode == EventResetMode.AutoReset))
            {
                lock (_syncObject)
                {
                    if (_isSignaled && _resetMode == EventResetMode.AutoReset)
                    {
                        _isSignaled = false;
                    }
                    else if (!_isSignaled)
                    {
                        AsyncWaiter waiter = new AsyncWaiter(this, callback, state);

                        if (_asyncWaiters == null)
                        {
                            _asyncWaiters = new List<AsyncWaiter>();
                        }

                        _asyncWaiters.Add(waiter);

                        if (timeout != TimeSpan.MaxValue)
                        {
                            if (s_timerCompleteCallback == null)
                            {
                                s_timerCompleteCallback = new Action<object>(OnTimerComplete);
                            }
                            waiter.SetTimer(s_timerCompleteCallback, waiter, timeout);
                        }
                        return false;
                    }
                }
            }

            return true;
        }

        private static void OnTimerComplete(object state)
        {
            AsyncWaiter waiter = (AsyncWaiter)state;
            AsyncWaitHandle thisPtr = waiter.Parent;
            bool callWaiter = false;

            lock (thisPtr._syncObject)
            {
                // If still in the waiting list (that means it hasn't been signaled)
                if (thisPtr._asyncWaiters != null && thisPtr._asyncWaiters.Remove(waiter))
                {
                    waiter.TimedOut = true;
                    callWaiter = true;
                }
            }

            waiter.CancelTimer();

            if (callWaiter)
            {
                waiter.Call();
            }
        }

        [Fx.Tag.Blocking]
        public bool Wait(TimeSpan timeout)
        {
            if (!_isSignaled || (_isSignaled && _resetMode == EventResetMode.AutoReset))
            {
                lock (_syncObject)
                {
                    if (_isSignaled && _resetMode == EventResetMode.AutoReset)
                    {
                        _isSignaled = false;
                    }
                    else if (!_isSignaled)
                    {
                        bool decrementRequired = false;

                        try
                        {
                            try
                            {
                            }
                            finally
                            {
                                _syncWaiterCount++;
                                decrementRequired = true;
                            }

                            if (timeout == TimeSpan.MaxValue)
                            {
                                if (!Monitor.Wait(_syncObject, Timeout.Infinite))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (!Monitor.Wait(_syncObject, timeout))
                                {
                                    return false;
                                }
                            }
                        }
                        finally
                        {
                            if (decrementRequired)
                            {
                                _syncWaiterCount--;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public void Set()
        {
            List<AsyncWaiter> toCallList = null;
            AsyncWaiter toCall = null;

            if (!_isSignaled)
            {
                lock (_syncObject)
                {
                    if (!_isSignaled)
                    {
                        if (_resetMode == EventResetMode.ManualReset)
                        {
                            _isSignaled = true;
                            Monitor.PulseAll(_syncObject);
                            toCallList = _asyncWaiters;
                            _asyncWaiters = null;
                        }
                        else
                        {
                            if (_syncWaiterCount > 0)
                            {
                                Monitor.Pulse(_syncObject);
                            }
                            else if (_asyncWaiters != null && _asyncWaiters.Count > 0)
                            {
                                toCall = _asyncWaiters[0];
                                _asyncWaiters.RemoveAt(0);
                            }
                            else
                            {
                                _isSignaled = true;
                            }
                        }
                    }
                }
            }

            if (toCallList != null)
            {
                foreach (AsyncWaiter waiter in toCallList)
                {
                    waiter.CancelTimer();
                    waiter.Call();
                }
            }

            if (toCall != null)
            {
                toCall.CancelTimer();
                toCall.Call();
            }
        }

        public void Reset()
        {
            // Doesn't matter if this changes during processing of another method
            _isSignaled = false;
        }

        private class AsyncWaiter : ActionItem
        {
            [Fx.Tag.SecurityNote(Critical = "Store the delegate to be invoked")]
            [SecurityCritical]
            private Action<object, TimeoutException> _callback;
            [Fx.Tag.SecurityNote(Critical = "Stores the state object to be passed to the callback")]
            [SecurityCritical]
            private object _state;
            //IOThreadTimer timer;
            private DelayTimer _timer;
            private TimeSpan _originalTimeout;

            [Fx.Tag.SecurityNote(Critical = "Access critical members", Safe = "Doesn't leak information")]
            [SecuritySafeCritical]
            public AsyncWaiter(AsyncWaitHandle parent, Action<object, TimeoutException> callback, object state)
            {
                this.Parent = parent;
                _callback = callback;
                _state = state;
            }

            public AsyncWaitHandle Parent
            {
                get;
                private set;
            }

            public bool TimedOut
            {
                get;
                set;
            }

            [Fx.Tag.SecurityNote(Critical = "Calls into critical method Schedule", Safe = "Invokes the given delegate under the current context")]
            [SecuritySafeCritical]
            public void Call()
            {
                Schedule();
            }

            [Fx.Tag.SecurityNote(Critical = "Overriding an inherited critical method, access critical members")]
            [SecurityCritical]
            protected override void Invoke()
            {
                _callback(_state,
                    this.TimedOut ? new TimeoutException(InternalSR.TimeoutOnOperation(_originalTimeout)) : null);
            }

            public void SetTimer(Action<object> callback, object state, TimeSpan timeout)
            {
                if (_timer != null)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(InternalSR.MustCancelOldTimer));
                }

                _originalTimeout = timeout;
                //this.timer = new IOThreadTimer(callback, state, false);

                //this.timer.Set(timeout);
                _timer = new DelayTimer(callback, state, timeout);
            }

            public void CancelTimer()
            {
                if (_timer != null)
                {
                    _timer.Cancel();
                    _timer = null;
                }
            }
        }
    }
}

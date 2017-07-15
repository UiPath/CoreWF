// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace CoreWf.Runtime
{
    internal abstract class ActionItem
    {
        private bool _isScheduled;
        private bool _lowPriority;

        protected ActionItem()
        {
        }

        public bool LowPriority
        {
            get
            {
                return _lowPriority;
            }
            protected set
            {
                _lowPriority = value;
            }
        }

        public static void Schedule(Action<object> callback, object state)
        {
            //Contract.Assert(callback != null, "Cannot schedule a null callback");
            Task.Factory.StartNew(callback, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [Fx.Tag.SecurityNote(Critical = "Called after applying the user context on the stack or (potentially) " +
            "without any user context on the stack")]
        [SecurityCritical]
        protected abstract void Invoke();

        [Fx.Tag.SecurityNote(Critical = "Access critical field context and critical property " +
            "CallbackHelper.InvokeWithContextCallback, calls into critical method " +
            "PartialTrustHelpers.CaptureSecurityContextNoIdentityFlow, calls into critical method ScheduleCallback; " +
            "since the invoked method and the capturing of the security contex are de-coupled, can't " +
            "be treated as safe")]
        [SecurityCritical]
        protected void Schedule()
        {
            if (_isScheduled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(InternalSR.ActionItemIsAlreadyScheduled));
            }

            _isScheduled = true;
            ScheduleCallback(CallbackHelper.InvokeCallbackAction);
        }
        [Fx.Tag.SecurityNote(Critical = "Calls into critical static method ScheduleCallback")]
        [SecurityCritical]

        private void ScheduleCallback(Action<object> callback)
        {
            Fx.Assert(callback != null, "Cannot schedule a null callback");
            Task.Factory.StartNew(callback, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        [SecurityCritical]
        internal static class CallbackHelper
        {
            [Fx.Tag.SecurityNote(Critical = "Stores a delegate to a critical method")]
            private static Action<object> s_invokeCallback;

            public static Action<object> InvokeCallbackAction
            {
                get
                {
                    if (s_invokeCallback == null)
                    {
                        s_invokeCallback = new Action<object>(InvokeCallback);
                    }
                    return s_invokeCallback;
                }
            }

            [Fx.Tag.SecurityNote(Critical = "Called by the scheduler without any user context on the stack")]
            private static void InvokeCallback(object state)
            {
                ((ActionItem)state).Invoke();
                ((ActionItem)state)._isScheduled = false;
            }
        }
    }
}

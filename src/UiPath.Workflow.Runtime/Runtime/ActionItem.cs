// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Activities.Runtime;

internal abstract class ActionItem
{
    private bool _isScheduled;
    private bool _lowPriority;

    protected ActionItem() { }

    public bool LowPriority
    {
        get => _lowPriority;
        protected set => _lowPriority = value;
    }

    public static void Schedule(Action<object> callback, object state) =>
        //Contract.Assert(callback != null, "Cannot schedule a null callback");
        Task.Factory.StartNew(callback, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

    protected abstract void Invoke();

    protected void Schedule()
    {
        if (_isScheduled)
        {
            throw Fx.Exception.AsError(new InvalidOperationException(SR.ActionItemIsAlreadyScheduled));
        }

        _isScheduled = true;
        ScheduleCallback(CallbackHelper.InvokeCallbackAction);
    }

    private void ScheduleCallback(Action<object> callback)
    {
        Fx.Assert(callback != null, "Cannot schedule a null callback");
        Task.Factory.StartNew(callback, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    internal static class CallbackHelper
    {
        private static Action<object> s_invokeCallback;

        public static Action<object> InvokeCallbackAction
        {
            get
            {
                s_invokeCallback ??= new Action<object>(InvokeCallback);
                return s_invokeCallback;
            }
        }

        /// <remarks>
        ///     Called by the scheduler without any user context on the stack
        /// </remarks>
        private static void InvokeCallback(object state)
        {
            ((ActionItem)state).Invoke();
            ((ActionItem)state)._isScheduled = false;
        }
    }
}

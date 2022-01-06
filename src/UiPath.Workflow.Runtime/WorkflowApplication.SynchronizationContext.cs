// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities;
using Internals;
using Runtime;

public partial class WorkflowApplication
{
    // this class is not a general purpose SyncContext and is only meant to work for workflow scenarios, where the scheduler ensures 
    // at most one work item pending. The scheduler ensures that Invoke must run before Post is called on a different thread.
    private class PumpBasedSynchronizationContext : SynchronizationContext
    {
        // The waitObject is cached per thread so that we can avoid the cost of creating
        // events for multiple synchronous invokes.
        [ThreadStatic]
        private static AutoResetEvent waitObject;
        private AutoResetEvent _queueWaiter;
        private WorkItem _currentWorkItem;
        private readonly object _thisLock;
        private TimeoutHelper _timeoutHelper;

        public PumpBasedSynchronizationContext(TimeSpan timeout)
        {
            _timeoutHelper = new TimeoutHelper(timeout);
            _thisLock = new object();
        }

        private bool IsInvokeCompleted { get; set; }

        public void DoPump()
        {
            Fx.Assert(_currentWorkItem != null, "the work item cannot be null");
            WorkItem workItem;

            lock (_thisLock)
            {
                if (waitObject == null)
                {
                    waitObject = new AutoResetEvent(false);
                }
                _queueWaiter = waitObject;

                workItem = _currentWorkItem;
                _currentWorkItem = null;
                workItem.Invoke();
            }

            Fx.Assert(_queueWaiter != null, "queue waiter cannot be null");

            while (WaitForNextItem())
            {
                Fx.Assert(_currentWorkItem != null, "the work item cannot be null");
                workItem = _currentWorkItem;
                _currentWorkItem = null;
                workItem.Invoke();
            }
        }

        public override void Post(SendOrPostCallback d, object state) => ScheduleWorkItem(new WorkItem(d, state));

        public override void Send(SendOrPostCallback d, object state) => throw FxTrace.Exception.AsError(new NotSupportedException(SR.SendNotSupported));

        // Since tracking can go async this may or may not be called directly
        // under a call to workItem.Invoke.  Also, the scheduler may call
        // OnNotifyPaused or OnNotifyUnhandledException from any random thread
        // if runtime goes async (post-work item tracking, AsyncCodeActivity).
        public void OnInvokeCompleted()
        {
            Fx.AssertAndFailFast(_currentWorkItem == null, "There can be no pending work items when complete");

            IsInvokeCompleted = true;

            lock (_thisLock)
            {
                if (_queueWaiter != null)
                {
                    // Since we don't know which thread this is being called
                    // from we just set the waiter directly rather than
                    // doing our SetWaiter cleanup.
                    _queueWaiter.Set();
                }
            }
        }

        private void ScheduleWorkItem(WorkItem item)
        {
            lock (_thisLock)
            {
                Fx.AssertAndFailFast(_currentWorkItem == null, "There cannot be more than 1 work item at a given time");
                _currentWorkItem = item;
                if (_queueWaiter != null)
                {
                    // Since we don't know which thread this is being called
                    // from we just set the waiter directly rather than
                    // doing our SetWaiter cleanup.
                    _queueWaiter.Set();
                }
            }
        }

        private static bool WaitOne(AutoResetEvent waiter, TimeSpan timeout)
        {
            bool success = false;
            try
            {
                bool result = TimeoutHelper.WaitOne(waiter, timeout);
                // if the wait timed out, reset the thread static
                success = result;
                return result;
            }
            finally
            {
                if (!success)
                {
                    waitObject = null;
                }
            }
        }

        private bool WaitForNextItem()
        {
            if (!WaitOne(_queueWaiter, _timeoutHelper.RemainingTime()))
            {
                throw FxTrace.Exception.AsError(new TimeoutException(SR.TimeoutOnOperation(_timeoutHelper.OriginalTimeout)));
            }

            // We need to check this after the wait as well in 
            // case the notification came in asynchronously
            return !IsInvokeCompleted;
        }

        private class WorkItem
        {
            private readonly SendOrPostCallback _callback;
            private readonly object _state;

            public WorkItem(SendOrPostCallback callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            public void Invoke() => _callback(_state);
        }
    }

    internal class SynchronousSynchronizationContext : SynchronizationContext
    {
        private static SynchronousSynchronizationContext value;

        private SynchronousSynchronizationContext() { }

        public static SynchronousSynchronizationContext Value
        {
            get
            {
                value ??= new SynchronousSynchronizationContext();
                return value;
            }
        }

        public override void Post(SendOrPostCallback d, object state) => d(state);

        public override void Send(SendOrPostCallback d, object state) => d(state);
    }
}

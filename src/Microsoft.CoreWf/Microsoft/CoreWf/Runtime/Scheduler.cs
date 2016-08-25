// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract(Name = XD.Runtime.Scheduler, Namespace = XD.Runtime.Namespace)]
    internal class Scheduler
    {
        private static ContinueAction s_continueAction = new ContinueAction();
        private static YieldSilentlyAction s_yieldSilentlyAction = new YieldSilentlyAction();
        private static AbortAction s_abortAction = new AbortAction();

        private WorkItem _firstWorkItem;

        private static SendOrPostCallback s_onScheduledWorkCallback = Fx.ThunkCallback(new SendOrPostCallback(OnScheduledWork));

        private SynchronizationContext _synchronizationContext;

        private bool _isPausing;
        private bool _isRunning;

        private bool _resumeTraceRequired;

        private Callbacks _callbacks;

        private Quack<WorkItem> _workItemQueue;

        public Scheduler(Callbacks callbacks)
        {
            this.Initialize(callbacks);
        }

        public static RequestedAction Continue
        {
            get
            {
                return s_continueAction;
            }
        }

        public static RequestedAction YieldSilently
        {
            get
            {
                return s_yieldSilentlyAction;
            }
        }

        public static RequestedAction Abort
        {
            get
            {
                return s_abortAction;
            }
        }

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }

        public bool IsIdle
        {
            get
            {
                return _firstWorkItem == null;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "firstWorkItem")]
        internal WorkItem SerializedFirstWorkItem
        {
            get { return _firstWorkItem; }
            set { _firstWorkItem = value; }
        }

        [DataMember(EmitDefaultValue = false)]
        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode)]
        internal WorkItem[] SerializedWorkItemQueue
        {
            get
            {
                if (_workItemQueue != null && _workItemQueue.Count > 0)
                {
                    return _workItemQueue.ToArray();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Fx.Assert(value != null, "EmitDefaultValue is false so we should never get null.");

                // this.firstWorkItem is serialized out separately, so don't use ScheduleWork() here
                _workItemQueue = new Quack<WorkItem>(value);
            }
        }

        public void FillInstanceMap(ActivityInstanceMap instanceMap)
        {
            if (_firstWorkItem != null)
            {
                ActivityInstanceMap.IActivityReference activityReference = _firstWorkItem as ActivityInstanceMap.IActivityReference;
                if (activityReference != null)
                {
                    instanceMap.AddEntry(activityReference, true);
                }

                if (_workItemQueue != null && _workItemQueue.Count > 0)
                {
                    for (int i = 0; i < _workItemQueue.Count; i++)
                    {
                        activityReference = _workItemQueue[i] as ActivityInstanceMap.IActivityReference;
                        if (activityReference != null)
                        {
                            instanceMap.AddEntry(activityReference, true);
                        }
                    }
                }
            }
        }

        public static RequestedAction CreateNotifyUnhandledExceptionAction(Exception exception, ActivityInstance sourceInstance)
        {
            return new NotifyUnhandledExceptionAction(exception, sourceInstance);
        }

        public void ClearAllWorkItems(ActivityExecutor executor)
        {
            if (_firstWorkItem != null)
            {
                _firstWorkItem.Release(executor);
                _firstWorkItem = null;

                if (_workItemQueue != null)
                {
                    while (_workItemQueue.Count > 0)
                    {
                        WorkItem item = _workItemQueue.Dequeue();
                        item.Release(executor);
                    }
                }
            }

            Fx.Assert(_workItemQueue == null || _workItemQueue.Count == 0, "We either didn't have a first work item and therefore don't have anything in the queue, or we drained the queue.");

            // For consistency we set this to null even if it is empty
            _workItemQueue = null;
        }

        public void OnDeserialized(Callbacks callbacks)
        {
            Initialize(callbacks);
            Fx.Assert(_firstWorkItem != null || _workItemQueue == null, "cannot have items in the queue unless we also have a firstWorkItem set");
        }

        // This method should only be called when we relinquished the thread but did not
        // complete the operation (silent yield is the current example)
        public void InternalResume(RequestedAction action)
        {
            Fx.Assert(_isRunning, "We should still be processing work - we just don't have a thread");

            bool isTracingEnabled = Microsoft.CoreWf.Internals.FxTrace.ShouldTraceInformation;
            bool notifiedCompletion = false;
            bool isInstanceComplete = false;

            if (_callbacks.IsAbortPending)
            {
                _isPausing = false;
                _isRunning = false;

                this.NotifyWorkCompletion();
                notifiedCompletion = true;

                if (isTracingEnabled)
                {
                    isInstanceComplete = _callbacks.IsCompleted;
                }

                // After calling SchedulerIdle we no longer have the lock.  That means
                // that any subsequent processing in this method won't have the single
                // threaded guarantee.
                _callbacks.SchedulerIdle();
            }
            else if (object.ReferenceEquals(action, s_continueAction))
            {
                ScheduleWork(false);
            }
            else
            {
                Fx.Assert(action is NotifyUnhandledExceptionAction, "This is the only other choice because we should never have YieldSilently here");

                NotifyUnhandledExceptionAction notifyAction = (NotifyUnhandledExceptionAction)action;

                // We only set isRunning back to false so that the host doesn't
                // have to treat this like a pause notification.  As an example,
                // a host could turn around and call run again in response to
                // UnhandledException without having to go through its operation
                // dispatch loop first (or request pause again).  If we reset
                // isPausing here then any outstanding operations wouldn't get
                // signaled with that type of host.
                _isRunning = false;

                this.NotifyWorkCompletion();
                notifiedCompletion = true;

                if (isTracingEnabled)
                {
                    isInstanceComplete = _callbacks.IsCompleted;
                }

                _callbacks.NotifyUnhandledException(notifyAction.Exception, notifyAction.Source);
            }

            if (isTracingEnabled)
            {
                if (notifiedCompletion)
                {
                    Guid oldActivityId = Guid.Empty;
                    bool resetId = false;

                    if (isInstanceComplete)
                    {
                        if (TD.WorkflowActivityStopIsEnabled())
                        {
                            TD.SetActivityId(_callbacks.WorkflowInstanceId, out oldActivityId);
                            resetId = true;

                            TD.WorkflowActivityStop(_callbacks.WorkflowInstanceId);
                        }
                    }
                    else
                    {
                        if (TD.WorkflowActivitySuspendIsEnabled())
                        {
                            TD.SetActivityId(_callbacks.WorkflowInstanceId, out oldActivityId);
                            resetId = true;

                            TD.WorkflowActivitySuspend(_callbacks.WorkflowInstanceId);
                        }
                    }

                    if (resetId)
                    {
                        TD.CurrentActivityId = oldActivityId;
                    }
                }
            }
        }

        // called from ctor and OnDeserialized intialization paths
        private void Initialize(Callbacks callbacks)
        {
            _callbacks = callbacks;
        }

        public void Open(SynchronizationContext synchronizationContext)
        {
            Fx.Assert(_synchronizationContext == null, "can only open when in the created state");
            if (synchronizationContext != null)
            {
                _synchronizationContext = synchronizationContext;
            }
            else
            {
                _synchronizationContext = SynchronizationContextHelper.GetDefaultSynchronizationContext();
            }
        }

        internal void Open(Scheduler oldScheduler)
        {
            Fx.Assert(_synchronizationContext == null, "can only open when in the created state");
            _synchronizationContext = SynchronizationContextHelper.CloneSynchronizationContext(oldScheduler._synchronizationContext);
        }

        private void ScheduleWork(bool notifyStart)
        {
            if (notifyStart)
            {
                _synchronizationContext.OperationStarted();
                _resumeTraceRequired = true;
            }
            else
            {
                _resumeTraceRequired = false;
            }
            _synchronizationContext.Post(Scheduler.s_onScheduledWorkCallback, this);
        }

        private void NotifyWorkCompletion()
        {
            _synchronizationContext.OperationCompleted();
        }

        // signal the scheduler to stop processing work. If we are processing work
        // then we will catch this signal at our next iteration. Pause process completes
        // when idle is signalled. Can be called while we're processing work since
        // the worst thing that could happen in a race is that we pause one extra work item later
        public void Pause()
        {
            _isPausing = true;
        }

        public void MarkRunning()
        {
            _isRunning = true;
        }

        public void Resume()
        {
            Fx.Assert(_isRunning, "This should only be called after we've been set to process work.");

            if (this.IsIdle || _isPausing || _callbacks.IsAbortPending)
            {
                _isPausing = false;
                _isRunning = false;
                _callbacks.SchedulerIdle();
            }
            else
            {
                ScheduleWork(true);
            }
        }

        public void PushWork(WorkItem workItem)
        {
            if (_firstWorkItem == null)
            {
                _firstWorkItem = workItem;
            }
            else
            {
                if (_workItemQueue == null)
                {
                    _workItemQueue = new Quack<WorkItem>();
                }

                _workItemQueue.PushFront(_firstWorkItem);
                _firstWorkItem = workItem;
            }

            // To avoid the virt call on EVERY work item we check
            // the Verbose flag.  All of our Schedule traces are
            // verbose.
            if (Microsoft.CoreWf.Internals.FxTrace.ShouldTraceVerboseToTraceSource)
            {
                workItem.TraceScheduled();
            }
        }

        public void EnqueueWork(WorkItem workItem)
        {
            if (_firstWorkItem == null)
            {
                _firstWorkItem = workItem;
            }
            else
            {
                if (_workItemQueue == null)
                {
                    _workItemQueue = new Quack<WorkItem>();
                }

                _workItemQueue.Enqueue(workItem);
            }

            if (Microsoft.CoreWf.Internals.FxTrace.ShouldTraceVerboseToTraceSource)
            {
                workItem.TraceScheduled();
            }
        }

        private static void OnScheduledWork(object state)
        {
            Scheduler thisPtr = (Scheduler)state;

            // We snapshot these values here so that we can
            // use them after calling OnSchedulerIdle.
            bool isTracingEnabled = TD.IsEnd2EndActivityTracingEnabled() && TD.ShouldTraceToTraceSource(EventLevel.Informational);
            Guid oldActivityId = Guid.Empty;
            Guid workflowInstanceId = Guid.Empty;

            if (isTracingEnabled)
            {
                oldActivityId = TD.CurrentActivityId;
                workflowInstanceId = thisPtr._callbacks.WorkflowInstanceId;
                TD.TraceTransfer(workflowInstanceId);

                if (thisPtr._resumeTraceRequired)
                {
                    if (TD.WorkflowActivityResumeIsEnabled())
                    {
                        TD.WorkflowActivityResume(workflowInstanceId);
                    }
                }
            }

            thisPtr._callbacks.ThreadAcquired();

            RequestedAction nextAction = s_continueAction;
            bool idleOrPaused = false;

            while (object.ReferenceEquals(nextAction, s_continueAction))
            {
                if (thisPtr.IsIdle || thisPtr._isPausing)
                {
                    idleOrPaused = true;
                    break;
                }

                // cycle through (queue->thisPtr.firstWorkItem->currentWorkItem)
                WorkItem currentWorkItem = thisPtr._firstWorkItem;

                // promote an item out of our work queue if necessary
                if (thisPtr._workItemQueue != null && thisPtr._workItemQueue.Count > 0)
                {
                    thisPtr._firstWorkItem = thisPtr._workItemQueue.Dequeue();
                }
                else
                {
                    thisPtr._firstWorkItem = null;
                }

                if (TD.ExecuteWorkItemStartIsEnabled())
                {
                    TD.ExecuteWorkItemStart();
                }

                nextAction = thisPtr._callbacks.ExecuteWorkItem(currentWorkItem);

                if (TD.ExecuteWorkItemStopIsEnabled())
                {
                    TD.ExecuteWorkItemStop();
                }
            }

            bool notifiedCompletion = false;
            bool isInstanceComplete = false;

            if (idleOrPaused || object.ReferenceEquals(nextAction, s_abortAction))
            {
                thisPtr._isPausing = false;
                thisPtr._isRunning = false;

                thisPtr.NotifyWorkCompletion();
                notifiedCompletion = true;

                if (isTracingEnabled)
                {
                    isInstanceComplete = thisPtr._callbacks.IsCompleted;
                }

                // After calling SchedulerIdle we no longer have the lock.  That means
                // that any subsequent processing in this method won't have the single
                // threaded guarantee.
                thisPtr._callbacks.SchedulerIdle();
            }
            else if (!object.ReferenceEquals(nextAction, s_yieldSilentlyAction))
            {
                Fx.Assert(nextAction is NotifyUnhandledExceptionAction, "This is the only other option");

                NotifyUnhandledExceptionAction notifyAction = (NotifyUnhandledExceptionAction)nextAction;

                // We only set isRunning back to false so that the host doesn't
                // have to treat this like a pause notification.  As an example,
                // a host could turn around and call run again in response to
                // UnhandledException without having to go through its operation
                // dispatch loop first (or request pause again).  If we reset
                // isPausing here then any outstanding operations wouldn't get
                // signaled with that type of host.
                thisPtr._isRunning = false;

                thisPtr.NotifyWorkCompletion();
                notifiedCompletion = true;

                if (isTracingEnabled)
                {
                    isInstanceComplete = thisPtr._callbacks.IsCompleted;
                }

                thisPtr._callbacks.NotifyUnhandledException(notifyAction.Exception, notifyAction.Source);
            }

            if (isTracingEnabled)
            {
                if (notifiedCompletion)
                {
                    if (isInstanceComplete)
                    {
                        if (TD.WorkflowActivityStopIsEnabled())
                        {
                            TD.WorkflowActivityStop(workflowInstanceId);
                        }
                    }
                    else
                    {
                        if (TD.WorkflowActivitySuspendIsEnabled())
                        {
                            TD.WorkflowActivitySuspend(workflowInstanceId);
                        }
                    }
                }

                TD.CurrentActivityId = oldActivityId;
            }
        }

        public struct Callbacks
        {
            private readonly ActivityExecutor _activityExecutor;

            public Callbacks(ActivityExecutor activityExecutor)
            {
                _activityExecutor = activityExecutor;
            }

            public Guid WorkflowInstanceId
            {
                get
                {
                    return _activityExecutor.WorkflowInstanceId;
                }
            }

            public bool IsAbortPending
            {
                get
                {
                    return _activityExecutor.IsAbortPending || _activityExecutor.IsTerminatePending;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    return ActivityUtilities.IsCompletedState(_activityExecutor.State);
                }
            }

            public RequestedAction ExecuteWorkItem(WorkItem workItem)
            {
                Fx.Assert(_activityExecutor != null, "ActivityExecutor null in ExecuteWorkItem.");

                // We check the Verbose flag to avoid the 
                // virt call if possible
                if (Microsoft.CoreWf.Internals.FxTrace.ShouldTraceVerboseToTraceSource)
                {
                    workItem.TraceStarting();
                }

                RequestedAction action = _activityExecutor.OnExecuteWorkItem(workItem);

                if (!object.ReferenceEquals(action, Scheduler.YieldSilently))
                {
                    if (_activityExecutor.IsAbortPending || _activityExecutor.IsTerminatePending)
                    {
                        action = Scheduler.Abort;
                    }

                    // if the caller yields, then the work item is still active and the callback
                    // is responsible for releasing it back to the pool                    
                    workItem.Dispose(_activityExecutor);
                }

                return action;
            }

            public void SchedulerIdle()
            {
                Fx.Assert(_activityExecutor != null, "ActivityExecutor null in SchedulerIdle.");
                _activityExecutor.OnSchedulerIdle();
            }

            public void ThreadAcquired()
            {
                Fx.Assert(_activityExecutor != null, "ActivityExecutor null in ThreadAcquired.");
                _activityExecutor.OnSchedulerThreadAcquired();
            }

            public void NotifyUnhandledException(Exception exception, ActivityInstance source)
            {
                Fx.Assert(_activityExecutor != null, "ActivityExecutor null in NotifyUnhandledException.");
                _activityExecutor.NotifyUnhandledException(exception, source);
            }
        }

        internal abstract class RequestedAction
        {
            protected RequestedAction()
            {
            }
        }

        private class ContinueAction : RequestedAction
        {
            public ContinueAction()
            {
            }
        }

        private class YieldSilentlyAction : RequestedAction
        {
            public YieldSilentlyAction()
            {
            }
        }

        private class AbortAction : RequestedAction
        {
            public AbortAction()
            {
            }
        }

        private class NotifyUnhandledExceptionAction : RequestedAction
        {
            public NotifyUnhandledExceptionAction(Exception exception, ActivityInstance source)
            {
                this.Exception = exception;
                this.Source = source;
            }

            public Exception Exception
            {
                get;
                private set;
            }

            public ActivityInstance Source
            {
                get;
                private set;
            }
        }
    }
}

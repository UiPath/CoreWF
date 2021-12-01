// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities.Runtime;

[DataContract(Name = XD.Runtime.Scheduler, Namespace = XD.Runtime.Namespace)]
internal class Scheduler
{
    private static readonly ContinueAction continueAction = new();
    private static readonly YieldSilentlyAction yieldSilentlyAction = new();
    private static readonly AbortAction abortAction = new();
    private WorkItem _firstWorkItem;
    private static readonly SendOrPostCallback onScheduledWorkCallback = Fx.ThunkCallback(new SendOrPostCallback(OnScheduledWork));
    private SynchronizationContext _synchronizationContext;
    private bool _isPausing;
    private bool _isRunning;
    //private bool resumeTraceRequired;
    private Callbacks _callbacks;
    private Quack<WorkItem> _workItemQueue;

    public Scheduler(Callbacks callbacks)
    {
        Initialize(callbacks);
    }

    public static RequestedAction Continue => continueAction;

    public static RequestedAction YieldSilently => yieldSilentlyAction;

    public static RequestedAction Abort => abortAction;

    public bool IsRunning => _isRunning;

    public bool IsIdle => _firstWorkItem == null;

    [DataMember(EmitDefaultValue = false, Name = "firstWorkItem")]
    internal WorkItem SerializedFirstWorkItem
    {
        get => _firstWorkItem;
        set => _firstWorkItem = value;
    }

    [DataMember(EmitDefaultValue = false)]
    //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode)]
    internal WorkItem[] SerializedWorkItemQueue
    {
        get => _workItemQueue != null && _workItemQueue.Count > 0 ? _workItemQueue.ToArray() : null;
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
            if (_firstWorkItem is ActivityInstanceMap.IActivityReference activityReference)
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
        => new NotifyUnhandledExceptionAction(exception, sourceInstance);

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

        bool isTracingEnabled = FxTrace.ShouldTraceInformation;
        bool notifiedCompletion = false;
        bool isInstanceComplete = false;

        if (_callbacks.IsAbortPending)
        {
            _isPausing = false;
            _isRunning = false;

            NotifyWorkCompletion();
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
        else if (ReferenceEquals(action, continueAction))
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

            NotifyWorkCompletion();
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
                        oldActivityId = System.Diagnostics.Tracing.EventSource.CurrentThreadActivityId;
                        System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(_callbacks.WorkflowInstanceId);
                        resetId = true;

                        TD.WorkflowActivityStop(_callbacks.WorkflowInstanceId);
                    }
                }
                else
                {
                    if (TD.WorkflowActivitySuspendIsEnabled())
                    {
                        oldActivityId = System.Diagnostics.Tracing.EventSource.CurrentThreadActivityId;
                        System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(_callbacks.WorkflowInstanceId);
                        resetId = true;

                        TD.WorkflowActivitySuspend(_callbacks.WorkflowInstanceId);
                    }
                }

                if (resetId)
                {
                    System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(oldActivityId);
                }
            }
        }
    }

    // called from ctor and OnDeserialized intialization paths
    private void Initialize(Callbacks callbacks) => _callbacks = callbacks;

    public void Open(SynchronizationContext synchronizationContext)
    {
        Fx.Assert(_synchronizationContext == null, "can only open when in the created state");
        _synchronizationContext = synchronizationContext ?? SynchronizationContextHelper.GetDefaultSynchronizationContext();
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
            //this.resumeTraceRequired = true;
        }
        else
        {
            //this.resumeTraceRequired = false;
        }
        _synchronizationContext.Post(onScheduledWorkCallback, this);
    }

    private void NotifyWorkCompletion() => _synchronizationContext.OperationCompleted();

    // signal the scheduler to stop processing work. If we are processing work
    // then we will catch this signal at our next iteration. Pause process completes
    // when idle is signalled. Can be called while we're processing work since
    // the worst thing that could happen in a race is that we pause one extra work item later
    public void Pause() => _isPausing = true;

    public void MarkRunning() => _isRunning = true;

    public void Resume()
    {
        Fx.Assert(_isRunning, "This should only be called after we've been set to process work.");

        if (IsIdle || _isPausing || _callbacks.IsAbortPending)
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
            _workItemQueue ??= new Quack<WorkItem>();
            _workItemQueue.PushFront(_firstWorkItem);
            _firstWorkItem = workItem;
        }

        // To avoid the virt call on EVERY work item we check
        // the Verbose flag.  All of our Schedule traces are
        // verbose.
        if (FxTrace.ShouldTraceVerboseToTraceSource)
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
            _workItemQueue ??= new Quack<WorkItem>();
            _workItemQueue.Enqueue(workItem);
        }

        if (FxTrace.ShouldTraceVerboseToTraceSource)
        {
            workItem.TraceScheduled();
        }
    }

    private static void OnScheduledWork(object state)
    {
        Scheduler thisPtr = (Scheduler)state;

        // We snapshot these values here so that we can
        // use them after calling OnSchedulerIdle.
        //bool isTracingEnabled = FxTrace.Trace.ShouldTraceToTraceSource(TraceEventLevel.Informational);
#pragma warning disable IDE0059 // Unnecessary assignment of a value
        Guid oldActivityId = Guid.Empty;
        Guid workflowInstanceId = Guid.Empty;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

        //if (isTracingEnabled)
        //{
        //    oldActivityId = DiagnosticTraceBase.ActivityId;
        //    workflowInstanceId = thisPtr.callbacks.WorkflowInstanceId;
        //    FxTrace.Trace.SetAndTraceTransfer(workflowInstanceId, true);

        //    if (thisPtr.resumeTraceRequired)
        //    {
        //        if (TD.WorkflowActivityResumeIsEnabled())
        //        {
        //            TD.WorkflowActivityResume(workflowInstanceId);
        //        }
        //    }
        //}

        thisPtr._callbacks.ThreadAcquired();

        RequestedAction nextAction = continueAction;
        bool idleOrPaused = false;

        while (ReferenceEquals(nextAction, continueAction))
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

        //bool notifiedCompletion = false;
        //bool isInstanceComplete = false;

        if (idleOrPaused || ReferenceEquals(nextAction, abortAction))
        {
            thisPtr._isPausing = false;
            thisPtr._isRunning = false;

            thisPtr.NotifyWorkCompletion();
            //notifiedCompletion = true;

            //if (isTracingEnabled)
            //{
            //    isInstanceComplete = thisPtr.callbacks.IsCompleted;
            //}

            // After calling SchedulerIdle we no longer have the lock.  That means
            // that any subsequent processing in this method won't have the single
            // threaded guarantee.
            thisPtr._callbacks.SchedulerIdle();
        }
        else if (!ReferenceEquals(nextAction, yieldSilentlyAction))
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
            //notifiedCompletion = true;

            //if (isTracingEnabled)
            //{
            //    isInstanceComplete = thisPtr.callbacks.IsCompleted;
            //}

            thisPtr._callbacks.NotifyUnhandledException(notifyAction.Exception, notifyAction.Source);
        }

        //if (isTracingEnabled)
        //{
        //    if (notifiedCompletion)
        //    {
        //        if (isInstanceComplete)
        //        {
        //            if (TD.WorkflowActivityStopIsEnabled())
        //            {
        //                TD.WorkflowActivityStop(workflowInstanceId);
        //            }
        //        }
        //        else
        //        {
        //            if (TD.WorkflowActivitySuspendIsEnabled())
        //            {
        //                TD.WorkflowActivitySuspend(workflowInstanceId);
        //            }
        //        }
        //    }

        //    DiagnosticTraceBase.ActivityId = oldActivityId;
        //}
    }

    public struct Callbacks
    {
        private readonly ActivityExecutor _activityExecutor;

        public Callbacks(ActivityExecutor activityExecutor)
        {
            _activityExecutor = activityExecutor;
        }

        public Guid WorkflowInstanceId => _activityExecutor.WorkflowInstanceId;

        public bool IsAbortPending => _activityExecutor.IsAbortPending || _activityExecutor.IsTerminatePending;

        public bool IsCompleted => ActivityUtilities.IsCompletedState(_activityExecutor.State);

        public RequestedAction ExecuteWorkItem(WorkItem workItem)
        {
            Fx.Assert(_activityExecutor != null, "ActivityExecutor null in ExecuteWorkItem.");

            // We check the Verbose flag to avoid the 
            // virt call if possible
            if (FxTrace.ShouldTraceVerboseToTraceSource)
            {
                workItem.TraceStarting();
            }

            RequestedAction action = _activityExecutor.OnExecuteWorkItem(workItem);

            if (!ReferenceEquals(action, YieldSilently))
            {
                if (_activityExecutor.IsAbortPending || _activityExecutor.IsTerminatePending)
                {
                    action = Abort;
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
        protected RequestedAction() { }
    }

    private class ContinueAction : RequestedAction
    {
        public ContinueAction() { }
    }

    private class YieldSilentlyAction : RequestedAction
    {
        public YieldSilentlyAction() { }
    }

    private class AbortAction : RequestedAction
    {
        public AbortAction() { }
    }

    private class NotifyUnhandledExceptionAction : RequestedAction
    {
        public NotifyUnhandledExceptionAction(Exception exception, ActivityInstance source)
        {
            Exception = exception;
            Source = source;
        }

        public Exception Exception { get; private set; }

        public ActivityInstance Source { get; private set; }
    }
}

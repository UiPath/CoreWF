// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.DurableInstancing;

namespace System.Activities.Runtime;

[DataContract]
public abstract class WorkItem
{
    private static AsyncCallback associateCallback;
    private static AsyncCallback trackingCallback;

    // We use a protected field here because it works well with
    // ref style Cleanup exception handling.
    protected Exception _workflowAbortException;
    private ActivityInstance _activityInstance;
    private bool _isEmpty;
    private Exception _exceptionToPropagate;

    // Used by subclasses in the pooled case.
    protected WorkItem() { }

    protected WorkItem(ActivityInstance activityInstance)
    {
        _activityInstance = activityInstance;
        _activityInstance.IncrementBusyCount();
    }

    public ActivityInstance ActivityInstance => _activityInstance;

    public Exception WorkflowAbortException => _workflowAbortException;

    public Exception ExceptionToPropagate
    {
        get => _exceptionToPropagate;
        set
        {
            Fx.Assert(value != null, "We should never set this back to null explicitly.  Use the ExceptionPropagated method below.");

            _exceptionToPropagate = value;
        }
    }

    public abstract ActivityInstance PropertyManagerOwner { get; }

    public virtual ActivityInstance OriginalExceptionSource => ActivityInstance;

    public bool IsEmpty
    {
        get => _isEmpty;
        protected set => _isEmpty = value;
    }

    public bool ExitNoPersistRequired { get; protected set; }

    protected bool IsPooled { get; set; }

    public abstract bool IsValid { get; }

    [DataMember(Name = "activityInstance")]
    internal ActivityInstance SerializedActivityInstance
    {
        get => _activityInstance;
        set => _activityInstance = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "IsEmpty")]
    internal bool SerializedIsEmpty
    {
        get => IsEmpty;
        set => IsEmpty = value;
    }

    public void Dispose(ActivityExecutor executor)
    {
        if (FxTrace.ShouldTraceVerboseToTraceSource)
        {
            TraceCompleted();
        }

        if (IsPooled)
        {
            ReleaseToPool(executor);
        }
    }

    protected virtual void ClearForReuse()
    {
        _exceptionToPropagate = null;
        _workflowAbortException = null;
        _activityInstance = null;
    }

    protected virtual void Reinitialize(ActivityInstance activityInstance)
    {
        _activityInstance = activityInstance;
        _activityInstance.IncrementBusyCount();
    }

    // this isn't just public for performance reasons. We avoid the virtual call
    // by going through Dispose()
    protected virtual void ReleaseToPool(ActivityExecutor executor) => Fx.Assert("This should never be called ... only overridden versions should get called.");

    private static void OnAssociateComplete(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }

        CallbackData data = (CallbackData)result.AsyncState;

        try
        {
            ActivityExecutor.EndAssociateKeys(result);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            data.WorkItem._workflowAbortException = e;
        }

        data.Executor.FinishWorkItem(data.WorkItem);
    }

    private static void OnTrackingComplete(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }

        CallbackData data = (CallbackData)result.AsyncState;

        try
        {
            data.Executor.EndTrackPendingRecords(result);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            data.WorkItem._workflowAbortException = e;
        }

        data.Executor.FinishWorkItemAfterTracking(data.WorkItem);
    }

    /// <remarks>
    /// We just null this out, but using this API helps with readability over the property setter
    /// </remarks>
    public void ExceptionPropagated() => _exceptionToPropagate = null;

    public void Release(ActivityExecutor executor)
    {
        _activityInstance.DecrementBusyCount();

        if (ExitNoPersistRequired)
        {
            executor.ExitNoPersist();
        }
    }

    public abstract void TraceScheduled();

    protected void TraceRuntimeWorkItemScheduled()
    {
        if (TD.ScheduleRuntimeWorkItemIsEnabled())
        {
            TD.ScheduleRuntimeWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
        }
    }

    public abstract void TraceStarting();

    protected void TraceRuntimeWorkItemStarting()
    {
        if (TD.StartRuntimeWorkItemIsEnabled())
        {
            TD.StartRuntimeWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
        }
    }

    public abstract void TraceCompleted();

    protected void TraceRuntimeWorkItemCompleted()
    {
        if (TD.CompleteRuntimeWorkItemIsEnabled())
        {
            TD.CompleteRuntimeWorkItem(ActivityInstance.Activity.GetType().ToString(), ActivityInstance.Activity.DisplayName, ActivityInstance.Id);
        }
    }

    public abstract bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager);

    public abstract void PostProcess(ActivityExecutor executor);

    public bool FlushBookmarkScopeKeys(ActivityExecutor executor)
    {
        Fx.Assert(executor.BookmarkScopeManager.HasKeysToUpdate, "We should not have been called if we don't have pending keys.");

        try
        {
            // disassociation is local-only so we don't need to yield 
            ICollection<InstanceKey> keysToDisassociate = executor.BookmarkScopeManager.GetKeysToDisassociate();
            if (keysToDisassociate != null && keysToDisassociate.Count > 0)
            {
                executor.DisassociateKeys(keysToDisassociate);
            }

            // if we have keys to associate, provide them for an asynchronous association
            ICollection<InstanceKey> keysToAssociate = executor.BookmarkScopeManager.GetKeysToAssociate();

            // It could be that we only had keys to Disassociate. We should only do BeginAssociateKeys
            // if we have keysToAssociate.
            if (keysToAssociate != null && keysToAssociate.Count > 0)
            {
                associateCallback ??= Fx.ThunkCallback(new AsyncCallback(OnAssociateComplete));
                IAsyncResult result = executor.BeginAssociateKeys(keysToAssociate, associateCallback, new CallbackData(executor, this));
                if (result.CompletedSynchronously)
                {
                    ActivityExecutor.EndAssociateKeys(result);
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            _workflowAbortException = e;
        }

        return true;
    }

    public bool FlushTracking(ActivityExecutor executor)
    {
        Fx.Assert(executor.HasPendingTrackingRecords, "We should not have been called if we don't have pending tracking records");

        try
        {
            trackingCallback ??= Fx.ThunkCallback(new AsyncCallback(OnTrackingComplete));
            IAsyncResult result = executor.BeginTrackPendingRecords(
                trackingCallback,
                new CallbackData(executor, this));

            if (result.CompletedSynchronously)
            {
                executor.EndTrackPendingRecords(result);
            }
            else
            {
                // Completed async so we'll return false
                return false;
            }
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            _workflowAbortException = e;
        }

        return true;
    }

    private class CallbackData
    {
        public CallbackData(ActivityExecutor executor, WorkItem workItem)
        {
            Executor = executor;
            WorkItem = workItem;
        }

        public ActivityExecutor Executor { get; private set; }

        public WorkItem WorkItem { get; private set; }
    }
}

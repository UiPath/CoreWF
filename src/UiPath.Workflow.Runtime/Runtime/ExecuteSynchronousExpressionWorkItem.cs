// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Tracking;

namespace System.Activities.Runtime;

/// <summary>
/// Evaluates a new-fast-path (SkipArgumentsResolution and Not UseOldFastPath) expression
/// </summary>
[DataContract]
internal class ExecuteSynchronousExpressionWorkItem : ActivityExecutionWorkItem, ActivityInstanceMap.IActivityReference
{
    private ActivityWithResult _expressionActivity;
    private long _instanceId;
    private ResolveNextArgumentWorkItem _nextArgumentWorkItem;
    private Location _resultLocation;

    /// <summary>
    /// Initializes a new instance of the ExecuteSynchronousExpressionWorkItem class.
    /// Called by the pool.
    /// </summary>
    public ExecuteSynchronousExpressionWorkItem()
    {
        IsPooled = true;
    }

    [DataMember(EmitDefaultValue = false, Name = "instanceId")]
    internal long SerializedInstanceId
    {
        get => _instanceId;
        set => _instanceId = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "nextArgumentWorkItem")]
    internal ResolveNextArgumentWorkItem SerializedNextArgumentWorkItem
    {
        get => _nextArgumentWorkItem;
        set => _nextArgumentWorkItem = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "resultLocation")]
    internal Location SerializedResultLocation
    {
        get => _resultLocation;
        set => _resultLocation = value;
    }

    /// <summary>
    /// Gets the Activity reference to serialize at persistence
    /// </summary>
    Activity ActivityInstanceMap.IActivityReference.Activity => _expressionActivity;

    /// <summary>
    /// Called each time a work item is acquired from the pool
    /// </summary>
    /// <param name="parentInstance">The ActivityInstance containin the variable or argument that contains this expression</param>
    /// <param name="expressionActivity">The expression to evaluate</param>
    /// <param name="instanceId">The ActivityInstanceID to use for expressionActivity</param>
    /// <param name="resultLocation">Location where the result of expressionActivity should be placed</param>
    /// <param name="nextArgumentWorkItem">WorkItem to execute after this one</param>
    public void Initialize(ActivityInstance parentInstance, ActivityWithResult expressionActivity, long instanceId, Location resultLocation, ResolveNextArgumentWorkItem nextArgumentWorkItem)
    {
        Reinitialize(parentInstance);

        Fx.Assert(resultLocation != null, "We should only use this work item when we are resolving arguments/variables and therefore have a result location.");
        Fx.Assert(expressionActivity.IsFastPath, "Should only use this work item for fast path expressions");

        _expressionActivity = expressionActivity;
        _instanceId = instanceId;
        _resultLocation = resultLocation;
        _nextArgumentWorkItem = nextArgumentWorkItem;
    }

    /// <summary>
    /// Trace when we're scheduled
    /// </summary>
    public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

    /// <summary>
    /// Trace when we start
    /// </summary>
    public override void TraceStarting() => TraceRuntimeWorkItemStarting();

    /// <summary>
    /// Trace when we complete
    /// </summary>
    public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

    /// <summary>
    /// Execute the work item
    /// </summary>
    /// <param name="executor">The executor</param>
    /// <param name="bookmarkManager">The bookmark manager</param>
    /// <returns>True to continue executing work items, false to yield the thread</returns>
    public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        ActivityInfo activityInfo = null;
        TrackExecuting(executor, ref activityInfo);

        try
        {
            executor.ExecuteInResolutionContextUntyped(ActivityInstance, _expressionActivity, _instanceId, _resultLocation);
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            TrackFaulted(executor, ref activityInfo);

            if (_nextArgumentWorkItem != null)
            {
                executor.ScheduleItem(_nextArgumentWorkItem);
            }

            executor.ScheduleExpressionFaultPropagation(_expressionActivity, _instanceId, ActivityInstance, e);
            return true;
        }
        finally
        {
            ActivityInstance.InstanceMap?.RemoveEntry(this);
        }

        TrackClosed(executor, ref activityInfo);

        if (_nextArgumentWorkItem != null)
        {
            EvaluateNextArgument(executor);
        }

        return true;
    }

    /// <summary>
    /// Fix up activity reference after persistence
    /// </summary>
    /// <param name="activity">The persisted activity reference</param>
    /// <param name="instanceMap">The map containing persisted activity references</param>
    void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap)
    {
        if (activity is not ActivityWithResult activityWithResult)
        {
            throw FxTrace.Exception.AsError(
                new ValidationException(SR.ActivityTypeMismatch(activity.DisplayName, typeof(ActivityWithResult).Name)));
        }

        _expressionActivity = activityWithResult;
    }

    /// <summary>
    /// Release work item back to pool
    /// </summary>
    /// <param name="executor">Executor that owns the work item.</param>
    protected override void ReleaseToPool(ActivityExecutor executor)
    {
        ClearForReuse();

        _expressionActivity = null;
        _instanceId = 0;
        _resultLocation = null;
        _nextArgumentWorkItem = null;

        executor.ExecuteSynchronousExpressionWorkItemPool.Release(this);
    }

    private void EvaluateNextArgument(ActivityExecutor executor)
    {
        if (executor.HasPendingTrackingRecords && _nextArgumentWorkItem.CanExecuteUserCode())
        {
            // Need to schedule a separate work item so we flush tracking before we continue.
            // This ensures consistent ordering of tracking output and user code.
            executor.ScheduleItem(_nextArgumentWorkItem);
        }
        else
        {
            executor.ExecuteSynchronousWorkItem(_nextArgumentWorkItem);
        }
    }

    private void EnsureActivityInfo(ref ActivityInfo activityInfo)
    {
        activityInfo ??= new ActivityInfo(_expressionActivity, _instanceId);
    }

    private void TrackClosed(ActivityExecutor executor, ref ActivityInfo activityInfo)
    {
        if (executor.ShouldTrackActivityStateRecordsClosedState)
        {
            TrackState(executor, ActivityInstanceState.Closed, ref activityInfo);
        }
    }

    private void TrackExecuting(ActivityExecutor executor, ref ActivityInfo activityInfo)
    {
        if (executor.ShouldTrackActivityStateRecordsExecutingState)
        {
            TrackState(executor, ActivityInstanceState.Executing, ref activityInfo);
        }
    }

    private void TrackFaulted(ActivityExecutor executor, ref ActivityInfo activityInfo)
    {
        if (executor.ShouldTrackActivityStateRecords)
        {
            TrackState(executor, ActivityInstanceState.Faulted, ref activityInfo);
        }
    }

    private void TrackState(ActivityExecutor executor, ActivityInstanceState state, ref ActivityInfo activityInfo)
    {
        if (executor.ShouldTrackActivity(_expressionActivity.DisplayName))
        {
            EnsureActivityInfo(ref activityInfo);
            executor.AddTrackingRecord(new ActivityStateRecord(executor.WorkflowInstanceId, activityInfo, state));
        }
    }
}

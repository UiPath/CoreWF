// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
public class ResolveNextArgumentWorkItem : ActivityExecutionWorkItem
{
    private int _nextArgumentIndex;
    private IDictionary<string, object> _argumentValueOverrides;
    private Location _resultLocation;

    public ResolveNextArgumentWorkItem()
    {
        IsPooled = true;
    }

    [DataMember(EmitDefaultValue = false, Name = "nextArgumentIndex")]
    internal int SerializedNextArgumentIndex
    {
        get => _nextArgumentIndex;
        set => _nextArgumentIndex = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "argumentValueOverrides")]
    internal IDictionary<string, object> SerializedArgumentValueOverrides
    {
        get => _argumentValueOverrides;
        set => _argumentValueOverrides = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "resultLocation")]
    internal Location SerializedResultLocation
    {
        get => _resultLocation;
        set => _resultLocation = value;
    }

    public override void TraceScheduled() => TraceRuntimeWorkItemScheduled();

    public override void TraceStarting() => TraceRuntimeWorkItemStarting();

    public override void TraceCompleted() => TraceRuntimeWorkItemCompleted();

    public void Initialize(ActivityInstance activityInstance, int nextArgumentIndex, IDictionary<string, object> argumentValueOverrides, Location resultLocation)
    {
        Fx.Assert(nextArgumentIndex > 0, "The nextArgumentIndex must be greater than 0 otherwise we will incorrectly set the sub-state when ResolveArguments completes");
        base.Reinitialize(activityInstance);
        _nextArgumentIndex = nextArgumentIndex;
        _argumentValueOverrides = argumentValueOverrides;
        _resultLocation = resultLocation;
    }

    // Knowledge at a distance! This method relies on the fact that ResolveArguments will
    // always schedule a separate work item for expressions that aren't OldFastPath.
    internal bool CanExecuteUserCode()
    {
        Activity activity = ActivityInstance.Activity;
        for (int i = _nextArgumentIndex; i < activity.RuntimeArguments.Count; i++)
        {
            RuntimeArgument argument = activity.RuntimeArguments[i];
            if (argument.IsBound && argument.BoundArgument.Expression != null)
            {
                return argument.BoundArgument.Expression.UseOldFastPath;
            }
        }
        return false;
    }

    protected override void ReleaseToPool(ActivityExecutor executor)
    {
        base.ClearForReuse();
        _nextArgumentIndex = 0;
        _resultLocation = null;
        _argumentValueOverrides = null;

        executor.ResolveNextArgumentWorkItemPool.Release(this);
    }

    public override bool Execute(ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        ActivityInstance.ResolveArguments(executor, _argumentValueOverrides, _resultLocation, _nextArgumentIndex);

        // Return true always to prevent scheduler from yielding silently.
        return true;
    }
}

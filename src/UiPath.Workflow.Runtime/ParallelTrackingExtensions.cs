namespace System.Activities.ParallelTracking;

[DataContract]
public struct BranchesStack
{
    internal const string BranchIdPropertyName = "__ParallelBranchId";

    [DataMember]
    internal string BranchesStackString { get; init; }

    public readonly BranchesStack Push() => new() { BranchesStackString = $"{BranchesStackString}.{Guid.NewGuid():N}".Trim('.') };

    public static BranchesStack ReadFrom(ActivityInstance instance) =>
        new()
        {
            BranchesStackString =
            instance.PropertyManager?.GetPropertyAtCurrentScope(BranchIdPropertyName) as string
        };

    public readonly void SaveTo(ActivityInstance instance)
    {
        new ExecutionProperties(null, instance, instance.PropertyManager)
            .Add(BranchIdPropertyName, this.BranchesStackString, true, false);
    }
}

/// <summary>
/// This feature introduces a context.GetCurrentParallelId() queryable from an activity,
/// which should identify a "parallelism" branch on which the activity executes.
/// Only containers which schedule concurrently (Parallel, ParallelForEach, Pick, and Flowchart with Split nodes)
/// would induce a change of the background parallel id.
/// The initial parallel id, before using the above containers, is null.
/// </summary>
public static class ParallelTrackingExtensions
{
    public static ActivityInstance MarkNewParallelBranch(this ActivityInstance instance)
    {
        BranchesStack.ReadFrom(instance).Push().SaveTo(instance);
        return instance;
    }

    public static string GetCurrentParallelBranchId(this ActivityInstance instance) =>
        BranchesStack.ReadFrom(instance).BranchesStackString;

    public static ActivityInstance MarkNewParallelBranch(this ActivityContext context) =>
         context.CurrentInstance?.MarkNewParallelBranch();

    public static string GetCurrentParallelBranchId(this ActivityContext context) =>
         context.CurrentInstance?.GetCurrentParallelBranchId();
}

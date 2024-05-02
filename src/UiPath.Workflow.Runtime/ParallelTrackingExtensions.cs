using System.Diagnostics;
namespace System.Activities.ParallelTracking;

[DataContract]
public record struct ParallelBranch
{
    [DataMember]
    internal string InstanceId { get; init; }

    [DataMember]
    internal string BranchesStackString { get; init; }

    public readonly ParallelBranch Push() => new()
    {
        BranchesStackString = $"{BranchesStackString}.{Guid.NewGuid():N}".Trim('.'),
        InstanceId = InstanceId
    };
}

/// <summary>
/// This feature introduces a context.GetCurrentParallelId() queryable from an activity,
/// which should identify a "parallelism" branch on which the activity executes.
/// Only containers which schedule concurrently (Parallel, ParallelForEach, Pick)
/// would induce a change of the background parallel id.
/// The initial parallel id, before using the above containers, is null.
/// </summary>
public static class ParallelTrackingExtensions
{
    public const string BranchIdPropertyName = "__ParallelBranchId";

    public static ActivityInstance MarkNewParallelBranch(this ActivityInstance instance)
    {
        var parentId = instance.GetCurrentParallelBranchId();
        var newBranch = new ParallelBranch() { BranchesStackString = parentId }.Push();
        instance.SetCurrentParallelBranchId(newBranch.BranchesStackString);
        return instance;
    }

    public static string GetCurrentParallelBranchId(this ActivityInstance instance) =>
        instance.PropertyManager?.GetPropertyAtCurrentScope(BranchIdPropertyName) as string;

    public static string GetCurrentParallelBranchId(this ActivityContext context) =>
         context.CurrentInstance?.GetCurrentParallelBranchId();

    public static ParallelBranch GetCurrentParallelBranch(this ActivityInstance instance) =>
        new()
        {
            BranchesStackString = instance.GetCurrentParallelBranchId(),
            InstanceId = instance.Id
        };

    public static void SetCurrentParallelBranch(this ActivityInstance currentOrChildInstance, ParallelBranch parallelBranch)
    {
        if (currentOrChildInstance.Id != parallelBranch.InstanceId && currentOrChildInstance?.Parent?.Id != parallelBranch.InstanceId)
            throw new ArgumentException($"{nameof(ParallelBranch)} must be obtained from this activity instance or it's parent.", nameof(currentOrChildInstance));

        currentOrChildInstance.SetCurrentParallelBranchId(parallelBranch.BranchesStackString);
    }

    private static void SetCurrentParallelBranchId(this ActivityInstance instance, string branchId) =>
        GetExecutionProperties(instance)
            .Add(BranchIdPropertyName, branchId, skipValidations: true, onlyVisibleToPublicChildren: false);

    private static ExecutionProperties GetExecutionProperties(ActivityInstance instance) => 
        new(null, instance, instance.PropertyManager);
}

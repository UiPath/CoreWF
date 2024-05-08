namespace System.Activities.ParallelTracking;

[DataContract]
public readonly record struct ParallelBranch
{
    [DataMember]
    internal string BranchesStackString { get; init; }
    [DataMember]
    internal string InstanceId { get; init; }

    public readonly ParallelBranch Push() =>
        new()
        {
            BranchesStackString = ParallelTrackingExtensions.PushNewBranch(BranchesStackString),
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
    private const char StackDelimiter = '.';


    public static ActivityInstance MarkNewParallelBranch(this ActivityInstance instance)
    {
        var parentId = instance.GetCurrentParallelBranchId();
        instance.SetCurrentParallelBranchId(PushNewBranch(parentId));
        return instance;
    }

    public static string GetCurrentParallelBranchId(this ActivityInstance instance) =>
        instance.PropertyManager?.GetPropertyAtCurrentScope(BranchIdPropertyName) as string;

    public static string GetCurrentParallelBranchId(this ActivityContext context) =>
         context.CurrentInstance?.GetCurrentParallelBranchId();

    public static ParallelBranch GetCurrentParallelBranch(this ActivityInstance instance) =>
        new() { BranchesStackString = instance.GetCurrentParallelBranchId() };

    public static void SetCurrentParallelBranch(this ActivityInstance currentOrChildInstance, ParallelBranch parallelBranch)
    {
        if (parallelBranch.InstanceId is not null && parallelBranch.InstanceId != currentOrChildInstance.Id)
            throw new ArgumentException($"{nameof(parallelBranch)} must be a pop or a push.", nameof(currentOrChildInstance));

        currentOrChildInstance.SetCurrentParallelBranchId(parallelBranch.BranchesStackString);
    }

    private static void SetCurrentParallelBranchId(this ActivityInstance instance, string branchId)
    {
        var currentBranchId = instance.GetCurrentParallelBranchId();

        if (!IsAncestorOf(thisStack: branchId, descendantStack: currentBranchId)
            && !IsAncestorOf(thisStack: currentBranchId, descendantStack: branchId))
            throw new ArgumentException($"{nameof(branchId)} must be a pop or a push.", nameof(instance));
        RemoveIfExists();
        GetExecutionProperties(instance).Add(BranchIdPropertyName, branchId, skipValidations: true, onlyVisibleToPublicChildren: false);

        void RemoveIfExists()
        {
            if (instance.PropertyManager?.IsOwner(instance) is true
                && instance.PropertyManager.Properties.ContainsKey(BranchIdPropertyName))
                instance.PropertyManager.Remove(BranchIdPropertyName);
        }
    }

    private static ExecutionProperties GetExecutionProperties(ActivityInstance instance) =>
        new(null, instance, instance.PropertyManager?.IsOwner(instance) is true ? instance.PropertyManager : null);
    private static bool IsAncestorOf(string thisStack, string descendantStack) =>
        (thisStack ?? string.Empty)
                .StartsWith(descendantStack ?? string.Empty, StringComparison.Ordinal);
    internal static string PushNewBranch(string thisStack) =>
        $"{thisStack}.{Guid.NewGuid():N}".Trim(StackDelimiter);
}

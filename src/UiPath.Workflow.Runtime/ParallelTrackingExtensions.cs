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
        GetExecutionProperties(instance).Find(BranchIdPropertyName) as string;

    public static string GetCurrentParallelBranchId(this ActivityContext context) =>
         context.CurrentInstance?.GetCurrentParallelBranchId();

    public static ParallelBranch GetCurrentParallelBranch(this ActivityInstance instance) =>
        new() { BranchesStackString = instance.GetCurrentParallelBranchId(), InstanceId = instance.Id };

    public static void SetCurrentParallelBranch(this ActivityInstance currentOrChildInstance, ParallelBranch parallelBranch)
    {
        if (parallelBranch.InstanceId != currentOrChildInstance.Id 
            && parallelBranch.InstanceId != currentOrChildInstance.Parent?.Id)
            throw new ArgumentException($"{nameof(parallelBranch)} must be a pop or a push.", nameof(currentOrChildInstance));

        currentOrChildInstance.SetCurrentParallelBranchId(parallelBranch.BranchesStackString);
    }

    /// <summary>
    /// Sets the parallelBranchId for the current activity instance
    /// </summary>
    /// <param name="branchId">null or empty removes the branch setting from current instance</param>
    /// <exception cref="ArgumentException">when not a pop or a push</exception>
    private static void SetCurrentParallelBranchId(this ActivityInstance instance, string branchId)
    {
        var currentBranchId = instance.GetCurrentParallelBranchId();

        if (!IsAncestorOf(thisStack: branchId, descendantStack: currentBranchId)
            && !IsAncestorOf(thisStack: currentBranchId, descendantStack: branchId))
            throw new ArgumentException($"{nameof(branchId)} must be a pop or a push.", nameof(instance));

        var props = GetExecutionProperties(instance);
        props.Remove(BranchIdPropertyName, skipValidations: true);
        if (string.IsNullOrEmpty(branchId))
            return;
        
        props.Add(BranchIdPropertyName, branchId, skipValidations: true, onlyVisibleToPublicChildren: false);
    }

    private static ExecutionProperties GetExecutionProperties(ActivityInstance instance) =>
        new(null, instance, instance.PropertyManager);

    private static bool IsAncestorOf(string thisStack, string descendantStack) =>
        (thisStack ?? string.Empty)
                .StartsWith(descendantStack ?? string.Empty, StringComparison.Ordinal);

    internal static string PushNewBranch(string thisStack) =>
        $"{thisStack}.{Guid.NewGuid():N}".Trim(StackDelimiter);
}

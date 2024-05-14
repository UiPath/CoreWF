namespace System.Activities.ParallelTracking;

/// <summary>
/// This feature introduces a context.GetCurrentParallelId() queryable from an activity,
/// which should identify a "parallelism" branch on which the activity executes.
/// Only containers which schedule concurrently (Parallel, ParallelForEach, Pick)
/// would induce a change of the background parallel id.
/// The initial parallel id, before using the above containers, is null.
/// </summary>
public static class ParallelTrackingExtensions
{
    private const string BranchIdPropertyName = "__ParallelBranchId";

    public static ActivityInstance MarkNewParallelBranch(this ActivityInstance instance)
    {
        var parentId = instance.GetParallelBranchId();
        instance.SetParallelBranchId(GenerateChildParallelBranchId(parentId));
        return instance;
    }

    public static string GetParallelBranchId(this ActivityInstance instance) =>
        GetExecutionProperties(instance).Find(BranchIdPropertyName) as string;

    public static string GetCurrentParallelBranchId(this ActivityContext context) =>
         context.CurrentInstance?.GetParallelBranchId();

    /// <summary>
    /// Sets the parallelBranchId for the current activity instance
    /// </summary>
    /// <param name="branchId">null or empty removes the branch setting from current instance</param>
    /// <exception cref="ArgumentException">when not a pop or a push</exception>
    public static void SetParallelBranchId(this ActivityInstance instance, string branchId)
    {
        var currentBranchId = instance.GetParallelBranchId();

        if (!IsAncestorOf(thisStack: branchId, descendantStack: currentBranchId)
            && !IsAncestorOf(thisStack: currentBranchId, descendantStack: branchId))
            throw new ArgumentException($"{nameof(branchId)} must be a pop or a push.", nameof(branchId));

        var props = GetExecutionProperties(instance);
        props.Remove(BranchIdPropertyName, skipValidations: true);
        if (string.IsNullOrEmpty(branchId))
            return;
        
        props.Add(BranchIdPropertyName, branchId, skipValidations: true, onlyVisibleToPublicChildren: false);
    }
    public static string GenerateChildParallelBranchId(string branchId) =>
        $"{branchId}.{Guid.NewGuid():N}".Trim('.');

    private static ExecutionProperties GetExecutionProperties(ActivityInstance instance) =>
        new(null, instance, instance.PropertyManager);

    private static bool IsAncestorOf(string thisStack, string descendantStack) =>
        (thisStack ?? string.Empty)
                .StartsWith(descendantStack ?? string.Empty, StringComparison.Ordinal);
}

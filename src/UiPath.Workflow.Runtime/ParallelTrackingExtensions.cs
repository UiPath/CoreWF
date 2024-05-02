namespace System.Activities.ParallelTracking;

[DataContract]
public record struct ParallelBranch
{
    private const char StackDelimiter = '.';

    [DataMember]
    internal string BranchesStackString { get; init; }
    [DataMember]
    internal string InstanceId { get; init; }

    public readonly ParallelBranch Push() =>
        new()
        {
            BranchesStackString = $"{BranchesStackString}.{Guid.NewGuid():N}".Trim('.'),
            InstanceId = InstanceId
        };

    internal bool IsAncestorOf(ParallelBranch descendantBranch)
    {
        var descendantStack = descendantBranch.BranchesStackString ?? string.Empty;
        var lastIndex = descendantStack.LastIndexOf(StackDelimiter);
        var thisStack = BranchesStackString ?? string.Empty;
        return thisStack.StartsWith(descendantStack, StringComparison.Ordinal) 
            && descendantBranch.InstanceId == InstanceId;
    }
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
        new() { BranchesStackString = instance.GetCurrentParallelBranchId() };

    public static void SetCurrentParallelBranch(this ActivityInstance currentOrChildInstance, ParallelBranch parallelBranch)
    {
        var currentBranch = currentOrChildInstance.GetCurrentParallelBranch();
        if (!parallelBranch.IsAncestorOf(currentBranch)
            && !currentBranch.IsAncestorOf(parallelBranch))
            throw new ArgumentException($"{nameof(parallelBranch)} must be a pop or a push.", nameof(currentOrChildInstance));

        currentOrChildInstance.SetCurrentParallelBranchId(parallelBranch.BranchesStackString);
    }

    private static void SetCurrentParallelBranchId(this ActivityInstance instance, string branchId)
    {
        var props = GetExecutionProperties(instance);
        if (props.Find(BranchIdPropertyName) is not null)
            props.Remove(BranchIdPropertyName, skipValidations: true);

        props.Add(BranchIdPropertyName, branchId, skipValidations: true, onlyVisibleToPublicChildren: false);
    }

    private static ExecutionProperties GetExecutionProperties(ActivityInstance instance) => 
        new(null, instance, instance.PropertyManager);
}

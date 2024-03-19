using System;
using System.Activities;

namespace UiPath.Workflow.Runtime.ParallelTracking
{
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
            var properties = new ExecutionProperties(null, instance, instance.PropertyManager);
            var parentId = properties.Find(BranchIdPropertyName) as string;
            properties.Add(BranchIdPropertyName, $"{parentId}.{Guid.NewGuid():N}".Trim('.'), true, false);

            return instance;
        }

        public static string GetCurrentParallelBranchId(this ActivityInstance instance) =>
            instance.PropertyManager?.GetPropertyAtCurrentScope(BranchIdPropertyName) as string;

        public static ActivityInstance MarkNewParallelBranch(this ActivityContext context) =>
             context.CurrentInstance?.MarkNewParallelBranch();

        public static string GetCurrentParallelBranchId(this ActivityContext context) =>
             context.CurrentInstance?.GetCurrentParallelBranchId();
    }
}

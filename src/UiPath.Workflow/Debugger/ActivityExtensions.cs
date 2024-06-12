using System;
using System.Activities;
using System.Activities.Runtime;
using System.Activities.Tracking;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq;

namespace UiPath.Workflow.Debugger;

/// <summary>
/// TODO - add unit tests.
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// A client may change properties at runtime (while execution is paused),
    /// and so, based on the activity internal logic, a new call to CacheMetadata may be required.
    /// This extension method will update the activity state.
    /// ATTENTION - calling this method will generate a new activity.Id
    /// </summary>
    /// <returns>a list of validation errors</returns>
    public static IList<ValidationError> RefreshCacheMetadata(this Activity activity)
    {
        if (activity == null)
            return Array.Empty<ValidationError>();

        ClearCacheIds(activity);
        activity.ClearCachedInformation();

        var options = ProcessActivityTreeOptions.FullCachingOptions;
        IList<ValidationError> validationErrors = null;
        var childActivity = new ActivityUtilities.ChildActivity(activity, true);
        ActivityUtilities.ProcessActivityTreeCore(childActivity, null, options, null, ref validationErrors);
        return validationErrors;
    }

    /// <summary>
    /// If properties where changed at runtime while execution is paused,
    /// by an external client than the <paramref name="activityInfo"/> environment 
    /// needs to be updated.
    /// </summary>
    public static void UpdateEnvironment(this ActivityInfo activityInfo)
        => activityInfo?.Instance?.UpdateEnvironment();

    /// <summary>
    /// While debugging the activity may call again CacheMetadata,
    /// and the validation of RuntimeArguments may fail due to already initialized CacheIds    
    /// </summary>
    private static void ClearCacheIds(this Activity activity)
    {
        if (activity.RuntimeArguments != null)
        {
            foreach (var item in activity
                                    .RuntimeArguments
                                    .Where(a => a?.BoundArgument?.Expression?.CacheId == activity.CacheId))
            {
                item.BoundArgument.Expression.CacheId = 0;
            }
        }

        ResetVariablesCacheId(activity.RuntimeVariables);
        ResetVariablesCacheId(activity.ImplementationVariables);

        void ResetVariablesCacheId(IList<Variable> variables)
        {
            if (variables == null)
                return;

            foreach (var item in variables.Where(v => v?.CacheId == activity.CacheId))
            {
                item.CacheId = 0;
                if (item.Default != null)
                {
                    item.Default.CacheId = 0;
                }
            }
        }
    }

    private static void UpdateEnvironment(this ActivityInstance activityInstance)
    {
        var newCapacity = activityInstance.Activity.SymbolCount + activityInstance.DelegateParameterCount;
        activityInstance.Environment.EnsureCapacity(newCapacity);
    }

    private static void EnsureCapacity(this LocationEnvironment environment, int newCapacity)
    {
        if (environment.SerializedLocations == null
            || newCapacity <= environment.SerializedLocations.Length)
            return;

        environment.SerializedLocations = new Location[newCapacity];
    }
}

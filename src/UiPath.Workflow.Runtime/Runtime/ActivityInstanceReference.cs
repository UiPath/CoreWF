// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
public class ActivityInstanceReference : ActivityInstanceMap.IActivityReference
{
    private ActivityInstance _activityInstance;
    
    internal ActivityInstanceReference() { }

    internal ActivityInstanceReference(ActivityInstance activity)
    {
        _activityInstance = activity;
    }

    [DataMember(Name = "activityInstance")]
    internal ActivityInstance SerializedActivityInstance
    {
        get => _activityInstance;
        set => _activityInstance = value;
    }

    Activity ActivityInstanceMap.IActivityReference.Activity => _activityInstance.Activity;

    public ActivityInstance ActivityInstance => _activityInstance;

    void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap)
    {
        // The conditional calling of ActivityInstance.Load is the value
        // added by this wrapper class.  This is because we can't guarantee
        // that multiple activities won't have a reference to the same
        // ActivityInstance.
        if (_activityInstance.Activity == null)
        {
            ((ActivityInstanceMap.IActivityReference)_activityInstance).Load(activity, instanceMap);
        }
    }
}

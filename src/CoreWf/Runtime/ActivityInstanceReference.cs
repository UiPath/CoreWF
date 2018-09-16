// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Runtime
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class ActivityInstanceReference : ActivityInstanceMap.IActivityReference
    {
        private ActivityInstance activityInstance;

        internal ActivityInstanceReference(ActivityInstance activity)
        {
            this.activityInstance = activity;
        }

        [DataMember(Name = "activityInstance")]
        internal ActivityInstance SerializedActivityInstance
        {
            get { return this.activityInstance; }
            set { this.activityInstance = value; }
        }

        Activity ActivityInstanceMap.IActivityReference.Activity
        {
            get
            {
                return this.activityInstance.Activity;
            }
        }


        public ActivityInstance ActivityInstance
        {
            get
            {
                return this.activityInstance;
            }
        }

        void ActivityInstanceMap.IActivityReference.Load(Activity activity, ActivityInstanceMap instanceMap)
        {
            // The conditional calling of ActivityInstance.Load is the value
            // added by this wrapper class.  This is because we can't guarantee
            // that multiple activities won't have a reference to the same
            // ActivityInstance.
            if (this.activityInstance.Activity == null)
            {
                ((ActivityInstanceMap.IActivityReference)this.activityInstance).Load(activity, instanceMap);
            }
        }
    }
}



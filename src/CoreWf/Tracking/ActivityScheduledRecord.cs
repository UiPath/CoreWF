// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking
{
    using System;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Activities.Runtime;
    using System.Activities.Internals;

    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class ActivityScheduledRecord : TrackingRecord
    {
        private ActivityInfo activity;
        private ActivityInfo child;

        internal ActivityScheduledRecord(Guid instanceId, ActivityInstance instance, ActivityInstance child)
            : this(instanceId, instance, new ActivityInfo(child))
        {
        }

        internal ActivityScheduledRecord(Guid instanceId, ActivityInstance instance, ActivityInfo child)
            : base(instanceId)
        {
            Fx.Assert(child != null, "Child activity cannot be null.");
            if (instance != null)
            {
                this.Activity = new ActivityInfo(instance);
            }
            this.Child = child;
        }

        //parameter activity is null if the root activity is being scheduled.
        public ActivityScheduledRecord(
            Guid instanceId,
            long recordNumber,
            ActivityInfo activity,
            ActivityInfo child)
            : base(instanceId, recordNumber)
        {
            this.Activity = activity;            
            this.Child = child ?? throw FxTrace.Exception.ArgumentNull(nameof(child));
        }

        private ActivityScheduledRecord(ActivityScheduledRecord record)
            : base(record)
        {
            this.Activity = record.Activity;
            this.Child = record.Child;            
        }
        
        public ActivityInfo Activity
        {
            get
            {
                return this.activity;
            }
            private set
            {
                this.activity = value;
            }
        }
        
        public ActivityInfo Child
        {
            get
            {
                return this.child;
            }
            private set
            {
                this.child = value;
            }
        }

        [DataMember(Name = "Activity")]
        internal ActivityInfo SerializedActivity
        {
            get { return this.Activity; }
            set { this.Activity = value; }
        }

        [DataMember(Name = "Child")]
        internal ActivityInfo SerializedChild
        {
            get { return this.Child; }
            set { this.Child = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new ActivityScheduledRecord(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
               "ActivityScheduledRecord {{ {0}, Activity {{ {1} }}, ChildActivity {{ {2} }} }}",
               base.ToString(),
               this.Activity == null ? "<null>" : this.Activity.ToString(),               
               this.Child.ToString());
        }

    }
}

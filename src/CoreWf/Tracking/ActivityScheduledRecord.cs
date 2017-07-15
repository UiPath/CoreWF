// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class ActivityScheduledRecord : TrackingRecord
    {
        private ActivityInfo _activity;
        private ActivityInfo _child;

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
            if (child == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("child");
            }

            this.Activity = activity;
            this.Child = child;
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
                return _activity;
            }
            private set
            {
                _activity = value;
            }
        }

        public ActivityInfo Child
        {
            get
            {
                return _child;
            }
            private set
            {
                _child = value;
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

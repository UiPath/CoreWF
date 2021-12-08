// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class ActivityScheduledRecord : TrackingRecord
{
    private ActivityInfo _activity;
    private ActivityInfo _child;

    internal ActivityScheduledRecord(Guid instanceId, ActivityInstance instance, ActivityInstance child)
        : this(instanceId, instance, new ActivityInfo(child)) { }

    internal ActivityScheduledRecord(Guid instanceId, ActivityInstance instance, ActivityInfo child)
        : base(instanceId)
    {
        Fx.Assert(child != null, "Child activity cannot be null.");
        if (instance != null)
        {
            Activity = new ActivityInfo(instance);
        }
        Child = child;
    }

    //parameter activity is null if the root activity is being scheduled.
    public ActivityScheduledRecord(
        Guid instanceId,
        long recordNumber,
        ActivityInfo activity,
        ActivityInfo child)
        : base(instanceId, recordNumber)
    {
        Activity = activity;
        Child = child ?? throw FxTrace.Exception.ArgumentNull(nameof(child));
    }

    private ActivityScheduledRecord(ActivityScheduledRecord record)
        : base(record)
    {
        Activity = record.Activity;
        Child = record.Child;
    }

    public ActivityInfo Activity
    {
        get => _activity;
        private set => _activity = value;
    }

    public ActivityInfo Child
    {
        get => _child;
        private set => _child = value;
    }

    [DataMember(Name = "Activity")]
    internal ActivityInfo SerializedActivity
    {
        get => Activity;
        set => Activity = value;
    }

    [DataMember(Name = "Child")]
    internal ActivityInfo SerializedChild
    {
        get => Child;
        set => Child = value;
    }

    protected internal override TrackingRecord Clone() => new ActivityScheduledRecord(this);

    public override string ToString() => string.Format(CultureInfo.CurrentCulture,
        "ActivityScheduledRecord {{ {0}, Activity {{ {1} }}, ChildActivity {{ {2} }} }}",
        base.ToString(),
        Activity == null ? "<null>" : Activity.ToString(),
        Child.ToString());
}

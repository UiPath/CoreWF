// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Activities.Tracking;

[DataContract]
[Fx.Tag.XamlVisible(false)]
public class CustomTrackingRecord : TrackingRecord
{
    private IDictionary<string, object> _data;
    private string _name;
    private ActivityInfo _activity;

    public CustomTrackingRecord(string name)
        : this(name, EventLevel.Informational) { }

    public CustomTrackingRecord(string name, EventLevel level)
        : this(Guid.Empty, name, level) { }

    public CustomTrackingRecord(Guid instanceId, string name, EventLevel level)
        : base(instanceId)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNull(nameof(name));
        }
        Name = name;
        Level = level;
    }

    protected CustomTrackingRecord(CustomTrackingRecord record)
        : base(record)
    {
        Name = record.Name;
        Activity = record.Activity;
        if (record._data != null && record._data.Count > 0)
        {
            foreach (KeyValuePair<string, object> item in record._data)
            {
                Data.Add(item);
            }
        }
    }

    public string Name
    {
        get => _name;
        private set => _name = value;
    }

    public ActivityInfo Activity
    {
        get => _activity;
        internal set => _activity = value;
    }

    public IDictionary<string, object> Data
    {
        get
        {
            _data ??= new Dictionary<string, object>();
            return _data;
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "data")]
    internal IDictionary<string, object> SerializedData
    {
        get => _data;
        set => _data = value;
    }

    [DataMember(Name = "Name")]
    internal string SerializedName
    {
        get => Name;
        set => Name = value;
    }

    [DataMember(Name = "Activity")]
    internal ActivityInfo SerializedActivity
    {
        get => Activity;
        set => Activity = value;
    }

    protected internal override TrackingRecord Clone() => new CustomTrackingRecord(this);

    public override string ToString()
        => string.Format(CultureInfo.InvariantCulture,
            "CustomTrackingRecord {{ {0}, Name={1}, Activity {{ {2} }}, Level = {3} }}",
            base.ToString(),
            Name,
            Activity == null ? "<null>" : Activity.ToString(),
            Level);
}

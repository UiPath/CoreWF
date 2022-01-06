// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class ActivityInfo
{
    private string _name;
    private string _id;
    private string _instanceId;
    private readonly long _instanceIdInternal;
    private string _typeName;

    public ActivityInfo(string name, string id, string instanceId, string typeName)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }
        if (string.IsNullOrEmpty(id))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(id));
        }
        if (string.IsNullOrEmpty(instanceId))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(instanceId));
        }
        if (string.IsNullOrEmpty(typeName))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(typeName));
        }
        Name = name;
        Id = id;
        InstanceId = instanceId;
        TypeName = typeName;
    }

    internal ActivityInfo(ActivityInstance instance)
        : this(instance.Activity, instance.InternalId)
    {
        Instance = instance;
    }

    internal ActivityInfo(Activity activity, long instanceId)
    {
        Activity = activity;
        _instanceIdInternal = instanceId;
    }

    internal ActivityInstance Instance { get; private set; }

    [DataMember]
    public string Name
    {
        get
        {
            if (string.IsNullOrEmpty(_name))
            {
                Fx.Assert(Activity != null, "Activity not set");
                _name = Activity.DisplayName;
            }
            return _name;
        }
        // Internal visibility for partial trust serialization purposes only.
        internal set
        {
            Fx.Assert(!string.IsNullOrEmpty(value), "Name cannot be null or empty");
            _name = value;
        }
    }

    [DataMember]
    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(_id))
            {
                Fx.Assert(Activity != null, "Activity not set");
                _id = Activity.Id;
            }
            return _id;
        }
        // Internal visibility for partial trust serialization purposes only.
        internal set
        {
            Fx.Assert(!string.IsNullOrEmpty(value), "Id cannot be null or empty");
            _id = value;
        }
    }

    [DataMember]
    public string InstanceId
    {
        get
        {
            if (string.IsNullOrEmpty(_instanceId))
            {
                _instanceId = _instanceIdInternal.ToString(CultureInfo.InvariantCulture);
            }
            return _instanceId;
        }
        // Internal visibility for partial trust serialization purposes only.
        internal set
        {
            Fx.Assert(!string.IsNullOrEmpty(value), "InstanceId cannot be null or empty");
            _instanceId = value;
        }
    }

    [DataMember]
    public string TypeName
    {
        get
        {
            if (string.IsNullOrEmpty(_typeName))
            {
                Fx.Assert(Activity != null, "Activity not set");
                _typeName = Activity.GetType().FullName;
            }
            return _typeName;
        }
        // Internal visibility for partial trust serialization purposes only.
        internal set
        {
            Fx.Assert(!string.IsNullOrEmpty(value), "TypeName cannot be null or empty");
            _typeName = value;
        }
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture,
            "Name={0}, ActivityId = {1}, ActivityInstanceId = {2}, TypeName={3}",
            Name,
            Id,
            InstanceId,
            TypeName);
    }

    internal Activity Activity { get; private set; }
}

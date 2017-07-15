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
    public sealed class ActivityInfo
    {
        private string _name;
        private string _id;
        private string _instanceId;
        private long _instanceIdInternal;
        private string _typeName;

        public ActivityInfo(string name, string id, string instanceId, string typeName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
            }
            if (string.IsNullOrEmpty(id))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("id");
            }
            if (string.IsNullOrEmpty(instanceId))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("instanceId");
            }
            if (string.IsNullOrEmpty(typeName))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("typeName");
            }
            this.Name = name;
            this.Id = id;
            this.InstanceId = instanceId;
            this.TypeName = typeName;
        }

        internal ActivityInfo(ActivityInstance instance)
            : this(instance.Activity, instance.InternalId)
        {
            this.Instance = instance;
        }

        internal ActivityInfo(Activity activity, long instanceId)
        {
            this.Activity = activity;
            _instanceIdInternal = instanceId;
        }

        internal ActivityInstance Instance
        {
            get;
            private set;
        }

        [DataMember]
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                {
                    Fx.Assert(this.Activity != null, "Activity not set");
                    _name = this.Activity.DisplayName;
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
                if (String.IsNullOrEmpty(_id))
                {
                    Fx.Assert(this.Activity != null, "Activity not set");
                    _id = this.Activity.Id;
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
                    Fx.Assert(this.Activity != null, "Activity not set");
                    _typeName = this.Activity.GetType().FullName;
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
                this.Name,
                this.Id,
                this.InstanceId,
                this.TypeName);
        }

        internal Activity Activity
        {
            get;
            private set;
        }
    }
}

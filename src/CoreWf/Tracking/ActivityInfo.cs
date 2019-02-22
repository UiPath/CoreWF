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
    public sealed class ActivityInfo
    {
        private string name;
        private string id;
        private string instanceId;
        private readonly long instanceIdInternal;
        private string typeName;

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
            this.instanceIdInternal = instanceId;
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
                if (string.IsNullOrEmpty(this.name))
                {
                    Fx.Assert(this.Activity != null, "Activity not set");
                    this.name = this.Activity.DisplayName;
                }
                return this.name;
            }
            // Internal visibility for partial trust serialization purposes only.
            internal set
            {
                Fx.Assert(!string.IsNullOrEmpty(value), "Name cannot be null or empty");
                this.name = value;
            }
        }

        [DataMember]
        public string Id
        {
            get
            {
                if (String.IsNullOrEmpty(this.id))
                {
                    Fx.Assert(this.Activity != null, "Activity not set");
                    this.id = this.Activity.Id;
                }
                return this.id;
            }
            // Internal visibility for partial trust serialization purposes only.
            internal set
            {
                Fx.Assert(!string.IsNullOrEmpty(value), "Id cannot be null or empty");
                this.id = value;
            }
        }

        [DataMember]
        public string InstanceId
        {
            get
            {
                if (string.IsNullOrEmpty(this.instanceId))
                {
                    this.instanceId = this.instanceIdInternal.ToString(CultureInfo.InvariantCulture);
                }
                return this.instanceId;
            }
            // Internal visibility for partial trust serialization purposes only.
            internal set
            {
                Fx.Assert(!string.IsNullOrEmpty(value), "InstanceId cannot be null or empty");
                this.instanceId = value;
            }
        }

        [DataMember]
        public string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(this.typeName))
                {
                    Fx.Assert(this.Activity != null, "Activity not set");
                    this.typeName = this.Activity.GetType().FullName;
                }
                return this.typeName;
            }
            // Internal visibility for partial trust serialization purposes only.
            internal set
            {
                Fx.Assert(!string.IsNullOrEmpty(value), "TypeName cannot be null or empty");
                this.typeName = value;
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

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [DataContract]
    [Fx.Tag.XamlVisible(false)]
    public class CustomTrackingRecord : TrackingRecord
    {
        private IDictionary<string, object> _data;
        private string _name;
        private ActivityInfo _activity;

        public CustomTrackingRecord(string name)
            : this(name, EventLevel.Informational)
        {
        }

        public CustomTrackingRecord(string name, EventLevel level)
            : this(Guid.Empty, name, level)
        {
        }

        public CustomTrackingRecord(Guid instanceId, string name, EventLevel level)
            : base(instanceId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull(nameof(name));
            }
            this.Name = name;
            this.Level = level;
        }

        protected CustomTrackingRecord(CustomTrackingRecord record)
            : base(record)
        {
            this.Name = record.Name;
            this.Activity = record.Activity;
            if (record._data != null && record._data.Count > 0)
            {
                foreach (KeyValuePair<string, object> item in record._data)
                {
                    this.Data.Add(item);
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
            }
        }

        public ActivityInfo Activity
        {
            get { return _activity; }
            internal set { _activity = value; }
        }

        public IDictionary<string, object> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new Dictionary<string, object>();
                }
                return _data;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "data")]
        internal IDictionary<string, object> SerializedData
        {
            get { return _data; }
            set { _data = value; }
        }

        [DataMember(Name = "Name")]
        internal string SerializedName
        {
            get { return this.Name; }
            set { this.Name = value; }
        }

        [DataMember(Name = "Activity")]
        internal ActivityInfo SerializedActivity
        {
            get { return this.Activity; }
            set { this.Activity = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new CustomTrackingRecord(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "CustomTrackingRecord {{ {0}, Name={1}, Activity {{ {2} }}, Level = {3} }}",
                base.ToString(),
                this.Name,
                this.Activity == null ? "<null>" : this.Activity.ToString(),
                this.Level);
        }
    }
}

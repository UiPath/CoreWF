// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Activities.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public abstract class TrackingRecord
    {
        private IDictionary<string, string> _annotations;
        private DateTime _eventTime;
        private EventLevel _level;
        private EventTraceActivity _eventTraceActivity;

        private static ReadOnlyDictionary<string, string> s_readonlyEmptyAnnotations;

        protected TrackingRecord(Guid instanceId)
        {
            this.InstanceId = instanceId;
            this.EventTime = DateTime.UtcNow;
            this.Level = EventLevel.Informational;
            _eventTraceActivity = new EventTraceActivity(instanceId);
        }

        protected TrackingRecord(Guid instanceId, long recordNumber)
            : this(instanceId)
        {
            this.RecordNumber = recordNumber;
        }

        protected TrackingRecord(TrackingRecord record)
        {
            this.InstanceId = record.InstanceId;
            this.RecordNumber = record.RecordNumber;
            this.EventTime = record.EventTime;
            this.Level = record.Level;
            if (record.HasAnnotations)
            {
                Dictionary<string, string> copy = new Dictionary<string, string>(record._annotations);
                _annotations = new ReadOnlyDictionary<string, string>(copy);
            }
        }


        [DataMember]
        public Guid InstanceId
        {
            get;
            internal set;
        }

        [DataMember]
        public long RecordNumber
        {
            get;
            internal set;
        }

        public DateTime EventTime
        {
            get
            {
                return _eventTime;
            }
            private set
            {
                _eventTime = value;
            }
        }

        public EventLevel Level
        {
            get
            {
                return _level;
            }
            protected set
            {
                _level = value;
            }
        }

        public IDictionary<string, string> Annotations
        {
            get
            {
                if (_annotations == null)
                {
                    _annotations = ReadOnlyEmptyAnnotations;
                }
                return _annotations;
            }
            internal set
            {
                Fx.Assert(value.IsReadOnly, "only readonly dictionary can be set for annotations");
                _annotations = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "annotations")]
        internal IDictionary<string, string> SerializedAnnotations
        {
            get { return _annotations; }
            set { _annotations = value; }
        }

        [DataMember(Name = "EventTime")]
        internal DateTime SerializedEventTime
        {
            get { return this.EventTime; }
            set { this.EventTime = value; }
        }

        [DataMember(Name = "Level")]
        internal EventLevel SerializedLevel
        {
            get { return this.Level; }
            set { this.Level = value; }
        }

        internal EventTraceActivity EventTraceActivity
        {
            get
            {
                if (_eventTraceActivity == null)
                {
                    _eventTraceActivity = new EventTraceActivity(this.InstanceId);
                }

                return _eventTraceActivity;
            }
        }

        private static ReadOnlyDictionary<string, string> ReadOnlyEmptyAnnotations
        {
            get
            {
                if (s_readonlyEmptyAnnotations == null)
                {
                    s_readonlyEmptyAnnotations = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0));
                }
                return s_readonlyEmptyAnnotations;
            }
        }

        public bool HasAnnotations
        {
            get
            {
                return (_annotations != null && _annotations.Count > 0);
            }
        }

        protected abstract internal TrackingRecord Clone();

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "InstanceId = {0}, RecordNumber = {1}, EventTime = {2}",
                this.InstanceId,
                this.RecordNumber,
                this.EventTime);
        }
    }
}

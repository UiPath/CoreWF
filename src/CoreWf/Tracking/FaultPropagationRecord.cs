// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class FaultPropagationRecord : TrackingRecord
    {
        private ActivityInfo _faultSource;
        private ActivityInfo _faultHandler;
        private bool _isFaultSource;
        private Exception _fault;

        internal FaultPropagationRecord(Guid instanceId, ActivityInstance source, ActivityInstance faultHandler, bool isFaultSource, Exception fault)
            : base(instanceId)
        {
            Fx.Assert(source != null, "Fault source cannot be null");
            this.FaultSource = new ActivityInfo(source);

            if (faultHandler != null)
            {
                this.FaultHandler = new ActivityInfo(faultHandler);
            }
            this.IsFaultSource = isFaultSource;
            this.Fault = fault;
            this.Level = EventLevel.Warning;
        }

        //parameter faultHandler is null if there are no handlers
        public FaultPropagationRecord(
             Guid instanceId,
             long recordNumber,
             ActivityInfo faultSource,
             ActivityInfo faultHandler,
             bool isFaultSource,
             Exception fault)
            : base(instanceId, recordNumber)
        {
            this.FaultSource = faultSource ?? throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(faultSource));
            this.FaultHandler = faultHandler;
            this.IsFaultSource = isFaultSource;
            this.Fault = fault;
            this.Level = EventLevel.Warning;
        }

        private FaultPropagationRecord(FaultPropagationRecord record)
            : base(record)
        {
            this.FaultSource = record.FaultSource;
            this.FaultHandler = record.FaultHandler;
            this.Fault = record.Fault;
            this.IsFaultSource = record.IsFaultSource;
        }

        public ActivityInfo FaultSource
        {
            get
            {
                return _faultSource;
            }
            private set
            {
                _faultSource = value;
            }
        }

        public ActivityInfo FaultHandler
        {
            get
            {
                return _faultHandler;
            }
            private set
            {
                _faultHandler = value;
            }
        }

        public bool IsFaultSource
        {
            get
            {
                return _isFaultSource;
            }
            private set
            {
                _isFaultSource = value;
            }
        }

        public Exception Fault
        {
            get
            {
                return _fault;
            }
            private set
            {
                _fault = value;
            }
        }

        [DataMember(Name = "FaultSource")]
        internal ActivityInfo SerializedFaultSource
        {
            get { return this.FaultSource; }
            set { this.FaultSource = value; }
        }

        [DataMember(Name = "FaultHandler")]
        internal ActivityInfo SerializedFaultHandler
        {
            get { return this.FaultHandler; }
            set { this.FaultHandler = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "IsFaultSource")]
        internal bool SerializedIsFaultSource
        {
            get { return this.IsFaultSource; }
            set { this.IsFaultSource = value; }
        }

        [DataMember(Name = "Fault")]
        internal Exception SerializedFault
        {
            get { return this.Fault; }
            set { this.Fault = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new FaultPropagationRecord(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "FaultPropagationRecord {{ {0}, FaultSource {{ {1} }}, FaultHandler {{ {2} }}, IsFaultSource = {3} }}",
                base.ToString(),
                this.FaultSource.ToString(),
                this.FaultHandler != null ? this.FaultHandler.ToString() : "<null>",
                this.IsFaultSource);
        }
    }
}

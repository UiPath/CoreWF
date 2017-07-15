// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class WorkflowInstanceTerminatedRecord : WorkflowInstanceRecord
    {
        private string _reason;

        public WorkflowInstanceTerminatedRecord(Guid instanceId, string activityDefinitionId, string reason)
            : base(instanceId, activityDefinitionId, WorkflowInstanceStates.Terminated)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("reason");
            }
            this.Reason = reason;
            this.Level = EventLevel.Error;
        }

        public WorkflowInstanceTerminatedRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string reason)
            : base(instanceId, recordNumber, activityDefinitionId, WorkflowInstanceStates.Terminated)
        {
            if (string.IsNullOrEmpty(reason))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("reason");
            }

            this.Reason = reason;
            this.Level = EventLevel.Error;
        }

        public WorkflowInstanceTerminatedRecord(Guid instanceId, string activityDefinitionId, string reason, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, activityDefinitionId, reason)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        public WorkflowInstanceTerminatedRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string reason, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, recordNumber, activityDefinitionId, reason)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        private WorkflowInstanceTerminatedRecord(WorkflowInstanceTerminatedRecord record)
            : base(record)
        {
            this.Reason = record.Reason;
        }

        public string Reason
        {
            get
            {
                return _reason;
            }
            private set
            {
                _reason = value;
            }
        }

        [DataMember(Name = "Reason")]
        internal string SerializedReason
        {
            get { return this.Reason; }
            set { this.Reason = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new WorkflowInstanceTerminatedRecord(this);
        }

        public override string ToString()
        {
            // For backward compatibility, the ToString() does not return 
            // WorkflowIdentity, if it is null.
            if (this.WorkflowDefinitionIdentity == null)
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceTerminatedRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4} }} ",
                    this.InstanceId,
                    this.RecordNumber,
                    this.EventTime,
                    this.ActivityDefinitionId,
                    this.Reason);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceTerminatedRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, WorkflowDefinitionIdentity = {5} }} ",
                    this.InstanceId,
                    this.RecordNumber,
                    this.EventTime,
                    this.ActivityDefinitionId,
                    this.Reason,
                    this.WorkflowDefinitionIdentity);
            }
        }
    }
}

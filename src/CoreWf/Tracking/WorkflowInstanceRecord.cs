// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public class WorkflowInstanceRecord : TrackingRecord
    {
        private WorkflowIdentity _workflowDefinitionIdentity;
        private string _state;
        private string _activityDefinitionId;

        public WorkflowInstanceRecord(Guid instanceId, string activityDefinitionId, string state)
            : base(instanceId)
        {
            if (string.IsNullOrEmpty(activityDefinitionId))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
            }
            if (string.IsNullOrEmpty(state))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(state));
            }
            this.ActivityDefinitionId = activityDefinitionId;
            this.State = state;
        }

        public WorkflowInstanceRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string state)
            : base(instanceId, recordNumber)
        {
            if (string.IsNullOrEmpty(activityDefinitionId))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
            }
            if (string.IsNullOrEmpty(state))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(state));
            }
            this.ActivityDefinitionId = activityDefinitionId;
            this.State = state;
        }

        public WorkflowInstanceRecord(Guid instanceId, string activityDefinitionId, string state, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, activityDefinitionId, state)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        public WorkflowInstanceRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string state, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, recordNumber, activityDefinitionId, state)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        protected WorkflowInstanceRecord(WorkflowInstanceRecord record)
            : base(record)
        {
            this.ActivityDefinitionId = record.ActivityDefinitionId;
            this.State = record.State;
            this.WorkflowDefinitionIdentity = record.WorkflowDefinitionIdentity;
        }

        public WorkflowIdentity WorkflowDefinitionIdentity
        {
            get
            {
                return _workflowDefinitionIdentity;
            }
            protected set
            {
                _workflowDefinitionIdentity = value;
            }
        }

        public string State
        {
            get
            {
                return _state;
            }
            private set
            {
                _state = value;
            }
        }

        public string ActivityDefinitionId
        {
            get
            {
                return _activityDefinitionId;
            }
            private set
            {
                _activityDefinitionId = value;
            }
        }

        [DataMember(Name = "WorkflowDefinitionIdentity")]
        internal WorkflowIdentity SerializedWorkflowDefinitionIdentity
        {
            get { return this.WorkflowDefinitionIdentity; }
            set { this.WorkflowDefinitionIdentity = value; }
        }

        [DataMember(Name = "State")]
        internal string SerializedState
        {
            get { return this.State; }
            set { this.State = value; }
        }

        [DataMember(Name = "ActivityDefinitionId")]
        internal string SerializedActivityDefinitionId
        {
            get { return this.ActivityDefinitionId; }
            set { this.ActivityDefinitionId = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new WorkflowInstanceRecord(this);
        }

        public override string ToString()
        {
            // For backward compatibility, the ToString() does not return 
            // WorkflowIdentity, if it is null.
            if (this.WorkflowDefinitionIdentity == null)
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceRecord {{ {0}, ActivityDefinitionId = {1}, State = {2} }}",
                    base.ToString(),
                    this.ActivityDefinitionId,
                    this.State);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceRecord {{ {0}, ActivityDefinitionId = {1}, State = {2}, WorkflowDefinitionIdentity = {3} }}",
                    base.ToString(),
                    this.ActivityDefinitionId,
                    this.State,
                    this.WorkflowDefinitionIdentity);
            }
        }
    }
}

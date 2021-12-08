// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;

namespace System.Activities.Tracking;

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
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
        }
        if (string.IsNullOrEmpty(state))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(state));
        }
        ActivityDefinitionId = activityDefinitionId;
        State = state;
    }

    public WorkflowInstanceRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string state)
        : base(instanceId, recordNumber)
    {
        if (string.IsNullOrEmpty(activityDefinitionId))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
        }
        if (string.IsNullOrEmpty(state))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(state));
        }
        ActivityDefinitionId = activityDefinitionId;
        State = state;
    }

    public WorkflowInstanceRecord(Guid instanceId, string activityDefinitionId, string state, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, activityDefinitionId, state)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    public WorkflowInstanceRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string state, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, recordNumber, activityDefinitionId, state)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    protected WorkflowInstanceRecord(WorkflowInstanceRecord record)
        : base(record)
    {
        ActivityDefinitionId = record.ActivityDefinitionId;
        State = record.State;
        WorkflowDefinitionIdentity = record.WorkflowDefinitionIdentity;
    }

    public WorkflowIdentity WorkflowDefinitionIdentity
    {
        get => _workflowDefinitionIdentity;
        protected set => _workflowDefinitionIdentity = value;
    }

    public string State
    {
        get => _state;
        private set => _state = value;
    }

    public string ActivityDefinitionId
    {
        get => _activityDefinitionId;
        private set => _activityDefinitionId = value;
    }

    [DataMember(Name = "WorkflowDefinitionIdentity")]
    internal WorkflowIdentity SerializedWorkflowDefinitionIdentity
    {
        get => WorkflowDefinitionIdentity;
        set => WorkflowDefinitionIdentity = value;
    }

    [DataMember(Name = "State")]
    internal string SerializedState
    {
        get => State;
        set => State = value;
    }

    [DataMember(Name = "ActivityDefinitionId")]
    internal string SerializedActivityDefinitionId
    {
        get => ActivityDefinitionId;
        set => ActivityDefinitionId = value;
    }

    protected internal override TrackingRecord Clone() => new WorkflowInstanceRecord(this);

    public override string ToString()
    {
        // For backward compatibility, the ToString() does not return 
        // WorkflowIdentity, if it is null.
        if (WorkflowDefinitionIdentity == null)
        {
            return string.Format(CultureInfo.CurrentCulture,
                "WorkflowInstanceRecord {{ {0}, ActivityDefinitionId = {1}, State = {2} }}",
                base.ToString(),
                ActivityDefinitionId,
                State);
        }
        else
        {
            return string.Format(CultureInfo.CurrentCulture,
                "WorkflowInstanceRecord {{ {0}, ActivityDefinitionId = {1}, State = {2}, WorkflowDefinitionIdentity = {3} }}",
                base.ToString(),
                ActivityDefinitionId,
                State,
                WorkflowDefinitionIdentity);
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class WorkflowInstanceSuspendedRecord : WorkflowInstanceRecord
{
    private string _reason;

    public WorkflowInstanceSuspendedRecord(Guid instanceId, string activityDefinitionId, string reason)
        : base(instanceId, activityDefinitionId, WorkflowInstanceStates.Suspended)
    {
        if (string.IsNullOrEmpty(reason))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(reason));
        }
        Reason = reason;
    }

    public WorkflowInstanceSuspendedRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string reason)
        : base(instanceId, recordNumber, activityDefinitionId, WorkflowInstanceStates.Suspended)
    {
        if (string.IsNullOrEmpty(reason))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(reason));
        }

        Reason = reason;
    }

    public WorkflowInstanceSuspendedRecord(Guid instanceId, string activityDefinitionId, string reason, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, activityDefinitionId, reason)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    public WorkflowInstanceSuspendedRecord(Guid instanceId, long recordNumber, string activityDefinitionId, string reason, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, recordNumber, activityDefinitionId, reason)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    private WorkflowInstanceSuspendedRecord(WorkflowInstanceSuspendedRecord record)
        : base(record)
    {
        Reason = record.Reason;
    }

    public string Reason
    {
        get => _reason;
        private set => _reason = value;
    }

    [DataMember(Name = "Reason")]
    internal string SerializedReason
    {
        get => Reason;
        set => Reason = value;
    }

    protected internal override TrackingRecord Clone() => new WorkflowInstanceSuspendedRecord(this);

    public override string ToString()
    {
        // For backward compatibility, the ToString() does not return 
        // WorkflowIdentity, if it is null.
        if (WorkflowDefinitionIdentity == null)
        {
            return string.Format(CultureInfo.CurrentCulture,
            "WorkflowInstanceSuspendedRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4} }} ",
            InstanceId,
            RecordNumber,
            EventTime,
            ActivityDefinitionId,
            Reason);
        }
        else
        {
            return string.Format(CultureInfo.CurrentCulture,
            "WorkflowInstanceSuspendedRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, WorkflowDefinitionIdentity = {5} }} ",
            InstanceId,
            RecordNumber,
            EventTime,
            ActivityDefinitionId,
            Reason,
            WorkflowDefinitionIdentity);
        }
    }
}

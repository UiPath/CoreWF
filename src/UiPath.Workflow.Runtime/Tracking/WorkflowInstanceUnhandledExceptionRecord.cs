// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class WorkflowInstanceUnhandledExceptionRecord : WorkflowInstanceRecord
{
    private Exception _unhandledException;
    private ActivityInfo _faultSource;

    public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, string activityDefinitionId, ActivityInfo faultSource, Exception exception)
        : this(instanceId, 0, activityDefinitionId, faultSource, exception) { }

    public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, long recordNumber, string activityDefinitionId, ActivityInfo faultSource, Exception exception)
        : base(instanceId, recordNumber, activityDefinitionId, WorkflowInstanceStates.UnhandledException)
    {
        if (string.IsNullOrEmpty(activityDefinitionId))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
        }

        FaultSource = faultSource ?? throw FxTrace.Exception.ArgumentNull(nameof(faultSource));
        UnhandledException = exception ?? throw FxTrace.Exception.ArgumentNull(nameof(exception));
        Level = EventLevel.Error;
    }

    public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, string activityDefinitionId, ActivityInfo faultSource, Exception exception, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, activityDefinitionId, faultSource, exception)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, long recordNumber, string activityDefinitionId, ActivityInfo faultSource, Exception exception, WorkflowIdentity workflowDefinitionIdentity)
        : this(instanceId, recordNumber, activityDefinitionId, faultSource, exception)
    {
        WorkflowDefinitionIdentity = workflowDefinitionIdentity;
    }

    private WorkflowInstanceUnhandledExceptionRecord(WorkflowInstanceUnhandledExceptionRecord record)
        : base(record)
    {
        FaultSource = record.FaultSource;
        UnhandledException = record.UnhandledException;
    }

    public Exception UnhandledException
    {
        get => _unhandledException;
        private set => _unhandledException = value;
    }

    public ActivityInfo FaultSource
    {
        get => _faultSource;
        private set => _faultSource = value;
    }

    [DataMember(Name = "UnhandledException")]
    internal Exception SerializedUnhandledException
    {
        get => UnhandledException;
        set => UnhandledException = value;
    }

    [DataMember(Name = "FaultSource")]
    internal ActivityInfo SerializedFaultSource
    {
        get => FaultSource;
        set => FaultSource = value;
    }

    protected internal override TrackingRecord Clone() => new WorkflowInstanceUnhandledExceptionRecord(this);

    public override string ToString()
    {
        // For backward compatibility, the ToString() does not return 
        // WorkflowIdentity, if it is null.
        if (WorkflowDefinitionIdentity == null)
        {
            return string.Format(CultureInfo.CurrentCulture,
                "WorkflowInstanceUnhandledExceptionRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, FaultSource {{ {4} }}, UnhandledException = {5} }} ",
                InstanceId,
                RecordNumber,
                EventTime,
                ActivityDefinitionId,
                FaultSource.ToString(),
                UnhandledException);
        }
        else
        {
            return string.Format(CultureInfo.CurrentCulture,
                "WorkflowInstanceUnhandledExceptionRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, FaultSource {{ {4} }}, UnhandledException = {5}, WorkflowDefinitionIdentity = {6} }} ",
                InstanceId,
                RecordNumber,
                EventTime,
                ActivityDefinitionId,
                FaultSource.ToString(),
                UnhandledException,
                WorkflowDefinitionIdentity);
        }
    }
}

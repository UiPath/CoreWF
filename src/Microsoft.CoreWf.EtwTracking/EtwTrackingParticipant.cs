// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System;
using Microsoft.CoreWf.Tracking;
using System.Diagnostics.Tracing;

namespace Microsoft.CoreWf.EtwTracking
{
    public sealed class EtwTrackingParticipant : TrackingParticipant
    {
        private const string truncatedItemsTag = "<items>...</items>";
        private const string emptyItemsTag = "<items />";
        private const string itemsTag = "items";
        private const string itemTag = "item";
        private const string nameAttribute = "name";
        private const string typeAttribute = "type";

        public EtwTrackingParticipant()
        {
            this.ApplicationReference = string.Empty;
        }

        public string ApplicationReference
        {
            get;

            set;
        }

        protected override IAsyncResult BeginTrack(TrackingRecord record, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Track(record, timeout);
            return new EtwTrackingAsyncResult(callback, state);
        }

        protected override void EndTrack(IAsyncResult result)
        {
            EtwTrackingAsyncResult.End(result as EtwTrackingAsyncResult);
        }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            if (record is ActivityStateRecord)
            {
                TrackActivityRecord((ActivityStateRecord)record);
            }
            else if (record is WorkflowInstanceRecord)
            {
                TrackWorkflowRecord((WorkflowInstanceRecord)record);
            }
            else if (record is BookmarkResumptionRecord)
            {
                TrackBookmarkRecord((BookmarkResumptionRecord)record);
            }
            else if (record is ActivityScheduledRecord)
            {
                TrackActivityScheduledRecord((ActivityScheduledRecord)record);
            }
            else if (record is CancelRequestedRecord)
            {
                TrackCancelRequestedRecord((CancelRequestedRecord)record);
            }
            else if (record is FaultPropagationRecord)
            {
                TrackFaultPropagationRecord((FaultPropagationRecord)record);
            }
            else if (record is CustomTrackingRecord)
            {
                TrackCustomRecord((CustomTrackingRecord)record);
            }
            else
            {
                throw new PlatformNotSupportedException(Resources.UnrecognizedTrackingRecord(record?.GetType().Name));
            }
        }

        private void TrackActivityRecord(ActivityStateRecord record)
        {
            if (WfEtwTrackingEventSource.Instance.ActivityStateRecordIsEnabled())
            {
                WfEtwTrackingEventSource.Instance.ActivityStateRecord(record.InstanceId,
                    record.RecordNumber, record.EventTime, record.State,
                    record.Activity.Name, record.Activity.Id, record.Activity.InstanceId, record.Activity.TypeName,
                    record.Arguments.Count > 0 ? JsonConvert.SerializeObject(record.Arguments, Formatting.Indented) : emptyItemsTag,
                    record.Variables.Count > 0 ? JsonConvert.SerializeObject(record.Variables, Formatting.Indented) : emptyItemsTag,
                    record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                    this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
            }
        }

        private void TrackActivityScheduledRecord(ActivityScheduledRecord scheduledRecord)
        {
            if (WfEtwTrackingEventSource.Instance.ActivityScheduledRecordIsEnabled())
            {
                WfEtwTrackingEventSource.Instance.ActivityScheduledRecord(scheduledRecord.InstanceId,
                    scheduledRecord.RecordNumber,
                    scheduledRecord.EventTime,
                    scheduledRecord.Activity == null ? string.Empty : scheduledRecord.Activity.Name,
                    scheduledRecord.Activity == null ? string.Empty : scheduledRecord.Activity.Id,
                    scheduledRecord.Activity == null ? string.Empty : scheduledRecord.Activity.InstanceId,
                    scheduledRecord.Activity == null ? string.Empty : scheduledRecord.Activity.TypeName,
                    scheduledRecord.Child.Name, scheduledRecord.Child.Id, scheduledRecord.Child.InstanceId, scheduledRecord.Child.TypeName,
                    scheduledRecord.HasAnnotations ? JsonConvert.SerializeObject(scheduledRecord.Annotations, Formatting.Indented) : emptyItemsTag,
                    this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
            }
        }

        private void TrackCancelRequestedRecord(CancelRequestedRecord cancelRecord)
        {
            if (WfEtwTrackingEventSource.Instance.CancelRequestedRecordIsEnabled())
            {
                WfEtwTrackingEventSource.Instance.CancelRequestedRecord(cancelRecord.InstanceId,
                    cancelRecord.RecordNumber,
                    cancelRecord.EventTime,
                    cancelRecord.Activity == null ? string.Empty : cancelRecord.Activity.Name,
                    cancelRecord.Activity == null ? string.Empty : cancelRecord.Activity.Id,
                    cancelRecord.Activity == null ? string.Empty : cancelRecord.Activity.InstanceId,
                    cancelRecord.Activity == null ? string.Empty : cancelRecord.Activity.TypeName,
                    cancelRecord.Child.Name, cancelRecord.Child.Id, cancelRecord.Child.InstanceId, cancelRecord.Child.TypeName,
                    cancelRecord.HasAnnotations ? JsonConvert.SerializeObject(cancelRecord.Annotations, Formatting.Indented) : emptyItemsTag,
                    this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
            }
        }

        private void TrackFaultPropagationRecord(FaultPropagationRecord faultRecord)
        {
            if (WfEtwTrackingEventSource.Instance.FaultPropagationRecordIsEnabled())
            {
                WfEtwTrackingEventSource.Instance.FaultPropagationRecord(faultRecord.InstanceId,
                    faultRecord.RecordNumber,
                    faultRecord.EventTime,
                    faultRecord.FaultSource.Name, faultRecord.FaultSource.Id, faultRecord.FaultSource.InstanceId, faultRecord.FaultSource.TypeName,
                    faultRecord.FaultHandler != null ? faultRecord.FaultHandler.Name : string.Empty,
                    faultRecord.FaultHandler != null ? faultRecord.FaultHandler.Id : string.Empty,
                    faultRecord.FaultHandler != null ? faultRecord.FaultHandler.InstanceId : string.Empty,
                    faultRecord.FaultHandler != null ? faultRecord.FaultHandler.TypeName : string.Empty,
                    faultRecord.Fault.ToString(), faultRecord.IsFaultSource,
                    faultRecord.HasAnnotations ? JsonConvert.SerializeObject(faultRecord.Annotations, Formatting.Indented) : emptyItemsTag,
                    this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
            }
        }

        private void TrackBookmarkRecord(BookmarkResumptionRecord record)
        {
            if (WfEtwTrackingEventSource.Instance.BookmarkResumptionRecordIsEnabled())
            {
                WfEtwTrackingEventSource.Instance.BookmarkResumptionRecord(record.InstanceId, record.RecordNumber, record.EventTime,
                    record.BookmarkName, record.BookmarkScope, record.Owner.Name, record.Owner.Id,
                    record.Owner.InstanceId, record.Owner.TypeName,
                    record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                    this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
            }
        }

        private void TrackCustomRecord(CustomTrackingRecord record)
        {
            switch (record.Level)
            {
                case EventLevel.Error:
                    if (WfEtwTrackingEventSource.Instance.CustomTrackingRecordErrorIsEnabled())
                    {
                        WfEtwTrackingEventSource.Instance.CustomTrackingRecordError(record.InstanceId,
                            record.RecordNumber, record.EventTime, record.Name,
                            record.Activity.Name, record.Activity.Id, record.Activity.InstanceId, record.Activity.TypeName,
                            JsonConvert.SerializeObject(record.Data, Formatting.Indented),
                            record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                            this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                    }
                    break;
                case EventLevel.Warning:
                    if (WfEtwTrackingEventSource.Instance.CustomTrackingRecordWarningIsEnabled())
                    {
                        WfEtwTrackingEventSource.Instance.CustomTrackingRecordWarning(record.InstanceId,
                            record.RecordNumber, record.EventTime, record.Name,
                            record.Activity.Name, record.Activity.Id, record.Activity.InstanceId, record.Activity.TypeName,
                            JsonConvert.SerializeObject(record.Data, Formatting.Indented),
                            record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                            this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                    }
                    break;

                default:
                    if (WfEtwTrackingEventSource.Instance.CustomTrackingRecordInfoIsEnabled())
                    {
                        WfEtwTrackingEventSource.Instance.CustomTrackingRecordInfo(record.InstanceId,
                            record.RecordNumber, record.EventTime, record.Name,
                            record.Activity.Name, record.Activity.Id, record.Activity.InstanceId, record.Activity.TypeName,
                            JsonConvert.SerializeObject(record.Data, Formatting.Indented),
                            record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                            this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                    }
                    break;
            }
        }

        private void TrackWorkflowRecord(WorkflowInstanceRecord record)
        {
            // In the TrackWorkflowInstance*Record methods below there are two code paths.
            // If the WorkflowIdentity is null, then we follow the exisiting 4.0 path.
            // If the WorkflowIdentity is provided, then if a particular field in the workflowInstance 
            // record is null, we need to ensure that we are passing string.Empty.
            // The WriteEvent method on the DiagnosticEventProvider which is called in the 
            // WriteEtwEvent in the EtwTrackingParticipantRecords class invokes the EventWrite
            // native method which relies on getting the record arguments in a particular order.
            if (record is WorkflowInstanceUnhandledExceptionRecord)
            {
                TrackWorkflowInstanceUnhandledExceptionRecord(record);
            }
            else if (record is WorkflowInstanceAbortedRecord)
            {
                TrackWorkflowInstanceAbortedRecord(record);
            }
            else if (record is WorkflowInstanceSuspendedRecord)
            {
                TrackWorkflowInstanceSuspendedRecord(record);
            }
            else if (record is WorkflowInstanceTerminatedRecord)
            {
                TrackWorkflowInstanceTerminatedRecord(record);
            }
            else
            {
                TrackWorkflowInstanceRecord(record);
            }
        }

        private void TrackWorkflowInstanceUnhandledExceptionRecord(WorkflowInstanceRecord record)
        {
            WorkflowInstanceUnhandledExceptionRecord unhandled = record as WorkflowInstanceUnhandledExceptionRecord;
            if (unhandled.WorkflowDefinitionIdentity == null)
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceUnhandledExceptionRecordIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceUnhandledExceptionRecord(unhandled.InstanceId,
                        unhandled.RecordNumber, unhandled.EventTime, unhandled.ActivityDefinitionId,
                        unhandled.FaultSource.Name, unhandled.FaultSource.Id, unhandled.FaultSource.InstanceId, unhandled.FaultSource.TypeName,
                        unhandled.UnhandledException == null ? string.Empty : unhandled.UnhandledException.ToString(),
                        unhandled.HasAnnotations ? JsonConvert.SerializeObject(unhandled.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                }
            }
            else
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceUnhandledExceptionRecordWithIdIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceUnhandledExceptionRecordWithId(unhandled.InstanceId,
                        unhandled.RecordNumber, unhandled.EventTime, unhandled.ActivityDefinitionId,
                        unhandled.FaultSource.Name, unhandled.FaultSource.Id, unhandled.FaultSource.InstanceId, unhandled.FaultSource.TypeName,
                        unhandled.UnhandledException == null ? string.Empty : unhandled.UnhandledException.ToString(),
                        unhandled.HasAnnotations ? JsonConvert.SerializeObject(unhandled.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name == null ? string.Empty : this.TrackingProfile.Name,
                        unhandled.WorkflowDefinitionIdentity.ToString(), this.ApplicationReference);
                }
            }
        }

        private void TrackWorkflowInstanceAbortedRecord(WorkflowInstanceRecord record)
        {
            WorkflowInstanceAbortedRecord aborted = record as WorkflowInstanceAbortedRecord;
            if (aborted.WorkflowDefinitionIdentity == null)
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceAbortedRecordIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceAbortedRecord(aborted.InstanceId, aborted.RecordNumber,
                        aborted.EventTime, aborted.ActivityDefinitionId, aborted.Reason,
                        aborted.HasAnnotations ? JsonConvert.SerializeObject(aborted.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                }
            }
            else
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceAbortedRecordWithIdIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceAbortedRecordWithId(aborted.InstanceId, aborted.RecordNumber,
                        aborted.EventTime, aborted.ActivityDefinitionId, aborted.Reason,
                        aborted.HasAnnotations ? JsonConvert.SerializeObject(aborted.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name == null ? string.Empty : this.TrackingProfile.Name,
                        aborted.WorkflowDefinitionIdentity.ToString(), this.ApplicationReference);
                }
            }
        }

        private void TrackWorkflowInstanceSuspendedRecord(WorkflowInstanceRecord record)
        {
            WorkflowInstanceSuspendedRecord suspended = record as WorkflowInstanceSuspendedRecord;
            if (suspended.WorkflowDefinitionIdentity == null)
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceSuspendedRecordIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceSuspendedRecord(suspended.InstanceId, suspended.RecordNumber,
                        suspended.EventTime, suspended.ActivityDefinitionId, suspended.Reason,
                        suspended.HasAnnotations ? JsonConvert.SerializeObject(suspended.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                }
            }
            else
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceSuspendedRecordWithIdIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceSuspendedRecordWithId(suspended.InstanceId, suspended.RecordNumber,
                        suspended.EventTime, suspended.ActivityDefinitionId, suspended.Reason,
                        suspended.HasAnnotations ? JsonConvert.SerializeObject(suspended.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name == null ? string.Empty : this.TrackingProfile.Name,
                        suspended.WorkflowDefinitionIdentity.ToString(), this.ApplicationReference);
                }
            }
        }

        private void TrackWorkflowInstanceTerminatedRecord(WorkflowInstanceRecord record)
        {
            WorkflowInstanceTerminatedRecord terminated = record as WorkflowInstanceTerminatedRecord;
            if (terminated.WorkflowDefinitionIdentity == null)
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceTerminatedRecordIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceTerminatedRecord(terminated.InstanceId, terminated.RecordNumber,
                        terminated.EventTime, terminated.ActivityDefinitionId, terminated.Reason,
                        terminated.HasAnnotations ? JsonConvert.SerializeObject(terminated.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                }
            }
            else
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceTerminatedRecordWithIdIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceTerminatedRecordWithId(terminated.InstanceId, terminated.RecordNumber,
                        terminated.EventTime, terminated.ActivityDefinitionId, terminated.Reason,
                        terminated.HasAnnotations ? JsonConvert.SerializeObject(terminated.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name == null ? string.Empty : this.TrackingProfile.Name,
                        terminated.WorkflowDefinitionIdentity.ToString(), this.ApplicationReference);
                }
            }
        }

        private void TrackWorkflowInstanceRecord(WorkflowInstanceRecord record)
        {
            if (record.WorkflowDefinitionIdentity == null)
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceRecordIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceRecord(record.InstanceId, record.RecordNumber,
                        record.EventTime, record.ActivityDefinitionId,
                        record.State,
                        record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name, this.ApplicationReference);
                }
            }
            else
            {
                if (WfEtwTrackingEventSource.Instance.WorkflowInstanceRecordWithIdIsEnabled())
                {
                    WfEtwTrackingEventSource.Instance.WorkflowInstanceRecordWithId(record.InstanceId, record.RecordNumber,
                        record.EventTime, record.ActivityDefinitionId,
                        record.State,
                        record.HasAnnotations ? JsonConvert.SerializeObject(record.Annotations, Formatting.Indented) : emptyItemsTag,
                        this.TrackingProfile == null ? string.Empty : this.TrackingProfile.Name == null ? string.Empty : this.TrackingProfile.Name,
                        record.WorkflowDefinitionIdentity.ToString(), this.ApplicationReference);
                }
            }
        }
    }
}

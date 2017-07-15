// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using Keywords = CoreWf.WfEventSource.Keywords;
using Opcodes = CoreWf.WfEventSource.Opcodes;
using Tasks = CoreWf.WfEventSource.Tasks;

namespace CoreWf.EtwTracking
{
    [EventSource(Name = "WF ETW Tracking Participant", Guid = "71346678-F84B-45B6-AD09-36658D8239CE", LocalizationResources = "CoreWf.Strings.EventSource")]
    public sealed class WfEtwTrackingEventSource : EventSource
    {
        public static WfEtwTrackingEventSource Instance = new WfEtwTrackingEventSource();

        public bool WorkflowInstanceRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord= WorkflowInstanceRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, State = {4}, Annotations = {5}, ProfileName = {6}")]
        public void WorkflowInstanceRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string State, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceRecord, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, State, Annotations, ProfileName, HostReference);
        }

        public bool WorkflowInstanceUnhandledExceptionRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceUnhandledExceptionRecord, Level = EventLevel.Error, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordUnhandledExceptionRecord, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceUnhandledExceptionRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, SourceName = {4}, SourceId = {5}, SourceInstanceId = {6}, SourceTypeName={7}, Exception={8}, Annotations= {9}, ProfileName = {10}")]
        public void WorkflowInstanceUnhandledExceptionRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string SourceName, string SourceId, string SourceInstanceId, string SourceTypeName, string Exception, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceUnhandledExceptionRecord, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, SourceName, SourceId, SourceInstanceId, SourceTypeName, Exception, Annotations, ProfileName, HostReference);
        }

        public bool WorkflowInstanceAbortedRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceAbortedRecord, Level = EventLevel.Warning, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordAbortedRecord, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceAbortedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, Annotations = {5}, ProfileName = {6}")]
        public void WorkflowInstanceAbortedRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceAbortedRecord, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, HostReference);
        }

        public bool ActivityStateRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.ActivityStateRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowTracking,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = ActivityStateRecord, InstanceID = {0}, RecordNumber={1}, EventTime={2}, State = {3}, Name={4}, ActivityId={5}, ActivityInstanceId={6}, ActivityTypeName={7}, Arguments={8}, Variables={9}, Annotations={10}, ProfileName = {11}")]
        public void ActivityStateRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string State, string Name, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string Arguments, string Variables, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.ActivityStateRecord, InstanceId, RecordNumber, EventTime, State, Name, ActivityId, ActivityInstanceId, ActivityTypeName, Arguments, Variables, Annotations, ProfileName, HostReference);
        }

        public bool ActivityScheduledRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.ActivityScheduledRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowTracking,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = ActivityScheduledRecord, InstanceID = {0},  RecordNumber = {1}, EventTime = {2}, Name = {3}, ActivityId = {4}, ActivityInstanceId = {5}, ActivityTypeName = {6}, ChildActivityName = {7}, ChildActivityId = {8}, ChildActivityInstanceId = {9}, ChildActivityTypeName ={10}, Annotations={11}, ProfileName = {12}")]
        public void ActivityScheduledRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string ChildActivityName, string ChildActivityId, string ChildActivityInstanceId, string ChildActivityTypeName, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.ActivityScheduledRecord, InstanceId, RecordNumber, EventTime, Name, ActivityId, ActivityInstanceId, ActivityTypeName, ChildActivityName, ChildActivityId, ChildActivityInstanceId, ChildActivityTypeName, Annotations, ProfileName, HostReference);
        }

        public bool FaultPropagationRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.FaultPropagationRecord, Level = EventLevel.Warning, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowTracking,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = FaultPropagationRecord, InstanceID={0}, RecordNumber={1}, EventTime={2}, FaultSourceActivityName={3}, FaultSourceActivityId={4}, FaultSourceActivityInstanceId={5}, FaultSourceActivityTypeName={6}, FaultHandlerActivityName={7},  FaultHandlerActivityId = {8}, FaultHandlerActivityInstanceId ={9}, FaultHandlerActivityTypeName={10}, Fault={11}, IsFaultSource={12}, Annotations={13}, ProfileName = {14}")]
        public void FaultPropagationRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string FaultSourceActivityName, string FaultSourceActivityId, string FaultSourceActivityInstanceId, string FaultSourceActivityTypeName, string FaultHandlerActivityName, string FaultHandlerActivityId, string FaultHandlerActivityInstanceId, string FaultHandlerActivityTypeName, string Fault, bool IsFaultSource, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.FaultPropagationRecord, InstanceId, RecordNumber, EventTime, FaultSourceActivityName, FaultSourceActivityId, FaultSourceActivityInstanceId, FaultSourceActivityTypeName, FaultHandlerActivityName, FaultHandlerActivityId, FaultHandlerActivityInstanceId, FaultHandlerActivityTypeName, Fault, IsFaultSource, Annotations, ProfileName, HostReference);
        }

        public bool CancelRequestedRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.CancelRequestedRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowTracking,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = CancelRequestedRecord, InstanceID={0}, RecordNumber={1}, EventTime={2}, Name={3}, ActivityId={4}, ActivityInstanceId={5}, ActivityTypeName = {6}, ChildActivityName = {7}, ChildActivityId = {8}, ChildActivityInstanceId = {9}, ChildActivityTypeName ={10}, Annotations={11}, ProfileName = {12}")]
        public void CancelRequestedRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string ChildActivityName, string ChildActivityId, string ChildActivityInstanceId, string ChildActivityTypeName, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.CancelRequestedRecord, InstanceId, RecordNumber, EventTime, Name, ActivityId, ActivityInstanceId, ActivityTypeName, ChildActivityName, ChildActivityId, ChildActivityInstanceId, ChildActivityTypeName, Annotations, ProfileName, HostReference);
        }

        public bool BookmarkResumptionRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.BookmarkResumptionRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowTracking,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = BookmarkResumptionRecord, InstanceID={0}, RecordNumber={1},EventTime={2}, Name={3}, SubInstanceID={4},  OwnerActivityName={5}, OwnerActivityId ={6}, OwnerActivityInstanceId={7}, OwnerActivityTypeName={8}, Annotations={9}, ProfileName = {10}")]
        public void BookmarkResumptionRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, Guid SubInstanceID, string OwnerActivityName, string OwnerActivityId, string OwnerActivityInstanceId, string OwnerActivityTypeName, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.BookmarkResumptionRecord, InstanceId, RecordNumber, EventTime, Name, SubInstanceID, OwnerActivityName, OwnerActivityId, OwnerActivityInstanceId, OwnerActivityTypeName, Annotations, ProfileName, HostReference);
        }

        public bool CustomTrackingRecordInfoIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.CustomTrackingRecordInfo, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.CustomTrackingRecord,
            Keywords = Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = CustomTrackingRecord, InstanceID = {0}, RecordNumber={1}, EventTime={2},  Name={3}, ActivityName={4}, ActivityId={5}, ActivityInstanceId={6}, ActivityTypeName={7}, Data={8}, Annotations={9}, ProfileName = {10}")]
        public void CustomTrackingRecordInfo(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, string ActivityName, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string Data, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.CustomTrackingRecordInfo, InstanceId, RecordNumber, EventTime, Name, ActivityName, ActivityId, ActivityInstanceId, ActivityTypeName, Data, Annotations, ProfileName, HostReference);
        }

        public bool CustomTrackingRecordWarningIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.CustomTrackingRecordWarning, Level = EventLevel.Warning, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.CustomTrackingRecord,
            Keywords = Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = CustomTrackingRecord, InstanceID = {0}, RecordNumber={1}, EventTime={2}, Name={3}, ActivityName={4}, ActivityId={5}, ActivityInstanceId={6}, ActivityTypeName={7}, Data={8}, Annotations={9}, ProfileName = {10}")]
        public void CustomTrackingRecordWarning(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, string ActivityName, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string Data, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.CustomTrackingRecordWarning, InstanceId, RecordNumber, EventTime, Name, ActivityName, ActivityId, ActivityInstanceId, ActivityTypeName, Data, Annotations, ProfileName, HostReference);
        }

        public bool CustomTrackingRecordErrorIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.CustomTrackingRecordError, Level = EventLevel.Error, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.CustomTrackingRecord,
            Keywords = Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = CustomTrackingRecord, InstanceID = {0}, RecordNumber={1}, EventTime={2}, Name={3}, ActivityName={4}, ActivityId={5}, ActivityInstanceId={6}, ActivityTypeName={7}, Data={8}, Annotations={9}, ProfileName = {10}")]
        public void CustomTrackingRecordError(Guid InstanceId, long RecordNumber, DateTime EventTime, string Name, string ActivityName, string ActivityId, string ActivityInstanceId, string ActivityTypeName, string Data, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.CustomTrackingRecordError, InstanceId, RecordNumber, EventTime, Name, ActivityName, ActivityId, ActivityInstanceId, ActivityTypeName, Data, Annotations, ProfileName, HostReference);
        }

        public bool WorkflowInstanceSuspendedRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceSuspendedRecord, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordSuspendedRecord, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceSuspendedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, Annotations = {5}, ProfileName = {6}")]
        public void WorkflowInstanceSuspendedRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceSuspendedRecord, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, HostReference);
        }

        public bool WorkflowInstanceTerminatedRecordIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceTerminatedRecord, Level = EventLevel.Error, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordTerminatedRecord, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.EndToEndMonitoring | Keywords.Troubleshooting | Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceTerminatedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, Annotations = {5}, ProfileName = {6}")]
        public void WorkflowInstanceTerminatedRecord(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceTerminatedRecord, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, HostReference);
        }

        public bool WorkflowInstanceRecordWithIdIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceRecordWithId, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = EventOpcode.Info, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord= WorkflowInstanceRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, State = {4}, Annotations = {5}, ProfileName = {6}, WorkflowDefinitionIdentity = {7}")]
        public void WorkflowInstanceRecordWithId(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string State, string Annotations, string ProfileName, string WorkflowDefinitionIdentity, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceRecordWithId, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, State, Annotations, ProfileName, WorkflowDefinitionIdentity, HostReference);
        }

        public bool WorkflowInstanceAbortedRecordWithIdIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceAbortedRecordWithId, Level = EventLevel.Warning, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordAbortedWithId, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceAbortedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4},  Annotations = {5}, ProfileName = {6}, WorkflowDefinitionIdentity = {7}")]
        public void WorkflowInstanceAbortedRecordWithId(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string WorkflowDefinitionIdentity, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceAbortedRecordWithId, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, WorkflowDefinitionIdentity, HostReference);
        }

        public bool WorkflowInstanceSuspendedRecordWithIdIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceSuspendedRecordWithId, Level = EventLevel.Informational, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordSuspendedWithId, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceSuspendedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4}, Annotations = {5}, ProfileName = {6}, WorkflowDefinitionIdentity = {7}")]
        public void WorkflowInstanceSuspendedRecordWithId(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string WorkflowDefinitionIdentity, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceSuspendedRecordWithId, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, WorkflowDefinitionIdentity, HostReference);
        }

        public bool WorkflowInstanceTerminatedRecordWithIdIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceTerminatedRecordWithId, Level = EventLevel.Error, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordTerminatedWithId, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceTerminatedRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, Reason = {4},  Annotations = {5}, ProfileName = {6}, WorkflowDefinitionIdentity = {7}")]
        public void WorkflowInstanceTerminatedRecordWithId(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string Reason, string Annotations, string ProfileName, string WorkflowDefinitionIdentity, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceTerminatedRecordWithId, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, Reason, Annotations, ProfileName, WorkflowDefinitionIdentity, HostReference);
        }

        public bool WorkflowInstanceUnhandledExceptionRecordWithIdIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.HealthMonitoring | Keywords.WFTracking, EventChannel.Analytic);
        }

        [Event(EventIds.WorkflowInstanceUnhandledExceptionRecordWithId, Level = EventLevel.Error, Channel = EventChannel.Analytic, Opcode = Opcodes.WorkflowInstanceRecordUnhandledExceptionWithId, Task = Tasks.WorkflowInstanceRecord,
            Keywords = Keywords.HealthMonitoring | Keywords.WFTracking,
            Message = "TrackRecord = WorkflowInstanceUnhandledExceptionRecord, InstanceID = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, SourceName = {4}, SourceId = {5}, SourceInstanceId = {6}, SourceTypeName={7}, Exception={8},  Annotations= {9}, ProfileName = {10}, WorkflowDefinitionIdentity = {11}")]
        public void WorkflowInstanceUnhandledExceptionRecordWithId(Guid InstanceId, long RecordNumber, DateTime EventTime, string ActivityDefinitionId, string SourceName, string SourceId, string SourceInstanceId, string SourceTypeName, string Exception, string Annotations, string ProfileName, string WorkflowDefinitionIdentity, string HostReference)
        {
            WriteEvent(EventIds.WorkflowInstanceUnhandledExceptionRecordWithId, InstanceId, RecordNumber, EventTime, ActivityDefinitionId, SourceName, SourceId, SourceInstanceId, SourceTypeName, Exception, Annotations, ProfileName, WorkflowDefinitionIdentity, HostReference);
        }

        public class EventIds
        {
            public const int WorkflowInstanceRecord = 100;
            public const int WorkflowInstanceUnhandledExceptionRecord = 101;
            public const int WorkflowInstanceAbortedRecord = 102;
            public const int ActivityStateRecord = 103;
            public const int ActivityScheduledRecord = 104;
            public const int FaultPropagationRecord = 105;
            public const int CancelRequestedRecord = 106;
            public const int BookmarkResumptionRecord = 107;
            public const int CustomTrackingRecordInfo = 108;
            public const int CustomTrackingRecordWarning = 110;
            public const int CustomTrackingRecordError = 111;
            public const int WorkflowInstanceSuspendedRecord = 112;
            public const int WorkflowInstanceTerminatedRecord = 113;
            public const int WorkflowInstanceRecordWithId = 114;
            public const int WorkflowInstanceAbortedRecordWithId = 115;
            public const int WorkflowInstanceSuspendedRecordWithId = 116;
            public const int WorkflowInstanceTerminatedRecordWithId = 117;
            public const int WorkflowInstanceUnhandledExceptionRecordWithId = 118;
        }
    }
}

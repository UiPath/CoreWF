// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace System.Activities
{
    [EventSource(Name = "Workflow Foundation", Guid = "c651f5f6-1c0d-492e-8ae1-b4efd7c9d503", LocalizationResources = "System.Activities.Resources.EventSourceStrings")]
    public sealed class WfEventSource : EventSource
    {
        public static WfEventSource Instance = new WfEventSource();

        public bool TransferEmittedIsEnabled()
        {
            return base.IsEnabled(EventLevel.LogAlways, Keywords.Troubleshooting | Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.ServiceModel | Keywords.WFTracking | Keywords.ServiceHost | Keywords.WCFMessageLogging, EventChannel.Analytic);
        }

        [Event(EventIds.TransferEmitted, Level = EventLevel.LogAlways, Channel = EventChannel.Analytic,
            Keywords = Keywords.Troubleshooting | Keywords.UserEvents | Keywords.EndToEndMonitoring | Keywords.ServiceModel | Keywords.WFTracking | Keywords.ServiceHost | Keywords.WCFMessageLogging,
            Message = "Transfer event emitted.")]
        public void TransferEmitted(Guid newId)
        {
            WriteEvent(EventIds.TransferEmitted, newId);
        }

        public bool WorkflowApplicationCompletedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationCompleted, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeCompleted, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' has completed in the Closed state.")]
        public void WorkflowApplicationCompleted(string data1)
        {
            WriteEvent(EventIds.WorkflowApplicationCompleted, data1);
        }

        public bool WorkflowApplicationTerminatedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationTerminated, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeTerminated, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowApplication Id: '{0}' was terminated. It has completed in the Faulted state with an exception.")]
        public void WorkflowApplicationTerminated(string data1, string SerializedException)
        {
            WriteEvent(EventIds.WorkflowApplicationTerminated, data1, SerializedException);
        }

        public bool WorkflowInstanceCanceledIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowInstanceCanceled, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeInstanceCanceled, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' has completed in the Canceled state.")]
        public void WorkflowInstanceCanceled(string data1)
        {
            WriteEvent(EventIds.WorkflowInstanceCanceled, data1);
        }

        public bool WorkflowInstanceAbortedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowInstanceAborted, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeInstanceAborted, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' was aborted with an exception.")]
        public void WorkflowInstanceAborted(string data1, string SerializedException)
        {
            WriteEvent(EventIds.WorkflowInstanceAborted, data1, SerializedException);
        }

        public bool WorkflowApplicationIdledIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationIdled, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeIdled, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowApplication Id: '{0}' went idle.")]
        public void WorkflowApplicationIdled(string data1)
        {
            WriteEvent(EventIds.WorkflowApplicationIdled, data1);
        }

        public bool WorkflowApplicationUnhandledExceptionIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationUnhandledException, Level = EventLevel.Error, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeUnhandledException, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.")]
        public void WorkflowApplicationUnhandledException(string data1, string data2, string data3, string data4, string SerializedException)
        {
            WriteEvent(EventIds.WorkflowApplicationUnhandledException, data1, data2, data3, data4, SerializedException);
        }

        public bool WorkflowApplicationPersistedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationPersisted, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangePersisted, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowApplication Id: '{0}' was Persisted.")]
        public void WorkflowApplicationPersisted(string data1)
        {
            WriteEvent(EventIds.WorkflowApplicationPersisted, data1);
        }

        public bool WorkflowApplicationUnloadedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationUnloaded, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangeUnloaded, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' was Unloaded.")]
        public void WorkflowApplicationUnloaded(string data1)
        {
            WriteEvent(EventIds.WorkflowApplicationUnloaded, data1);
        }

        public bool ActivityScheduledIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ActivityScheduled, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.ScheduleActivity,
            Keywords = Keywords.WFRuntime,
            Message = "Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void ActivityScheduled(string data1, string data2, string data3, string data4, string data5, string data6)
        {
            WriteEvent(EventIds.ActivityScheduled, data1, data2, data3, data4, data5, data6);
        }

        public bool ActivityCompletedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ActivityCompleted, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.CompleteActivity,
            Keywords = Keywords.WFRuntime,
            Message = "Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.")]
        public void ActivityCompleted(string data1, string data2, string data3, string data4)
        {
            WriteEvent(EventIds.ActivityCompleted, data1, data2, data3, data4);
        }

        public bool ScheduleExecuteActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleExecuteActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleExecuteActivity, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "An ExecuteActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void ScheduleExecuteActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.ScheduleExecuteActivityWorkItem, data1, data2, data3);
        }

        public bool StartExecuteActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartExecuteActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartExecuteActivity, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of an ExecuteActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void StartExecuteActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.StartExecuteActivityWorkItem, data1, data2, data3);
        }

        public bool CompleteExecuteActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteExecuteActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteExecuteActivity, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "An ExecuteActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void CompleteExecuteActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.CompleteExecuteActivityWorkItem, data1, data2, data3);
        }

        public bool ScheduleCompletionWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleCompletionWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleCompletion, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void ScheduleCompletionWorkItem(string data1, string data2, string data3, string data4, string data5, string data6)
        {
            WriteEvent(EventIds.ScheduleCompletionWorkItem, data1, data2, data3, data4, data5, data6);
        }

        public bool StartCompletionWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartCompletionWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartCompletion, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void StartCompletionWorkItem(string data1, string data2, string data3, string data4, string data5, string data6)
        {
            WriteEvent(EventIds.StartCompletionWorkItem, data1, data2, data3, data4, data5, data6);
        }

        public bool CompleteCompletionWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteCompletionWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteCompletion, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void CompleteCompletionWorkItem(string data1, string data2, string data3, string data4, string data5, string data6)
        {
            WriteEvent(EventIds.CompleteCompletionWorkItem, data1, data2, data3, data4, data5, data6);
        }

        public bool ScheduleCancelActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleCancelActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleCancelActivity, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A CancelActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void ScheduleCancelActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.ScheduleCancelActivityWorkItem, data1, data2, data3);
        }

        public bool StartCancelActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartCancelActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartCancelActivity, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a CancelActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void StartCancelActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.StartCancelActivityWorkItem, data1, data2, data3);
        }

        public bool CompleteCancelActivityWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteCancelActivityWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteCancelActivity, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A CancelActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void CompleteCancelActivityWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.CompleteCancelActivityWorkItem, data1, data2, data3);
        }

        public bool CreateBookmarkIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CreateBookmark, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.CreateBookmark,
            Keywords = Keywords.WFRuntime,
            Message = "A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.")]
        public void CreateBookmark(string data1, string data2, string data3, string data4, string data5)
        {
            WriteEvent(EventIds.CreateBookmark, data1, data2, data3, data4, data5);
        }

        public bool ScheduleBookmarkWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleBookmarkWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleBookmark, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.")]
        public void ScheduleBookmarkWorkItem(string data1, string data2, string data3, string data4, string data5)
        {
            WriteEvent(EventIds.ScheduleBookmarkWorkItem, data1, data2, data3, data4, data5);
        }

        public bool StartBookmarkWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartBookmarkWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartBookmark, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.")]
        public void StartBookmarkWorkItem(string data1, string data2, string data3, string data4, string data5)
        {
            WriteEvent(EventIds.StartBookmarkWorkItem, data1, data2, data3, data4, data5);
        }

        public bool CompleteBookmarkWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteBookmarkWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteBookmark, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.")]
        public void CompleteBookmarkWorkItem(string data1, string data2, string data3, string data4, string data5)
        {
            WriteEvent(EventIds.CompleteBookmarkWorkItem, data1, data2, data3, data4, data5);
        }

        public bool CreateBookmarkScopeIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CreateBookmarkScope, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.CreateBookmark,
            Keywords = Keywords.WFRuntime,
            Message = "A BookmarkScope has been created: {0}.")]
        public void CreateBookmarkScope(string data1)
        {
            WriteEvent(EventIds.CreateBookmarkScope, data1);
        }

        public bool BookmarkScopeInitializedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.BookmarkScopeInitialized, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.InitializeBookmarkScope,
            Keywords = Keywords.WFRuntime,
            Message = "The BookmarkScope that had TemporaryId: '{0}' has been initialized with Id: '{1}'.")]
        public void BookmarkScopeInitialized(string data1, string data2)
        {
            WriteEvent(EventIds.BookmarkScopeInitialized, data1, data2);
        }

        public bool ScheduleTransactionContextWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleTransactionContextWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleTransactionContext, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A TransactionContextWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void ScheduleTransactionContextWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.ScheduleTransactionContextWorkItem, data1, data2, data3);
        }

        public bool StartTransactionContextWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartTransactionContextWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartTransactionContext, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a TransactionContextWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void StartTransactionContextWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.StartTransactionContextWorkItem, data1, data2, data3);
        }

        public bool CompleteTransactionContextWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteTransactionContextWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteTransactionContext, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A TransactionContextWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void CompleteTransactionContextWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.CompleteTransactionContextWorkItem, data1, data2, data3);
        }

        public bool ScheduleFaultWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleFaultWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleFault, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void ScheduleFaultWorkItem(string data1, string data2, string data3, string data4, string data5, string data6, string SerializedException)
        {
            WriteEvent(EventIds.ScheduleFaultWorkItem, data1, data2, data3, data4, data5, data6, SerializedException);
        }

        public bool StartFaultWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartFaultWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartFault, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void StartFaultWorkItem(string data1, string data2, string data3, string data4, string data5, string data6, string SerializedException)
        {
            WriteEvent(EventIds.StartFaultWorkItem, data1, data2, data3, data4, data5, data6, SerializedException);
        }

        public bool CompleteFaultWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteFaultWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteFault, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void CompleteFaultWorkItem(string data1, string data2, string data3, string data4, string data5, string data6, string SerializedException)
        {
            WriteEvent(EventIds.CompleteFaultWorkItem, data1, data2, data3, data4, data5, data6, SerializedException);
        }

        public bool ScheduleRuntimeWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ScheduleRuntimeWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.ScheduleWorkItemScheduleRuntime, Task = Tasks.ScheduleWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A runtime work item has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void ScheduleRuntimeWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.ScheduleRuntimeWorkItem, data1, data2, data3);
        }

        public bool StartRuntimeWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.StartRuntimeWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.StartWorkItemStartRuntime, Task = Tasks.StartWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Starting execution of a runtime work item for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void StartRuntimeWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.StartRuntimeWorkItem, data1, data2, data3);
        }

        public bool CompleteRuntimeWorkItemIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompleteRuntimeWorkItem, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.CompleteWorkItemCompleteRuntime, Task = Tasks.CompleteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "A runtime work item has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.")]
        public void CompleteRuntimeWorkItem(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.CompleteRuntimeWorkItem, data1, data2, data3);
        }

        public bool RuntimeTransactionSetIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFServices, EventChannel.Debug);
        }

        [Event(EventIds.RuntimeTransactionSet, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.RuntimeTransactionSet, Task = Tasks.RuntimeTransaction,
            Keywords = Keywords.WFServices,
            Message = "The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.")]
        public void RuntimeTransactionSet(string data1, string data2, string data3, string data4, string data5, string data6)
        {
            WriteEvent(EventIds.RuntimeTransactionSet, data1, data2, data3, data4, data5, data6);
        }

        public bool RuntimeTransactionCompletionRequestedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFServices, EventChannel.Debug);
        }

        [Event(EventIds.RuntimeTransactionCompletionRequested, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.RuntimeTransactionCompletionRequested, Task = Tasks.RuntimeTransaction,
            Keywords = Keywords.WFServices,
            Message = "Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has scheduled completion of the runtime transaction.")]
        public void RuntimeTransactionCompletionRequested(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.RuntimeTransactionCompletionRequested, data1, data2, data3);
        }

        public bool RuntimeTransactionCompleteIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFServices, EventChannel.Debug);
        }

        [Event(EventIds.RuntimeTransactionComplete, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.RuntimeTransactionComplete, Task = Tasks.RuntimeTransaction,
            Keywords = Keywords.WFServices,
            Message = "The runtime transaction has completed with the state '{0}'.")]
        public void RuntimeTransactionComplete(string data1)
        {
            WriteEvent(EventIds.RuntimeTransactionComplete, data1);
        }

        public bool EnterNoPersistBlockIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.EnterNoPersistBlock, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.NoPersistBlock,
            Keywords = Keywords.WFRuntime,
            Message = "Entering a no persist block.")]
        public void EnterNoPersistBlock()
        {
            WriteEvent(EventIds.EnterNoPersistBlock);
        }

        public bool ExitNoPersistBlockIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ExitNoPersistBlock, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.NoPersistBlock,
            Keywords = Keywords.WFRuntime,
            Message = "Exiting a no persist block.")]
        public void ExitNoPersistBlock()
        {
            WriteEvent(EventIds.ExitNoPersistBlock);
        }

        public bool InArgumentBoundIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.InArgumentBound, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.ExecuteActivity,
            Keywords = Keywords.WFActivities,
            Message = "In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.")]
        public void InArgumentBound(string data1, string data2, string data3, string data4, string data5)
        {
            WriteEvent(EventIds.InArgumentBound, data1, data2, data3, data4, data5);
        }

        public bool WorkflowApplicationPersistableIdleIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowApplicationPersistableIdle, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.WFApplicationStateChangePersistableIdle, Task = Tasks.WFApplicationStateChange,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowApplication Id: '{0}' is idle and persistable.  The following action will be taken: {1}.")]
        public void WorkflowApplicationPersistableIdle(string data1, string data2)
        {
            WriteEvent(EventIds.WorkflowApplicationPersistableIdle, data1, data2);
        }

        public bool WorkflowActivityStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowActivityStart, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Start, Task = Tasks.WorkflowActivity,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' E2E Activity")]
        public void WorkflowActivityStart(Guid Id)
        {
            WriteEvent(EventIds.WorkflowActivityStart, Id);
        }

        public bool WorkflowActivityStopIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowActivityStop, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Stop, Task = Tasks.WorkflowActivity,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' E2E Activity")]
        public void WorkflowActivityStop(Guid Id)
        {
            WriteEvent(EventIds.WorkflowActivityStop, Id);
        }

        public bool WorkflowActivitySuspendIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowActivitySuspend, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Suspend, Task = Tasks.WorkflowActivity,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' E2E Activity")]
        public void WorkflowActivitySuspend(Guid Id)
        {
            WriteEvent(EventIds.WorkflowActivitySuspend, Id);
        }

        public bool WorkflowActivityResumeIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.WorkflowActivityResume, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Resume, Task = Tasks.WorkflowActivity,
            Keywords = Keywords.WFRuntime,
            Message = "WorkflowInstance Id: '{0}' E2E Activity")]
        public void WorkflowActivityResume(Guid Id)
        {
            WriteEvent(EventIds.WorkflowActivityResume, Id);
        }

        public bool InvokeMethodIsStaticIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InvokeMethodIsStatic, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.InvokeMethodIsStatic, Task = Tasks.InvokeMethod,
            Keywords = Keywords.WFRuntime,
            Message = "InvokeMethod '{0}' - method is Static.")]
        public void InvokeMethodIsStatic(string data1)
        {
            WriteEvent(EventIds.InvokeMethodIsStatic, data1);
        }

        public bool InvokeMethodIsNotStaticIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InvokeMethodIsNotStatic, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.InvokeMethodIsNotStatic, Task = Tasks.InvokeMethod,
            Keywords = Keywords.WFRuntime,
            Message = "InvokeMethod '{0}' - method is not Static.")]
        public void InvokeMethodIsNotStatic(string data1)
        {
            WriteEvent(EventIds.InvokeMethodIsNotStatic, data1);
        }

        public bool InvokedMethodThrewExceptionIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InvokedMethodThrewException, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.InvokeMethodThrewException, Task = Tasks.InvokeMethod,
            Keywords = Keywords.WFRuntime,
            Message = "An exception was thrown in the method called by the activity '{0}'. {1}")]
        public void InvokedMethodThrewException(string data1, string data2)
        {
            WriteEvent(EventIds.InvokedMethodThrewException, data1, data2);
        }

        public bool InvokeMethodUseAsyncPatternIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InvokeMethodUseAsyncPattern, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.InvokeMethodUseAsyncPattern, Task = Tasks.InvokeMethod,
            Keywords = Keywords.WFRuntime,
            Message = "InvokeMethod '{0}' - method uses asynchronous pattern of '{1}' and '{2}'.")]
        public void InvokeMethodUseAsyncPattern(string data1, string data2, string data3)
        {
            WriteEvent(EventIds.InvokeMethodUseAsyncPattern, data1, data2, data3);
        }

        public bool InvokeMethodDoesNotUseAsyncPatternIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InvokeMethodDoesNotUseAsyncPattern, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.InvokeMethodDoesNotUseAsyncPattern, Task = Tasks.InvokeMethod,
            Keywords = Keywords.WFRuntime,
            Message = "InvokeMethod '{0}' - method does not use asynchronous pattern.")]
        public void InvokeMethodDoesNotUseAsyncPattern(string data1)
        {
            WriteEvent(EventIds.InvokeMethodDoesNotUseAsyncPattern, data1);
        }

        public bool FlowchartStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartStart, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartBegin, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}' - Start has been scheduled.")]
        public void FlowchartStart(string data1)
        {
            WriteEvent(EventIds.FlowchartStart, data1);
        }

        public bool FlowchartEmptyIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartEmpty, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartEmpty, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}' - was executed with no Nodes.")]
        public void FlowchartEmpty(string data1)
        {
            WriteEvent(EventIds.FlowchartEmpty, data1);
        }

        public bool FlowchartNextNullIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartNextNull, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartNextNull, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}'/FlowStep - Next node is null. Flowchart execution will end.")]
        public void FlowchartNextNull(string data1)
        {
            WriteEvent(EventIds.FlowchartNextNull, data1);
        }

        public bool FlowchartSwitchCaseIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartSwitchCase, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartSwitchCase, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}'/FlowSwitch - Case '{1}' was selected.")]
        public void FlowchartSwitchCase(string data1, string data2)
        {
            WriteEvent(EventIds.FlowchartSwitchCase, data1, data2);
        }

        public bool FlowchartSwitchDefaultIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartSwitchDefault, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartSwitchDefault, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}'/FlowSwitch - Default Case was selected.")]
        public void FlowchartSwitchDefault(string data1)
        {
            WriteEvent(EventIds.FlowchartSwitchDefault, data1);
        }

        public bool FlowchartSwitchCaseNotFoundIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.FlowchartSwitchCaseNotFound, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.ExecuteFlowchartSwitchCaseNotFound, Task = Tasks.ExecuteFlowchart,
            Keywords = Keywords.WFActivities,
            Message = "Flowchart '{0}'/FlowSwitch - could find neither a Case activity nor a Default Case matching the Expression result. Flowchart execution will end.")]
        public void FlowchartSwitchCaseNotFound(string data1)
        {
            WriteEvent(EventIds.FlowchartSwitchCaseNotFound, data1);
        }

        public bool CompensationStateIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.CompensationState, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.CompensationState,
            Keywords = Keywords.WFActivities,
            Message = "CompensableActivity '{0}' is in the '{1}' state.")]
        public void CompensationState(string data1, string data2)
        {
            WriteEvent(EventIds.CompensationState, data1, data2);
        }

        public bool SwitchCaseNotFoundIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.SwitchCaseNotFound, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.ExecuteActivity,
            Keywords = Keywords.WFActivities,
            Message = "The Switch activity '{0}' could not find a Case activity matching the Expression result.")]
        public void SwitchCaseNotFound(string data1)
        {
            WriteEvent(EventIds.SwitchCaseNotFound, data1);
        }

        public bool ExecuteWorkItemStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ExecuteWorkItemStart, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Start, Task = Tasks.ExecuteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Execute work item start")]
        public void ExecuteWorkItemStart()
        {
            WriteEvent(EventIds.ExecuteWorkItemStart);
        }

        public bool ExecuteWorkItemStopIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.ExecuteWorkItemStop, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Stop, Task = Tasks.ExecuteWorkItem,
            Keywords = Keywords.WFRuntime,
            Message = "Execute work item stop")]
        public void ExecuteWorkItemStop()
        {
            WriteEvent(EventIds.ExecuteWorkItemStop);
        }

        public bool SendMessageChannelCacheMissIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.SendMessageChannelCacheMiss, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = Opcodes.MessageChannelCacheMissed, Task = Tasks.MessageChannelCache,
            Keywords = Keywords.WFRuntime,
            Message = "SendMessageChannelCache miss")]
        public void SendMessageChannelCacheMiss()
        {
            WriteEvent(EventIds.SendMessageChannelCacheMiss);
        }

        public bool InternalCacheMetadataStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InternalCacheMetadataStart, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Start, Task = Tasks.InternalCacheMetadata,
            Keywords = Keywords.WFRuntime,
            Message = "InternalCacheMetadata started on activity '{0}'.")]
        public void InternalCacheMetadataStart(string id)
        {
            WriteEvent(EventIds.InternalCacheMetadataStart, id);
        }

        public bool InternalCacheMetadataStopIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.InternalCacheMetadataStop, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Stop, Task = Tasks.InternalCacheMetadata,
            Keywords = Keywords.WFRuntime,
            Message = "InternalCacheMetadata stopped on activity '{0}'.")]
        public void InternalCacheMetadataStop(string id)
        {
            WriteEvent(EventIds.InternalCacheMetadataStop, id);
        }

        public bool CompileVbExpressionStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompileVbExpressionStart, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Start, Task = Tasks.VBExpressionCompile,
            Keywords = Keywords.WFRuntime,
            Message = "Compiling VB expression '{0}'")]
        public void CompileVbExpressionStart(string expr)
        {
            WriteEvent(EventIds.CompileVbExpressionStart, expr);
        }

        public bool CacheRootMetadataStartIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CacheRootMetadataStart, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Start, Task = Tasks.CacheRootMetadata,
            Keywords = Keywords.WFRuntime,
            Message = "CacheRootMetadata started on activity '{0}'")]
        public void CacheRootMetadataStart(string activityName)
        {
            WriteEvent(EventIds.CacheRootMetadataStart, activityName);
        }

        public bool CacheRootMetadataStopIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CacheRootMetadataStop, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Stop, Task = Tasks.CacheRootMetadata,
            Keywords = Keywords.WFRuntime,
            Message = "CacheRootMetadata stopped on activity {0}.")]
        public void CacheRootMetadataStop(string activityName)
        {
            WriteEvent(EventIds.CacheRootMetadataStop, activityName);
        }

        public bool CompileVbExpressionStopIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.CompileVbExpressionStop, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Stop, Task = Tasks.VBExpressionCompile,
            Keywords = Keywords.WFRuntime,
            Message = "Finished compiling VB expression.")]
        public void CompileVbExpressionStop()
        {
            WriteEvent(EventIds.CompileVbExpressionStop);
        }

        public bool TryCatchExceptionFromTryIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.TryCatchExceptionFromTry, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.TryCatchExceptionFromTry, Task = Tasks.TryCatchException,
            Keywords = Keywords.WFActivities,
            Message = "The TryCatch activity '{0}' has caught an exception of type '{1}'.")]
        public void TryCatchExceptionFromTry(string data1, string data2)
        {
            WriteEvent(EventIds.TryCatchExceptionFromTry, data1, data2);
        }

        public bool TryCatchExceptionDuringCancelationIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.TryCatchExceptionDuringCancelation, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = Opcodes.TryCatchExceptionDuringCancelation, Task = Tasks.TryCatchException,
            Keywords = Keywords.WFActivities,
            Message = "A child activity of the TryCatch activity '{0}' has thrown an exception during cancelation.")]
        public void TryCatchExceptionDuringCancelation(string data1)
        {
            WriteEvent(EventIds.TryCatchExceptionDuringCancelation, data1);
        }

        public bool TryCatchExceptionFromCatchOrFinallyIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFActivities, EventChannel.Debug);
        }

        [Event(EventIds.TryCatchExceptionFromCatchOrFinally, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = Opcodes.TryCatchExceptionFromCatchOrFinally, Task = Tasks.TryCatchException,
            Keywords = Keywords.WFActivities,
            Message = "A Catch or Finally activity that is associated with the TryCatch activity '{0}' has thrown an exception.")]
        public void TryCatchExceptionFromCatchOrFinally(string data1)
        {
            WriteEvent(EventIds.TryCatchExceptionFromCatchOrFinally, data1);
        }

        public bool TrackingRecordDroppedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFTracking, EventChannel.Debug);
        }

        [Event(EventIds.TrackingRecordDropped, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = Opcodes.TrackingRecordDropped, Task = Tasks.TrackingRecord,
            Keywords = Keywords.WFTracking,
            Message = "Size of tracking record {0} exceeds maximum allowed by the ETW session for provider {1}")]
        public void TrackingRecordDropped(long RecordNumber, Guid ProviderId)
        {
            WriteEvent(EventIds.TrackingRecordDropped, RecordNumber, ProviderId);
        }

        public bool TrackingRecordRaisedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.TrackingRecordRaised, Level = EventLevel.Informational, Channel = EventChannel.Debug, Opcode = Opcodes.TrackingRecordRaised, Task = Tasks.TrackingRecord,
            Keywords = Keywords.WFRuntime,
            Message = "Tracking Record {0} raised to {1}.")]
        public void TrackingRecordRaised(string data1, string data2)
        {
            WriteEvent(EventIds.TrackingRecordRaised, data1, data2);
        }

        public bool TrackingRecordTruncatedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFTracking, EventChannel.Debug);
        }

        [Event(EventIds.TrackingRecordTruncated, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = Opcodes.TrackingRecordTruncated, Task = Tasks.TrackingRecord,
            Keywords = Keywords.WFTracking,
            Message = "Truncated tracking record {0} written to ETW session with provider {1}. Variables/annotations/user data have been removed")]
        public void TrackingRecordTruncated(long RecordNumber, Guid ProviderId)
        {
            WriteEvent(EventIds.TrackingRecordTruncated, RecordNumber, ProviderId);
        }

        public bool TrackingDataExtractedIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.WFRuntime, EventChannel.Debug);
        }

        [Event(EventIds.TrackingDataExtracted, Level = EventLevel.Verbose, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.TrackingProfile,
            Keywords = Keywords.WFRuntime,
            Message = "Tracking data {0} extracted in activity {1}.")]
        public void TrackingDataExtracted(string Data, string Activity)
        {
            WriteEvent(EventIds.TrackingDataExtracted, Data, Activity);
        }

        public bool TrackingValueNotSerializableIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.WFTracking, EventChannel.Debug);
        }

        [Event(EventIds.TrackingValueNotSerializable, Level = EventLevel.Warning, Channel = EventChannel.Debug, Opcode = EventOpcode.Info, Task = Tasks.TrackingProfile,
            Keywords = Keywords.WFTracking,
            Message = "The extracted argument/variable '{0}' is not serializable.")]
        public void TrackingValueNotSerializable(string name)
        {
            WriteEvent(EventIds.TrackingValueNotSerializable, name);
        }

        public bool HandledExceptionIsEnabled()
        {
            return base.IsEnabled(EventLevel.Informational, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.HandledException, Level = EventLevel.Informational, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "Handling an exception.  Exception details: {0}")]
        public void HandledException(string data1, string SerializedException)
        {
            WriteEvent(EventIds.HandledException, data1, SerializedException);
        }

        public bool ShipAssertExceptionMessageIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.ShipAssertExceptionMessage, Level = EventLevel.Error, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "An unexpected failure occurred. Applications should not attempt to handle this error. For diagnostic purposes, this English message is associated with the failure: {0}.")]
        public void ShipAssertExceptionMessage(string data1)
        {
            WriteEvent(EventIds.ShipAssertExceptionMessage, data1);
        }

        public bool ThrowingExceptionIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.ThrowingException, Level = EventLevel.Warning, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "Throwing an exception. Source: {0}. Exception details: {1}")]
        public void ThrowingException(string data1, string data2, string SerializedException)
        {
            WriteEvent(EventIds.ThrowingException, data1, data2, SerializedException);
        }

        public bool UnhandledExceptionIsEnabled()
        {
            return base.IsEnabled(EventLevel.Critical, Keywords.Infrastructure, EventChannel.Operational);
        }

        [Event(EventIds.UnhandledException, Level = EventLevel.Critical, Channel = EventChannel.Operational,
            Keywords = Keywords.Infrastructure,
            Message = "Unhandled exception.  Exception details: {0}")]
        public void UnhandledException(string data1, string SerializedException)
        {
            WriteEvent(EventIds.UnhandledException, data1, SerializedException);
        }

        public bool HandledExceptionWarningIsEnabled()
        {
            return base.IsEnabled(EventLevel.Warning, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.HandledExceptionWarning, Level = EventLevel.Warning, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "Handling an exception. Exception details: {0}")]
        public void HandledExceptionWarning(string data1, string SerializedException)
        {
            WriteEvent(EventIds.HandledExceptionWarning, data1, SerializedException);
        }

        public bool HandledExceptionErrorIsEnabled()
        {
            return base.IsEnabled(EventLevel.Error, Keywords.Infrastructure, EventChannel.Operational);
        }

        [Event(EventIds.HandledExceptionError, Level = EventLevel.Error, Channel = EventChannel.Operational,
            Keywords = Keywords.Infrastructure,
            Message = "Handling an exception. Exception details: {0}")]
        public void HandledExceptionError(string data1, string SerializedException)
        {
            WriteEvent(EventIds.HandledExceptionError, data1, SerializedException);
        }

        public bool HandledExceptionVerboseIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.HandledExceptionVerbose, Level = EventLevel.Verbose, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "Handling an exception  Exception details: {0}")]
        public void HandledExceptionVerbose(string data1, string SerializedException)
        {
            WriteEvent(EventIds.HandledExceptionVerbose, data1, SerializedException);
        }

        public bool ThrowingExceptionVerboseIsEnabled()
        {
            return base.IsEnabled(EventLevel.Verbose, Keywords.Infrastructure, EventChannel.Analytic);
        }

        [Event(EventIds.ThrowingExceptionVerbose, Level = EventLevel.Verbose, Channel = EventChannel.Analytic,
            Keywords = Keywords.Infrastructure,
            Message = "Throwing an exception. Source: {0}. Exception details: {1}")]
        public void ThrowingExceptionVerbose(string data1, string data2, string SerializedException)
        {
            WriteEvent(EventIds.ThrowingExceptionVerbose, data1, data2, SerializedException);
        }

        #region Keywords / Tasks / Opcodes

        public class EventIds
        {
            public const int TransferEmitted = 84;
            public const int WorkflowApplicationCompleted = 83;
            public const int WorkflowApplicationTerminated = 82;
            public const int WorkflowInstanceCanceled = 81;
            public const int WorkflowInstanceAborted = 80;
            public const int WorkflowApplicationIdled = 79;
            public const int WorkflowApplicationUnhandledException = 78;
            public const int WorkflowApplicationPersisted = 77;
            public const int WorkflowApplicationUnloaded = 76;
            public const int ActivityScheduled = 75;
            public const int ActivityCompleted = 74;
            public const int ScheduleExecuteActivityWorkItem = 73;
            public const int StartExecuteActivityWorkItem = 72;
            public const int CompleteExecuteActivityWorkItem = 71;
            public const int ScheduleCompletionWorkItem = 70;
            public const int StartCompletionWorkItem = 69;
            public const int CompleteCompletionWorkItem = 68;
            public const int ScheduleCancelActivityWorkItem = 67;
            public const int StartCancelActivityWorkItem = 66;
            public const int CompleteCancelActivityWorkItem = 65;
            public const int CreateBookmark = 64;
            public const int ScheduleBookmarkWorkItem = 63;
            public const int StartBookmarkWorkItem = 62;
            public const int CompleteBookmarkWorkItem = 61;
            public const int CreateBookmarkScope = 60;
            public const int BookmarkScopeInitialized = 59;
            public const int ScheduleTransactionContextWorkItem = 58;
            public const int StartTransactionContextWorkItem = 57;
            public const int CompleteTransactionContextWorkItem = 56;
            public const int ScheduleFaultWorkItem = 55;
            public const int StartFaultWorkItem = 54;
            public const int CompleteFaultWorkItem = 53;
            public const int ScheduleRuntimeWorkItem = 52;
            public const int StartRuntimeWorkItem = 51;
            public const int CompleteRuntimeWorkItem = 50;
            public const int RuntimeTransactionSet = 49;
            public const int RuntimeTransactionCompletionRequested = 48;
            public const int RuntimeTransactionComplete = 47;
            public const int EnterNoPersistBlock = 46;
            public const int ExitNoPersistBlock = 45;
            public const int InArgumentBound = 44;
            public const int WorkflowApplicationPersistableIdle = 43;
            public const int WorkflowActivityStart = 42;
            public const int WorkflowActivityStop = 41;
            public const int WorkflowActivitySuspend = 40;
            public const int WorkflowActivityResume = 39;
            public const int InvokeMethodIsStatic = 38;
            public const int InvokeMethodIsNotStatic = 37;
            public const int InvokedMethodThrewException = 36;
            public const int InvokeMethodUseAsyncPattern = 35;
            public const int InvokeMethodDoesNotUseAsyncPattern = 34;
            public const int FlowchartStart = 33;
            public const int FlowchartEmpty = 32;
            public const int FlowchartNextNull = 31;
            public const int FlowchartSwitchCase = 30;
            public const int FlowchartSwitchDefault = 29;
            public const int FlowchartSwitchCaseNotFound = 28;
            public const int CompensationState = 27;
            public const int SwitchCaseNotFound = 26;
            public const int ExecuteWorkItemStart = 25;
            public const int ExecuteWorkItemStop = 24;
            public const int SendMessageChannelCacheMiss = 23;
            public const int InternalCacheMetadataStart = 22;
            public const int InternalCacheMetadataStop = 21;
            public const int CompileVbExpressionStart = 20;
            public const int CacheRootMetadataStart = 19;
            public const int CacheRootMetadataStop = 18;
            public const int CompileVbExpressionStop = 17;
            public const int TryCatchExceptionFromTry = 16;
            public const int TryCatchExceptionDuringCancelation = 15;
            public const int TryCatchExceptionFromCatchOrFinally = 14;
            public const int TrackingRecordDropped = 13;
            public const int TrackingRecordRaised = 12;
            public const int TrackingRecordTruncated = 11;
            public const int TrackingDataExtracted = 10;
            public const int TrackingValueNotSerializable = 9;
            public const int HandledException = 8;
            public const int ShipAssertExceptionMessage = 7;
            public const int ThrowingException = 6;
            public const int UnhandledException = 5;
            public const int HandledExceptionWarning = 4;
            public const int HandledExceptionError = 3;
            public const int HandledExceptionVerbose = 2;
            public const int ThrowingExceptionVerbose = 1;
        }

        public class Tasks
        {
            public const EventTask ActivationDispatchSession = (EventTask)2500;
            public const EventTask ActivationDuplicateSocket = (EventTask)2501;
            public const EventTask ActivationListenerOpen = (EventTask)2502;
            public const EventTask ActivationPipeListenerListening = (EventTask)2503;
            public const EventTask ActivationRoutingTableLookup = (EventTask)2504;
            public const EventTask ActivationServiceStart = (EventTask)2505;
            public const EventTask ActivationTcpListenerListening = (EventTask)2506;
            public const EventTask AddServiceEndpoint = (EventTask)2507;
            public const EventTask BufferOutOfOrder = (EventTask)2508;
            public const EventTask BufferPooling = (EventTask)2509;
            public const EventTask CacheRootMetadata = (EventTask)2510;
            public const EventTask ChannelFactoryCaching = (EventTask)2511;
            public const EventTask ChannelFactoryCreate = (EventTask)2512;
            public const EventTask ChannelReceive = (EventTask)2513;
            public const EventTask ClientRuntime = (EventTask)2514;
            public const EventTask ClientSendPreamble = (EventTask)2515;
            public const EventTask CompensationState = (EventTask)2516;
            public const EventTask CompleteActivity = (EventTask)2517;
            public const EventTask CompleteWorkItem = (EventTask)2518;
            public const EventTask Connect = (EventTask)2519;
            public const EventTask ConnectionAbort = (EventTask)2520;
            public const EventTask ConnectionAccept = (EventTask)2521;
            public const EventTask ConnectionPooling = (EventTask)2522;
            public const EventTask Correlation = (EventTask)2523;
            public const EventTask CreateBookmark = (EventTask)2524;
            public const EventTask CreateWorkflowServiceHost = (EventTask)2526;
            public const EventTask CustomTrackingRecord = (EventTask)2527;
            public const EventTask DataContractResolver = (EventTask)2528;
            public const EventTask DiscoveryClient = (EventTask)2529;
            public const EventTask DiscoveryClientChannel = (EventTask)2530;
            public const EventTask DiscoveryMessage = (EventTask)2531;
            public const EventTask DiscoverySynchronizationContext = (EventTask)2532;
            public const EventTask DispatchMessage = (EventTask)2533;
            public const EventTask EndpointDiscoverability = (EventTask)2534;
            public const EventTask ExecuteActivity = (EventTask)2535;
            public const EventTask ExecuteFlowchart = (EventTask)2536;
            public const EventTask ExecuteWorkItem = (EventTask)2537;
            public const EventTask FormatterDeserializeReply = (EventTask)2539;
            public const EventTask FormatterDeserializeRequest = (EventTask)2540;
            public const EventTask FormatterSerializeReply = (EventTask)2541;
            public const EventTask FormatterSerializeRequest = (EventTask)2542;
            public const EventTask GenerateDeserializer = (EventTask)2543;
            public const EventTask GenerateSerializer = (EventTask)2544;
            public const EventTask GenerateXmlSerializable = (EventTask)2545;
            public const EventTask HostedTransportConfigurationManagerConfigInit = (EventTask)2546;
            public const EventTask ImportKnownType = (EventTask)2547;
            public const EventTask InferDescription = (EventTask)2548;
            public const EventTask InitializeBookmarkScope = (EventTask)2549;
            public const EventTask InternalCacheMetadata = (EventTask)2550;
            public const EventTask InvokeMethod = (EventTask)2551;
            public const EventTask ListenerOpen = (EventTask)2552;
            public const EventTask LockWorkflowInstance = (EventTask)2553;
            public const EventTask MessageChannelCache = (EventTask)2554;
            public const EventTask MessageDecoding = (EventTask)2555;
            public const EventTask MessageEncoding = (EventTask)2556;
            public const EventTask MessageQueueRegister = (EventTask)2557;
            public const EventTask MsmqQuotas = (EventTask)2558;
            public const EventTask NoPersistBlock = (EventTask)2559;
            public const EventTask Quotas = (EventTask)2560;
            public const EventTask ReliableSession = (EventTask)2561;
            public const EventTask RoutingService = (EventTask)2562;
            public const EventTask RoutingServiceClient = (EventTask)2563;
            public const EventTask RoutingServiceFilterTableMatch = (EventTask)2564;
            public const EventTask RoutingServiceMessage = (EventTask)2565;
            public const EventTask RoutingServiceReceiveContext = (EventTask)2566;
            public const EventTask RoutingServiceTransaction = (EventTask)2567;
            public const EventTask RuntimeTransaction = (EventTask)2568;
            public const EventTask ScheduleActivity = (EventTask)2569;
            public const EventTask ScheduleWorkItem = (EventTask)2570;
            public const EventTask SecureMessage = (EventTask)2571;
            public const EventTask SecurityImpersonation = (EventTask)2572;
            public const EventTask SecurityNegotiation = (EventTask)2573;
            public const EventTask SecurityVerification = (EventTask)2574;
            public const EventTask ServiceActivation = (EventTask)2575;
            public const EventTask ServiceChannelCall = (EventTask)2576;
            public const EventTask ServiceChannelOpen = (EventTask)2577;
            public const EventTask ServiceHostActivation = (EventTask)2578;
            public const EventTask ServiceHostCompilation = (EventTask)2579;
            public const EventTask ServiceHostCreate = (EventTask)2580;
            public const EventTask ServiceHostFactoryCreation = (EventTask)2581;
            public const EventTask ServiceHostFault = (EventTask)2582;
            public const EventTask ServiceHostOpen = (EventTask)2583;
            public const EventTask ServiceInstance = (EventTask)2584;
            public const EventTask ServiceShutdown = (EventTask)2585;
            public const EventTask SessionStart = (EventTask)2586;
            public const EventTask SessionUpgrade = (EventTask)2587;
            public const EventTask Signpost = (EventTask)2588;
            public const EventTask SqlCommandExecute = (EventTask)2589;
            public const EventTask StartWorkItem = (EventTask)2590;
            public const EventTask SurrogateDeserialize = (EventTask)2591;
            public const EventTask SurrogateSerialize = (EventTask)2592;
            public const EventTask ThreadScheduling = (EventTask)2593;
            public const EventTask Throttles = (EventTask)2594;
            public const EventTask Timeout = (EventTask)2595;
            public const EventTask TimeoutException = (EventTask)2596;
            public const EventTask TrackingProfile = (EventTask)2597;
            public const EventTask TrackingRecord = (EventTask)2598;
            public const EventTask TransportReceive = (EventTask)2599;
            public const EventTask TransportSend = (EventTask)2600;
            public const EventTask TryCatchException = (EventTask)2601;
            public const EventTask VBExpressionCompile = (EventTask)2602;
            public const EventTask WASActivation = (EventTask)2603;
            public const EventTask WebHostRequest = (EventTask)2604;
            public const EventTask WFApplicationStateChange = (EventTask)2605;
            public const EventTask WFMessage = (EventTask)2606;
            public const EventTask WorkflowActivity = (EventTask)2607;
            public const EventTask WorkflowInstanceRecord = (EventTask)2608;
            public const EventTask WorkflowTracking = (EventTask)2609;
            public const EventTask XamlServicesLoad = (EventTask)2610;
            public const EventTask SignatureVerification = (EventTask)2611;
            public const EventTask TokenValidation = (EventTask)2612;
            public const EventTask GetIssuerName = (EventTask)2613;
            public const EventTask WrappedKeyDecryption = (EventTask)2614;
            public const EventTask EncryptedDataProcessing = (EventTask)2615;
            public const EventTask FederationMessageProcessing = (EventTask)2616;
            public const EventTask FederationMessageCreation = (EventTask)2617;
            public const EventTask SessionCookieReading = (EventTask)2618;
            public const EventTask PrincipalSetting = (EventTask)2619;
        }

        public class Opcodes
        {
            public const EventOpcode BufferOutOfOrderNoBookmark = (EventOpcode)10;
            public const EventOpcode ExecuteFlowchartBegin = (EventOpcode)11;
            public const EventOpcode BufferPoolingAllocate = (EventOpcode)12;
            public const EventOpcode BufferPoolingTune = (EventOpcode)13;
            public const EventOpcode ClientRuntimeClientChannelOpenStart = (EventOpcode)14;
            public const EventOpcode ClientRuntimeClientChannelOpenStop = (EventOpcode)15;
            public const EventOpcode ClientRuntimeClientMessageInspectorAfterReceiveInvoked = (EventOpcode)16;
            public const EventOpcode ClientRuntimeClientMessageInspectorBeforeSendInvoked = (EventOpcode)17; public const EventOpcode ClientRuntimeClientParameterInspectorStart = (EventOpcode)18;
            public const EventOpcode ClientRuntimeClientParameterInspectorStop = (EventOpcode)19;
            public const EventOpcode ClientRuntimeOperationPrepared = (EventOpcode)20;
            public const EventOpcode CompleteWorkItemCompleteBookmark = (EventOpcode)21;
            public const EventOpcode CompleteWorkItemCompleteCancelActivity = (EventOpcode)22;
            public const EventOpcode CompleteWorkItemCompleteCompletion = (EventOpcode)23;
            public const EventOpcode CompleteWorkItemCompleteExecuteActivity = (EventOpcode)24;
            public const EventOpcode CompleteWorkItemCompleteFault = (EventOpcode)25;
            public const EventOpcode CompleteWorkItemCompleteRuntime = (EventOpcode)26;
            public const EventOpcode CompleteWorkItemCompleteTransactionContext = (EventOpcode)27;
            public const EventOpcode CorrelationDuplicateQuery = (EventOpcode)28;
            public const EventOpcode DiscoveryClientExceptionSuppressed = (EventOpcode)29;
            public const EventOpcode DiscoveryClientFailedToClose = (EventOpcode)30;
            public const EventOpcode DiscoveryClientReceivedMulticastSuppression = (EventOpcode)31;
            public const EventOpcode DiscoveryClientChannelCreationFailed = (EventOpcode)32;
            public const EventOpcode DiscoveryClientChannelFindInitiated = (EventOpcode)33;
            public const EventOpcode DiscoveryClientChannelOpenFailed = (EventOpcode)34;
            public const EventOpcode DiscoveryClientChannelOpenSucceeded = (EventOpcode)35;
            public const EventOpcode DiscoveryMessageDuplicate = (EventOpcode)36;
            public const EventOpcode DiscoveryMessageInvalidContent = (EventOpcode)37;
            public const EventOpcode DiscoveryMessageInvalidRelatesToOrOperationCompleted = (EventOpcode)38;
            public const EventOpcode DiscoveryMessageInvalidReplyTo = (EventOpcode)39;
            public const EventOpcode DiscoveryMessageNoContent = (EventOpcode)40;
            public const EventOpcode DiscoveryMessageNullMessageId = (EventOpcode)41;
            public const EventOpcode DiscoveryMessageNullMessageSequence = (EventOpcode)42;
            public const EventOpcode DiscoveryMessageNullRelatesTo = (EventOpcode)43;
            public const EventOpcode DiscoveryMessageNullReplyTo = (EventOpcode)44;
            public const EventOpcode DiscoveryMessageReceivedAfterOperationCompleted = (EventOpcode)45;
            public const EventOpcode DiscoverySynchronizationContextReset = (EventOpcode)46;
            public const EventOpcode DiscoverySynchronizationContextSetToNull = (EventOpcode)47;
            public const EventOpcode DispatchMessageBeforeAuthorization = (EventOpcode)48;
            public const EventOpcode DispatchMessageDispatchStart = (EventOpcode)49;
            public const EventOpcode DispatchMessageDispatchStop = (EventOpcode)50;
            public const EventOpcode DispatchMessageDispathMessageInspectorAfterReceiveInvoked = (EventOpcode)51;
            public const EventOpcode DispatchMessageDispathMessageInspectorBeforeSendInvoked = (EventOpcode)52;
            public const EventOpcode DispatchMessageOperationInvokerStart = (EventOpcode)53;
            public const EventOpcode DispatchMessageOperationInvokerStop = (EventOpcode)54;
            public const EventOpcode DispatchMessageParameterInspectorStart = (EventOpcode)55;
            public const EventOpcode DispatchMessageParameterInspectorStop = (EventOpcode)56;
            public const EventOpcode DispatchMessageTransactionScopeCreate = (EventOpcode)57;
            public const EventOpcode EndpointDiscoverabilityDisabled = (EventOpcode)58;
            public const EventOpcode EndpointDiscoverabilityEnabled = (EventOpcode)59;
            public const EventOpcode ExecuteFlowchartEmpty = (EventOpcode)60;
            public const EventOpcode ExecuteFlowchartNextNull = (EventOpcode)61;
            public const EventOpcode ExecuteFlowchartSwitchCase = (EventOpcode)62;
            public const EventOpcode ExecuteFlowchartSwitchCaseNotFound = (EventOpcode)63;
            public const EventOpcode ExecuteFlowchartSwitchDefault = (EventOpcode)64;
            public const EventOpcode InferDescriptionContract = (EventOpcode)69;
            public const EventOpcode InferDescriptionOperation = (EventOpcode)70;
            public const EventOpcode InvokeMethodDoesNotUseAsyncPattern = (EventOpcode)71;
            public const EventOpcode InvokeMethodIsNotStatic = (EventOpcode)72;
            public const EventOpcode InvokeMethodIsStatic = (EventOpcode)73;
            public const EventOpcode InvokeMethodThrewException = (EventOpcode)74;
            public const EventOpcode InvokeMethodUseAsyncPattern = (EventOpcode)75;
            public const EventOpcode MessageChannelCacheMissed = (EventOpcode)76;
            public const EventOpcode ReliableSessionFaulted = (EventOpcode)77;
            public const EventOpcode ReliableSessionReconnect = (EventOpcode)78;
            public const EventOpcode ReliableSessionSequenceAck = (EventOpcode)79;
            public const EventOpcode RoutingServiceAbortingChannel = (EventOpcode)80;
            public const EventOpcode RoutingServiceCloseFailed = (EventOpcode)81;
            public const EventOpcode RoutingServiceConfigurationApplied = (EventOpcode)82;
            public const EventOpcode RoutingServiceDuplexCallbackException = (EventOpcode)83;
            public const EventOpcode RoutingServiceHandledException = (EventOpcode)84;
            public const EventOpcode RoutingServiceTransmitFailed = (EventOpcode)85;
            public const EventOpcode RoutingServiceClientChannelFaulted = (EventOpcode)86;
            public const EventOpcode RoutingServiceClientClosing = (EventOpcode)87;
            public const EventOpcode RoutingServiceClientCreatingForEndpoint = (EventOpcode)88;
            public const EventOpcode RoutingServiceMessageCompletingOneWay = (EventOpcode)89;
            public const EventOpcode RoutingServiceMessageCompletingTwoWay = (EventOpcode)90;
            public const EventOpcode RoutingServiceMessageMovedToBackup = (EventOpcode)91;
            public const EventOpcode RoutingServiceMessageProcessingFailure = (EventOpcode)92;
            public const EventOpcode RoutingServiceMessageProcessingMessage = (EventOpcode)93;
            public const EventOpcode RoutingServiceMessageRoutedToEndpoints = (EventOpcode)94;
            public const EventOpcode RoutingServiceMessageSendingFaultResponse = (EventOpcode)95;
            public const EventOpcode RoutingServiceMessageSendingResponse = (EventOpcode)96;
            public const EventOpcode RoutingServiceMessageTransmitSucceeded = (EventOpcode)97;
            public const EventOpcode RoutingServiceMessageTransmittingMessage = (EventOpcode)98;
            public const EventOpcode RoutingServiceReceiveContextAbandoning = (EventOpcode)99;
            public const EventOpcode RoutingServiceReceiveContextCompleting = (EventOpcode)100;
            public const EventOpcode RoutingServiceTransactionCommittingTransaction = (EventOpcode)101;
            public const EventOpcode RoutingServiceTransactionCreating = (EventOpcode)102;
            public const EventOpcode RoutingServiceTransactionUsingExisting = (EventOpcode)103;
            public const EventOpcode RuntimeTransactionComplete = (EventOpcode)104;
            public const EventOpcode RuntimeTransactionCompletionRequested = (EventOpcode)105;
            public const EventOpcode RuntimeTransactionSet = (EventOpcode)106;
            public const EventOpcode ScheduleWorkItemScheduleBookmark = (EventOpcode)107;
            public const EventOpcode ScheduleWorkItemScheduleCancelActivity = (EventOpcode)108;
            public const EventOpcode ScheduleWorkItemScheduleCompletion = (EventOpcode)109;
            public const EventOpcode ScheduleWorkItemScheduleExecuteActivity = (EventOpcode)110;
            public const EventOpcode ScheduleWorkItemScheduleFault = (EventOpcode)111;
            public const EventOpcode ScheduleWorkItemScheduleRuntime = (EventOpcode)112;
            public const EventOpcode ScheduleWorkItemScheduleTransactionContext = (EventOpcode)113;
            public const EventOpcode SessionUpgradeAccept = (EventOpcode)114;
            public const EventOpcode SessionUpgradeInitiate = (EventOpcode)115;
            public const EventOpcode Signpostsuspend = (EventOpcode)116;
            public const EventOpcode StartWorkItemStartBookmark = (EventOpcode)117;
            public const EventOpcode StartWorkItemStartCancelActivity = (EventOpcode)118;
            public const EventOpcode StartWorkItemStartCompletion = (EventOpcode)119;
            public const EventOpcode StartWorkItemStartExecuteActivity = (EventOpcode)120;
            public const EventOpcode StartWorkItemStartFault = (EventOpcode)121;
            public const EventOpcode StartWorkItemStartRuntime = (EventOpcode)122;
            public const EventOpcode StartWorkItemStartTransactionContext = (EventOpcode)123;
            public const EventOpcode TrackingProfileNotFound = (EventOpcode)124;
            public const EventOpcode TrackingRecordDropped = (EventOpcode)125;
            public const EventOpcode TrackingRecordRaised = (EventOpcode)126;
            public const EventOpcode TrackingRecordTruncated = (EventOpcode)127;
            public const EventOpcode TransportReceiveBeforeAuthentication = (EventOpcode)128;
            public const EventOpcode TryCatchExceptionDuringCancelation = (EventOpcode)129;
            public const EventOpcode TryCatchExceptionFromCatchOrFinally = (EventOpcode)130;
            public const EventOpcode TryCatchExceptionFromTry = (EventOpcode)131;
            public const EventOpcode WASActivationConnected = (EventOpcode)132;
            public const EventOpcode WASActivationDisconnect = (EventOpcode)133;
            public const EventOpcode WFApplicationStateChangeCompleted = (EventOpcode)134;
            public const EventOpcode WFApplicationStateChangeIdled = (EventOpcode)135;
            public const EventOpcode WFApplicationStateChangeInstanceAborted = (EventOpcode)136;
            public const EventOpcode WFApplicationStateChangeInstanceCanceled = (EventOpcode)137;
            public const EventOpcode WFApplicationStateChangePersistableIdle = (EventOpcode)138;
            public const EventOpcode WFApplicationStateChangePersisted = (EventOpcode)139;
            public const EventOpcode WFApplicationStateChangeTerminated = (EventOpcode)140;
            public const EventOpcode WFApplicationStateChangeUnhandledException = (EventOpcode)141;
            public const EventOpcode WFApplicationStateChangeUnloaded = (EventOpcode)142;
            public const EventOpcode WorkflowActivitysuspend = (EventOpcode)143;
            public const EventOpcode WorkflowInstanceRecordAbortedRecord = (EventOpcode)144;
            public const EventOpcode WorkflowInstanceRecordAbortedWithId = (EventOpcode)145;
            public const EventOpcode WorkflowInstanceRecordSuspendedRecord = (EventOpcode)146;
            public const EventOpcode WorkflowInstanceRecordSuspendedWithId = (EventOpcode)147;
            public const EventOpcode WorkflowInstanceRecordTerminatedRecord = (EventOpcode)148;
            public const EventOpcode WorkflowInstanceRecordTerminatedWithId = (EventOpcode)149;
            public const EventOpcode WorkflowInstanceRecordUnhandledExceptionRecord = (EventOpcode)150;
            public const EventOpcode WorkflowInstanceRecordUnhandledExceptionWithId = (EventOpcode)151;
            public const EventOpcode WorkflowInstanceRecordUpdatedRecord = (EventOpcode)152;
        }

        public class Keywords
        {
            public const EventKeywords ServiceHost = (EventKeywords)0x1;
            public const EventKeywords Serialization = (EventKeywords)0x2;
            public const EventKeywords ServiceModel = (EventKeywords)0x4;
            public const EventKeywords Transaction = (EventKeywords)0x8;
            public const EventKeywords Security = (EventKeywords)0x10;
            public const EventKeywords WCFMessageLogging = (EventKeywords)0x20;
            public const EventKeywords WFTracking = (EventKeywords)0x40;
            public const EventKeywords WebHost = (EventKeywords)0x80;
            public const EventKeywords HTTP = (EventKeywords)0x100;
            public const EventKeywords TCP = (EventKeywords)0x200;
            public const EventKeywords TransportGeneral = (EventKeywords)0x400;
            public const EventKeywords ActivationServices = (EventKeywords)0x800;
            public const EventKeywords Channel = (EventKeywords)0x1000;
            public const EventKeywords WebHTTP = (EventKeywords)0x2000;
            public const EventKeywords Discovery = (EventKeywords)0x4000;
            public const EventKeywords RoutingServices = (EventKeywords)0x8000;
            public const EventKeywords Infrastructure = (EventKeywords)0x10000;
            public const EventKeywords EndToEndMonitoring = (EventKeywords)0x20000;
            public const EventKeywords HealthMonitoring = (EventKeywords)0x40000;
            public const EventKeywords Troubleshooting = (EventKeywords)0x80000;
            public const EventKeywords UserEvents = (EventKeywords)0x100000;
            public const EventKeywords Threading = (EventKeywords)0x200000;
            public const EventKeywords Quota = (EventKeywords)0x400000;
            public const EventKeywords WFRuntime = (EventKeywords)0x1000000;
            public const EventKeywords WFActivities = (EventKeywords)0x2000000;
            public const EventKeywords WFServices = (EventKeywords)0x4000000;
            public const EventKeywords WFInstanceStore = (EventKeywords)0x8000000;
        }

        #endregion
    }
}

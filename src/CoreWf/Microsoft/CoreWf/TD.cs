// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace CoreWf
{
    internal partial class TD
    {
        internal static Guid CurrentActivityId
        {
            get { return WfEventSource.CurrentThreadActivityId; }
            set { WfEventSource.SetCurrentThreadActivityId(value); }
        }

        internal static bool IsEtwProviderEnabled
        {
            get { return WfEventSource.Instance.IsEnabled(); }
        }

        internal static void SetActivityId(Guid newId, out Guid oldId)
        {
            WfEventSource.SetCurrentThreadActivityId(newId, out oldId);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=39456, Level=Warning, Channel=debug
        /// </summary>
        internal static bool TrackingRecordDroppedIsEnabled()
        {
            return WfEventSource.Instance.TrackingRecordDroppedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Size of tracking record {0} exceeds maximum allowed by the ETW session for provider {1}
        /// Event description ID=39456, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="RecordNumber">Parameter 0 for event: Size of tracking record {0} exceeds maximum allowed by the ETW session for provider {1}</param>
        /// <param name="ProviderId">Parameter 1 for event: Size of tracking record {0} exceeds maximum allowed by the ETW session for provider {1}</param>
        internal static void TrackingRecordDropped(long RecordNumber, System.Guid ProviderId)
        {
            WfEventSource.Instance.TrackingRecordDropped(RecordNumber, ProviderId);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=39457, Level=informational, Channel=debug
        /// </summary>
        internal static bool TrackingRecordRaisedIsEnabled()
        {
            return WfEventSource.Instance.TrackingRecordRaisedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Tracking Record {0} raised to {1}.
        /// Event description ID=39457, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Tracking Record {0} raised to {1}.</param>
        /// <param name="param1">Parameter 1 for event: Tracking Record {0} raised to {1}.</param>
        internal static void TrackingRecordRaised(string param0, string param1)
        {
            WfEventSource.Instance.TrackingRecordRaised(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=39458, Level=Warning, Channel=debug
        /// </summary>
        internal static bool TrackingRecordTruncatedIsEnabled()
        {
            return WfEventSource.Instance.TrackingRecordTruncatedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Truncated tracking record {0} written to ETW session with provider {1}. Variables/annotations/user data have been removed
        /// Event description ID=39458, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="RecordNumber">Parameter 0 for event: Truncated tracking record {0} written to ETW session with provider {1}. Variables/annotations/user data have been removed</param>
        /// <param name="ProviderId">Parameter 1 for event: Truncated tracking record {0} written to ETW session with provider {1}. Variables/annotations/user data have been removed</param>
        internal static void TrackingRecordTruncated(long RecordNumber, System.Guid ProviderId)
        {
            WfEventSource.Instance.TrackingRecordTruncated(RecordNumber, ProviderId);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=39459, Level=verbose, Channel=debug
        /// </summary>
        internal static bool TrackingDataExtractedIsEnabled()
        {
            return WfEventSource.Instance.TrackingDataExtractedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Tracking data {0} extracted in activity {1}.
        /// Event description ID=39459, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="Data">Parameter 0 for event: Tracking data {0} extracted in activity {1}.</param>
        /// <param name="Activity">Parameter 1 for event: Tracking data {0} extracted in activity {1}.</param>
        internal static void TrackingDataExtracted(string Data, string Activity)
        {
            WfEventSource.Instance.TrackingDataExtracted(Data, Activity);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1223, Level=Informational, Channel=debug
        /// </summary>
        internal static bool SwitchCaseNotFoundIsEnabled()
        {
            return WfEventSource.Instance.SwitchCaseNotFoundIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The Switch activity '{0}' could not find a Case activity matching the Expression result.
        /// Event description ID=1223, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: The Switch activity '{0}' could not find a Case activity matching the Expression result.</param>
        internal static void SwitchCaseNotFound(string param0)
        {
            WfEventSource.Instance.SwitchCaseNotFound(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2576, Level=Informational, Channel=debug
        /// </summary>
        internal static bool TryCatchExceptionFromTryIsEnabled()
        {
            return WfEventSource.Instance.TryCatchExceptionFromTryIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The TryCatch activity '{0}' has caught an exception of type '{1}'.
        /// Event description ID=2576, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: The TryCatch activity '{0}' has caught an exception of type '{1}'.</param>
        /// <param name="param1">Parameter 1 for event: The TryCatch activity '{0}' has caught an exception of type '{1}'.</param>
        internal static void TryCatchExceptionFromTry(string param0, string param1)
        {
            WfEventSource.Instance.TryCatchExceptionFromTry(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2577, Level=Warning, Channel=debug
        /// </summary>
        internal static bool TryCatchExceptionDuringCancelationIsEnabled()
        {
            return WfEventSource.Instance.TryCatchExceptionDuringCancelationIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A child activity of the TryCatch activity '{0}' has thrown an exception during cancelation.
        /// Event description ID=2577, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A child activity of the TryCatch activity '{0}' has thrown an exception during cancelation.</param>
        internal static void TryCatchExceptionDuringCancelation(string param0)
        {
            WfEventSource.Instance.TryCatchExceptionDuringCancelation(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2578, Level=Warning, Channel=debug
        /// </summary>
        internal static bool TryCatchExceptionFromCatchOrFinallyIsEnabled()
        {
            return WfEventSource.Instance.TryCatchExceptionFromCatchOrFinallyIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A Catch or Finally activity that is associated with the TryCatch activity '{0}' has thrown an exception.
        /// Event description ID=2578, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A Catch or Finally activity that is associated with the TryCatch activity '{0}' has thrown an exception.</param>
        internal static void TryCatchExceptionFromCatchOrFinally(string param0)
        {
            WfEventSource.Instance.TryCatchExceptionFromCatchOrFinally(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1023, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteBookmarkWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteBookmarkWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.
        /// Event description ID=1023, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param1">Parameter 1 for event: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param2">Parameter 2 for event: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param3">Parameter 3 for event: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param4">Parameter 4 for event: A BookmarkWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. BookmarkName: {3}, BookmarkScope: {4}.</param>
        internal static void CompleteBookmarkWorkItem(string param0, string param1, string param2, string param3, string param4)
        {
            WfEventSource.Instance.CompleteBookmarkWorkItem(param0, param1, param2, param3, param4);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1019, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteCancelActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteCancelActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A CancelActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1019, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A CancelActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A CancelActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A CancelActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void CompleteCancelActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.CompleteCancelActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1016, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteCompletionWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteCompletionWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1016, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: A CompletionWorkItem has completed for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        internal static void CompleteCompletionWorkItem(string param0, string param1, string param2, string param3, string param4, string param5)
        {
            WfEventSource.Instance.CompleteCompletionWorkItem(param0, param1, param2, param3, param4, param5);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1013, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteExecuteActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteExecuteActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: An ExecuteActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1013, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: An ExecuteActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: An ExecuteActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: An ExecuteActivityWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void CompleteExecuteActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.CompleteExecuteActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1031, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteFaultWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteFaultWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1031, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: A FaultWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void CompleteFaultWorkItem(string param0, string param1, string param2, string param3, string param4, string param5, System.Exception exception)
        {
            WfEventSource.Instance.CompleteFaultWorkItem(param0, param1, param2, param3, param4, param5, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1034, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteRuntimeWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteRuntimeWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A runtime work item has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1034, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A runtime work item has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A runtime work item has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A runtime work item has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void CompleteRuntimeWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.CompleteRuntimeWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1028, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CompleteTransactionContextWorkItemIsEnabled()
        {
            return WfEventSource.Instance.CompleteTransactionContextWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A TransactionContextWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1028, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A TransactionContextWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A TransactionContextWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A TransactionContextWorkItem has completed for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void CompleteTransactionContextWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.CompleteTransactionContextWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1020, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CreateBookmarkIsEnabled()
        {
            return WfEventSource.Instance.CreateBookmarkIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.
        /// Event description ID=1020, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param1">Parameter 1 for event: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param2">Parameter 2 for event: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param3">Parameter 3 for event: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param4">Parameter 4 for event: A Bookmark has been created for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        internal static void CreateBookmark(string param0, string param1, string param2, string param3, string param4)
        {
            WfEventSource.Instance.CreateBookmark(param0, param1, param2, param3, param4);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1024, Level=verbose, Channel=debug
        /// </summary>
        internal static bool CreateBookmarkScopeIsEnabled()
        {
            return WfEventSource.Instance.CreateBookmarkScopeIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A BookmarkScope has been created: {0}.
        /// Event description ID=1024, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A BookmarkScope has been created: {0}.</param>
        internal static void CreateBookmarkScope(string param0)
        {
            WfEventSource.Instance.CreateBookmarkScope(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1038, Level=verbose, Channel=debug
        /// </summary>
        internal static bool EnterNoPersistBlockIsEnabled()
        {
            return WfEventSource.Instance.EnterNoPersistBlockIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Entering a no persist block.
        /// Event description ID=1038, Level=verbose, Channel=debug
        /// </summary>
        internal static void EnterNoPersistBlock()
        {
            WfEventSource.Instance.EnterNoPersistBlock();
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1039, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ExitNoPersistBlockIsEnabled()
        {
            return WfEventSource.Instance.ExitNoPersistBlockIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Exiting a no persist block.
        /// Event description ID=1039, Level=verbose, Channel=debug
        /// </summary>
        internal static void ExitNoPersistBlock()
        {
            WfEventSource.Instance.ExitNoPersistBlock();
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1040, Level=verbose, Channel=debug
        /// </summary>
        internal static bool InArgumentBoundIsEnabled()
        {
            return WfEventSource.Instance.InArgumentBoundIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.
        /// Event description ID=1040, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.</param>
        /// <param name="param1">Parameter 1 for event: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.</param>
        /// <param name="param2">Parameter 2 for event: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.</param>
        /// <param name="param3">Parameter 3 for event: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.</param>
        /// <param name="param4">Parameter 4 for event: In argument '{0}' on Activity '{1}', DisplayName: '{2}', InstanceId: '{3}' has been bound with value: {4}.</param>
        internal static void InArgumentBound(string param0, string param1, string param2, string param3, string param4)
        {
            WfEventSource.Instance.InArgumentBound(param0, param1, param2, param3, param4);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1037, Level=verbose, Channel=debug
        /// </summary>
        internal static bool RuntimeTransactionCompleteIsEnabled()
        {
            return WfEventSource.Instance.RuntimeTransactionCompleteIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The runtime transaction has completed with the state '{0}'.
        /// Event description ID=1037, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: The runtime transaction has completed with the state '{0}'.</param>
        internal static void RuntimeTransactionComplete(string param0)
        {
            WfEventSource.Instance.RuntimeTransactionComplete(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1036, Level=verbose, Channel=debug
        /// </summary>
        internal static bool RuntimeTransactionCompletionRequestedIsEnabled()
        {
            return WfEventSource.Instance.RuntimeTransactionCompletionRequestedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has scheduled completion of the runtime transaction.
        /// Event description ID=1036, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has scheduled completion of the runtime transaction.</param>
        /// <param name="param1">Parameter 1 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has scheduled completion of the runtime transaction.</param>
        /// <param name="param2">Parameter 2 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has scheduled completion of the runtime transaction.</param>
        internal static void RuntimeTransactionCompletionRequested(string param0, string param1, string param2)
        {
            WfEventSource.Instance.RuntimeTransactionCompletionRequested(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1035, Level=verbose, Channel=debug
        /// </summary>
        internal static bool RuntimeTransactionSetIsEnabled()
        {
            return WfEventSource.Instance.RuntimeTransactionSetIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1035, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: The runtime transaction has been set by Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Execution isolated to Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        internal static void RuntimeTransactionSet(string param0, string param1, string param2, string param3, string param4, string param5)
        {
            WfEventSource.Instance.RuntimeTransactionSet(param0, param1, param2, param3, param4, param5);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1021, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleBookmarkWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleBookmarkWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.
        /// Event description ID=1021, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param1">Parameter 1 for event: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param2">Parameter 2 for event: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param3">Parameter 3 for event: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param4">Parameter 4 for event: A BookmarkWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        internal static void ScheduleBookmarkWorkItem(string param0, string param1, string param2, string param3, string param4)
        {
            WfEventSource.Instance.ScheduleBookmarkWorkItem(param0, param1, param2, param3, param4);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1017, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleCancelActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleCancelActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A CancelActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1017, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A CancelActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A CancelActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A CancelActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void ScheduleCancelActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.ScheduleCancelActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1014, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleCompletionWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleCompletionWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1014, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: A CompletionWorkItem has been scheduled for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        internal static void ScheduleCompletionWorkItem(string param0, string param1, string param2, string param3, string param4, string param5)
        {
            WfEventSource.Instance.ScheduleCompletionWorkItem(param0, param1, param2, param3, param4, param5);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1011, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleExecuteActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleExecuteActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: An ExecuteActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1011, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: An ExecuteActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: An ExecuteActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: An ExecuteActivityWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void ScheduleExecuteActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.ScheduleExecuteActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1029, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleFaultWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleFaultWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1029, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: A FaultWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void ScheduleFaultWorkItem(string param0, string param1, string param2, string param3, string param4, string param5, System.Exception exception)
        {
            WfEventSource.Instance.ScheduleFaultWorkItem(param0, param1, param2, param3, param4, param5, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1032, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleRuntimeWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleRuntimeWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A runtime work item has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1032, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A runtime work item has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A runtime work item has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A runtime work item has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void ScheduleRuntimeWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.ScheduleRuntimeWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1026, Level=verbose, Channel=debug
        /// </summary>
        internal static bool ScheduleTransactionContextWorkItemIsEnabled()
        {
            return WfEventSource.Instance.ScheduleTransactionContextWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: A TransactionContextWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1026, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: A TransactionContextWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: A TransactionContextWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: A TransactionContextWorkItem has been scheduled for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void ScheduleTransactionContextWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.ScheduleTransactionContextWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1022, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartBookmarkWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartBookmarkWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.
        /// Event description ID=1022, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param3">Parameter 3 for event: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        /// <param name="param4">Parameter 4 for event: Starting execution of a BookmarkWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  BookmarkName: {3}, BookmarkScope: {4}.</param>
        internal static void StartBookmarkWorkItem(string param0, string param1, string param2, string param3, string param4)
        {
            WfEventSource.Instance.StartBookmarkWorkItem(param0, param1, param2, param3, param4);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1018, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartCancelActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartCancelActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a CancelActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1018, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a CancelActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a CancelActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a CancelActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void StartCancelActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.StartCancelActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1015, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartCompletionWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartCompletionWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1015, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: Starting execution of a CompletionWorkItem for parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'. Completed Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        internal static void StartCompletionWorkItem(string param0, string param1, string param2, string param3, string param4, string param5)
        {
            WfEventSource.Instance.StartCompletionWorkItem(param0, param1, param2, param3, param4, param5);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1012, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartExecuteActivityWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartExecuteActivityWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of an ExecuteActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1012, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of an ExecuteActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of an ExecuteActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of an ExecuteActivityWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void StartExecuteActivityWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.StartExecuteActivityWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1030, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartFaultWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartFaultWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1030, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: Starting execution of a FaultWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.  The exception was propagated from Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void StartFaultWorkItem(string param0, string param1, string param2, string param3, string param4, string param5, System.Exception exception)
        {
            WfEventSource.Instance.StartFaultWorkItem(param0, param1, param2, param3, param4, param5, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1033, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartRuntimeWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartRuntimeWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a runtime work item for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1033, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a runtime work item for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a runtime work item for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a runtime work item for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void StartRuntimeWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.StartRuntimeWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1027, Level=verbose, Channel=debug
        /// </summary>
        internal static bool StartTransactionContextWorkItemIsEnabled()
        {
            return WfEventSource.Instance.StartTransactionContextWorkItemIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Starting execution of a TransactionContextWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.
        /// Event description ID=1027, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Starting execution of a TransactionContextWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: Starting execution of a TransactionContextWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: Starting execution of a TransactionContextWorkItem for Activity '{0}', DisplayName: '{1}', InstanceId: '{2}'.</param>
        internal static void StartTransactionContextWorkItem(string param0, string param1, string param2)
        {
            WfEventSource.Instance.StartTransactionContextWorkItem(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1025, Level=verbose, Channel=debug
        /// </summary>
        internal static bool BookmarkScopeInitializedIsEnabled()
        {
            return WfEventSource.Instance.BookmarkScopeInitializedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The BookmarkScope that had TemporaryId: '{0}' has been initialized with Id: '{1}'.
        /// Event description ID=1025, Level=verbose, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: The BookmarkScope that had TemporaryId: '{0}' has been initialized with Id: '{1}'.</param>
        /// <param name="param1">Parameter 1 for event: The BookmarkScope that had TemporaryId: '{0}' has been initialized with Id: '{1}'.</param>
        internal static void BookmarkScopeInitialized(string param0, string param1)
        {
            WfEventSource.Instance.BookmarkScopeInitialized(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1104, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowActivityResumeIsEnabled()
        {
            return WfEventSource.Instance.WorkflowActivityResumeIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' E2E Activity
        /// Event description ID=1104, Level=informational, Channel=debug
        /// </summary>
        /// <param name="Id">Parameter 0 for event: WorkflowInstance Id: '{0}' E2E Activity</param>
        internal static void WorkflowActivityResume(System.Guid Id)
        {
            WfEventSource.Instance.WorkflowActivityResume(Id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1101, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowActivityStartIsEnabled()
        {
            return WfEventSource.Instance.WorkflowActivityStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' E2E Activity
        /// Event description ID=1101, Level=informational, Channel=debug
        /// </summary>
        /// <param name="Id">Parameter 0 for event: WorkflowInstance Id: '{0}' E2E Activity</param>
        internal static void WorkflowActivityStart(System.Guid Id)
        {
            WfEventSource.Instance.WorkflowActivityStart(Id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1102, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowActivityStopIsEnabled()
        {
            return WfEventSource.Instance.WorkflowActivityStopIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' E2E Activity
        /// Event description ID=1102, Level=informational, Channel=debug
        /// </summary>
        /// <param name="Id">Parameter 0 for event: WorkflowInstance Id: '{0}' E2E Activity</param>
        internal static void WorkflowActivityStop(System.Guid Id)
        {
            WfEventSource.Instance.WorkflowActivityStop(Id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1103, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowActivitySuspendIsEnabled()
        {
            return WfEventSource.Instance.WorkflowActivitySuspendIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' E2E Activity
        /// Event description ID=1103, Level=informational, Channel=debug
        /// </summary>
        /// <param name="Id">Parameter 0 for event: WorkflowInstance Id: '{0}' E2E Activity</param>
        internal static void WorkflowActivitySuspend(System.Guid Id)
        {
            WfEventSource.Instance.WorkflowActivitySuspend(Id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1010, Level=informational, Channel=debug
        /// </summary>
        internal static bool ActivityCompletedIsEnabled()
        {
            return WfEventSource.Instance.ActivityCompletedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.
        /// Event description ID=1010, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.</param>
        /// <param name="param1">Parameter 1 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.</param>
        /// <param name="param2">Parameter 2 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.</param>
        /// <param name="param3">Parameter 3 for event: Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' has completed in the '{3}' state.</param>
        internal static void ActivityCompleted(string param0, string param1, string param2, string param3)
        {
            WfEventSource.Instance.ActivityCompleted(param0, param1, param2, param3);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1009, Level=informational, Channel=debug
        /// </summary>
        internal static bool ActivityScheduledIsEnabled()
        {
            return WfEventSource.Instance.ActivityScheduledIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.
        /// Event description ID=1009, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param1">Parameter 1 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param2">Parameter 2 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param3">Parameter 3 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param4">Parameter 4 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        /// <param name="param5">Parameter 5 for event: Parent Activity '{0}', DisplayName: '{1}', InstanceId: '{2}' scheduled child Activity '{3}', DisplayName: '{4}', InstanceId: '{5}'.</param>
        internal static void ActivityScheduled(string param0, string param1, string param2, string param3, string param4, string param5)
        {
            WfEventSource.Instance.ActivityScheduled(param0, param1, param2, param3, param4, param5);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1004, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowInstanceAbortedIsEnabled()
        {
            return WfEventSource.Instance.WorkflowInstanceAbortedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' was aborted with an exception.
        /// Event description ID=1004, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowInstance Id: '{0}' was aborted with an exception.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void WorkflowInstanceAborted(string param0, System.Exception exception)
        {
            WfEventSource.Instance.WorkflowInstanceAborted(param0, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1003, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowInstanceCanceledIsEnabled()
        {
            return WfEventSource.Instance.WorkflowInstanceCanceledIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' has completed in the Canceled state.
        /// Event description ID=1003, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowInstance Id: '{0}' has completed in the Canceled state.</param>
        internal static void WorkflowInstanceCanceled(string param0)
        {
            WfEventSource.Instance.WorkflowInstanceCanceled(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1001, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationCompletedIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationCompletedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' has completed in the Closed state.
        /// Event description ID=1001, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowInstance Id: '{0}' has completed in the Closed state.</param>
        internal static void WorkflowApplicationCompleted(string param0)
        {
            WfEventSource.Instance.WorkflowApplicationCompleted(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1005, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationIdledIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationIdledIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowApplication Id: '{0}' went idle.
        /// Event description ID=1005, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowApplication Id: '{0}' went idle.</param>
        internal static void WorkflowApplicationIdled(string param0)
        {
            WfEventSource.Instance.WorkflowApplicationIdled(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1041, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationPersistableIdleIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationPersistableIdleIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowApplication Id: '{0}' is idle and persistable.  The following action will be taken: {1}.
        /// Event description ID=1041, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowApplication Id: '{0}' is idle and persistable.  The following action will be taken: {1}.</param>
        /// <param name="param1">Parameter 1 for event: WorkflowApplication Id: '{0}' is idle and persistable.  The following action will be taken: {1}.</param>
        internal static void WorkflowApplicationPersistableIdle(string param0, string param1)
        {
            WfEventSource.Instance.WorkflowApplicationPersistableIdle(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1007, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationPersistedIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationPersistedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowApplication Id: '{0}' was Persisted.
        /// Event description ID=1007, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowApplication Id: '{0}' was Persisted.</param>
        internal static void WorkflowApplicationPersisted(string param0)
        {
            WfEventSource.Instance.WorkflowApplicationPersisted(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1002, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationTerminatedIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationTerminatedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowApplication Id: '{0}' was terminated. It has completed in the Faulted state with an exception.
        /// Event description ID=1002, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowApplication Id: '{0}' was terminated. It has completed in the Faulted state with an exception.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void WorkflowApplicationTerminated(string param0, System.Exception exception)
        {
            WfEventSource.Instance.WorkflowApplicationTerminated(param0, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1006, Level=error, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationUnhandledExceptionIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationUnhandledExceptionIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.
        /// Event description ID=1006, Level=error, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.</param>
        /// <param name="param1">Parameter 1 for event: WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.</param>
        /// <param name="param2">Parameter 2 for event: WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.</param>
        /// <param name="param3">Parameter 3 for event: WorkflowInstance Id: '{0}' has encountered an unhandled exception.  The exception originated from Activity '{1}', DisplayName: '{2}'.  The following action will be taken: {3}.</param>
        /// <param name="exception">Exception associated with the event</param>
        internal static void WorkflowApplicationUnhandledException(string param0, string param1, string param2, string param3, System.Exception exception)
        {
            WfEventSource.Instance.WorkflowApplicationUnhandledException(param0, param1, param2, param3, exception.ToString());
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1008, Level=informational, Channel=debug
        /// </summary>
        internal static bool WorkflowApplicationUnloadedIsEnabled()
        {
            return WfEventSource.Instance.WorkflowApplicationUnloadedIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: WorkflowInstance Id: '{0}' was Unloaded.
        /// Event description ID=1008, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: WorkflowInstance Id: '{0}' was Unloaded.</param>
        internal static void WorkflowApplicationUnloaded(string param0)
        {
            WfEventSource.Instance.WorkflowApplicationUnloaded(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1124, Level=Informational, Channel=debug
        /// </summary>
        internal static bool InvokeMethodIsStaticIsEnabled()
        {
            return WfEventSource.Instance.InvokeMethodIsStaticIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InvokeMethod '{0}' - method is Static.
        /// Event description ID=1124, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: InvokeMethod '{0}' - method is Static.</param>
        internal static void InvokeMethodIsStatic(string param0)
        {
            WfEventSource.Instance.InvokeMethodIsStatic(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1125, Level=Informational, Channel=debug
        /// </summary>
        internal static bool InvokeMethodIsNotStaticIsEnabled()
        {
            return WfEventSource.Instance.InvokeMethodIsNotStaticIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InvokeMethod '{0}' - method is not Static.
        /// Event description ID=1125, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: InvokeMethod '{0}' - method is not Static.</param>
        internal static void InvokeMethodIsNotStatic(string param0)
        {
            WfEventSource.Instance.InvokeMethodIsNotStatic(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1126, Level=Informational, Channel=debug
        /// </summary>
        internal static bool InvokedMethodThrewExceptionIsEnabled()
        {
            return WfEventSource.Instance.InvokedMethodThrewExceptionIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: An exception was thrown in the method called by the activity '{0}'. {1}
        /// Event description ID=1126, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: An exception was thrown in the method called by the activity '{0}'. {1}</param>
        /// <param name="param1">Parameter 1 for event: An exception was thrown in the method called by the activity '{0}'. {1}</param>
        internal static void InvokedMethodThrewException(string param0, string param1)
        {
            WfEventSource.Instance.InvokedMethodThrewException(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1131, Level=Informational, Channel=debug
        /// </summary>
        internal static bool InvokeMethodUseAsyncPatternIsEnabled()
        {
            return WfEventSource.Instance.InvokeMethodUseAsyncPatternIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InvokeMethod '{0}' - method uses asynchronous pattern of '{1}' and '{2}'.
        /// Event description ID=1131, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: InvokeMethod '{0}' - method uses asynchronous pattern of '{1}' and '{2}'.</param>
        /// <param name="param1">Parameter 1 for event: InvokeMethod '{0}' - method uses asynchronous pattern of '{1}' and '{2}'.</param>
        /// <param name="param2">Parameter 2 for event: InvokeMethod '{0}' - method uses asynchronous pattern of '{1}' and '{2}'.</param>
        internal static void InvokeMethodUseAsyncPattern(string param0, string param1, string param2)
        {
            WfEventSource.Instance.InvokeMethodUseAsyncPattern(param0, param1, param2);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1132, Level=Informational, Channel=debug
        /// </summary>
        internal static bool InvokeMethodDoesNotUseAsyncPatternIsEnabled()
        {
            return WfEventSource.Instance.InvokeMethodDoesNotUseAsyncPatternIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InvokeMethod '{0}' - method does not use asynchronous pattern.
        /// Event description ID=1132, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: InvokeMethod '{0}' - method does not use asynchronous pattern.</param>
        internal static void InvokeMethodDoesNotUseAsyncPattern(string param0)
        {
            WfEventSource.Instance.InvokeMethodDoesNotUseAsyncPattern(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1140, Level=Informational, Channel=debug
        /// </summary>
        internal static bool FlowchartStartIsEnabled()
        {
            return WfEventSource.Instance.FlowchartStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}' - Start has been scheduled.
        /// Event description ID=1140, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}' - Start has been scheduled.</param>
        internal static void FlowchartStart(string param0)
        {
            WfEventSource.Instance.FlowchartStart(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1141, Level=Warning, Channel=debug
        /// </summary>
        internal static bool FlowchartEmptyIsEnabled()
        {
            return WfEventSource.Instance.FlowchartEmptyIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}' - was executed with no Nodes.
        /// Event description ID=1141, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}' - was executed with no Nodes.</param>
        internal static void FlowchartEmpty(string param0)
        {
            WfEventSource.Instance.FlowchartEmpty(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1143, Level=Informational, Channel=debug
        /// </summary>
        internal static bool FlowchartNextNullIsEnabled()
        {
            return WfEventSource.Instance.FlowchartNextNullIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}'/FlowStep - Next node is null. Flowchart execution will end.
        /// Event description ID=1143, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}'/FlowStep - Next node is null. Flowchart execution will end.</param>
        internal static void FlowchartNextNull(string param0)
        {
            WfEventSource.Instance.FlowchartNextNull(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1146, Level=Informational, Channel=debug
        /// </summary>
        internal static bool FlowchartSwitchCaseIsEnabled()
        {
            return WfEventSource.Instance.FlowchartSwitchCaseIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}'/FlowSwitch - Case '{1}' was selected.
        /// Event description ID=1146, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}'/FlowSwitch - Case '{1}' was selected.</param>
        /// <param name="param1">Parameter 1 for event: Flowchart '{0}'/FlowSwitch - Case '{1}' was selected.</param>
        internal static void FlowchartSwitchCase(string param0, string param1)
        {
            WfEventSource.Instance.FlowchartSwitchCase(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1147, Level=Informational, Channel=debug
        /// </summary>
        internal static bool FlowchartSwitchDefaultIsEnabled()
        {
            return WfEventSource.Instance.FlowchartSwitchDefaultIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}'/FlowSwitch - Default Case was selected.
        /// Event description ID=1147, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}'/FlowSwitch - Default Case was selected.</param>
        internal static void FlowchartSwitchDefault(string param0)
        {
            WfEventSource.Instance.FlowchartSwitchDefault(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1148, Level=Informational, Channel=debug
        /// </summary>
        internal static bool FlowchartSwitchCaseNotFoundIsEnabled()
        {
            return WfEventSource.Instance.FlowchartSwitchCaseNotFoundIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Flowchart '{0}'/FlowSwitch - could find neither a Case activity nor a Default Case matching the Expression result. Flowchart execution will end.
        /// Event description ID=1148, Level=Informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: Flowchart '{0}'/FlowSwitch - could find neither a Case activity nor a Default Case matching the Expression result. Flowchart execution will end.</param>
        internal static void FlowchartSwitchCaseNotFound(string param0)
        {
            WfEventSource.Instance.FlowchartSwitchCaseNotFound(param0);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=1150, Level=informational, Channel=debug
        /// </summary>
        internal static bool CompensationStateIsEnabled()
        {
            return WfEventSource.Instance.CompensationStateIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: CompensableActivity '{0}' is in the '{1}' state.
        /// Event description ID=1150, Level=informational, Channel=debug
        /// </summary>
        /// <param name="param0">Parameter 0 for event: CompensableActivity '{0}' is in the '{1}' state.</param>
        /// <param name="param1">Parameter 1 for event: CompensableActivity '{0}' is in the '{1}' state.</param>
        internal static void CompensationState(string param0, string param1)
        {
            WfEventSource.Instance.CompensationState(param0, param1);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=39460, Level=Warning, Channel=debug
        /// </summary>
        internal static bool TrackingValueNotSerializableIsEnabled()
        {
            return WfEventSource.Instance.TrackingValueNotSerializableIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: The extracted argument/variable '{0}' is not serializable.
        /// Event description ID=39460, Level=Warning, Channel=debug
        /// </summary>
        /// <param name="name">Parameter 0 for event: The extracted argument/variable '{0}' is not serializable.</param>
        internal static void TrackingValueNotSerializable(string name)
        {
            WfEventSource.Instance.TrackingValueNotSerializable(name);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2021, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool ExecuteWorkItemStartIsEnabled()
        {
            return WfEventSource.Instance.ExecuteWorkItemStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Execute work item start
        /// Event description ID=2021, Level=Verbose, Channel=debug
        /// </summary>
        internal static void ExecuteWorkItemStart()
        {
            WfEventSource.Instance.ExecuteWorkItemStart();
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2022, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool ExecuteWorkItemStopIsEnabled()
        {
            return WfEventSource.Instance.ExecuteWorkItemStopIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Execute work item stop
        /// Event description ID=2022, Level=Verbose, Channel=debug
        /// </summary>
        internal static void ExecuteWorkItemStop()
        {
            WfEventSource.Instance.ExecuteWorkItemStop();
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2024, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool InternalCacheMetadataStartIsEnabled()
        {
            return WfEventSource.Instance.InternalCacheMetadataStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InternalCacheMetadata started on activity '{0}'.
        /// Event description ID=2024, Level=Verbose, Channel=debug
        /// </summary>
        /// <param name="id">Parameter 0 for event: InternalCacheMetadata started on activity '{0}'.</param>
        internal static void InternalCacheMetadataStart(string id)
        {
            WfEventSource.Instance.InternalCacheMetadataStart(id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2025, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool InternalCacheMetadataStopIsEnabled()
        {
            return WfEventSource.Instance.InternalCacheMetadataStopIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: InternalCacheMetadata stopped on activity '{0}'.
        /// Event description ID=2025, Level=Verbose, Channel=debug
        /// </summary>
        /// <param name="id">Parameter 0 for event: InternalCacheMetadata stopped on activity '{0}'.</param>
        internal static void InternalCacheMetadataStop(string id)
        {
            WfEventSource.Instance.InternalCacheMetadataStop(id);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2026, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool CompileVbExpressionStartIsEnabled()
        {
            return WfEventSource.Instance.CompileVbExpressionStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Compiling VB expression '{0}'
        /// Event description ID=2026, Level=Verbose, Channel=debug
        /// </summary>
        /// <param name="expr">Parameter 0 for event: Compiling VB expression '{0}'</param>
        internal static void CompileVbExpressionStart(string expr)
        {
            WfEventSource.Instance.CompileVbExpressionStart(expr);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2029, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool CompileVbExpressionStopIsEnabled()
        {
            return WfEventSource.Instance.CompileVbExpressionStopIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: Finished compiling VB expression.
        /// Event description ID=2029, Level=Verbose, Channel=debug
        /// </summary>
        internal static void CompileVbExpressionStop()
        {
            WfEventSource.Instance.CompileVbExpressionStop();
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2027, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool CacheRootMetadataStartIsEnabled()
        {
            return WfEventSource.Instance.CacheRootMetadataStartIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: CacheRootMetadata started on activity '{0}'
        /// Event description ID=2027, Level=Verbose, Channel=debug
        /// </summary>
        /// <param name="activityName">Parameter 0 for event: CacheRootMetadata started on activity '{0}'</param>
        internal static void CacheRootMetadataStart(string activityName)
        {
            WfEventSource.Instance.CacheRootMetadataStart(activityName);
        }

        /// <summary>
        /// Check if trace definition is enabled
        /// Event description ID=2028, Level=Verbose, Channel=debug
        /// </summary>
        internal static bool CacheRootMetadataStopIsEnabled()
        {
            return WfEventSource.Instance.CacheRootMetadataStopIsEnabled();
        }

        /// <summary>
        /// Gets trace definition like: CacheRootMetadata stopped on activity {0}.
        /// Event description ID=2028, Level=Verbose, Channel=debug
        /// </summary>
        /// <param name="activityName">Parameter 0 for event: CacheRootMetadata stopped on activity {0}.</param>
        internal static void CacheRootMetadataStop(string activityName)
        {
            WfEventSource.Instance.CacheRootMetadataStop(activityName);
        }

        internal static void TraceTransfer(Guid newId)
        {
            if (IsEnd2EndActivityTracingEnabled())
            {
                Guid activityId = WfEventSource.CurrentThreadActivityId;
                if (newId != activityId)
                {
                    if (WfEventSource.Instance.TransferEmittedIsEnabled())
                    {
                        WfEventSource.Instance.TransferEmitted(newId);
                    }
                }
            }
        }

        internal static bool ShouldTraceToTraceSource(EventLevel level)
        {
            return WfEventSource.Instance.IsEnabled(level, EventKeywords.All);
        }

        internal static bool IsEnd2EndActivityTracingEnabled()
        {
            return false;
        }
    }
}

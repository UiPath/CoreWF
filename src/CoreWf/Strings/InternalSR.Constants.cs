// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Resources;

namespace CoreWf
{
    partial class SR : StringResourceBase
    {
        internal static string ActionItemIsAlreadyScheduled { get { return SR.Instance["ActionItemIsAlreadyScheduled"]; } }
        internal static string ArgumentNullOrEmpty(object p0) { return SR.Instance["ArgumentNullOrEmpty", p0]; }
        internal static string AsyncCallbackThrewException { get { return SR.Instance["AsyncCallbackThrewException"]; } }
        internal static string AsyncResultAlreadyEnded { get { return SR.Instance["AsyncResultAlreadyEnded"]; } }
        internal static string FailFastMessage(object p0) { return SR.Instance["FailFastMessage", p0]; }
        internal static string IncompatibleArgumentType(object p0, object p1) { return SR.Instance["IncompatibleArgumentType", p0, p1]; }
        internal static string InvalidAsyncResult { get { return SR.Instance["InvalidAsyncResult"]; } }
        internal static string InvalidSemaphoreExit { get { return SR.Instance["InvalidSemaphoreExit"]; } }
        internal static string LockTimeoutExceptionMessage(object p0) { return SR.Instance["LockTimeoutExceptionMessage", p0]; }
        internal static string MustCancelOldTimer { get { return SR.Instance["MustCancelOldTimer"]; } }
        internal static string BufferAllocationFailed(object p0) { return SR.Instance["BufferAllocationFailed", p0]; }
        internal static string BufferIsNotRightSizeForBufferManager { get { return SR.Instance["BufferIsNotRightSizeForBufferManager"]; } }
        internal static string BufferedOutputStreamQuotaExceeded(object p0) { return SR.Instance["BufferedOutputStreamQuotaExceeded", p0]; }
        internal static string ReadNotSupported { get { return SR.Instance["ReadNotSupported"]; } }
        internal static string SeekNotSupported { get { return SR.Instance["SeekNotSupported"]; } }
        internal static string ShipAssertExceptionMessage(object p0) { return SR.Instance["ShipAssertExceptionMessage", p0]; }
        internal static string ThreadNeutralSemaphoreAborted { get { return SR.Instance["ThreadNeutralSemaphoreAborted"]; } }
        internal static string TimeoutInputQueueDequeue(object p0) { return SR.Instance["TimeoutInputQueueDequeue", p0]; }
        internal static string TimeoutMustBeNonNegative(object p0, object p1) { return SR.Instance["TimeoutMustBeNonNegative", p0, p1]; }
        internal static string TimeoutMustBePositive(object p0, object p1) { return SR.Instance["TimeoutMustBePositive", p0, p1]; }
        internal static string CannotConvertObject(object p0, object p1) { return SR.Instance["CannotConvertObject", p0, p1]; }
        internal static string ValueMustBeNonNegative { get { return SR.Instance["ValueMustBeNonNegative"]; } }
        internal static string EtwAPIMaxStringCountExceeded(object p0) { return SR.Instance["EtwAPIMaxStringCountExceeded", p0]; }
        internal static string EtwMaxNumberArgumentsExceeded(object p0) { return SR.Instance["EtwMaxNumberArgumentsExceeded", p0]; }
        internal static string EtwRegistrationFailed(object p0) { return SR.Instance["EtwRegistrationFailed", p0]; }
        internal static string KeyNotFoundInDictionary { get { return SR.Instance["KeyNotFoundInDictionary"]; } }
        internal static string InvalidAsyncResultImplementation(object p0) { return SR.Instance["InvalidAsyncResultImplementation", p0]; }
        internal static string InvalidAsyncResultImplementationGeneric { get { return SR.Instance["InvalidAsyncResultImplementationGeneric"]; } }
        internal static string AsyncResultCompletedTwice(object p0) { return SR.Instance["AsyncResultCompletedTwice", p0]; }
        internal static string InvalidNullAsyncResult { get { return SR.Instance["InvalidNullAsyncResult"]; } }
        internal static string AsyncEventArgsCompletedTwice(object p0) { return SR.Instance["AsyncEventArgsCompletedTwice", p0]; }
        internal static string AsyncEventArgsCompletionPending(object p0) { return SR.Instance["AsyncEventArgsCompletionPending", p0]; }
        internal static string NullKeyAlreadyPresent { get { return SR.Instance["NullKeyAlreadyPresent"]; } }
        internal static string KeyCollectionUpdatesNotAllowed { get { return SR.Instance["KeyCollectionUpdatesNotAllowed"]; } }
        internal static string ValueCollectionUpdatesNotAllowed { get { return SR.Instance["ValueCollectionUpdatesNotAllowed"]; } }
        internal static string SFxTaskNotStarted { get { return SR.Instance["SFxTaskNotStarted"]; } }
        internal static string TaskTimedOutError(object p0) { return SR.Instance["TaskTimedOutError", p0]; }
    }
}

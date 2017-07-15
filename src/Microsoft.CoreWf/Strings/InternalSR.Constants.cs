// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Resources;

namespace CoreWf
{
    internal class InternalSR : StringResourceBase
    {
        internal static readonly InternalSR Instance = new InternalSR();

        private ResourceManager _resourceManager;

        protected internal override ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("CoreWf.Strings.InternalSR", typeof(InternalSR).GetTypeInfo().Assembly);
                }
                return _resourceManager;
            }
        }

        internal static string ActionItemIsAlreadyScheduled { get { return InternalSR.Instance["ActionItemIsAlreadyScheduled"]; } }
        internal static string ArgumentNullOrEmpty(object p0) { return InternalSR.Instance["ArgumentNullOrEmpty", p0]; }
        internal static string AsyncCallbackThrewException { get { return InternalSR.Instance["AsyncCallbackThrewException"]; } }
        internal static string AsyncResultAlreadyEnded { get { return InternalSR.Instance["AsyncResultAlreadyEnded"]; } }
        internal static string DictionaryIsReadOnly { get { return InternalSR.Instance["DictionaryIsReadOnly"]; } }
        internal static string FailFastMessage(object p0) { return InternalSR.Instance["FailFastMessage", p0]; }
        internal static string IncompatibleArgumentType(object p0, object p1) { return InternalSR.Instance["IncompatibleArgumentType", p0, p1]; }
        internal static string InvalidAsyncResult { get { return InternalSR.Instance["InvalidAsyncResult"]; } }
        internal static string InvalidSemaphoreExit { get { return InternalSR.Instance["InvalidSemaphoreExit"]; } }
        internal static string LockTimeoutExceptionMessage(object p0) { return InternalSR.Instance["LockTimeoutExceptionMessage", p0]; }
        internal static string MustCancelOldTimer { get { return InternalSR.Instance["MustCancelOldTimer"]; } }
        internal static string BufferAllocationFailed(object p0) { return InternalSR.Instance["BufferAllocationFailed", p0]; }
        internal static string BufferIsNotRightSizeForBufferManager { get { return InternalSR.Instance["BufferIsNotRightSizeForBufferManager"]; } }
        internal static string BufferedOutputStreamQuotaExceeded(object p0) { return InternalSR.Instance["BufferedOutputStreamQuotaExceeded", p0]; }
        internal static string ReadNotSupported { get { return InternalSR.Instance["ReadNotSupported"]; } }
        internal static string SeekNotSupported { get { return InternalSR.Instance["SeekNotSupported"]; } }
        internal static string ShipAssertExceptionMessage(object p0) { return InternalSR.Instance["ShipAssertExceptionMessage", p0]; }
        internal static string ThreadNeutralSemaphoreAborted { get { return InternalSR.Instance["ThreadNeutralSemaphoreAborted"]; } }
        internal static string TimeoutInputQueueDequeue(object p0) { return InternalSR.Instance["TimeoutInputQueueDequeue", p0]; }
        internal static string TimeoutMustBeNonNegative(object p0, object p1) { return InternalSR.Instance["TimeoutMustBeNonNegative", p0, p1]; }
        internal static string TimeoutMustBePositive(object p0, object p1) { return InternalSR.Instance["TimeoutMustBePositive", p0, p1]; }
        internal static string TimeoutOnOperation(object p0) { return InternalSR.Instance["TimeoutOnOperation", p0]; }
        internal static string CannotConvertObject(object p0, object p1) { return InternalSR.Instance["CannotConvertObject", p0, p1]; }
        internal static string ValueMustBeNonNegative { get { return InternalSR.Instance["ValueMustBeNonNegative"]; } }
        internal static string EtwAPIMaxStringCountExceeded(object p0) { return InternalSR.Instance["EtwAPIMaxStringCountExceeded", p0]; }
        internal static string EtwMaxNumberArgumentsExceeded(object p0) { return InternalSR.Instance["EtwMaxNumberArgumentsExceeded", p0]; }
        internal static string EtwRegistrationFailed(object p0) { return InternalSR.Instance["EtwRegistrationFailed", p0]; }
        internal static string BadCopyToArray { get { return InternalSR.Instance["BadCopyToArray"]; } }
        internal static string KeyNotFoundInDictionary { get { return InternalSR.Instance["KeyNotFoundInDictionary"]; } }
        internal static string InvalidAsyncResultImplementation(object p0) { return InternalSR.Instance["InvalidAsyncResultImplementation", p0]; }
        internal static string InvalidAsyncResultImplementationGeneric { get { return InternalSR.Instance["InvalidAsyncResultImplementationGeneric"]; } }
        internal static string AsyncResultCompletedTwice(object p0) { return InternalSR.Instance["AsyncResultCompletedTwice", p0]; }
        internal static string InvalidNullAsyncResult { get { return InternalSR.Instance["InvalidNullAsyncResult"]; } }
        internal static string AsyncEventArgsCompletedTwice(object p0) { return InternalSR.Instance["AsyncEventArgsCompletedTwice", p0]; }
        internal static string AsyncEventArgsCompletionPending(object p0) { return InternalSR.Instance["AsyncEventArgsCompletionPending", p0]; }
        internal static string NullKeyAlreadyPresent { get { return InternalSR.Instance["NullKeyAlreadyPresent"]; } }
        internal static string KeyCollectionUpdatesNotAllowed { get { return InternalSR.Instance["KeyCollectionUpdatesNotAllowed"]; } }
        internal static string ValueCollectionUpdatesNotAllowed { get { return InternalSR.Instance["ValueCollectionUpdatesNotAllowed"]; } }
        internal static string SFxTaskNotStarted { get { return InternalSR.Instance["SFxTaskNotStarted"]; } }
        internal static string TaskTimedOutError(object p0) { return InternalSR.Instance["TaskTimedOutError", p0]; }
    }
}

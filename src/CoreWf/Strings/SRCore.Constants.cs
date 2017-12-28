// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Resources;

namespace CoreWf
{
    partial class SR : StringResourceBase
    {
        internal static string IncorrectValueType(object p0, object p1) { return SR.Instance["IncorrectValueType", p0, p1]; }
        internal static string NullAssignedToValueType(object p0) { return SR.Instance["NullAssignedToValueType", p0]; }
        internal static string PersistenceInitializerThrew { get { return SR.Instance["PersistenceInitializerThrew"]; } }
        internal static string InvalidStateInAsyncResult { get { return SR.Instance["InvalidStateInAsyncResult"]; } }
        internal static string ExtensionsCannotBeSetByIndex { get { return SR.Instance["ExtensionsCannotBeSetByIndex"]; } }
        internal static string CouldNotResolveNamespacePrefix(object p0) { return SR.Instance["CouldNotResolveNamespacePrefix", p0]; }
        internal static string CannotCreateContextWithNullId { get { return SR.Instance["CannotCreateContextWithNullId"]; } }
        internal static string CannotReplaceTransaction { get { return SR.Instance["CannotReplaceTransaction"]; } }
        internal static string CommandExecutionCannotOverlap { get { return SR.Instance["CommandExecutionCannotOverlap"]; } }
        internal static string CompletedMustNotHaveAssociatedKeys { get { return SR.Instance["CompletedMustNotHaveAssociatedKeys"]; } }
        internal static string ContextAlreadyBoundToInstance { get { return SR.Instance["ContextAlreadyBoundToInstance"]; } }
        internal static string ContextAlreadyBoundToLock { get { return SR.Instance["ContextAlreadyBoundToLock"]; } }
        internal static string ContextAlreadyBoundToOwner { get { return SR.Instance["ContextAlreadyBoundToOwner"]; } }
        internal static string ContextMustBeBoundToInstance { get { return SR.Instance["ContextMustBeBoundToInstance"]; } }
        internal static string ContextMustBeBoundToOwner { get { return SR.Instance["ContextMustBeBoundToOwner"]; } }
        internal static string ContextNotFromThisStore { get { return SR.Instance["ContextNotFromThisStore"]; } }
        internal static string GenericInstanceCommand(object p0) { return SR.Instance["GenericInstanceCommand", p0]; }
        internal static string GenericInstanceCommandNull { get { return SR.Instance["GenericInstanceCommandNull"]; } }
        internal static string GetParameterTypeMismatch(object p0, object p1) { return SR.Instance["GetParameterTypeMismatch", p0, p1]; }
        internal static string HandleFreed { get { return SR.Instance["HandleFreed"]; } }
        internal static string HandleFreedBeforeInitialized { get { return SR.Instance["HandleFreedBeforeInitialized"]; } }
        internal static string InitialMetadataCannotBeDeleted(object p0) { return SR.Instance["InitialMetadataCannotBeDeleted", p0]; }
        internal static string InstanceOperationRequiresInstance { get { return SR.Instance["InstanceOperationRequiresInstance"]; } }
        internal static string InstanceOperationRequiresLock { get { return SR.Instance["InstanceOperationRequiresLock"]; } }
        internal static string InstanceOperationRequiresNotCompleted { get { return SR.Instance["InstanceOperationRequiresNotCompleted"]; } }
        internal static string InstanceOperationRequiresNotUninitialized { get { return SR.Instance["InstanceOperationRequiresNotUninitialized"]; } }
        internal static string InstanceOperationRequiresOwner { get { return SR.Instance["InstanceOperationRequiresOwner"]; } }
        internal static string InvalidInstanceState { get { return SR.Instance["InvalidInstanceState"]; } }
        internal static string InvalidKeyArgument { get { return SR.Instance["InvalidKeyArgument"]; } }
        internal static string InvalidLockToken { get { return SR.Instance["InvalidLockToken"]; } }
        internal static string KeyAlreadyAssociated { get { return SR.Instance["KeyAlreadyAssociated"]; } }
        internal static string KeyAlreadyCompleted { get { return SR.Instance["KeyAlreadyCompleted"]; } }
        internal static string KeyAlreadyUnassociated { get { return SR.Instance["KeyAlreadyUnassociated"]; } }
        internal static string KeyNotAssociated { get { return SR.Instance["KeyNotAssociated"]; } }
        internal static string KeyNotCompleted { get { return SR.Instance["KeyNotCompleted"]; } }
        internal static string LoadedWriteOnlyValue { get { return SR.Instance["LoadedWriteOnlyValue"]; } }
        internal static string MetadataCannotContainNullKey { get { return SR.Instance["MetadataCannotContainNullKey"]; } }
        internal static string MetadataCannotContainNullValue(object p0) { return SR.Instance["MetadataCannotContainNullValue", p0]; }
        internal static string MustSetTransactionOnFirstCall { get { return SR.Instance["MustSetTransactionOnFirstCall"]; } }
        internal static string OnFreeInstanceHandleThrew { get { return SR.Instance["OnFreeInstanceHandleThrew"]; } }
        internal static string OutsideInstanceExecutionScope(object p0) { return SR.Instance["OutsideInstanceExecutionScope", p0]; }
        internal static string OutsideTransactionalCommand(object p0) { return SR.Instance["OutsideTransactionalCommand", p0]; }
        internal static string ProviderDoesNotSupportCommand(object p0) { return SR.Instance["ProviderDoesNotSupportCommand", p0]; }
        internal static string TransactionInDoubtNonHost { get { return SR.Instance["TransactionInDoubtNonHost"]; } }
        internal static string TransactionRolledBackNonHost { get { return SR.Instance["TransactionRolledBackNonHost"]; } }
        internal static string UninitializedCannotHaveData { get { return SR.Instance["UninitializedCannotHaveData"]; } }
        internal static string CannotCompleteWithKeys { get { return SR.Instance["CannotCompleteWithKeys"]; } }
        internal static string OnCancelRequestedThrew { get { return SR.Instance["OnCancelRequestedThrew"]; } }
        internal static string AlreadyBoundToInstance { get { return SR.Instance["AlreadyBoundToInstance"]; } }
        internal static string AlreadyBoundToOwner { get { return SR.Instance["AlreadyBoundToOwner"]; } }
        internal static string InstanceRequired { get { return SR.Instance["InstanceRequired"]; } }
        internal static string LoadOpAssociateKeysCannotContainLookupKey { get { return SR.Instance["LoadOpAssociateKeysCannotContainLookupKey"]; } }
        internal static string LoadOpFreeKeyRequiresAcceptUninitialized { get { return SR.Instance["LoadOpFreeKeyRequiresAcceptUninitialized"]; } }
        internal static string LoadOpKeyMustBeValid { get { return SR.Instance["LoadOpKeyMustBeValid"]; } }
        internal static string OwnerRequired { get { return SR.Instance["OwnerRequired"]; } }
        internal static string ValidateUnlockInstance { get { return SR.Instance["ValidateUnlockInstance"]; } }
        internal static string InstanceKeyRequiresValidGuid { get { return SR.Instance["InstanceKeyRequiresValidGuid"]; } }
        internal static string AsyncTransactionException { get { return SR.Instance["AsyncTransactionException"]; } }
        internal static string ExecuteMustBeNested { get { return SR.Instance["ExecuteMustBeNested"]; } }
        internal static string TryCommandCannotExecuteSubCommandsAndReduce { get { return SR.Instance["TryCommandCannotExecuteSubCommandsAndReduce"]; } }
        internal static string CannotAcquireLockDefault { get { return SR.Instance["CannotAcquireLockDefault"]; } }
        internal static string CannotAcquireLockSpecific(object p0) { return SR.Instance["CannotAcquireLockSpecific", p0]; }
        internal static string InstanceNotReadyDefault { get { return SR.Instance["InstanceNotReadyDefault"]; } }
        internal static string InstanceNotReadySpecific(object p0) { return SR.Instance["InstanceNotReadySpecific", p0]; }
        internal static string KeyNotReadyDefault { get { return SR.Instance["KeyNotReadyDefault"]; } }
        internal static string KeyNotReadySpecific(object p0) { return SR.Instance["KeyNotReadySpecific", p0]; }
        internal static string KeyCollisionDefault { get { return SR.Instance["KeyCollisionDefault"]; } }
        internal static string KeyCollisionSpecific(object p0, object p1, object p2) { return SR.Instance["KeyCollisionSpecific", p0, p1, p2]; }
        internal static string NameCollisionOnCollect(object p0, object p1) { return SR.Instance["NameCollisionOnCollect", p0, p1]; }
        internal static string NameCollisionOnMap(object p0, object p1) { return SR.Instance["NameCollisionOnMap", p0, p1]; }
        internal static string PersistencePipelineAbortThrew(object p0) { return SR.Instance["PersistencePipelineAbortThrew", p0]; }
        internal static string KeyCollisionSpecificKeyOnly(object p0) { return SR.Instance["KeyCollisionSpecificKeyOnly", p0]; }
        internal static string KeyCompleteDefault { get { return SR.Instance["KeyCompleteDefault"]; } }
        internal static string KeyCompleteSpecific(object p0) { return SR.Instance["KeyCompleteSpecific", p0]; }
        internal static string InstanceCompleteDefault { get { return SR.Instance["InstanceCompleteDefault"]; } }
        internal static string InstanceCompleteSpecific(object p0) { return SR.Instance["InstanceCompleteSpecific", p0]; }
        internal static string CannotAcquireLockSpecificWithOwner(object p0, object p1) { return SR.Instance["CannotAcquireLockSpecificWithOwner", p0, p1]; }
        internal static string InstanceCollisionDefault { get { return SR.Instance["InstanceCollisionDefault"]; } }
        internal static string InstanceCollisionSpecific(object p0) { return SR.Instance["InstanceCollisionSpecific", p0]; }
        internal static string InstanceLockLostDefault { get { return SR.Instance["InstanceLockLostDefault"]; } }
        internal static string InstanceLockLostSpecific(object p0) { return SR.Instance["InstanceLockLostSpecific", p0]; }
        internal static string InstanceOwnerDefault { get { return SR.Instance["InstanceOwnerDefault"]; } }
        internal static string InstanceOwnerSpecific(object p0) { return SR.Instance["InstanceOwnerSpecific", p0]; }
        internal static string InstanceHandleConflictDefault { get { return SR.Instance["InstanceHandleConflictDefault"]; } }
        internal static string InstanceHandleConflictSpecific(object p0) { return SR.Instance["InstanceHandleConflictSpecific", p0]; }
        internal static string BindLockRequiresCommandFlag { get { return SR.Instance["BindLockRequiresCommandFlag"]; } }
        internal static string CannotInvokeBindingFromNonBinding { get { return SR.Instance["CannotInvokeBindingFromNonBinding"]; } }
        internal static string CannotInvokeTransactionalFromNonTransactional { get { return SR.Instance["CannotInvokeTransactionalFromNonTransactional"]; } }
        internal static string DoNotCompleteTryCommandWithPendingReclaim { get { return SR.Instance["DoNotCompleteTryCommandWithPendingReclaim"]; } }
        internal static string GuidCannotBeEmpty { get { return SR.Instance["GuidCannotBeEmpty"]; } }
        internal static string InstanceStoreBoundSameVersionTwice { get { return SR.Instance["InstanceStoreBoundSameVersionTwice"]; } }
        internal static string MayBindLockCommandShouldValidateOwner { get { return SR.Instance["MayBindLockCommandShouldValidateOwner"]; } }
        internal static string StoreReportedConflictingLockTokens { get { return SR.Instance["StoreReportedConflictingLockTokens"]; } }
        internal static string TimedOutWaitingForLockResolution { get { return SR.Instance["TimedOutWaitingForLockResolution"]; } }
        internal static string BindReclaimedLockException { get { return SR.Instance["BindReclaimedLockException"]; } }
        internal static string BindReclaimSucceeded { get { return SR.Instance["BindReclaimSucceeded"]; } }
        internal static string OwnerBelongsToWrongStore { get { return SR.Instance["OwnerBelongsToWrongStore"]; } }
        internal static string WaitAlreadyInProgress { get { return SR.Instance["WaitAlreadyInProgress"]; } }
        internal static string WaitForEventsTimedOut(object p0) { return SR.Instance["WaitForEventsTimedOut", p0]; }
    }
}

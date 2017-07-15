// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Resources;

namespace CoreWf
{
    internal class SRCore : StringResourceBase
    {
        internal static readonly SRCore Instance = new SRCore();

        private ResourceManager _resourceManager;

        protected internal override ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("CoreWf.Strings.SRCore", typeof(SRCore).GetTypeInfo().Assembly);
                }
                return _resourceManager;
            }
        }

        internal static string IncorrectValueType(object p0, object p1) { return SRCore.Instance["IncorrectValueType", p0, p1]; }
        internal static string NullAssignedToValueType(object p0) { return SRCore.Instance["NullAssignedToValueType", p0]; }
        internal static string PersistenceInitializerThrew { get { return SRCore.Instance["PersistenceInitializerThrew"]; } }
        internal static string InvalidStateInAsyncResult { get { return SRCore.Instance["InvalidStateInAsyncResult"]; } }
        internal static string ExtensionsCannotBeSetByIndex { get { return SRCore.Instance["ExtensionsCannotBeSetByIndex"]; } }
        internal static string CouldNotResolveNamespacePrefix(object p0) { return SRCore.Instance["CouldNotResolveNamespacePrefix", p0]; }
        internal static string CannotCreateContextWithNullId { get { return SRCore.Instance["CannotCreateContextWithNullId"]; } }
        internal static string CannotReplaceTransaction { get { return SRCore.Instance["CannotReplaceTransaction"]; } }
        internal static string CommandExecutionCannotOverlap { get { return SRCore.Instance["CommandExecutionCannotOverlap"]; } }
        internal static string CompletedMustNotHaveAssociatedKeys { get { return SRCore.Instance["CompletedMustNotHaveAssociatedKeys"]; } }
        internal static string ContextAlreadyBoundToInstance { get { return SRCore.Instance["ContextAlreadyBoundToInstance"]; } }
        internal static string ContextAlreadyBoundToLock { get { return SRCore.Instance["ContextAlreadyBoundToLock"]; } }
        internal static string ContextAlreadyBoundToOwner { get { return SRCore.Instance["ContextAlreadyBoundToOwner"]; } }
        internal static string ContextMustBeBoundToInstance { get { return SRCore.Instance["ContextMustBeBoundToInstance"]; } }
        internal static string ContextMustBeBoundToOwner { get { return SRCore.Instance["ContextMustBeBoundToOwner"]; } }
        internal static string ContextNotFromThisStore { get { return SRCore.Instance["ContextNotFromThisStore"]; } }
        internal static string GenericInstanceCommand(object p0) { return SRCore.Instance["GenericInstanceCommand", p0]; }
        internal static string GenericInstanceCommandNull { get { return SRCore.Instance["GenericInstanceCommandNull"]; } }
        internal static string GetParameterTypeMismatch(object p0, object p1) { return SRCore.Instance["GetParameterTypeMismatch", p0, p1]; }
        internal static string HandleFreed { get { return SRCore.Instance["HandleFreed"]; } }
        internal static string HandleFreedBeforeInitialized { get { return SRCore.Instance["HandleFreedBeforeInitialized"]; } }
        internal static string InitialMetadataCannotBeDeleted(object p0) { return SRCore.Instance["InitialMetadataCannotBeDeleted", p0]; }
        internal static string InstanceOperationRequiresInstance { get { return SRCore.Instance["InstanceOperationRequiresInstance"]; } }
        internal static string InstanceOperationRequiresLock { get { return SRCore.Instance["InstanceOperationRequiresLock"]; } }
        internal static string InstanceOperationRequiresNotCompleted { get { return SRCore.Instance["InstanceOperationRequiresNotCompleted"]; } }
        internal static string InstanceOperationRequiresNotUninitialized { get { return SRCore.Instance["InstanceOperationRequiresNotUninitialized"]; } }
        internal static string InstanceOperationRequiresOwner { get { return SRCore.Instance["InstanceOperationRequiresOwner"]; } }
        internal static string InvalidInstanceState { get { return SRCore.Instance["InvalidInstanceState"]; } }
        internal static string InvalidKeyArgument { get { return SRCore.Instance["InvalidKeyArgument"]; } }
        internal static string InvalidLockToken { get { return SRCore.Instance["InvalidLockToken"]; } }
        internal static string KeyAlreadyAssociated { get { return SRCore.Instance["KeyAlreadyAssociated"]; } }
        internal static string KeyAlreadyCompleted { get { return SRCore.Instance["KeyAlreadyCompleted"]; } }
        internal static string KeyAlreadyUnassociated { get { return SRCore.Instance["KeyAlreadyUnassociated"]; } }
        internal static string KeyNotAssociated { get { return SRCore.Instance["KeyNotAssociated"]; } }
        internal static string KeyNotCompleted { get { return SRCore.Instance["KeyNotCompleted"]; } }
        internal static string LoadedWriteOnlyValue { get { return SRCore.Instance["LoadedWriteOnlyValue"]; } }
        internal static string MetadataCannotContainNullKey { get { return SRCore.Instance["MetadataCannotContainNullKey"]; } }
        internal static string MetadataCannotContainNullValue(object p0) { return SRCore.Instance["MetadataCannotContainNullValue", p0]; }
        internal static string MustSetTransactionOnFirstCall { get { return SRCore.Instance["MustSetTransactionOnFirstCall"]; } }
        internal static string OnFreeInstanceHandleThrew { get { return SRCore.Instance["OnFreeInstanceHandleThrew"]; } }
        internal static string OutsideInstanceExecutionScope(object p0) { return SRCore.Instance["OutsideInstanceExecutionScope", p0]; }
        internal static string OutsideTransactionalCommand(object p0) { return SRCore.Instance["OutsideTransactionalCommand", p0]; }
        internal static string ProviderDoesNotSupportCommand(object p0) { return SRCore.Instance["ProviderDoesNotSupportCommand", p0]; }
        internal static string TransactionInDoubtNonHost { get { return SRCore.Instance["TransactionInDoubtNonHost"]; } }
        internal static string TransactionRolledBackNonHost { get { return SRCore.Instance["TransactionRolledBackNonHost"]; } }
        internal static string UninitializedCannotHaveData { get { return SRCore.Instance["UninitializedCannotHaveData"]; } }
        internal static string CannotCompleteWithKeys { get { return SRCore.Instance["CannotCompleteWithKeys"]; } }
        internal static string OnCancelRequestedThrew { get { return SRCore.Instance["OnCancelRequestedThrew"]; } }
        internal static string AlreadyBoundToInstance { get { return SRCore.Instance["AlreadyBoundToInstance"]; } }
        internal static string AlreadyBoundToOwner { get { return SRCore.Instance["AlreadyBoundToOwner"]; } }
        internal static string InstanceRequired { get { return SRCore.Instance["InstanceRequired"]; } }
        internal static string LoadOpAssociateKeysCannotContainLookupKey { get { return SRCore.Instance["LoadOpAssociateKeysCannotContainLookupKey"]; } }
        internal static string LoadOpFreeKeyRequiresAcceptUninitialized { get { return SRCore.Instance["LoadOpFreeKeyRequiresAcceptUninitialized"]; } }
        internal static string LoadOpKeyMustBeValid { get { return SRCore.Instance["LoadOpKeyMustBeValid"]; } }
        internal static string OwnerRequired { get { return SRCore.Instance["OwnerRequired"]; } }
        internal static string ValidateUnlockInstance { get { return SRCore.Instance["ValidateUnlockInstance"]; } }
        internal static string InstanceKeyRequiresValidGuid { get { return SRCore.Instance["InstanceKeyRequiresValidGuid"]; } }
        internal static string AsyncTransactionException { get { return SRCore.Instance["AsyncTransactionException"]; } }
        internal static string ExecuteMustBeNested { get { return SRCore.Instance["ExecuteMustBeNested"]; } }
        internal static string TryCommandCannotExecuteSubCommandsAndReduce { get { return SRCore.Instance["TryCommandCannotExecuteSubCommandsAndReduce"]; } }
        internal static string CannotAcquireLockDefault { get { return SRCore.Instance["CannotAcquireLockDefault"]; } }
        internal static string CannotAcquireLockSpecific(object p0) { return SRCore.Instance["CannotAcquireLockSpecific", p0]; }
        internal static string InstanceNotReadyDefault { get { return SRCore.Instance["InstanceNotReadyDefault"]; } }
        internal static string InstanceNotReadySpecific(object p0) { return SRCore.Instance["InstanceNotReadySpecific", p0]; }
        internal static string KeyNotReadyDefault { get { return SRCore.Instance["KeyNotReadyDefault"]; } }
        internal static string KeyNotReadySpecific(object p0) { return SRCore.Instance["KeyNotReadySpecific", p0]; }
        internal static string KeyCollisionDefault { get { return SRCore.Instance["KeyCollisionDefault"]; } }
        internal static string KeyCollisionSpecific(object p0, object p1, object p2) { return SRCore.Instance["KeyCollisionSpecific", p0, p1, p2]; }
        internal static string NameCollisionOnCollect(object p0, object p1) { return SRCore.Instance["NameCollisionOnCollect", p0, p1]; }
        internal static string NameCollisionOnMap(object p0, object p1) { return SRCore.Instance["NameCollisionOnMap", p0, p1]; }
        internal static string PersistencePipelineAbortThrew(object p0) { return SRCore.Instance["PersistencePipelineAbortThrew", p0]; }
        internal static string KeyCollisionSpecificKeyOnly(object p0) { return SRCore.Instance["KeyCollisionSpecificKeyOnly", p0]; }
        internal static string KeyCompleteDefault { get { return SRCore.Instance["KeyCompleteDefault"]; } }
        internal static string KeyCompleteSpecific(object p0) { return SRCore.Instance["KeyCompleteSpecific", p0]; }
        internal static string InstanceCompleteDefault { get { return SRCore.Instance["InstanceCompleteDefault"]; } }
        internal static string InstanceCompleteSpecific(object p0) { return SRCore.Instance["InstanceCompleteSpecific", p0]; }
        internal static string CannotAcquireLockSpecificWithOwner(object p0, object p1) { return SRCore.Instance["CannotAcquireLockSpecificWithOwner", p0, p1]; }
        internal static string InstanceCollisionDefault { get { return SRCore.Instance["InstanceCollisionDefault"]; } }
        internal static string InstanceCollisionSpecific(object p0) { return SRCore.Instance["InstanceCollisionSpecific", p0]; }
        internal static string InstanceLockLostDefault { get { return SRCore.Instance["InstanceLockLostDefault"]; } }
        internal static string InstanceLockLostSpecific(object p0) { return SRCore.Instance["InstanceLockLostSpecific", p0]; }
        internal static string InstanceOwnerDefault { get { return SRCore.Instance["InstanceOwnerDefault"]; } }
        internal static string InstanceOwnerSpecific(object p0) { return SRCore.Instance["InstanceOwnerSpecific", p0]; }
        internal static string InstanceHandleConflictDefault { get { return SRCore.Instance["InstanceHandleConflictDefault"]; } }
        internal static string InstanceHandleConflictSpecific(object p0) { return SRCore.Instance["InstanceHandleConflictSpecific", p0]; }
        internal static string BindLockRequiresCommandFlag { get { return SRCore.Instance["BindLockRequiresCommandFlag"]; } }
        internal static string CannotInvokeBindingFromNonBinding { get { return SRCore.Instance["CannotInvokeBindingFromNonBinding"]; } }
        internal static string CannotInvokeTransactionalFromNonTransactional { get { return SRCore.Instance["CannotInvokeTransactionalFromNonTransactional"]; } }
        internal static string DoNotCompleteTryCommandWithPendingReclaim { get { return SRCore.Instance["DoNotCompleteTryCommandWithPendingReclaim"]; } }
        internal static string GuidCannotBeEmpty { get { return SRCore.Instance["GuidCannotBeEmpty"]; } }
        internal static string InstanceStoreBoundSameVersionTwice { get { return SRCore.Instance["InstanceStoreBoundSameVersionTwice"]; } }
        internal static string MayBindLockCommandShouldValidateOwner { get { return SRCore.Instance["MayBindLockCommandShouldValidateOwner"]; } }
        internal static string StoreReportedConflictingLockTokens { get { return SRCore.Instance["StoreReportedConflictingLockTokens"]; } }
        internal static string TimedOutWaitingForLockResolution { get { return SRCore.Instance["TimedOutWaitingForLockResolution"]; } }
        internal static string BindReclaimedLockException { get { return SRCore.Instance["BindReclaimedLockException"]; } }
        internal static string BindReclaimSucceeded { get { return SRCore.Instance["BindReclaimSucceeded"]; } }
        internal static string OwnerBelongsToWrongStore { get { return SRCore.Instance["OwnerBelongsToWrongStore"]; } }
        internal static string WaitAlreadyInProgress { get { return SRCore.Instance["WaitAlreadyInProgress"]; } }
        internal static string WaitForEventsTimedOut(object p0) { return SRCore.Instance["WaitForEventsTimedOut", p0]; }
    }
}

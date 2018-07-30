// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Resources;

namespace CoreWf
{
    internal class SR : StringResourceBase
    {
        internal static readonly SR Instance = new SR();

        private ResourceManager _resourceManager;

        protected internal override ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("CoreWf.Resources.strings", typeof(SR).GetTypeInfo().Assembly);
                }
                return _resourceManager;
            }
        }

        internal static string AbortingDueToDynamicUpdateFailure { get { return SR.Instance["AbortingDueToDynamicUpdateFailure"]; } }
        internal static string AbortingDueToInstanceTimeout { get { return SR.Instance["AbortingDueToInstanceTimeout"]; } }
        internal static string AbortingDueToLoadFailure { get { return SR.Instance["AbortingDueToLoadFailure"]; } }
        internal static string AbortingDueToVersionMismatch { get { return SR.Instance["AbortingDueToVersionMismatch"]; } }
        internal static string AbortInstanceOnTransactionFailureDoesNotMatch { get { return SR.Instance["AbortInstanceOnTransactionFailureDoesNotMatch"]; } }
        internal static string ActionItemIsAlreadyScheduled { get { return SR.Instance["ActionItemIsAlreadyScheduled"]; } }
        internal static string ActivityBlockingUpdate(object p0, object p1) { return SR.Instance["ActivityBlockingUpdate", p0, p1]; }
        internal static string ActivityCannotBeReferenced(object p0, object p1, object p2, object p3) { return SR.Instance["ActivityCannotBeReferenced", p0, p1, p2, p3]; }
        internal static string ActivityCannotBeReferencedWithoutTarget(object p0, object p1, object p2) { return SR.Instance["ActivityCannotBeReferencedWithoutTarget", p0, p1, p2]; }
        internal static string ActivityCannotReferenceItself(object p0) { return SR.Instance["ActivityCannotReferenceItself", p0]; }
        internal static string ActivityDefinitionCannotBeShared(object p0) { return SR.Instance["ActivityDefinitionCannotBeShared", p0]; }
        internal static string ActivityDelegateAlreadyOpened(object p0, object p1, object p2) { return SR.Instance["ActivityDelegateAlreadyOpened", p0, p1, p2]; }
        internal static string ActivityDelegateCannotBeReferenced(object p0, object p1, object p2, object p3) { return SR.Instance["ActivityDelegateCannotBeReferenced", p0, p1, p2, p3]; }
        internal static string ActivityDelegateCannotBeReferencedNoHandler(object p0, object p1, object p2) { return SR.Instance["ActivityDelegateCannotBeReferencedNoHandler", p0, p1, p2]; }
        internal static string ActivityDelegateCannotBeReferencedWithoutTarget(object p0, object p1, object p2) { return SR.Instance["ActivityDelegateCannotBeReferencedWithoutTarget", p0, p1, p2]; }
        internal static string ActivityDelegateCannotBeReferencedWithoutTargetNoHandler(object p0, object p1) { return SR.Instance["ActivityDelegateCannotBeReferencedWithoutTargetNoHandler", p0, p1]; }
        internal static string ActivityDelegateHandlersMustBeDeclarations(object p0, object p1, object p2) { return SR.Instance["ActivityDelegateHandlersMustBeDeclarations", p0, p1, p2]; }
        internal static string ActivityDelegateNotOpened(object p0) { return SR.Instance["ActivityDelegateNotOpened", p0]; }
        internal static string ActivityDelegateOwnerEnvironmentMissing(object p0, object p1) { return SR.Instance["ActivityDelegateOwnerEnvironmentMissing", p0, p1]; }
        internal static string ActivityDelegateOwnerMissing(object p0) { return SR.Instance["ActivityDelegateOwnerMissing", p0]; }
        internal static string ActivityDelegateOwnerNotInParentScope(object p0, object p1) { return SR.Instance["ActivityDelegateOwnerNotInParentScope", p0, p1]; }
        internal static string ActivityFailedToOpenBefore { get { return SR.Instance["ActivityFailedToOpenBefore"]; } }
        internal static string ActivityHasNoImplementation(object p0) { return SR.Instance["ActivityHasNoImplementation", p0]; }
        internal static string ActivityInstanceFixupFailed { get { return SR.Instance["ActivityInstanceFixupFailed"]; } }
        internal static string ActivityIsNotRoot { get { return SR.Instance["ActivityIsNotRoot"]; } }
        internal static string ActivityIsUncached { get { return SR.Instance["ActivityIsUncached"]; } }
        internal static string ActivityMapIsCorrupt { get { return SR.Instance["ActivityMapIsCorrupt"]; } }
        internal static string ActivityNotFound(object p0) { return SR.Instance["ActivityNotFound", p0]; }
        internal static string ActivityNotICompiledExpressionRoot { get { return SR.Instance["ActivityNotICompiledExpressionRoot"]; } }
        internal static string ActivityNotPartOfThisTree(object p0, object p1) { return SR.Instance["ActivityNotPartOfThisTree", p0, p1]; }
        internal static string ActivityPropertyMustBeSet(object p0, object p1) { return SR.Instance["ActivityPropertyMustBeSet", p0, p1]; }
        internal static string ActivityPropertyNotSet(object p0, object p1) { return SR.Instance["ActivityPropertyNotSet", p0, p1]; }
        internal static string ActivityPropertyRequiresName(object p0) { return SR.Instance["ActivityPropertyRequiresName", p0]; }
        internal static string ActivityPropertyRequiresType(object p0) { return SR.Instance["ActivityPropertyRequiresType", p0]; }
        internal static string ActivityTypeMismatch(object p0, object p1) { return SR.Instance["ActivityTypeMismatch", p0, p1]; }
        internal static string ActivityXamlServiceLineString { get { return SR.Instance["ActivityXamlServiceLineString"]; } }
        internal static string ActivityXamlServicesCompilationFailed(object p0) { return SR.Instance["ActivityXamlServicesCompilationFailed", p0]; }
        internal static string ActivityXamlServicesRequiresActivity(object p0) { return SR.Instance["ActivityXamlServicesRequiresActivity", p0]; }
        internal static string AddedIdleArgumentBlockDU(object p0, object p1) { return SR.Instance["AddedIdleArgumentBlockDU", p0, p1]; }
        internal static string AddedIdleExpressionBlockDU { get { return SR.Instance["AddedIdleExpressionBlockDU"]; } }
        internal static string AddedIdleVariableDefaultBlockDU(object p0, object p1) { return SR.Instance["AddedIdleVariableDefaultBlockDU", p0, p1]; }
        internal static string AddMatchActivityNewAndOldParentMismatch(object p0, object p1, object p2, object p3) { return SR.Instance["AddMatchActivityNewAndOldParentMismatch", p0, p1, p2, p3]; }
        internal static string AddMatchActivityNewParentMismatch(object p0, object p1, object p2) { return SR.Instance["AddMatchActivityNewParentMismatch", p0, p1, p2]; }
        internal static string AddMatchActivityPrivateChild(object p0) { return SR.Instance["AddMatchActivityPrivateChild", p0]; }
        internal static string AddMatchVariableNewAndOldParentMismatch(object p0, object p1, object p2, object p3) { return SR.Instance["AddMatchVariableNewAndOldParentMismatch", p0, p1, p2, p3]; }
        internal static string AddMatchVariableNewParentMismatch(object p0, object p1, object p2) { return SR.Instance["AddMatchVariableNewParentMismatch", p0, p1, p2]; }
        internal static string AddMatchVariablePrivateChild(object p0) { return SR.Instance["AddMatchVariablePrivateChild", p0]; }
        internal static string AddMatchVariableSignatureMismatch(object p0, object p1, object p2, object p3, object p4, object p5, object p6) { return SR.Instance["AddMatchVariableSignatureMismatch", p0, p1, p2, p3, p4, p5, p6]; }
        internal static string AddValidationErrorMustBeCalledFromConstraint(object p0) { return SR.Instance["AddValidationErrorMustBeCalledFromConstraint", p0]; }
        internal static string AECDisposed { get { return SR.Instance["AECDisposed"]; } }
        internal static string AECForPropertiesHasBeenDisposed { get { return SR.Instance["AECForPropertiesHasBeenDisposed"]; } }
        internal static string AlreadyBoundToInstance { get { return SR.Instance["AlreadyBoundToInstance"]; } }
        internal static string AlreadyBoundToOwner { get { return SR.Instance["AlreadyBoundToOwner"]; } }
        internal static string AlreadySetupNoPersist { get { return SR.Instance["AlreadySetupNoPersist"]; } }
        internal static string AmbiguousVBVariableReference(object p0) { return SR.Instance["AmbiguousVBVariableReference", p0]; }
        internal static string ArgumentAlreadyInitialized(object p0, object p1) { return SR.Instance["ArgumentAlreadyInitialized", p0, p1]; }
        internal static string ArgumentAlreadyInUse(object p0, object p1, object p2) { return SR.Instance["ArgumentAlreadyInUse", p0, p1, p2]; }
        internal static string ArgumentDirectionMismatch(object p0, object p1, object p2) { return SR.Instance["ArgumentDirectionMismatch", p0, p1, p2]; }
        internal static string ArgumentDoesNotExist(object p0) { return SR.Instance["ArgumentDoesNotExist", p0]; }
        internal static string ArgumentDoesNotExistInEnvironment(object p0) { return SR.Instance["ArgumentDoesNotExistInEnvironment", p0]; }
        internal static string ArgumentIsAddedMoreThanOnce(object p0, object p1) { return SR.Instance["ArgumentIsAddedMoreThanOnce", p0, p1]; }
        internal static string ArgumentLocationExpressionTypeMismatch(object p0, object p1) { return SR.Instance["ArgumentLocationExpressionTypeMismatch", p0, p1]; }
        internal static string ArgumentMustbePropertyofWorkflowElement { get { return SR.Instance["ArgumentMustbePropertyofWorkflowElement"]; } }
        internal static string ArgumentNameRequired { get { return SR.Instance["ArgumentNameRequired"]; } }
        internal static string ArgumentNotFound(object p0) { return SR.Instance["ArgumentNotFound", p0]; }
        internal static string ArgumentNotInTree(object p0) { return SR.Instance["ArgumentNotInTree", p0]; }
        internal static string ArgumentNullOrEmpty(object p0) { return SR.Instance["ArgumentNullOrEmpty", p0]; }
        internal static string ArgumentNumberRequiresTheSameAsParameterNumber(object p0) { return SR.Instance["ArgumentNumberRequiresTheSameAsParameterNumber", p0]; }
        internal static string ArgumentRequired(object p0, object p1) { return SR.Instance["ArgumentRequired", p0, p1]; }
        internal static string ArgumentTypeCannotBeNull { get { return SR.Instance["ArgumentTypeCannotBeNull"]; } }
        internal static string ArgumentTypeMismatch(object p0, object p1, object p2) { return SR.Instance["ArgumentTypeMismatch", p0, p1, p2]; }
        internal static string ArgumentTypeMustBeCompatible(object p0, object p1, object p2) { return SR.Instance["ArgumentTypeMustBeCompatible", p0, p1, p2]; }
        internal static string ArgumentValueExpressionTypeMismatch(object p0, object p1) { return SR.Instance["ArgumentValueExpressionTypeMismatch", p0, p1]; }
        internal static string ArgumentViolationsFound(object p0, object p1) { return SR.Instance["ArgumentViolationsFound", p0, p1]; }
        internal static string AssemblyReferenceIsImmutable { get { return SR.Instance["AssemblyReferenceIsImmutable"]; } }
        internal static string AsyncCallbackThrewException { get { return SR.Instance["AsyncCallbackThrewException"]; } }
        internal static string AsyncEventArgsCompletedTwice(object p0) { return SR.Instance["AsyncEventArgsCompletedTwice", p0]; }
        internal static string AsyncEventArgsCompletionPending(object p0) { return SR.Instance["AsyncEventArgsCompletionPending", p0]; }
        internal static string AsyncMethodsMustAllBeStaticOrInstance { get { return SR.Instance["AsyncMethodsMustAllBeStaticOrInstance"]; } }
        internal static string AsyncMethodsMustFromSameType { get { return SR.Instance["AsyncMethodsMustFromSameType"]; } }
        internal static string AsyncResultAlreadyEnded { get { return SR.Instance["AsyncResultAlreadyEnded"]; } }
        internal static string AsyncResultCompletedTwice(object p0) { return SR.Instance["AsyncResultCompletedTwice", p0]; }
        internal static string AsyncTransactionException { get { return SR.Instance["AsyncTransactionException"]; } }
        internal static string BadCopyToArray { get { return SR.Instance["BadCopyToArray"]; } }
        internal static string BadWorkflowIdentityFormat { get { return SR.Instance["BadWorkflowIdentityFormat"]; } }
        internal static string BeforeInitializeComponentXBTExtensionResourceNotFound { get { return SR.Instance["BeforeInitializeComponentXBTExtensionResourceNotFound"]; } }
        internal static string BeginExecuteMustNotReturnANullAsyncResult { get { return SR.Instance["BeginExecuteMustNotReturnANullAsyncResult"]; } }
        internal static string BeginExecuteMustUseProvidedStateAsAsyncResultState { get { return SR.Instance["BeginExecuteMustUseProvidedStateAsAsyncResultState"]; } }
        internal static string BinaryExpressionActivityRequiresArgument(object p0, object p1, object p2) { return SR.Instance["BinaryExpressionActivityRequiresArgument", p0, p1, p2]; }
        internal static string BindLockRequiresCommandFlag { get { return SR.Instance["BindLockRequiresCommandFlag"]; } }
        internal static string BindReclaimedLockException { get { return SR.Instance["BindReclaimedLockException"]; } }
        internal static string BindReclaimSucceeded { get { return SR.Instance["BindReclaimSucceeded"]; } }
        internal static string BlockedUpdateInsideActivityUpdateByUserError { get { return SR.Instance["BlockedUpdateInsideActivityUpdateByUserError"]; } }
        internal static string BlockedUpdateInsideActivityUpdateError { get { return SR.Instance["BlockedUpdateInsideActivityUpdateError"]; } }
        internal static string BookmarkAlreadyExists(object p0) { return SR.Instance["BookmarkAlreadyExists", p0]; }
        internal static string BookmarkNotFoundGeneric { get { return SR.Instance["BookmarkNotFoundGeneric"]; } }
        internal static string BookmarkNotRegistered(object p0) { return SR.Instance["BookmarkNotRegistered", p0]; }
        internal static string BookmarkScopeAlreadyInitialized { get { return SR.Instance["BookmarkScopeAlreadyInitialized"]; } }
        internal static string BookmarkScopeHasBookmarks { get { return SR.Instance["BookmarkScopeHasBookmarks"]; } }
        internal static string BookmarkScopeNotFound(object p0) { return SR.Instance["BookmarkScopeNotFound", p0]; }
        internal static string BookmarkScopeNotRegisteredForInitialize { get { return SR.Instance["BookmarkScopeNotRegisteredForInitialize"]; } }
        internal static string BookmarkScopeNotRegisteredForUnregister { get { return SR.Instance["BookmarkScopeNotRegisteredForUnregister"]; } }
        internal static string BookmarkScopesRequireKeys { get { return SR.Instance["BookmarkScopesRequireKeys"]; } }
        internal static string BookmarkScopeWithIdAlreadyExists(object p0) { return SR.Instance["BookmarkScopeWithIdAlreadyExists", p0]; }
        internal static string BookmarksOnlyResumableWhileIdle { get { return SR.Instance["BookmarksOnlyResumableWhileIdle"]; } }
        internal static string BufferAllocationFailed(object p0) { return SR.Instance["BufferAllocationFailed", p0]; }
        internal static string BufferedOutputStreamQuotaExceeded(object p0) { return SR.Instance["BufferedOutputStreamQuotaExceeded", p0]; }
        internal static string BufferIsNotRightSizeForBufferManager { get { return SR.Instance["BufferIsNotRightSizeForBufferManager"]; } }
        internal static string CallbackExceptionFromHostAbort(object p0) { return SR.Instance["CallbackExceptionFromHostAbort", p0]; }
        internal static string CallbackExceptionFromHostGetExtension(object p0) { return SR.Instance["CallbackExceptionFromHostGetExtension", p0]; }
        internal static string CancellationHandlerFatalException(object p0) { return SR.Instance["CancellationHandlerFatalException", p0]; }
        internal static string CanInduceIdleActivityInArgumentExpression(object p0, object p1, object p2) { return SR.Instance["CanInduceIdleActivityInArgumentExpression", p0, p1, p2]; }
        internal static string CanInduceIdleNotSpecified(object p0) { return SR.Instance["CanInduceIdleNotSpecified", p0]; }
        internal static string CannotAcquireLockDefault { get { return SR.Instance["CannotAcquireLockDefault"]; } }
        internal static string CannotAcquireLockSpecific(object p0) { return SR.Instance["CannotAcquireLockSpecific", p0]; }
        internal static string CannotAcquireLockSpecificWithOwner(object p0, object p1) { return SR.Instance["CannotAcquireLockSpecificWithOwner", p0, p1]; }
        internal static string CannotAddHandlesUpdateError { get { return SR.Instance["CannotAddHandlesUpdateError"]; } }
        internal static string CannotAddOrRemoveWithChildren { get { return SR.Instance["CannotAddOrRemoveWithChildren"]; } }
        internal static string CannotCallAbortInstanceFromWorkflowThread { get { return SR.Instance["CannotCallAbortInstanceFromWorkflowThread"]; } }
        internal static string CannotChangeAbortInstanceFlagAfterPropertyRegistration { get { return SR.Instance["CannotChangeAbortInstanceFlagAfterPropertyRegistration"]; } }
        internal static string CannotChangeMatchesInImplementation { get { return SR.Instance["CannotChangeMatchesInImplementation"]; } }
        internal static string CannotCompleteRuntimeOwnedTransaction { get { return SR.Instance["CannotCompleteRuntimeOwnedTransaction"]; } }
        internal static string CannotCompleteWithKeys { get { return SR.Instance["CannotCompleteWithKeys"]; } }
        internal static string CannotConvertObject(object p0, object p1) { return SR.Instance["CannotConvertObject", p0, p1]; }
        internal static string CannotCreateContextWithNullId { get { return SR.Instance["CannotCreateContextWithNullId"]; } }
        internal static string CannotCreateOwnerWithoutIdentity { get { return SR.Instance["CannotCreateOwnerWithoutIdentity"]; } }
        internal static string CannotDereferenceNull(object p0) { return SR.Instance["CannotDereferenceNull", p0]; }
        internal static string CannotEnlistMultipleTransactions { get { return SR.Instance["CannotEnlistMultipleTransactions"]; } }
        internal static string CanNotFindSymbolResolverInWorkflowInstanceExtensions { get { return SR.Instance["CanNotFindSymbolResolverInWorkflowInstanceExtensions"]; } }
        internal static string CannotGenerateSchemaForXmlSerializable(object p0) { return SR.Instance["CannotGenerateSchemaForXmlSerializable", p0]; }
        internal static string CannotGetValueOfOutArgument { get { return SR.Instance["CannotGetValueOfOutArgument"]; } }
        internal static string CannotInvokeBindingFromNonBinding { get { return SR.Instance["CannotInvokeBindingFromNonBinding"]; } }
        internal static string CannotInvokeOpenedActivity { get { return SR.Instance["CannotInvokeOpenedActivity"]; } }
        internal static string CannotInvokeTransactionalFromNonTransactional { get { return SR.Instance["CannotInvokeTransactionalFromNonTransactional"]; } }
        internal static string CannotModifyCatchAfterOpen { get { return SR.Instance["CannotModifyCatchAfterOpen"]; } }
        internal static string CannotMoveChildAcrossDifferentFlowNodeTypes { get { return SR.Instance["CannotMoveChildAcrossDifferentFlowNodeTypes"]; } }
        internal static string CannotNestTransactionScopeWhenAmbientHandleIsSuppressed(object p0) { return SR.Instance["CannotNestTransactionScopeWhenAmbientHandleIsSuppressed", p0]; }
        internal static string CannotPerformOperationFromHandlerThread { get { return SR.Instance["CannotPerformOperationFromHandlerThread"]; } }
        internal static string CannotPerformOperationOnHandle { get { return SR.Instance["CannotPerformOperationOnHandle"]; } }
        internal static string CannotPersistInsideIsolation { get { return SR.Instance["CannotPersistInsideIsolation"]; } }
        internal static string CannotPersistInsideNoPersist { get { return SR.Instance["CannotPersistInsideNoPersist"]; } }
        internal static string CannotPersistWhileDetached { get { return SR.Instance["CannotPersistWhileDetached"]; } }
        internal static string CannotPropagateExceptionWhileCanceling(object p0, object p1) { return SR.Instance["CannotPropagateExceptionWhileCanceling", p0, p1]; }
        internal static string CannotRemoveExecutingActivityUpdateError(object p0, object p1) { return SR.Instance["CannotRemoveExecutingActivityUpdateError", p0, p1]; }
        internal static string CannotReplaceTransaction { get { return SR.Instance["CannotReplaceTransaction"]; } }
        internal static string CannotResetPropertyInDataContext { get { return SR.Instance["CannotResetPropertyInDataContext"]; } }
        internal static string CannotSaveOriginalValueForActivity { get { return SR.Instance["CannotSaveOriginalValueForActivity"]; } }
        internal static string CannotSaveOriginalValueForNewActivity(object p0) { return SR.Instance["CannotSaveOriginalValueForNewActivity", p0]; }
        internal static string CannotSaveOriginalValuesForReferencedChildren { get { return SR.Instance["CannotSaveOriginalValuesForReferencedChildren"]; } }
        internal static string CannotScheduleChildrenWhileEnteringIsolation { get { return SR.Instance["CannotScheduleChildrenWhileEnteringIsolation"]; } }
        internal static string CannotSerializeExpression(object p0) { return SR.Instance["CannotSerializeExpression", p0]; }
        internal static string CannotSerializeVariableExpression { get { return SR.Instance["CannotSerializeVariableExpression"]; } }
        internal static string CannotSetRuntimeTransactionInNoPersist { get { return SR.Instance["CannotSetRuntimeTransactionInNoPersist"]; } }
        internal static string CannotSetupIsolationInsideIsolation { get { return SR.Instance["CannotSetupIsolationInsideIsolation"]; } }
        internal static string CannotSetupIsolationInsideNoPersist { get { return SR.Instance["CannotSetupIsolationInsideNoPersist"]; } }
        internal static string CannotSetupIsolationWithChildren { get { return SR.Instance["CannotSetupIsolationWithChildren"]; } }
        internal static string CannotSetValueOfInArgument { get { return SR.Instance["CannotSetValueOfInArgument"]; } }
        internal static string CannotSetValueToLocation(object p0, object p1, object p2) { return SR.Instance["CannotSetValueToLocation", p0, p1, p2]; }
        internal static string CannotSuppressAlreadyRegisteredHandle { get { return SR.Instance["CannotSuppressAlreadyRegisteredHandle"]; } }
        internal static string CannotUnregisterDefaultBookmarkScope { get { return SR.Instance["CannotUnregisterDefaultBookmarkScope"]; } }
        internal static string CannotUnregisterNullBookmarkScope { get { return SR.Instance["CannotUnregisterNullBookmarkScope"]; } }
        internal static string CannotUpdateEnvironmentInTheMiddleOfResolvingArguments { get { return SR.Instance["CannotUpdateEnvironmentInTheMiddleOfResolvingArguments"]; } }
        internal static string CannotUpdateEnvironmentInTheMiddleOfResolvingVariables { get { return SR.Instance["CannotUpdateEnvironmentInTheMiddleOfResolvingVariables"]; } }
        internal static string CannotUseInputsWithLoad { get { return SR.Instance["CannotUseInputsWithLoad"]; } }
        internal static string CannotValidateNullObject(object p0, object p1) { return SR.Instance["CannotValidateNullObject", p0, p1]; }
        internal static string CannotWaitForIdleSynchronously { get { return SR.Instance["CannotWaitForIdleSynchronously"]; } }
        internal static string CanOnlyAbortDirectChildren { get { return SR.Instance["CanOnlyAbortDirectChildren"]; } }
        internal static string CanOnlyCancelDirectChildren { get { return SR.Instance["CanOnlyCancelDirectChildren"]; } }
        internal static string CanOnlyGetOwnedArguments(object p0, object p1, object p2) { return SR.Instance["CanOnlyGetOwnedArguments", p0, p1, p2]; }
        internal static string CanOnlyScheduleDirectChildren(object p0, object p1, object p2) { return SR.Instance["CanOnlyScheduleDirectChildren", p0, p1, p2]; }
        internal static string CantFindTimerExtension { get { return SR.Instance["CantFindTimerExtension"]; } }
        internal static string CatchOrFinallyExpected(object p0) { return SR.Instance["CatchOrFinallyExpected", p0]; }
        internal static string ChangeConditionalTransitionToUnconditionalBlockDU { get { return SR.Instance["ChangeConditionalTransitionToUnconditionalBlockDU"]; } }
        internal static string ChangeTransitionTypeDuringTransitioningBlockDU { get { return SR.Instance["ChangeTransitionTypeDuringTransitioningBlockDU"]; } }
        internal static string ChangingTriggerOrUseOriginalConditionActionBlockDU { get { return SR.Instance["ChangingTriggerOrUseOriginalConditionActionBlockDU"]; } }
        internal static string CollectionActivityRequiresCollection(object p0) { return SR.Instance["CollectionActivityRequiresCollection", p0]; }
        internal static string ColumnNumberTooLarge(object p0) { return SR.Instance["ColumnNumberTooLarge", p0]; }
        internal static string CommandExecutionCannotOverlap { get { return SR.Instance["CommandExecutionCannotOverlap"]; } }
        internal static string CompensableActivityAlreadyConfirmedOrCompensated { get { return SR.Instance["CompensableActivityAlreadyConfirmedOrCompensated"]; } }
        internal static string CompensableActivityInsideTransactionScopeActivity { get { return SR.Instance["CompensableActivityInsideTransactionScopeActivity"]; } }
        internal static string CompensateWithNoTargetConstraint { get { return SR.Instance["CompensateWithNoTargetConstraint"]; } }
        internal static string CompensateWithoutCompensableActivity(object p0) { return SR.Instance["CompensateWithoutCompensableActivity", p0]; }
        internal static string CompensationHandlerFatalException(object p0) { return SR.Instance["CompensationHandlerFatalException", p0]; }
        internal static string CompiledExpressionIdNotFound(object p0) { return SR.Instance["CompiledExpressionIdNotFound", p0]; }
        internal static string CompiledExpressionsActivityException(object p0, object p1, object p2) { return SR.Instance["CompiledExpressionsActivityException", p0, p1, p2]; }
        internal static string CompiledExpressionsCacheMetadataException(object p0, object p1) { return SR.Instance["CompiledExpressionsCacheMetadataException", p0, p1]; }
        internal static string CompiledExpressionsDuplicateName(object p0) { return SR.Instance["CompiledExpressionsDuplicateName", p0]; }
        internal static string CompiledExpressionsIgnoringInvalidIdentifierVariable(object p0) { return SR.Instance["CompiledExpressionsIgnoringInvalidIdentifierVariable", p0]; }
        internal static string CompiledExpressionsIgnoringUnnamedVariable { get { return SR.Instance["CompiledExpressionsIgnoringUnnamedVariable"]; } }
        internal static string CompiledExpressionsNoCompiledRoot(object p0) { return SR.Instance["CompiledExpressionsNoCompiledRoot", p0]; }
        internal static string CompiledLocationNoDefaultConstructor(object p0) { return SR.Instance["CompiledLocationNoDefaultConstructor", p0]; }
        internal static string CompiledLocationReferenceGetLocation { get { return SR.Instance["CompiledLocationReferenceGetLocation"]; } }
        internal static string CompilerError { get { return SR.Instance["CompilerError"]; } }
        internal static string CompilerErrorSpecificExpression(object p0, object p1) { return SR.Instance["CompilerErrorSpecificExpression", p0, p1]; }
        internal static string CompletedMustNotHaveAssociatedKeys { get { return SR.Instance["CompletedMustNotHaveAssociatedKeys"]; } }
        internal static string CompletionConditionSetButNoBody(object p0) { return SR.Instance["CompletionConditionSetButNoBody", p0]; }
        internal static string ConfirmationHandlerFatalException(object p0) { return SR.Instance["ConfirmationHandlerFatalException", p0]; }
        internal static string ConfirmWithNoTargetConstraint { get { return SR.Instance["ConfirmWithNoTargetConstraint"]; } }
        internal static string ConfirmWithoutCompensableActivity(object p0) { return SR.Instance["ConfirmWithoutCompensableActivity", p0]; }
        internal static string ConstructorInfoNotFound(object p0) { return SR.Instance["ConstructorInfoNotFound", p0]; }
        internal static string ConstVariableCannotBeSet { get { return SR.Instance["ConstVariableCannotBeSet"]; } }
        internal static string ContextAlreadyBoundToInstance { get { return SR.Instance["ContextAlreadyBoundToInstance"]; } }
        internal static string ContextAlreadyBoundToLock { get { return SR.Instance["ContextAlreadyBoundToLock"]; } }
        internal static string ContextAlreadyBoundToOwner { get { return SR.Instance["ContextAlreadyBoundToOwner"]; } }
        internal static string ContextMustBeBoundToInstance { get { return SR.Instance["ContextMustBeBoundToInstance"]; } }
        internal static string ContextMustBeBoundToOwner { get { return SR.Instance["ContextMustBeBoundToOwner"]; } }
        internal static string ContextNotFromThisStore { get { return SR.Instance["ContextNotFromThisStore"]; } }
        internal static string ControllerInvalidBeforeInitialize { get { return SR.Instance["ControllerInvalidBeforeInitialize"]; } }
        internal static string ConvertVariableToValueExpressionFailed(object p0, object p1) { return SR.Instance["ConvertVariableToValueExpressionFailed", p0, p1]; }
        internal static string CopyToIndexOutOfRange { get { return SR.Instance["CopyToIndexOutOfRange"]; } }
        internal static string CopyToNotEnoughSpaceInArray { get { return SR.Instance["CopyToNotEnoughSpaceInArray"]; } }
        internal static string CopyToRankMustBeOne { get { return SR.Instance["CopyToRankMustBeOne"]; } }
        internal static string CouldNotResolveNamespacePrefix(object p0) { return SR.Instance["CouldNotResolveNamespacePrefix", p0]; }
        internal static string CreateBookmarkScopeFailed { get { return SR.Instance["CreateBookmarkScopeFailed"]; } }
        internal static string CSharpExpressionsMustBeCompiled { get { return SR.Instance["CSharpExpressionsMustBeCompiled"]; } }
        internal static string DebugInfoCannotEvaluateExpression(object p0) { return SR.Instance["DebugInfoCannotEvaluateExpression", p0]; }
        internal static string DebugInfoExceptionCaught(object p0, object p1) { return SR.Instance["DebugInfoExceptionCaught", p0, p1]; }
        internal static string DebugInfoNotSkipArgumentResolution { get { return SR.Instance["DebugInfoNotSkipArgumentResolution"]; } }
        internal static string DebugInfoTryGetValueFailed { get { return SR.Instance["DebugInfoTryGetValueFailed"]; } }
        internal static string DebugInstrumentationFailed(object p0) { return SR.Instance["DebugInstrumentationFailed", p0]; }
        internal static string DebugSymbolChecksumValueInvalid { get { return SR.Instance["DebugSymbolChecksumValueInvalid"]; } }
        internal static string DefaultAbortReason { get { return SR.Instance["DefaultAbortReason"]; } }
        internal static string DefaultCancelationRequiresCancelHasBeenRequested { get { return SR.Instance["DefaultCancelationRequiresCancelHasBeenRequested"]; } }
        internal static string DefaultInvalidWorkflowExceptionMessage { get { return SR.Instance["DefaultInvalidWorkflowExceptionMessage"]; } }
        internal static string DefaultWorkflowApplicationExceptionMessage { get { return SR.Instance["DefaultWorkflowApplicationExceptionMessage"]; } }
        internal static string DelegateArgumentAlreadyInUseOnActivity(object p0, object p1, object p2) { return SR.Instance["DelegateArgumentAlreadyInUseOnActivity", p0, p1, p2]; }
        internal static string DelegateArgumentChangeBlockDU { get { return SR.Instance["DelegateArgumentChangeBlockDU"]; } }
        internal static string DelegateArgumentDoesNotExist(object p0) { return SR.Instance["DelegateArgumentDoesNotExist", p0]; }
        internal static string DelegateArgumentMustBeReferenced(object p0) { return SR.Instance["DelegateArgumentMustBeReferenced", p0]; }
        internal static string DelegateArgumentMustBeSet { get { return SR.Instance["DelegateArgumentMustBeSet"]; } }
        internal static string DelegateArgumentNotVisible(object p0) { return SR.Instance["DelegateArgumentNotVisible", p0]; }
        internal static string DelegateArgumentTypeInvalid(object p0, object p1, object p2) { return SR.Instance["DelegateArgumentTypeInvalid", p0, p1, p2]; }
        internal static string DelegateHandlersCannotBeScheduledDirectly(object p0, object p1) { return SR.Instance["DelegateHandlersCannotBeScheduledDirectly", p0, p1]; }
        internal static string DelegateInArgumentTypeMismatch(object p0, object p1, object p2) { return SR.Instance["DelegateInArgumentTypeMismatch", p0, p1, p2]; }
        internal static string DelegateOutArgumentTypeMismatch(object p0, object p1, object p2) { return SR.Instance["DelegateOutArgumentTypeMismatch", p0, p1, p2]; }
        internal static string DelegateParameterCannotBeModifiedAfterOpen(object p0) { return SR.Instance["DelegateParameterCannotBeModifiedAfterOpen", p0]; }
        internal static string DelegateParameterDirectionalityMismatch(object p0, object p1, object p2) { return SR.Instance["DelegateParameterDirectionalityMismatch", p0, p1, p2]; }
        internal static string DictionaryIsReadOnly { get { return SR.Instance["DictionaryIsReadOnly"]; } }
        internal static string DirectLambdaParameterReference { get { return SR.Instance["DirectLambdaParameterReference"]; } }
        internal static string DoNotCompleteTryCommandWithPendingReclaim { get { return SR.Instance["DoNotCompleteTryCommandWithPendingReclaim"]; } }
        internal static string DoNotSupportArrayIndexerOnNonArrayType(object p0) { return SR.Instance["DoNotSupportArrayIndexerOnNonArrayType", p0]; }
        internal static string DoNotSupportArrayIndexerReferenceWithDifferentArrayTypeAndResultType(object p0, object p1) { return SR.Instance["DoNotSupportArrayIndexerReferenceWithDifferentArrayTypeAndResultType", p0, p1]; }
        internal static string DoNotSupportArrayIndexerValueWithIncompatibleArrayTypeAndResultType(object p0, object p1) { return SR.Instance["DoNotSupportArrayIndexerValueWithIncompatibleArrayTypeAndResultType", p0, p1]; }
        internal static string DoNotSupportArrayIndexerWithDifferentArrayTypeAndResultType(object p0, object p1) { return SR.Instance["DoNotSupportArrayIndexerWithDifferentArrayTypeAndResultType", p0, p1]; }
        internal static string DoNotSupportArrayIndexerWithNonIntIndex(object p0) { return SR.Instance["DoNotSupportArrayIndexerWithNonIntIndex", p0]; }
        internal static string DoWhileRequiresCondition(object p0) { return SR.Instance["DoWhileRequiresCondition", p0]; }
        internal static string DUActivityTypeMismatch(object p0, object p1) { return SR.Instance["DUActivityTypeMismatch", p0, p1]; }
        internal static string DUActivityTypeMismatchRuntime { get { return SR.Instance["DUActivityTypeMismatchRuntime"]; } }
        internal static string DUDisallowIfCannotFindingMatchingCondition { get { return SR.Instance["DUDisallowIfCannotFindingMatchingCondition"]; } }
        internal static string DuplicateAnnotationName(object p0) { return SR.Instance["DuplicateAnnotationName", p0]; }
        internal static string DuplicateCatchClause(object p0) { return SR.Instance["DuplicateCatchClause", p0]; }
        internal static string DuplicateEvaluationOrderValues(object p0, object p1) { return SR.Instance["DuplicateEvaluationOrderValues", p0, p1]; }
        internal static string DuplicateImportAttribute(object p0, object p1) { return SR.Instance["DuplicateImportAttribute", p0, p1]; }
        internal static string DuplicateInstrumentation(object p0) { return SR.Instance["DuplicateInstrumentation", p0]; }
        internal static string DuplicateMethodFound(object p0, object p1, object p2, object p3) { return SR.Instance["DuplicateMethodFound", p0, p1, p2, p3]; }
        internal static string DuplicateOriginActivityActivity(object p0, object p1, object p2) { return SR.Instance["DuplicateOriginActivityActivity", p0, p1, p2]; }
        internal static string DuplicateOriginActivityVariable(object p0, object p1, object p2) { return SR.Instance["DuplicateOriginActivityVariable", p0, p1, p2]; }
        internal static string DuplicateOriginVariableVariable(object p0, object p1, object p2) { return SR.Instance["DuplicateOriginVariableVariable", p0, p1, p2]; }
        internal static string DurationIsNegative(object p0) { return SR.Instance["DurationIsNegative", p0]; }
        internal static string DUTriggerOrConditionChangedDuringTransitioning { get { return SR.Instance["DUTriggerOrConditionChangedDuringTransitioning"]; } }
        internal static string DynamicActivityDuplicatePropertyDetected(object p0) { return SR.Instance["DynamicActivityDuplicatePropertyDetected", p0]; }
        internal static string DynamicActivityMultipleExpressionLanguages(object p0) { return SR.Instance["DynamicActivityMultipleExpressionLanguages", p0]; }
        internal static string EmptyGuidOnDeserializedInstance { get { return SR.Instance["EmptyGuidOnDeserializedInstance"]; } }
        internal static string EmptyIdReturnedFromHost(object p0) { return SR.Instance["EmptyIdReturnedFromHost", p0]; }
        internal static string EnlistedTransactionPropertiesRequireIsolationBlocks { get { return SR.Instance["EnlistedTransactionPropertiesRequireIsolationBlocks"]; } }
        internal static string EnumeratorNotStarted { get { return SR.Instance["EnumeratorNotStarted"]; } }
        internal static string EnvironmentDisposed { get { return SR.Instance["EnvironmentDisposed"]; } }
        internal static string ErrorExtractingValuesForLambdaRewrite(object p0, object p1, object p2) { return SR.Instance["ErrorExtractingValuesForLambdaRewrite", p0, p1, p2]; }
        internal static string ErrorsEncounteredWhileProcessingTree { get { return SR.Instance["ErrorsEncounteredWhileProcessingTree"]; } }
        internal static string EtwAPIMaxStringCountExceeded(object p0) { return SR.Instance["EtwAPIMaxStringCountExceeded", p0]; }
        internal static string EtwMaxNumberArgumentsExceeded(object p0) { return SR.Instance["EtwMaxNumberArgumentsExceeded", p0]; }
        internal static string EtwRegistrationFailed(object p0) { return SR.Instance["EtwRegistrationFailed", p0]; }
        internal static string ExclusiveHandleRegisterBookmarkScopeFailed { get { return SR.Instance["ExclusiveHandleRegisterBookmarkScopeFailed"]; } }
        internal static string ExclusiveHandleReinitializeFailed { get { return SR.Instance["ExclusiveHandleReinitializeFailed"]; } }
        internal static string ExecuteMustBeNested { get { return SR.Instance["ExecuteMustBeNested"]; } }
        internal static string ExecutionPropertyAlreadyDefined(object p0) { return SR.Instance["ExecutionPropertyAlreadyDefined", p0]; }
        internal static string ExpressionRequiredForConversion { get { return SR.Instance["ExpressionRequiredForConversion"]; } }
        internal static string ExtensionsCannotBeModified { get { return SR.Instance["ExtensionsCannotBeModified"]; } }
        internal static string ExtensionsCannotBeSetByIndex { get { return SR.Instance["ExtensionsCannotBeSetByIndex"]; } }
        internal static string ExternalLocationsGetOnly { get { return SR.Instance["ExternalLocationsGetOnly"]; } }
        internal static string ExtraOverloadGroupPropertiesConfigured(object p0, object p1, object p2) { return SR.Instance["ExtraOverloadGroupPropertiesConfigured", p0, p1, p2]; }
        internal static string FailFastMessage(object p0) { return SR.Instance["FailFastMessage", p0]; }
        internal static string FaultContextNotFound(object p0) { return SR.Instance["FaultContextNotFound", p0]; }
        internal static string FinalStateCannotHaveProperty(object p0, object p1) { return SR.Instance["FinalStateCannotHaveProperty", p0, p1]; }
        internal static string FinalStateCannotHaveTransition(object p0) { return SR.Instance["FinalStateCannotHaveTransition", p0]; }
        internal static string FlowchartContainsReferences { get { return SR.Instance["FlowchartContainsReferences"]; } }
        internal static string FlowchartContainsUnconnectedNodes(object p0) { return SR.Instance["FlowchartContainsUnconnectedNodes", p0]; }
        internal static string FlowchartMissingStartNode(object p0) { return SR.Instance["FlowchartMissingStartNode", p0]; }
        internal static string FlowDecisionRequiresCondition(object p0) { return SR.Instance["FlowDecisionRequiresCondition", p0]; }
        internal static string FlowNodeCannotBeShared(object p0, object p1) { return SR.Instance["FlowNodeCannotBeShared", p0, p1]; }
        internal static string FlowNodeLockedForRuntime(object p0) { return SR.Instance["FlowNodeLockedForRuntime", p0]; }
        internal static string FlowSwitchRequiresExpression(object p0) { return SR.Instance["FlowSwitchRequiresExpression", p0]; }
        internal static string ForEachRequiresNonNullValues(object p0) { return SR.Instance["ForEachRequiresNonNullValues", p0]; }
        internal static string GeneratedAndProvidedMapConflict { get { return SR.Instance["GeneratedAndProvidedMapConflict"]; } }
        internal static string GeneratedAndProvidedMapConflictRuntime { get { return SR.Instance["GeneratedAndProvidedMapConflictRuntime"]; } }
        internal static string GenericInstanceCommand(object p0) { return SR.Instance["GenericInstanceCommand", p0]; }
        internal static string GenericInstanceCommandNull { get { return SR.Instance["GenericInstanceCommandNull"]; } }
        internal static string GetLocationOnPublicAccessReference(object p0) { return SR.Instance["GetLocationOnPublicAccessReference", p0]; }
        internal static string GetParameterTypeMismatch(object p0, object p1) { return SR.Instance["GetParameterTypeMismatch", p0, p1]; }
        internal static string GetRunnableRequiresOwner { get { return SR.Instance["GetRunnableRequiresOwner"]; } }
        internal static string GuidCannotBeEmpty { get { return SR.Instance["GuidCannotBeEmpty"]; } }
        internal static string HandleFreed { get { return SR.Instance["HandleFreed"]; } }
        internal static string HandleFreedBeforeInitialized { get { return SR.Instance["HandleFreedBeforeInitialized"]; } }
        internal static string HandleInitializationContextDisposed { get { return SR.Instance["HandleInitializationContextDisposed"]; } }
        internal static string HandleNotInitialized { get { return SR.Instance["HandleNotInitialized"]; } }
        internal static string HasExecutingChildrenNoPersist { get { return SR.Instance["HasExecutingChildrenNoPersist"]; } }
        internal static string HostIdDoesNotMatchInstance(object p0, object p1) { return SR.Instance["HostIdDoesNotMatchInstance", p0, p1]; }
        internal static string IdentityControlCharacter { get { return SR.Instance["IdentityControlCharacter"]; } }
        internal static string IdentityNameSemicolon { get { return SR.Instance["IdentityNameSemicolon"]; } }
        internal static string IdentityWhitespace { get { return SR.Instance["IdentityWhitespace"]; } }
        internal static string IdNotFoundInWorkflow(object p0) { return SR.Instance["IdNotFoundInWorkflow", p0]; }
        internal static string ImplementationVersionMismatch(object p0, object p1, object p2) { return SR.Instance["ImplementationVersionMismatch", p0, p1, p2]; }
        internal static string IncompatibleArgumentType(object p0, object p1) { return SR.Instance["IncompatibleArgumentType", p0, p1]; }
        internal static string IncompatibleTypeForMultidimensionalArrayItemReference(object p0, object p1) { return SR.Instance["IncompatibleTypeForMultidimensionalArrayItemReference", p0, p1]; }
        internal static string IncorrectIndexForArgument(object p0, object p1, object p2) { return SR.Instance["IncorrectIndexForArgument", p0, p1, p2]; }
        internal static string IncorrectValueType(object p0, object p1) { return SR.Instance["IncorrectValueType", p0, p1]; }
        internal static string IndexOutOfBounds(object p0, object p1) { return SR.Instance["IndexOutOfBounds", p0, p1]; }
        internal static string IndicesAreNeeded(object p0, object p1) { return SR.Instance["IndicesAreNeeded", p0, p1]; }
        internal static string InitializationIncomplete { get { return SR.Instance["InitializationIncomplete"]; } }
        internal static string InitialMetadataCannotBeDeleted(object p0) { return SR.Instance["InitialMetadataCannotBeDeleted", p0]; }
        internal static string InitialStateCannotBeFinalState(object p0) { return SR.Instance["InitialStateCannotBeFinalState", p0]; }
        internal static string InitialStateNotInStatesCollection(object p0) { return SR.Instance["InitialStateNotInStatesCollection", p0]; }
        internal static string InlinedLocationReferenceOnlyAccessibleByOwner(object p0, object p1) { return SR.Instance["InlinedLocationReferenceOnlyAccessibleByOwner", p0, p1]; }
        internal static string InputParametersCountMismatch(object p0, object p1) { return SR.Instance["InputParametersCountMismatch", p0, p1]; }
        internal static string InputParametersMissing(object p0) { return SR.Instance["InputParametersMissing", p0]; }
        internal static string InputParametersTypeMismatch(object p0, object p1) { return SR.Instance["InputParametersTypeMismatch", p0, p1]; }
        internal static string InstanceCollisionDefault { get { return SR.Instance["InstanceCollisionDefault"]; } }
        internal static string InstanceCollisionSpecific(object p0) { return SR.Instance["InstanceCollisionSpecific", p0]; }
        internal static string InstanceCompleteDefault { get { return SR.Instance["InstanceCompleteDefault"]; } }
        internal static string InstanceCompleteSpecific(object p0) { return SR.Instance["InstanceCompleteSpecific", p0]; }
        internal static string InstanceHandleConflictDefault { get { return SR.Instance["InstanceHandleConflictDefault"]; } }
        internal static string InstanceHandleConflictSpecific(object p0) { return SR.Instance["InstanceHandleConflictSpecific", p0]; }
        internal static string InstanceKeyRequiresValidGuid { get { return SR.Instance["InstanceKeyRequiresValidGuid"]; } }
        internal static string InstanceLockLostDefault { get { return SR.Instance["InstanceLockLostDefault"]; } }
        internal static string InstanceLockLostSpecific(object p0) { return SR.Instance["InstanceLockLostSpecific", p0]; }
        internal static string InstanceMethodCallRequiresTargetObject { get { return SR.Instance["InstanceMethodCallRequiresTargetObject"]; } }
        internal static string InstanceMustBePaused { get { return SR.Instance["InstanceMustBePaused"]; } }
        internal static string InstanceMustNotBePaused { get { return SR.Instance["InstanceMustNotBePaused"]; } }
        internal static string InstanceNotReadyDefault { get { return SR.Instance["InstanceNotReadyDefault"]; } }
        internal static string InstanceNotReadySpecific(object p0) { return SR.Instance["InstanceNotReadySpecific", p0]; }
        internal static string InstanceOperationRequiresInstance { get { return SR.Instance["InstanceOperationRequiresInstance"]; } }
        internal static string InstanceOperationRequiresLock { get { return SR.Instance["InstanceOperationRequiresLock"]; } }
        internal static string InstanceOperationRequiresNotCompleted { get { return SR.Instance["InstanceOperationRequiresNotCompleted"]; } }
        internal static string InstanceOperationRequiresNotUninitialized { get { return SR.Instance["InstanceOperationRequiresNotUninitialized"]; } }
        internal static string InstanceOperationRequiresOwner { get { return SR.Instance["InstanceOperationRequiresOwner"]; } }
        internal static string InstanceOwnerDefault { get { return SR.Instance["InstanceOwnerDefault"]; } }
        internal static string InstanceOwnerSpecific(object p0) { return SR.Instance["InstanceOwnerSpecific", p0]; }
        internal static string InstanceRequired { get { return SR.Instance["InstanceRequired"]; } }
        internal static string InstanceStoreBoundSameVersionTwice { get { return SR.Instance["InstanceStoreBoundSameVersionTwice"]; } }
        internal static string InstanceStoreDoesntMatchWorkflowApplication { get { return SR.Instance["InstanceStoreDoesntMatchWorkflowApplication"]; } }
        internal static string InstanceStoreFailed { get { return SR.Instance["InstanceStoreFailed"]; } }
        internal static string InstanceStoreHasDefaultOwner { get { return SR.Instance["InstanceStoreHasDefaultOwner"]; } }
        internal static string InstanceStoreRequiredToPersist { get { return SR.Instance["InstanceStoreRequiredToPersist"]; } }
        internal static string InsufficientArraySize(object p0) { return SR.Instance["InsufficientArraySize", p0]; }
        internal static string InternalConstraintException(object p0, object p1, object p2, object p3) { return SR.Instance["InternalConstraintException", p0, p1, p2, p3]; }
        internal static string InvalidActivityIdFormat { get { return SR.Instance["InvalidActivityIdFormat"]; } }
        internal static string InvalidActivityToBlockUpdate(object p0) { return SR.Instance["InvalidActivityToBlockUpdate", p0]; }
        internal static string InvalidActivityToBlockUpdateServices(object p0) { return SR.Instance["InvalidActivityToBlockUpdateServices", p0]; }
        internal static string InvalidArgumentExpression(object p0, object p1) { return SR.Instance["InvalidArgumentExpression", p0, p1]; }
        internal static string InvalidAsyncBeginMethodSignature(object p0, object p1) { return SR.Instance["InvalidAsyncBeginMethodSignature", p0, p1]; }
        internal static string InvalidAsyncCancelMethodSignature(object p0, object p1) { return SR.Instance["InvalidAsyncCancelMethodSignature", p0, p1]; }
        internal static string InvalidAsyncEndMethodSignature(object p0, object p1) { return SR.Instance["InvalidAsyncEndMethodSignature", p0, p1]; }
        internal static string InvalidAsyncResult { get { return SR.Instance["InvalidAsyncResult"]; } }
        internal static string InvalidAsyncResultImplementation(object p0) { return SR.Instance["InvalidAsyncResultImplementation", p0]; }
        internal static string InvalidAsyncResultImplementationGeneric { get { return SR.Instance["InvalidAsyncResultImplementationGeneric"]; } }
        internal static string InvalidCallbackState(object p0) { return SR.Instance["InvalidCallbackState", p0]; }
        internal static string InvalidCompensateActivityUsage(object p0) { return SR.Instance["InvalidCompensateActivityUsage", p0]; }
        internal static string InvalidCompensationToken(object p0) { return SR.Instance["InvalidCompensationToken", p0]; }
        internal static string InvalidConfirmActivityUsage(object p0) { return SR.Instance["InvalidConfirmActivityUsage", p0]; }
        internal static string InvalidDirectionForArgument(object p0, object p1, object p2, object p3) { return SR.Instance["InvalidDirectionForArgument", p0, p1, p2, p3]; }
        internal static string InvalidDynamicActivityProperty(object p0) { return SR.Instance["InvalidDynamicActivityProperty", p0]; }
        internal static string InvalidEvaluationOrderValue { get { return SR.Instance["InvalidEvaluationOrderValue"]; } }
        internal static string InvalidExecutionCallback(object p0, object p1) { return SR.Instance["InvalidExecutionCallback", p0, p1]; }
        internal static string InvalidExpressionForLocation(object p0) { return SR.Instance["InvalidExpressionForLocation", p0]; }
        internal static string InvalidExpressionProperty(object p0) { return SR.Instance["InvalidExpressionProperty", p0]; }
        internal static string InvalidFileName(object p0) { return SR.Instance["InvalidFileName", p0]; }
        internal static string InvalidGenericTypeInfo(object p0) { return SR.Instance["InvalidGenericTypeInfo", p0]; }
        internal static string InvalidIdleAction { get { return SR.Instance["InvalidIdleAction"]; } }
        internal static string InvalidImplementationAsWorkflowRoot { get { return SR.Instance["InvalidImplementationAsWorkflowRoot"]; } }
        internal static string InvalidImplementationAsWorkflowRootForRuntimeState { get { return SR.Instance["InvalidImplementationAsWorkflowRootForRuntimeState"]; } }
        internal static string InvalidImplementationAsWorkflowRootForRuntimeStateBecauseArgumentsChanged { get { return SR.Instance["InvalidImplementationAsWorkflowRootForRuntimeStateBecauseArgumentsChanged"]; } }
        internal static string InvalidImplementationMap(object p0, object p1) { return SR.Instance["InvalidImplementationMap", p0, p1]; }
        internal static string InvalidImplementationMapAssociation(object p0) { return SR.Instance["InvalidImplementationMapAssociation", p0]; }
        internal static string InvalidImplementationMapAssociationServices(object p0) { return SR.Instance["InvalidImplementationMapAssociationServices", p0]; }
        internal static string InvalidImplementationMapRuntime { get { return SR.Instance["InvalidImplementationMapRuntime"]; } }
        internal static string InvalidInstanceState { get { return SR.Instance["InvalidInstanceState"]; } }
        internal static string InvalidKeyArgument { get { return SR.Instance["InvalidKeyArgument"]; } }
        internal static string InvalidLocationExpression { get { return SR.Instance["InvalidLocationExpression"]; } }
        internal static string InvalidLockToken { get { return SR.Instance["InvalidLockToken"]; } }
        internal static string InvalidLValueExpression { get { return SR.Instance["InvalidLValueExpression"]; } }
        internal static string InvalidMergeMap(object p0, object p1, object p2) { return SR.Instance["InvalidMergeMap", p0, p1, p2]; }
        internal static string InvalidMergeMapArgumentCount(object p0, object p1) { return SR.Instance["InvalidMergeMapArgumentCount", p0, p1]; }
        internal static string InvalidMergeMapArgumentsChanged { get { return SR.Instance["InvalidMergeMapArgumentsChanged"]; } }
        internal static string InvalidMergeMapEnvironmentCount(object p0, object p1, object p2, object p3, object p4, object p5) { return SR.Instance["InvalidMergeMapEnvironmentCount", p0, p1, p2, p3, p4, p5]; }
        internal static string InvalidMergeMapForImplementation(object p0, object p1) { return SR.Instance["InvalidMergeMapForImplementation", p0, p1]; }
        internal static string InvalidMergeMapMemberCount(object p0, object p1) { return SR.Instance["InvalidMergeMapMemberCount", p0, p1]; }
        internal static string InvalidNullAsyncResult { get { return SR.Instance["InvalidNullAsyncResult"]; } }
        internal static string InvalidOriginalWorkflowDefinitionForImplementationMapCreation { get { return SR.Instance["InvalidOriginalWorkflowDefinitionForImplementationMapCreation"]; } }
        internal static string InvalidParameterInfo(object p0, object p1) { return SR.Instance["InvalidParameterInfo", p0, p1]; }
        internal static string InvalidPrepareForRuntimeValidationSettings { get { return SR.Instance["InvalidPrepareForRuntimeValidationSettings"]; } }
        internal static string InvalidProperty(object p0) { return SR.Instance["InvalidProperty", p0]; }
        internal static string InvalidPropertyType(object p0, object p1) { return SR.Instance["InvalidPropertyType", p0, p1]; }
        internal static string InvalidRootMergeMap(object p0, object p1) { return SR.Instance["InvalidRootMergeMap", p0, p1]; }
        internal static string InvalidRuntimeState { get { return SR.Instance["InvalidRuntimeState"]; } }
        internal static string InvalidSemaphoreExit { get { return SR.Instance["InvalidSemaphoreExit"]; } }
        internal static string InvalidSourceLocationColumn(object p0, object p1) { return SR.Instance["InvalidSourceLocationColumn", p0, p1]; }
        internal static string InvalidSourceLocationLineNumber(object p0, object p1) { return SR.Instance["InvalidSourceLocationLineNumber", p0, p1]; }
        internal static string InvalidStateForAsyncCallback { get { return SR.Instance["InvalidStateForAsyncCallback"]; } }
        internal static string InvalidStateInAsyncResult { get { return SR.Instance["InvalidStateInAsyncResult"]; } }
        internal static string InvalidTypeConverterUsage { get { return SR.Instance["InvalidTypeConverterUsage"]; } }
        internal static string InvalidTypeForArgument(object p0, object p1, object p2, object p3) { return SR.Instance["InvalidTypeForArgument", p0, p1, p2, p3]; }
        internal static string InvalidUnhandledExceptionAction { get { return SR.Instance["InvalidUnhandledExceptionAction"]; } }
        internal static string InvalidUpdatedWorkflowDefinitionForImplementationMapCreation { get { return SR.Instance["InvalidUpdatedWorkflowDefinitionForImplementationMapCreation"]; } }
        internal static string InvalidUpdateMap(object p0) { return SR.Instance["InvalidUpdateMap", p0]; }
        internal static string InvalidVisualBasicSettingsValue { get { return SR.Instance["InvalidVisualBasicSettingsValue"]; } }
        internal static string InvalidXamlMember(object p0) { return SR.Instance["InvalidXamlMember", p0]; }
        internal static string IsolationLevelValidation { get { return SR.Instance["IsolationLevelValidation"]; } }
        internal static string ITextExpressionParameterMustBeActivity { get { return SR.Instance["ITextExpressionParameterMustBeActivity"]; } }
        internal static string KeyAlreadyAssociated { get { return SR.Instance["KeyAlreadyAssociated"]; } }
        internal static string KeyAlreadyCompleted { get { return SR.Instance["KeyAlreadyCompleted"]; } }
        internal static string KeyAlreadyUnassociated { get { return SR.Instance["KeyAlreadyUnassociated"]; } }
        internal static string KeyCollectionUpdatesNotAllowed { get { return SR.Instance["KeyCollectionUpdatesNotAllowed"]; } }
        internal static string KeyCollisionDefault { get { return SR.Instance["KeyCollisionDefault"]; } }
        internal static string KeyCollisionSpecific(object p0, object p1, object p2) { return SR.Instance["KeyCollisionSpecific", p0, p1, p2]; }
        internal static string KeyCollisionSpecificKeyOnly(object p0) { return SR.Instance["KeyCollisionSpecificKeyOnly", p0]; }
        internal static string KeyCompleteDefault { get { return SR.Instance["KeyCompleteDefault"]; } }
        internal static string KeyCompleteSpecific(object p0) { return SR.Instance["KeyCompleteSpecific", p0]; }
        internal static string KeyNotAssociated { get { return SR.Instance["KeyNotAssociated"]; } }
        internal static string KeyNotCompleted { get { return SR.Instance["KeyNotCompleted"]; } }
        internal static string KeyNotFoundInDictionary { get { return SR.Instance["KeyNotFoundInDictionary"]; } }
        internal static string KeyNotReadyDefault { get { return SR.Instance["KeyNotReadyDefault"]; } }
        internal static string KeyNotReadySpecific(object p0) { return SR.Instance["KeyNotReadySpecific", p0]; }
        internal static string LambdaExpressionReturnTypeInvalid { get { return SR.Instance["LambdaExpressionReturnTypeInvalid"]; } }
        internal static string LambdaExpressionTypeRequired { get { return SR.Instance["LambdaExpressionTypeRequired"]; } }
        internal static string LambdaNotXamlSerializable { get { return SR.Instance["LambdaNotXamlSerializable"]; } }
        internal static string LineNumberTooLarge(object p0) { return SR.Instance["LineNumberTooLarge", p0]; }
        internal static string LiteralsMustBeValueTypesOrImmutableTypes(object p0, object p1) { return SR.Instance["LiteralsMustBeValueTypesOrImmutableTypes", p0, p1]; }
        internal static string LoadedWriteOnlyValue { get { return SR.Instance["LoadedWriteOnlyValue"]; } }
        internal static string LoadingWorkflowApplicationRequiresInstanceStore { get { return SR.Instance["LoadingWorkflowApplicationRequiresInstanceStore"]; } }
        internal static string LoadOpAssociateKeysCannotContainLookupKey { get { return SR.Instance["LoadOpAssociateKeysCannotContainLookupKey"]; } }
        internal static string LoadOpFreeKeyRequiresAcceptUninitialized { get { return SR.Instance["LoadOpFreeKeyRequiresAcceptUninitialized"]; } }
        internal static string LoadOpKeyMustBeValid { get { return SR.Instance["LoadOpKeyMustBeValid"]; } }
        internal static string LocationExpressionCouldNotBeResolved(object p0) { return SR.Instance["LocationExpressionCouldNotBeResolved", p0]; }
        internal static string LocationTypeMismatch(object p0, object p1, object p2) { return SR.Instance["LocationTypeMismatch", p0, p1, p2]; }
        internal static string LockTimeoutExceptionMessage(object p0) { return SR.Instance["LockTimeoutExceptionMessage", p0]; }
        internal static string MapEntryNotFound(object p0) { return SR.Instance["MapEntryNotFound", p0]; }
        internal static string MarkCanceledOnlyCallableIfCancelRequested { get { return SR.Instance["MarkCanceledOnlyCallableIfCancelRequested"]; } }
        internal static string MayBindLockCommandShouldValidateOwner { get { return SR.Instance["MayBindLockCommandShouldValidateOwner"]; } }
        internal static string MemberCannotBeNull(object p0, object p1, object p2) { return SR.Instance["MemberCannotBeNull", p0, p1, p2]; }
        internal static string MemberIsReadOnly(object p0, object p1) { return SR.Instance["MemberIsReadOnly", p0, p1]; }
        internal static string MemberNotFound(object p0, object p1) { return SR.Instance["MemberNotFound", p0, p1]; }
        internal static string MemberNotSupportedByActivityXamlServices(object p0) { return SR.Instance["MemberNotSupportedByActivityXamlServices", p0]; }
        internal static string MetadataCannotContainNullKey { get { return SR.Instance["MetadataCannotContainNullKey"]; } }
        internal static string MetadataCannotContainNullValue(object p0) { return SR.Instance["MetadataCannotContainNullValue", p0]; }
        internal static string MethodInfoRequired(object p0) { return SR.Instance["MethodInfoRequired", p0]; }
        internal static string MethodNameRequired(object p0) { return SR.Instance["MethodNameRequired", p0]; }
        internal static string MissingArgument(object p0, object p1) { return SR.Instance["MissingArgument", p0, p1]; }
        internal static string MissingNameProperty(object p0) { return SR.Instance["MissingNameProperty", p0]; }
        internal static string MissingSetAccessorForIndexer(object p0, object p1) { return SR.Instance["MissingSetAccessorForIndexer", p0, p1]; }
        internal static string MovingActivitiesInStateBlockDU { get { return SR.Instance["MovingActivitiesInStateBlockDU"]; } }
        internal static string MultiDimensionalArraysNotSupported { get { return SR.Instance["MultiDimensionalArraysNotSupported"]; } }
        internal static string MultipleFlowNodesSharingSameChildBlockDU { get { return SR.Instance["MultipleFlowNodesSharingSameChildBlockDU"]; } }
        internal static string MultipleOverloadGroupsConfigured(object p0) { return SR.Instance["MultipleOverloadGroupsConfigured", p0]; }
        internal static string MustCallPrepareBeforeFinalize { get { return SR.Instance["MustCallPrepareBeforeFinalize"]; } }
        internal static string MustCancelOldTimer { get { return SR.Instance["MustCancelOldTimer"]; } }
        internal static string MustMatchReferenceExpressionReturnType { get { return SR.Instance["MustMatchReferenceExpressionReturnType"]; } }
        internal static string MustSetTransactionOnFirstCall { get { return SR.Instance["MustSetTransactionOnFirstCall"]; } }
        internal static string NameCollisionOnCollect(object p0, object p1) { return SR.Instance["NameCollisionOnCollect", p0, p1]; }
        internal static string NameCollisionOnMap(object p0, object p1) { return SR.Instance["NameCollisionOnMap", p0, p1]; }
        internal static string NativeActivityUpdateInstanceThrewException(object p0) { return SR.Instance["NativeActivityUpdateInstanceThrewException", p0]; }
        internal static string NAUCDisposed { get { return SR.Instance["NAUCDisposed"]; } }
        internal static string NewArrayBoundsRequiresIntegralArguments { get { return SR.Instance["NewArrayBoundsRequiresIntegralArguments"]; } }
        internal static string NewArrayRequiresArrayTypeAsResultType { get { return SR.Instance["NewArrayRequiresArrayTypeAsResultType"]; } }
        internal static string NoCAInSecondaryRoot { get { return SR.Instance["NoCAInSecondaryRoot"]; } }
        internal static string NoChangesMapQueryNotSupported { get { return SR.Instance["NoChangesMapQueryNotSupported"]; } }
        internal static string NoDynamicArgumentsInActivityDefinitionChange { get { return SR.Instance["NoDynamicArgumentsInActivityDefinitionChange"]; } }
        internal static string NoDynamicArgumentsInActivityDefinitionChangeRuntime { get { return SR.Instance["NoDynamicArgumentsInActivityDefinitionChangeRuntime"]; } }
        internal static string NoNamespace(object p0) { return SR.Instance["NoNamespace", p0]; }
        internal static string NoOutputLocationWasFound(object p0) { return SR.Instance["NoOutputLocationWasFound", p0]; }
        internal static string NoOverloadGroupsAreConfigured { get { return SR.Instance["NoOverloadGroupsAreConfigured"]; } }
        internal static string NoPersistScopeCannotContainPersist(object p0, object p1) { return SR.Instance["NoPersistScopeCannotContainPersist", p0, p1]; }
        internal static string NoRunnableInstances { get { return SR.Instance["NoRunnableInstances"]; } }
        internal static string NoRuntimeTransactionExists { get { return SR.Instance["NoRuntimeTransactionExists"]; } }
        internal static string NullAssignedToValueType(object p0) { return SR.Instance["NullAssignedToValueType", p0]; }
        internal static string NullKeyAlreadyPresent { get { return SR.Instance["NullKeyAlreadyPresent"]; } }
        internal static string NullReferencedMemberAccess(object p0, object p1) { return SR.Instance["NullReferencedMemberAccess", p0, p1]; }
        internal static string OnCancelRequestedThrew { get { return SR.Instance["OnCancelRequestedThrew"]; } }
        internal static string OneOfTwoPropertiesMustBeSet(object p0, object p1, object p2, object p3) { return SR.Instance["OneOfTwoPropertiesMustBeSet", p0, p1, p2, p3]; }
        internal static string OnFreeInstanceHandleThrew { get { return SR.Instance["OnFreeInstanceHandleThrew"]; } }
        internal static string OnlyBookmarkOwnerCanRemove { get { return SR.Instance["OnlyBookmarkOwnerCanRemove"]; } }
        internal static string OnlyOneOperationPerActivity { get { return SR.Instance["OnlyOneOperationPerActivity"]; } }
        internal static string OnlyOneRequireTransactionContextAllowed { get { return SR.Instance["OnlyOneRequireTransactionContextAllowed"]; } }
        internal static string OnlySingleCastDelegatesAllowed { get { return SR.Instance["OnlySingleCastDelegatesAllowed"]; } }
        internal static string OperationAlreadyCompleted { get { return SR.Instance["OperationAlreadyCompleted"]; } }
        internal static string OptionalExtensionTypeMatchedMultiple(object p0) { return SR.Instance["OptionalExtensionTypeMatchedMultiple", p0]; }
        internal static string OriginalActivityReusedInModifiedDefinition(object p0) { return SR.Instance["OriginalActivityReusedInModifiedDefinition", p0]; }
        internal static string OriginalVariableReusedInModifiedDefinition(object p0) { return SR.Instance["OriginalVariableReusedInModifiedDefinition", p0]; }
        internal static string OriginCannotBeRuntimeIntrinsic(object p0) { return SR.Instance["OriginCannotBeRuntimeIntrinsic", p0]; }
        internal static string OutArgumentCannotHaveInputValue(object p0) { return SR.Instance["OutArgumentCannotHaveInputValue", p0]; }
        internal static string OutOfIdSpaceIds { get { return SR.Instance["OutOfIdSpaceIds"]; } }
        internal static string OutOfInstanceIds { get { return SR.Instance["OutOfInstanceIds"]; } }
        internal static string OutOfInternalBookmarks { get { return SR.Instance["OutOfInternalBookmarks"]; } }
        internal static string OutOfRangeSourceLocationEndColumn(object p0) { return SR.Instance["OutOfRangeSourceLocationEndColumn", p0]; }
        internal static string OutOfRangeSourceLocationEndLine(object p0) { return SR.Instance["OutOfRangeSourceLocationEndLine", p0]; }
        internal static string OutsideInstanceExecutionScope(object p0) { return SR.Instance["OutsideInstanceExecutionScope", p0]; }
        internal static string OutsideTransactionalCommand(object p0) { return SR.Instance["OutsideTransactionalCommand", p0]; }
        internal static string OverloadGroupHasSubsets(object p0, object p1) { return SR.Instance["OverloadGroupHasSubsets", p0, p1]; }
        internal static string OverloadGroupsAreEquivalent(object p0) { return SR.Instance["OverloadGroupsAreEquivalent", p0]; }
        internal static string OverloadingMethodMustBeStatic { get { return SR.Instance["OverloadingMethodMustBeStatic"]; } }
        internal static string OverloadOnlyCallableFromWorkflowThread { get { return SR.Instance["OverloadOnlyCallableFromWorkflowThread"]; } }
        internal static string OwnerBelongsToWrongStore { get { return SR.Instance["OwnerBelongsToWrongStore"]; } }
        internal static string OwnerRequired { get { return SR.Instance["OwnerRequired"]; } }
        internal static string ParallelForEachRequiresNonNullValues(object p0) { return SR.Instance["ParallelForEachRequiresNonNullValues", p0]; }
        internal static string PauseWhenPersistableInvalidIfPersistable { get { return SR.Instance["PauseWhenPersistableInvalidIfPersistable"]; } }
        internal static string PermissionArgumentWrongType(object p0) { return SR.Instance["PermissionArgumentWrongType", p0]; }
        internal static string PersistenceInitializerThrew { get { return SR.Instance["PersistenceInitializerThrew"]; } }
        internal static string PersistencePipelineAbortThrew(object p0) { return SR.Instance["PersistencePipelineAbortThrew", p0]; }
        internal static string PickBranchRequiresTrigger(object p0) { return SR.Instance["PickBranchRequiresTrigger", p0]; }
        internal static string PickBranchTriggerActionSwapped { get { return SR.Instance["PickBranchTriggerActionSwapped"]; } }
        internal static string PrepareForSerializationRequiresPersistability { get { return SR.Instance["PrepareForSerializationRequiresPersistability"]; } }
        internal static string PrivateMembersHaveChanged { get { return SR.Instance["PrivateMembersHaveChanged"]; } }
        internal static string PropertyCannotBeModified(object p0) { return SR.Instance["PropertyCannotBeModified", p0]; }
        internal static string PropertyMemberNotSupportedByActivityXamlServices(object p0) { return SR.Instance["PropertyMemberNotSupportedByActivityXamlServices", p0]; }
        internal static string PropertyReadOnlyInWorkflowDataContext(object p0) { return SR.Instance["PropertyReadOnlyInWorkflowDataContext", p0]; }
        internal static string PropertyReferenceNotFound(object p0) { return SR.Instance["PropertyReferenceNotFound", p0]; }
        internal static string ProvidedStateInitializedForExecution { get { return SR.Instance["ProvidedStateInitializedForExecution"]; } }
        internal static string ProviderDoesNotSupportCommand(object p0) { return SR.Instance["ProviderDoesNotSupportCommand", p0]; }
        internal static string PublicChildrenChangeBlockDU { get { return SR.Instance["PublicChildrenChangeBlockDU"]; } }
        internal static string PublicEnvironmentAccessToNonGenericActivity { get { return SR.Instance["PublicEnvironmentAccessToNonGenericActivity"]; } }
        internal static string PublicMethodWithMatchingParameterDoesNotExist(object p0, object p1, object p2, object p3) { return SR.Instance["PublicMethodWithMatchingParameterDoesNotExist", p0, p1, p2, p3]; }
        internal static string PublicReferencesOnActivityRequiringArgumentResolution(object p0) { return SR.Instance["PublicReferencesOnActivityRequiringArgumentResolution", p0]; }
        internal static string QueryActivityIsInImplementation(object p0) { return SR.Instance["QueryActivityIsInImplementation", p0]; }
        internal static string QueryActivityIsNotInDefinition { get { return SR.Instance["QueryActivityIsNotInDefinition"]; } }
        internal static string QueryActivityIsPublic(object p0) { return SR.Instance["QueryActivityIsPublic", p0]; }
        internal static string QueryVariableIsInImplementation(object p0) { return SR.Instance["QueryVariableIsInImplementation", p0]; }
        internal static string QueryVariableIsNotInDefinition { get { return SR.Instance["QueryVariableIsNotInDefinition"]; } }
        internal static string QueryVariableIsNotPublic { get { return SR.Instance["QueryVariableIsNotPublic"]; } }
        internal static string QueryVariableIsPublic(object p0) { return SR.Instance["QueryVariableIsPublic", p0]; }
        internal static string ReadAccessToWriteOnlyPublicReference(object p0) { return SR.Instance["ReadAccessToWriteOnlyPublicReference", p0]; }
        internal static string ReadNotSupported { get { return SR.Instance["ReadNotSupported"]; } }
        internal static string ReadonlyNameScopeCannotBeUpdated { get { return SR.Instance["ReadonlyNameScopeCannotBeUpdated"]; } }
        internal static string ReadonlyPropertyCannotBeSet(object p0, object p1) { return SR.Instance["ReadonlyPropertyCannotBeSet", p0, p1]; }
        internal static string ReferencedChildInIsNewlyAdded { get { return SR.Instance["ReferencedChildInIsNewlyAdded"]; } }
        internal static string RegisteredBookmarkScopeRequired { get { return SR.Instance["RegisteredBookmarkScopeRequired"]; } }
        internal static string RemovingTransitionsBlockDU { get { return SR.Instance["RemovingTransitionsBlockDU"]; } }
        internal static string RequiredArgumentValueNotSupplied(object p0) { return SR.Instance["RequiredArgumentValueNotSupplied", p0]; }
        internal static string RequiredExtensionTypeNotFound(object p0) { return SR.Instance["RequiredExtensionTypeNotFound", p0]; }
        internal static string RequiredVariableCoundNotBeExtracted(object p0, object p1) { return SR.Instance["RequiredVariableCoundNotBeExtracted", p0, p1]; }
        internal static string RequireExtensionOnlyAcceptsReferenceTypes(object p0) { return SR.Instance["RequireExtensionOnlyAcceptsReferenceTypes", p0]; }
        internal static string ResultArgumentHasRequiredTypeAndDirection(object p0, object p1, object p2) { return SR.Instance["ResultArgumentHasRequiredTypeAndDirection", p0, p1, p2]; }
        internal static string ResultArgumentMustBeBoundToResultProperty { get { return SR.Instance["ResultArgumentMustBeBoundToResultProperty"]; } }
        internal static string ResultArgumentMustBeSpecificType(object p0) { return SR.Instance["ResultArgumentMustBeSpecificType", p0]; }
        internal static string ResultCannotBeSetOnArgumentExpressions { get { return SR.Instance["ResultCannotBeSetOnArgumentExpressions"]; } }
        internal static string RethrowMustBeAPublicChild(object p0) { return SR.Instance["RethrowMustBeAPublicChild", p0]; }
        internal static string RethrowNotInATryCatch(object p0) { return SR.Instance["RethrowNotInATryCatch", p0]; }
        internal static string ReturnTypeIncompatible(object p0, object p1, object p2, object p3, object p4) { return SR.Instance["ReturnTypeIncompatible", p0, p1, p2, p3, p4]; }
        internal static string RootActivityAlreadyAssociatedWithInstance(object p0) { return SR.Instance["RootActivityAlreadyAssociatedWithInstance", p0]; }
        internal static string RootActivityCannotBeReferenced(object p0, object p1) { return SR.Instance["RootActivityCannotBeReferenced", p0, p1]; }
        internal static string RootArgumentViolationsFound { get { return SR.Instance["RootArgumentViolationsFound"]; } }
        internal static string RootArgumentViolationsFoundNoInputs { get { return SR.Instance["RootArgumentViolationsFoundNoInputs"]; } }
        internal static string RuntimeArgumentBindingInvalid(object p0, object p1) { return SR.Instance["RuntimeArgumentBindingInvalid", p0, p1]; }
        internal static string RuntimeArgumentChangeBlockDU(object p0) { return SR.Instance["RuntimeArgumentChangeBlockDU", p0]; }
        internal static string RuntimeArgumentNotCreated { get { return SR.Instance["RuntimeArgumentNotCreated"]; } }
        internal static string RuntimeArgumentNotOpen(object p0) { return SR.Instance["RuntimeArgumentNotOpen", p0]; }
        internal static string RuntimeDelegateArgumentDirectionIncorrect { get { return SR.Instance["RuntimeDelegateArgumentDirectionIncorrect"]; } }
        internal static string RuntimeDelegateArgumentTypeIncorrect { get { return SR.Instance["RuntimeDelegateArgumentTypeIncorrect"]; } }
        internal static string RuntimeOperationInProgress { get { return SR.Instance["RuntimeOperationInProgress"]; } }
        internal static string RuntimeRunning { get { return SR.Instance["RuntimeRunning"]; } }
        internal static string RuntimeTransactionAlreadyExists { get { return SR.Instance["RuntimeTransactionAlreadyExists"]; } }
        internal static string RuntimeTransactionHandleNotRegisteredAsExecutionProperty(object p0) { return SR.Instance["RuntimeTransactionHandleNotRegisteredAsExecutionProperty", p0]; }
        internal static string RuntimeTransactionIsSuppressed { get { return SR.Instance["RuntimeTransactionIsSuppressed"]; } }
        internal static string SameUserStateUsedForMultipleInvokes { get { return SR.Instance["SameUserStateUsedForMultipleInvokes"]; } }
        internal static string SavingActivityToXamlNotSupported { get { return SR.Instance["SavingActivityToXamlNotSupported"]; } }
        internal static string SavingFuncToXamlNotSupported { get { return SR.Instance["SavingFuncToXamlNotSupported"]; } }
        internal static string SchemaContextFromBeforeInitializeComponentXBTExtensionCannotBeGeneric(object p0) { return SR.Instance["SchemaContextFromBeforeInitializeComponentXBTExtensionCannotBeGeneric", p0]; }
        internal static string SchemaContextFromBeforeInitializeComponentXBTExtensionNotFound(object p0) { return SR.Instance["SchemaContextFromBeforeInitializeComponentXBTExtensionNotFound", p0]; }
        internal static string SeekNotSupported { get { return SR.Instance["SeekNotSupported"]; } }
        internal static string SendNotSupported { get { return SR.Instance["SendNotSupported"]; } }
        internal static string SequenceDuplicateReferences { get { return SR.Instance["SequenceDuplicateReferences"]; } }
        internal static string SetupOrCleanupWorkflowThreadThrew { get { return SR.Instance["SetupOrCleanupWorkflowThreadThrew"]; } }
        internal static string SFxTaskNotStarted { get { return SR.Instance["SFxTaskNotStarted"]; } }
        internal static string ShipAssertExceptionMessage(object p0) { return SR.Instance["ShipAssertExceptionMessage", p0]; }
        internal static string SimpleStateMustHaveOneTransition(object p0) { return SR.Instance["SimpleStateMustHaveOneTransition", p0]; }
        internal static string SpecialMethodNotFound(object p0, object p1) { return SR.Instance["SpecialMethodNotFound", p0, p1]; }
        internal static string StateCannotBeAddedTwice(object p0) { return SR.Instance["StateCannotBeAddedTwice", p0]; }
        internal static string StateCannotBeSerialized(object p0) { return SR.Instance["StateCannotBeSerialized", p0]; }
        internal static string StateMachineMustHaveInitialState(object p0) { return SR.Instance["StateMachineMustHaveInitialState", p0]; }
        internal static string StateNotBelongToAnyParent(object p0, object p1) { return SR.Instance["StateNotBelongToAnyParent", p0, p1]; }
        internal static string StoreReportedConflictingLockTokens { get { return SR.Instance["StoreReportedConflictingLockTokens"]; } }
        internal static string SubexpressionResultWasNotVisible(object p0) { return SR.Instance["SubexpressionResultWasNotVisible", p0]; }
        internal static string SubexpressionResultWasNull(object p0) { return SR.Instance["SubexpressionResultWasNull", p0]; }
        internal static string SwitchCaseKeyTypesMustMatchExpressionType(object p0, object p1, object p2) { return SR.Instance["SwitchCaseKeyTypesMustMatchExpressionType", p0, p1, p2]; }
        internal static string SwitchCaseNullWithValueType(object p0) { return SR.Instance["SwitchCaseNullWithValueType", p0]; }
        internal static string SwitchCaseTypeMismatch(object p0, object p1) { return SR.Instance["SwitchCaseTypeMismatch", p0, p1]; }
        internal static string SymbolNamesMustBeUnique(object p0) { return SR.Instance["SymbolNamesMustBeUnique", p0]; }
        internal static string SymbolResolverAlreadyExists { get { return SR.Instance["SymbolResolverAlreadyExists"]; } }
        internal static string SymbolResolverDoesNotHaveSymbol(object p0, object p1) { return SR.Instance["SymbolResolverDoesNotHaveSymbol", p0, p1]; }
        internal static string SymbolResolverMustBeSingleton { get { return SR.Instance["SymbolResolverMustBeSingleton"]; } }
        internal static string TargetTypeAndTargetObjectAreMutuallyExclusive(object p0, object p1) { return SR.Instance["TargetTypeAndTargetObjectAreMutuallyExclusive", p0, p1]; }
        internal static string TargetTypeCannotBeEnum(object p0, object p1) { return SR.Instance["TargetTypeCannotBeEnum", p0, p1]; }
        internal static string TargetTypeIsValueType(object p0, object p1) { return SR.Instance["TargetTypeIsValueType", p0, p1]; }
        internal static string TaskTimedOutError(object p0) { return SR.Instance["TaskTimedOutError", p0]; }
        internal static string TextExpressionCompilerActivityNameRequired { get { return SR.Instance["TextExpressionCompilerActivityNameRequired"]; } }
        internal static string TextExpressionCompilerActivityNamespaceRequired { get { return SR.Instance["TextExpressionCompilerActivityNamespaceRequired"]; } }
        internal static string TextExpressionCompilerActivityRequired { get { return SR.Instance["TextExpressionCompilerActivityRequired"]; } }
        internal static string TextExpressionCompilerAddGeneratedFileRequired { get { return SR.Instance["TextExpressionCompilerAddGeneratedFileRequired"]; } }
        internal static string TextExpressionCompilerLanguageRequired { get { return SR.Instance["TextExpressionCompilerLanguageRequired"]; } }
        internal static string TextExpressionCompilerNoCodebase(object p0) { return SR.Instance["TextExpressionCompilerNoCodebase", p0]; }
        internal static string TextExpressionCompilerOutputPathRequired { get { return SR.Instance["TextExpressionCompilerOutputPathRequired"]; } }
        internal static string TextExpressionCompilerUnableToLoadAssembly(object p0) { return SR.Instance["TextExpressionCompilerUnableToLoadAssembly", p0]; }
        internal static string TextExpressionMetadataRequiresCompilation(object p0) { return SR.Instance["TextExpressionMetadataRequiresCompilation", p0]; }
        internal static string ThreadNeutralSemaphoreAborted { get { return SR.Instance["ThreadNeutralSemaphoreAborted"]; } }
        internal static string TimedOutWaitingForLockResolution { get { return SR.Instance["TimedOutWaitingForLockResolution"]; } }
        internal static string TimeoutInputQueueDequeue(object p0) { return SR.Instance["TimeoutInputQueueDequeue", p0]; }
        internal static string TimeoutMustBeNonNegative(object p0, object p1) { return SR.Instance["TimeoutMustBeNonNegative", p0, p1]; }
        internal static string TimeoutMustBePositive(object p0, object p1) { return SR.Instance["TimeoutMustBePositive", p0, p1]; }
        internal static string TimeoutOnOperation(object p0) { return SR.Instance["TimeoutOnOperation", p0]; }
        internal static string TimerExtensionAlreadyAttached { get { return SR.Instance["TimerExtensionAlreadyAttached"]; } }
        internal static string TimerExtensionRequiresWorkflowInstance { get { return SR.Instance["TimerExtensionRequiresWorkflowInstance"]; } }
        internal static string TooManyViolationsForExceptionMessage { get { return SR.Instance["TooManyViolationsForExceptionMessage"]; } }
        internal static string TrackingRelatedWorkflowAbort { get { return SR.Instance["TrackingRelatedWorkflowAbort"]; } }
        internal static string TransactionHandleAlreadyHasTransaction { get { return SR.Instance["TransactionHandleAlreadyHasTransaction"]; } }
        internal static string TransactionInDoubtNonHost { get { return SR.Instance["TransactionInDoubtNonHost"]; } }
        internal static string TransactionRolledBackNonHost { get { return SR.Instance["TransactionRolledBackNonHost"]; } }
        internal static string TransitionCannotBeAddedTwice(object p0, object p1, object p2) { return SR.Instance["TransitionCannotBeAddedTwice", p0, p1, p2]; }
        internal static string TransitionTargetCannotBeNull(object p0, object p1) { return SR.Instance["TransitionTargetCannotBeNull", p0, p1]; }
        internal static string TriggerOrConditionChangedDuringTransitioning { get { return SR.Instance["TriggerOrConditionChangedDuringTransitioning"]; } }
        internal static string TriggerOrConditionIsReferenced { get { return SR.Instance["TriggerOrConditionIsReferenced"]; } }
        internal static string TryCatchInvalidStateForUpdate(object p0) { return SR.Instance["TryCatchInvalidStateForUpdate", p0]; }
        internal static string TryCommandCannotExecuteSubCommandsAndReduce { get { return SR.Instance["TryCommandCannotExecuteSubCommandsAndReduce"]; } }
        internal static string TryLoadRequiresOwner { get { return SR.Instance["TryLoadRequiresOwner"]; } }
        internal static string TypeConverterHelperCacheAddFailed(object p0) { return SR.Instance["TypeConverterHelperCacheAddFailed", p0]; }
        internal static string TypeMismatchForAssign(object p0, object p1, object p2) { return SR.Instance["TypeMismatchForAssign", p0, p1, p2]; }
        internal static string TypeMustbeValueType(object p0) { return SR.Instance["TypeMustbeValueType", p0]; }
        internal static string TypeNotAssignableTo(object p0, object p1) { return SR.Instance["TypeNotAssignableTo", p0, p1]; }
        internal static string UnableToLocateCompiledLocationContext(object p0) { return SR.Instance["UnableToLocateCompiledLocationContext", p0]; }
        internal static string UnconditionalTransitionShouldNotShareNullTriggersWithOthers(object p0, object p1) { return SR.Instance["UnconditionalTransitionShouldNotShareNullTriggersWithOthers", p0, p1]; }
        internal static string UnconditionalTransitionShouldNotShareTriggersWithOthers(object p0, object p1, object p2) { return SR.Instance["UnconditionalTransitionShouldNotShareTriggersWithOthers", p0, p1, p2]; }
        internal static string UnexpectedArgumentCount(object p0, object p1, object p2) { return SR.Instance["UnexpectedArgumentCount", p0, p1, p2]; }
        internal static string UnexpectedExpressionNodeType(object p0, object p1) { return SR.Instance["UnexpectedExpressionNodeType", p0, p1]; }
        internal static string UninitializedCannotHaveData { get { return SR.Instance["UninitializedCannotHaveData"]; } }
        internal static string UnInitializedRuntimeTransactionHandle { get { return SR.Instance["UnInitializedRuntimeTransactionHandle"]; } }
        internal static string UnknownExpressionCompilationError(object p0) { return SR.Instance["UnknownExpressionCompilationError", p0]; }
        internal static string UnknownLanguage(object p0) { return SR.Instance["UnknownLanguage", p0]; }
        internal static string UnmatchedNoPersistExit { get { return SR.Instance["UnmatchedNoPersistExit"]; } }
        internal static string UnopenedActivitiesCannotBeExecuted(object p0) { return SR.Instance["UnopenedActivitiesCannotBeExecuted", p0]; }
        internal static string UnsupportedExpressionType(object p0) { return SR.Instance["UnsupportedExpressionType", p0]; }
        internal static string UnsupportedLocationReferenceValue { get { return SR.Instance["UnsupportedLocationReferenceValue"]; } }
        internal static string UnsupportedMemberExpressionWithType(object p0) { return SR.Instance["UnsupportedMemberExpressionWithType", p0]; }
        internal static string UnsupportedReferenceExpressionType(object p0) { return SR.Instance["UnsupportedReferenceExpressionType", p0]; }
        internal static string UnusedInputArguments(object p0) { return SR.Instance["UnusedInputArguments", p0]; }
        internal static string UpdateMapBuilderRequiredProperty(object p0) { return SR.Instance["UpdateMapBuilderRequiredProperty", p0]; }
        internal static string UpdateSymbolsMustMatch { get { return SR.Instance["UpdateSymbolsMustMatch"]; } }
        internal static string ValidateUnlockInstance { get { return SR.Instance["ValidateUnlockInstance"]; } }
        internal static string ValidationContextCannotBeNull(object p0, object p1) { return SR.Instance["ValidationContextCannotBeNull", p0, p1]; }
        internal static string ValidationErrorPrefixForHiddenActivity(object p0) { return SR.Instance["ValidationErrorPrefixForHiddenActivity", p0]; }
        internal static string ValidationErrorPrefixForPublicActivityWithHiddenParent(object p0, object p1) { return SR.Instance["ValidationErrorPrefixForPublicActivityWithHiddenParent", p0, p1]; }
        internal static string ValueCollectionUpdatesNotAllowed { get { return SR.Instance["ValueCollectionUpdatesNotAllowed"]; } }
        internal static string ValueMustBeAssignableToType { get { return SR.Instance["ValueMustBeAssignableToType"]; } }
        internal static string ValueMustBeNonNegative { get { return SR.Instance["ValueMustBeNonNegative"]; } }
        internal static string VariableAlreadyInUseOnActivity(object p0, object p1, object p2) { return SR.Instance["VariableAlreadyInUseOnActivity", p0, p1, p2]; }
        internal static string VariableCannotBePopulatedInLocationEnvironment(object p0) { return SR.Instance["VariableCannotBePopulatedInLocationEnvironment", p0]; }
        internal static string VariableDoesNotExist(object p0) { return SR.Instance["VariableDoesNotExist", p0]; }
        internal static string VariableExpressionTypeMismatch(object p0, object p1, object p2) { return SR.Instance["VariableExpressionTypeMismatch", p0, p1, p2]; }
        internal static string VariableIsReadOnly(object p0) { return SR.Instance["VariableIsReadOnly", p0]; }
        internal static string VariableMustBeSet { get { return SR.Instance["VariableMustBeSet"]; } }
        internal static string VariableNameNotAnIdentifier(object p0) { return SR.Instance["VariableNameNotAnIdentifier", p0]; }
        internal static string VariableNotOpen(object p0, object p1) { return SR.Instance["VariableNotOpen", p0, p1]; }
        internal static string VariableNotVisible(object p0) { return SR.Instance["VariableNotVisible", p0]; }
        internal static string VariableOnlyAccessibleAtScopeOfDeclaration(object p0, object p1) { return SR.Instance["VariableOnlyAccessibleAtScopeOfDeclaration", p0, p1]; }
        internal static string VariableOrArgumentAdditionToReferencedEnvironmentNoDUSupported { get { return SR.Instance["VariableOrArgumentAdditionToReferencedEnvironmentNoDUSupported"]; } }
        internal static string VariableOrArgumentDoesNotExist(object p0) { return SR.Instance["VariableOrArgumentDoesNotExist", p0]; }
        internal static string VariableShouldBeOpen(object p0) { return SR.Instance["VariableShouldBeOpen", p0]; }
        internal static string VariableTypeInvalid(object p0, object p1, object p2) { return SR.Instance["VariableTypeInvalid", p0, p1, p2]; }
        internal static string VariableTypeNotMatchLocationType(object p0, object p1) { return SR.Instance["VariableTypeNotMatchLocationType", p0, p1]; }
        internal static string VBExpressionTamperedSinceLastCompiled(object p0) { return SR.Instance["VBExpressionTamperedSinceLastCompiled", p0]; }
        internal static string WaitAlreadyInProgress { get { return SR.Instance["WaitAlreadyInProgress"]; } }
        internal static string WaitForEventsTimedOut(object p0) { return SR.Instance["WaitForEventsTimedOut", p0]; }
        internal static string WDCDisposed { get { return SR.Instance["WDCDisposed"]; } }
        internal static string WhileRequiresCondition(object p0) { return SR.Instance["WhileRequiresCondition", p0]; }
        internal static string WorkflowAbortedReason(object p0, object p1) { return SR.Instance["WorkflowAbortedReason", p0, p1]; }
        internal static string WorkflowApplicationAborted(object p0) { return SR.Instance["WorkflowApplicationAborted", p0]; }
        internal static string WorkflowApplicationAlreadyHasId { get { return SR.Instance["WorkflowApplicationAlreadyHasId"]; } }
        internal static string WorkflowApplicationCompleted(object p0) { return SR.Instance["WorkflowApplicationCompleted", p0]; }
        internal static string WorkflowApplicationInstanceAbandoned { get { return SR.Instance["WorkflowApplicationInstanceAbandoned"]; } }
        internal static string WorkflowApplicationInstanceLoaded { get { return SR.Instance["WorkflowApplicationInstanceLoaded"]; } }
        internal static string WorkflowApplicationTerminated(object p0) { return SR.Instance["WorkflowApplicationTerminated", p0]; }
        internal static string WorkflowApplicationUnloaded(object p0) { return SR.Instance["WorkflowApplicationUnloaded", p0]; }
        internal static string WorkflowIdentityNullHostId(object p0) { return SR.Instance["WorkflowIdentityNullHostId", p0]; }
        internal static string WorkflowIdentityNullStateId(object p0) { return SR.Instance["WorkflowIdentityNullStateId", p0]; }
        internal static string WorkflowIdentityStateIdHostIdMismatch(object p0, object p1) { return SR.Instance["WorkflowIdentityStateIdHostIdMismatch", p0, p1]; }
        internal static string WorkflowInstanceAborted(object p0) { return SR.Instance["WorkflowInstanceAborted", p0]; }
        internal static string WorkflowInstanceIsReadOnly(object p0) { return SR.Instance["WorkflowInstanceIsReadOnly", p0]; }
        internal static string WorkflowInstanceNotFoundInStore(object p0) { return SR.Instance["WorkflowInstanceNotFoundInStore", p0]; }
        internal static string WorkflowInstanceUnlocked(object p0) { return SR.Instance["WorkflowInstanceUnlocked", p0]; }
        internal static string WorkflowTerminatedExceptionDefaultMessage { get { return SR.Instance["WorkflowTerminatedExceptionDefaultMessage"]; } }
        internal static string WorkItemAbortedInstance { get { return SR.Instance["WorkItemAbortedInstance"]; } }
        internal static string WriteAccessToReadOnlyPublicReference(object p0) { return SR.Instance["WriteAccessToReadOnlyPublicReference", p0]; }
        internal static string WriteonlyPropertyCannotBeRead(object p0, object p1) { return SR.Instance["WriteonlyPropertyCannotBeRead", p0, p1]; }
        internal static string WrongArgumentType(object p0, object p1) { return SR.Instance["WrongArgumentType", p0, p1]; }
        internal static string WrongCacheMetadataForCodeActivity { get { return SR.Instance["WrongCacheMetadataForCodeActivity"]; } }
        internal static string WrongCacheMetadataForNativeActivity { get { return SR.Instance["WrongCacheMetadataForNativeActivity"]; } }
        internal static string WrongEnvironmentCount(object p0, object p1, object p2, object p3, object p4, object p5, object p6, object p7, object p8) { return SR.Instance["WrongEnvironmentCount", p0, p1, p2, p3, p4, p5, p6, p7, p8]; }
        internal static string WrongMemberCount(object p0, object p1, object p2) { return SR.Instance["WrongMemberCount", p0, p1, p2]; }
        internal static string WrongNumberOfArgumentsForActivityDelegate { get { return SR.Instance["WrongNumberOfArgumentsForActivityDelegate"]; } }
        internal static string WrongOnCreateDynamicUpdateMapForNativeActivity { get { return SR.Instance["WrongOnCreateDynamicUpdateMapForNativeActivity"]; } }
        internal static string WrongOriginalEnvironmentCount(object p0, object p1, object p2, object p3, object p4, object p5, object p6) { return SR.Instance["WrongOriginalEnvironmentCount", p0, p1, p2, p3, p4, p5, p6]; }
        internal static string XamlBuildTaskResourceNotFound(object p0) { return SR.Instance["XamlBuildTaskResourceNotFound", p0]; }
        internal static string XamlElementExpected { get { return SR.Instance["XamlElementExpected"]; } }
        internal static string XamlElementExpectedAt(object p0, object p1) { return SR.Instance["XamlElementExpectedAt", p0, p1]; }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Resources;
using System.Reflection;

namespace TestCases.Runtime.Common
{
    public static class ExceptionStrings
    {
        private static readonly ResourceManager s_activitiesResourceManager;

        static ExceptionStrings()
        {
            s_activitiesResourceManager = new ResourceManager("CoreWf.Strings.SR", typeof(CoreWf.Activity).GetTypeInfo().Assembly);
        }

        public static string ActivityAlreadyOpenInOtherWorkflow { get { return s_activitiesResourceManager.GetString("ActivityAlreadyOpenInOtherWorkflow"); } }
        public static string ActivityCannotBeReferencedWithoutTarget { get { return s_activitiesResourceManager.GetString("ActivityCannotBeReferencedWithoutTarget"); } }
        public static string ActivityCannotReferenceItself { get { return s_activitiesResourceManager.GetString("ActivityCannotReferenceItself"); } }
        public static string ActivityDefinitionCannotBeShared { get { return s_activitiesResourceManager.GetString("ActivityDefinitionCannotBeShared"); } }
        public static string ActivityDelegateAlreadyOpened { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeReferencedWithoutTarget"); } }
        public static string ActivityDelegateCannotBeReferenced { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeReferenced"); } }
        public static string ActivityDelegateCannotBeReferencedNoHandler { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeReferencedNoHandler"); } }
        public static string ActivityDelegateHandlersMustBeDeclarations { get { return s_activitiesResourceManager.GetString("ActivityDelegateHandlersMustBeDeclarations"); } }
        public static string ActivityDelegateOwnerMissing { get { return s_activitiesResourceManager.GetString("ActivityDelegateOwnerMissing"); } }
        public static string ActivityInstanceFixupFailed { get { return s_activitiesResourceManager.GetString("ActivityInstanceFixupFailed"); } }
        public static string ActivityNotPartOfThisTree { get { return s_activitiesResourceManager.GetString("ActivityNotPartOfThisTree"); } }
        public static string ActivityRuntimeAlreadyAbandoned { get { return s_activitiesResourceManager.GetString("WorkflowInstanceAborted"); } }
        public static string ActivityTypeMismatch { get { return s_activitiesResourceManager.GetString("ActivityTypeMismatch"); } }
        public static string AddValidationErrorMustBeCalledFromConstraint { get { return s_activitiesResourceManager.GetString("AddValidationErrorMustBeCalledFromConstraint"); } }
        public static string AECDisposed { get { return s_activitiesResourceManager.GetString("AECDisposed"); } }
        public static string ArgumentDirectionMismatch { get { return s_activitiesResourceManager.GetString("ArgumentDirectionMismatch"); } }
        public static string ArgumentIsAddedMoreThanOnce { get { return s_activitiesResourceManager.GetString("ArgumentIsAddedMoreThanOnce"); } }

        public static string ArgumentNotInTree { get { return s_activitiesResourceManager.GetString("ArgumentNotInTree"); } }

        public static string ArgumentNullExceptionHelper = "\r\nParameter name: {0}";
        public static string ArgumentOutOfRangeExceptionHelper = "\r\nParameter name: {0}\r\nActual value was {1}";
        public static string ArgumentTypeMismatch { get { return s_activitiesResourceManager.GetString("ArgumentTypeMismatch"); } }
        public static string ArgumentViolationsFound { get { return s_activitiesResourceManager.GetString("ArgumentViolationsFound"); } }
        public static string BeginExecuteMustNotReturnANullAsyncResult { get { return s_activitiesResourceManager.GetString("BeginExecuteMustNotReturnANullAsyncResult"); } }
        public static string BeginExecuteMustUseProvidedStateAsAsyncResultState { get { return s_activitiesResourceManager.GetString("BeginExecuteMustUseProvidedStateAsAsyncResultState"); } }
        public static string CanInduceIdleNotSpecified { get { return s_activitiesResourceManager.GetString("CanInduceIdleNotSpecified"); } }
        public static string CanInduceIdleActivityInArgumentExpression { get { return s_activitiesResourceManager.GetString("CanInduceIdleActivityInArgumentExpression"); } }
        public static string CannotDereferenceNull { get { return s_activitiesResourceManager.GetString("CannotDereferenceNull"); } }
        public static string CannotPerformOperationFromHandlerThread { get { return s_activitiesResourceManager.GetString("CannotPerformOperationFromHandlerThread"); } }
        public static string CannotPersistInsideIsolation { get { return s_activitiesResourceManager.GetString("CannotPersistInsideIsolation"); } }
        public static string CannotPersistInsideNoPersist { get { return s_activitiesResourceManager.GetString("CannotPersistInsideNoPersist"); } }
        public static string CannotPropagateExceptionWhileCanceling { get { return s_activitiesResourceManager.GetString("CannotPropagateExceptionWhileCanceling"); } }
        public static string CannotSetRuntimeTransactionInNoPersist { get { return s_activitiesResourceManager.GetString("CannotSetRuntimeTransactionInNoPersist"); } }
        public static string CannotSetupIsolationInsideIsolation { get { return s_activitiesResourceManager.GetString("CannotSetupIsolationInsideIsolation"); } }
        public static string CannotSetupIsolationInsideNoPersist { get { return s_activitiesResourceManager.GetString("CannotSetupIsolationInsideNoPersist"); } }
        public static string CannotSetValueToLocation { get { return s_activitiesResourceManager.GetString("CannotSetValueToLocation"); } }
        public static string CannotWaitForIdleSynchronously { get { return s_activitiesResourceManager.GetString("CannotWaitForIdleSynchronously"); } }
        public static string CanOnlyGetOwnedArguments { get { return s_activitiesResourceManager.GetString("CanOnlyGetOwnedArguments"); } }
        public static string CanOnlyScheduleDirectChildren { get { return s_activitiesResourceManager.GetString("CanOnlyScheduleDirectChildren"); } }
        public static string ConstVariableCannotBeSet { get { return s_activitiesResourceManager.GetString("ConstVariableCannotBeSet"); } }
        public static string CopyToNotEnoughSpaceInArray { get { return s_activitiesResourceManager.GetString("CopyToNotEnoughSpaceInArray"); } }
        public static string DefaultCancelationRequiresCancelHasBeenRequested { get { return s_activitiesResourceManager.GetString("DefaultCancelationRequiresCancelHasBeenRequested"); } }
        public static string DelegateInArgumentTypeMismatch { get { return s_activitiesResourceManager.GetString("DelegateInArgumentTypeMismatch"); } }
        public static string DelegateOutArgumentTypeMismatch { get { return s_activitiesResourceManager.GetString("DelegateOutArgumentTypeMismatch"); } }
        public static string DuplicateEvaluationOrderValues { get { return s_activitiesResourceManager.GetString("DuplicateEvaluationOrderValues"); } }
        public static string WorkflowApplicationAborted { get { return s_activitiesResourceManager.GetString("WorkflowApplicationAborted"); } }
        public static string WorkflowInstanceAborted { get { return s_activitiesResourceManager.GetString("WorkflowInstanceAborted"); } }
        public static string WorkflowInstanceCompleted { get { return s_activitiesResourceManager.GetString("WorkflowApplicationCompleted"); } }
        public static string WorkflowInstanceIsReadOnly { get { return s_activitiesResourceManager.GetString("WorkflowInstanceIsReadOnly"); } }
        public static string WorkflowInstanceTerminated { get { return s_activitiesResourceManager.GetString("WorkflowApplicationTerminated"); } }
        public static string WorkflowInstanceUnloaded { get { return s_activitiesResourceManager.GetString("WorkflowApplicationUnloaded"); } }



        public static string Format(string exceptionMessage, params object[] args)
        {
            if (args.Length > 0)
            {
                return string.Format(exceptionMessage, args);
            }

            return exceptionMessage;
        }
    }
}

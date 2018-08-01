// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Resources;

namespace Test.Common.TestObjects.Utilities
{
    /// <remarks>
    /// This class returned strings from resources in CoreWf, System.Workflow.Runtime, and mscorlib.
    /// mscorlib does not exist in .NET Core and it appears that the same resource strings are not available in the new code.
    /// System.Workflow.Runtime is for WF3 activities, so it is not included.
    /// </remarks>
    public static class ErrorStrings
    {
        private static readonly ResourceManager s_activitiesResourceManager;

        static ErrorStrings()
        {
            s_activitiesResourceManager = new ResourceManager("CoreWf.Resources.SR", typeof(CoreWf.Activity).GetTypeInfo().Assembly);
        }

        public static string WhileRequiresCondition { get { return s_activitiesResourceManager.GetString("WhileRequiresCondition"); } }
        public static string ActivityLockedForRuntime { get { return s_activitiesResourceManager.GetString("ActivityLockedForRuntime"); } }
        public static string ActivityDelegateCannotBeModifiedAfterOpen { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeModifiedAfterOpen"); } }
        public static string ForEachRequiresNonNullValues { get { return s_activitiesResourceManager.GetString("ForEachRequiresNonNullValues"); } }
        public static string FlowDecisionRequiresCondition { get { return s_activitiesResourceManager.GetString("FlowDecisionRequiresCondition"); } }
        public static string SymbolNamesMustBeUnique { get { return s_activitiesResourceManager.GetString("SymbolNamesMustBeUnique"); } }
        public static string FlowNodeCannotBeShared { get { return s_activitiesResourceManager.GetString("FlowNodeCannotBeShared"); } }
        public static string FlowSwitchRequiresExpression { get { return s_activitiesResourceManager.GetString("FlowSwitchRequiresExpression"); } }
        public static string PublicMethodWithMatchingParameterDoesNotExist { get { return s_activitiesResourceManager.GetString("PublicMethodWithMatchingParameterDoesNotExist"); } }
        public static string PickBranchRequiresTrigger { get { return s_activitiesResourceManager.GetString("PickBranchRequiresTrigger"); } }
        public static string CollectionActivityRequiresCollection { get { return s_activitiesResourceManager.GetString("CollectionActivityRequiresCollection"); } }
        public static string CompilerErrorSpecificExpression { get { return s_activitiesResourceManager.GetString("CompilerErrorSpecificExpression"); } }
        public static string UnsupportedExpressionType { get { return s_activitiesResourceManager.GetString("UnsupportedExpressionType"); } }
        public static string ExpressionRequiredForConversion { get { return s_activitiesResourceManager.GetString("ExpressionRequiredForConversion"); } }
        public static string UnsupportedReferenceExpressionType { get { return s_activitiesResourceManager.GetString("UnsupportedReferenceExpressionType"); } }
        public static string RequiredArgumentValueNotSupplied { get { return s_activitiesResourceManager.GetString("RequiredArgumentValueNotSupplied"); } }
        public static string OneOfTwoPropertiesMustBeSet { get { return s_activitiesResourceManager.GetString("OneOfTwoPropertiesMustBeSet"); } }
        public static string MemberNotSupportedByActivityXamlServices { get { return s_activitiesResourceManager.GetString("MemberNotSupportedByActivityXamlServices"); } }
        public static string FlowchartMissingStartNode { get { return s_activitiesResourceManager.GetString("FlowchartMissingStartNode"); } }
        public static string InvalidPropertyType { get { return s_activitiesResourceManager.GetString("InvalidPropertyType"); } }
        public static string ActivityPropertyRequiresName { get { return s_activitiesResourceManager.GetString("ActivityPropertyRequiresName"); } }
        public static string ActivityPropertyRequiresType { get { return s_activitiesResourceManager.GetString("ActivityPropertyRequiresType"); } }
        public static string TargetTypeCannotBeEnum { get { return s_activitiesResourceManager.GetString("TargetTypeCannotBeEnum"); } }
        public static string BinaryExpressionActivityRequiresArgument { get { return s_activitiesResourceManager.GetString("BinaryExpressionActivityRequiresArgument"); } }
        public static string MemberNotFound { get { return s_activitiesResourceManager.GetString("MemberNotFound"); } }
        public static string ConstructorInfoNotFound { get { return s_activitiesResourceManager.GetString("ConstructorInfoNotFound"); } }
        public static string NewArrayBoundsRequiresIntegralArguments { get { return s_activitiesResourceManager.GetString("NewArrayBoundsRequiresIntegralArguments"); } }
        public static string FieldValueActivityRequiresFieldName { get { return s_activitiesResourceManager.GetString("FieldValueActivityRequiresFieldName"); } }
        public static string TargetTypeIsValueType { get { return s_activitiesResourceManager.GetString("TargetTypeIsValueType"); } }
        public static string RethrowNotInATryCatch { get { return s_activitiesResourceManager.GetString("RethrowNotInATryCatch"); } }
        public static string MemberCannotBeNull { get { return s_activitiesResourceManager.GetString("MemberCannotBeNull"); } }
        public static string ActivityCannotBeReferencedWithoutTarget { get { return s_activitiesResourceManager.GetString("ActivityCannotBeReferencedWithoutTarget"); } }
        public static string ActivityCannotReferenceItself { get { return s_activitiesResourceManager.GetString("ActivityCannotReferenceItself"); } }
        public static string VariableNotVisible { get { return s_activitiesResourceManager.GetString("VariableNotVisible"); } }
        public static string ActivityPropertyMustBeSet { get { return s_activitiesResourceManager.GetString("ActivityPropertyMustBeSet"); } }
        public static string IndicesAreNeeded { get { return s_activitiesResourceManager.GetString("IndicesAreNeeded"); } }
        public static string SpecialMethodNotFound { get { return s_activitiesResourceManager.GetString("SpecialMethodNotFound"); } }
        public static string FaultContextNotFound { get { return s_activitiesResourceManager.GetString("FaultContextNotFound"); } }
        public static string TypeMustbeValueType { get { return s_activitiesResourceManager.GetString("TypeMustbeValueType"); } }
        public static string WriteonlyPropertyCannotBeRead { get { return s_activitiesResourceManager.GetString("WriteonlyPropertyCannotBeRead"); } }
        public static string MemberIsReadOnly { get { return s_activitiesResourceManager.GetString("MemberIsReadOnly"); } }
        public static string DynamicActivityDuplicatePropertyDetected { get { return s_activitiesResourceManager.GetString("DynamicActivityDuplicatePropertyDetected"); } }
        public static string IncompatibleTypeForMultidimensionalArrayItemReference { get { return s_activitiesResourceManager.GetString("IncompatibleTypeForMultidimensionalArrayItemReference"); } }
        public static string RootArgumentViolationsFoundNoInputs { get { return s_activitiesResourceManager.GetString("RootArgumentViolationsFoundNoInputs"); } }
        public static string VariableNotOpen { get { return s_activitiesResourceManager.GetString("VariableNotOpen"); } }
        public static string VariableShouldBeOpen { get { return s_activitiesResourceManager.GetString("VariableShouldBeOpen"); } }
        public static string InvalidWorkflowException { get { return s_activitiesResourceManager.GetString("CompilerErrorSpecificExpression"); } }
        public static string NewArrayRequiresArrayTypeAsResultType { get { return s_activitiesResourceManager.GetString("NewArrayRequiresArrayTypeAsResultType"); } }
        public static string ArgumentRequired { get { return s_activitiesResourceManager.GetString("ArgumentRequired"); } }
        public static string VariableAlreadyInUseOnActivity { get { return s_activitiesResourceManager.GetString("VariableAlreadyInUseOnActivity"); } }
        public static string RethrowMustBeAPublicChild { get { return s_activitiesResourceManager.GetString("RethrowMustBeAPublicChild"); } }
        public static string ValidationErrorPrefixForHiddenActivity { get { return s_activitiesResourceManager.GetString("ValidationErrorPrefixForHiddenActivity"); } }
        public static string TargetTypeAndTargetObjectAreMutuallyExclusive { get { return s_activitiesResourceManager.GetString("TargetTypeAndTargetObjectAreMutuallyExclusive"); } }
        public static string ArgumentMustbePropertyofWorkflowElement { get { return s_activitiesResourceManager.GetString("ArgumentMustbePropertyofWorkflowElement"); } }
        public static string SavingActivityToXamlNotSupported { get { return s_activitiesResourceManager.GetString("SavingActivityToXamlNotSupported"); } }
        public static string RuntimeArgumentBindingInvalid { get { return s_activitiesResourceManager.GetString("RuntimeArgumentBindingInvalid"); } }
        public static string ActivityDelegateCannotBeReferencedWithoutTargetNoHandler { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeReferencedWithoutTargetNoHandler"); } }
        public static string ActivityDelegateCannotBeReferencedNoHandler { get { return s_activitiesResourceManager.GetString("ActivityDelegateCannotBeReferencedNoHandler"); } }
    }
}

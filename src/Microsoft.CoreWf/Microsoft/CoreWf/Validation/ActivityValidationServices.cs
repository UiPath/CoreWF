// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CoreWf.Validation
{
    public static class ActivityValidationServices
    {
        internal static readonly ReadOnlyCollection<Activity> EmptyChildren = new ReadOnlyCollection<Activity>(new Activity[0]);
        private static ValidationSettings s_defaultSettings = new ValidationSettings();
        internal static ReadOnlyCollection<ValidationError> EmptyValidationErrors = new ReadOnlyCollection<ValidationError>(new List<ValidationError>(0));

        public static ValidationResults Validate(Activity toValidate)
        {
            return Validate(toValidate, s_defaultSettings);
        }

        public static ValidationResults Validate(Activity toValidate, ValidationSettings settings)
        {
            if (toValidate == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("toValidate");
            }

            if (settings == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("settings");
            }

            if (toValidate.HasBeenAssociatedWithAnInstance)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.RootActivityAlreadyAssociatedWithInstance(toValidate.DisplayName)));
            }

            if (settings.PrepareForRuntime && (settings.SingleLevel || settings.SkipValidatingRootConfiguration || settings.OnlyUseAdditionalConstraints))
            {
                throw CoreWf.Internals.FxTrace.Exception.Argument("settings", SR.InvalidPrepareForRuntimeValidationSettings);
            }

            InternalActivityValidationServices validator = new InternalActivityValidationServices(settings, toValidate);
            return validator.InternalValidate();
        }

        public static Activity Resolve(Activity root, string id)
        {
            return WorkflowInspectionServices.Resolve(root, id);
        }

        internal static void ThrowIfViolationsExist(IList<ValidationError> validationErrors, ExceptionReason reason = ExceptionReason.InvalidTree)
        {
            Exception exception = CreateExceptionFromValidationErrors(validationErrors, reason);

            if (exception != null)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(exception);
            }
        }

        private static Exception CreateExceptionFromValidationErrors(IList<ValidationError> validationErrors, ExceptionReason reason)
        {
            if (validationErrors != null && validationErrors.Count > 0)
            {
                string exceptionString = GenerateExceptionString(validationErrors, reason);

                if (exceptionString != null)
                {
                    return new InvalidWorkflowException(exceptionString);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        internal static List<Activity> GetChildren(ActivityUtilities.ChildActivity root, ActivityUtilities.ActivityCallStack parentChain, ProcessActivityTreeOptions options)
        {
            ActivityUtilities.FinishCachingSubtree(root, parentChain, options);

            List<Activity> listOfChildren = new List<Activity>();

            foreach (Activity activity in WorkflowInspectionServices.GetActivities(root.Activity))
            {
                listOfChildren.Add(activity);
            }

            int toProcessIndex = 0;

            while (toProcessIndex < listOfChildren.Count)
            {
                foreach (Activity activity in WorkflowInspectionServices.GetActivities(listOfChildren[toProcessIndex]))
                {
                    listOfChildren.Add(activity);
                }

                toProcessIndex++;
            }

            return listOfChildren;
        }

        internal static void ValidateRootInputs(Activity rootActivity, IDictionary<string, object> inputs)
        {
            IList<ValidationError> validationErrors = null;
            ValidationHelper.ValidateArguments(rootActivity, rootActivity.EquivalenceInfo, rootActivity.OverloadGroups, rootActivity.RequiredArgumentsNotInOverloadGroups, inputs, ref validationErrors);

            // Validate if there are any extra arguments passed in the input dictionary     
            if (inputs != null)
            {
                List<string> unusedArguments = null;
                IEnumerable<RuntimeArgument> arguments = rootActivity.RuntimeArguments.Where((a) => ArgumentDirectionHelper.IsIn(a.Direction));

                foreach (string key in inputs.Keys)
                {
                    bool found = false;
                    foreach (RuntimeArgument argument in arguments)
                    {
                        if (argument.Name == key)
                        {
                            found = true;

                            // Validate if the input argument type matches the expected argument type.
                            object inputArgumentValue = null;
                            if (inputs.TryGetValue(key, out inputArgumentValue))
                            {
                                if (!TypeHelper.AreTypesCompatible(inputArgumentValue, argument.Type))
                                {
                                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.InputParametersTypeMismatch(argument.Type, argument.Name), rootActivity));
                                }
                            }
                            // The ValidateArguments will validate Required in-args and hence not duplicating that validation if the key is not found. 

                            break;
                        }
                    }

                    if (!found)
                    {
                        if (unusedArguments == null)
                        {
                            unusedArguments = new List<string>();
                        }
                        unusedArguments.Add(key);
                    }
                }
                if (unusedArguments != null)
                {
                    ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.UnusedInputArguments(unusedArguments.AsCommaSeparatedValues()), rootActivity));
                }
            }

            if (validationErrors != null && validationErrors.Count > 0)
            {
                string parameterName = "rootArgumentValues";
                ExceptionReason reason = ExceptionReason.InvalidNonNullInputs;

                if (inputs == null)
                {
                    parameterName = "program";
                    reason = ExceptionReason.InvalidNullInputs;
                }

                string exceptionString = GenerateExceptionString(validationErrors, reason);

                if (exceptionString != null)
                {
                    throw CoreWf.Internals.FxTrace.Exception.Argument(parameterName, exceptionString);
                }
            }
        }

        internal static void ValidateArguments(Activity activity, bool isRoot, ref IList<ValidationError> validationErrors)
        {
            Fx.Assert(activity != null, "Activity to validate should not be null.");

            Dictionary<string, List<RuntimeArgument>> overloadGroups;
            List<RuntimeArgument> requiredArgumentsNotInOverloadGroups;
            ValidationHelper.OverloadGroupEquivalenceInfo equivalenceInfo;
            if (ValidationHelper.GatherAndValidateOverloads(activity, out overloadGroups, out requiredArgumentsNotInOverloadGroups, out equivalenceInfo, ref validationErrors))
            {
                // If we're not the root and the overload groups are valid
                // then we validate the arguments
                if (!isRoot)
                {
                    ValidationHelper.ValidateArguments(activity, equivalenceInfo, overloadGroups, requiredArgumentsNotInOverloadGroups, null, ref validationErrors);
                }
            }

            // If we are the root, regardless of whether the groups are
            // valid or not, we cache the group information
            if (isRoot)
            {
                activity.OverloadGroups = overloadGroups;
                activity.RequiredArgumentsNotInOverloadGroups = requiredArgumentsNotInOverloadGroups;
                activity.EquivalenceInfo = equivalenceInfo;
            }
        }

        private static string GenerateExceptionString(IList<ValidationError> validationErrors, ExceptionReason reason)
        {
            // 4096 is an arbitrary constant.  Currently clipped by character count (not bytes).
            const int maxExceptionStringSize = 4096;

            StringBuilder exceptionMessageBuilder = null;

            for (int i = 0; i < validationErrors.Count; i++)
            {
                ValidationError validationError = validationErrors[i];

                if (!validationError.IsWarning)
                {
                    // create the common exception string
                    if (exceptionMessageBuilder == null)
                    {
                        exceptionMessageBuilder = new StringBuilder();

                        switch (reason)
                        {
                            case ExceptionReason.InvalidTree:
                                exceptionMessageBuilder.Append(SR.ErrorsEncounteredWhileProcessingTree);
                                break;
                            case ExceptionReason.InvalidNonNullInputs:
                                exceptionMessageBuilder.Append(SR.RootArgumentViolationsFound);
                                break;
                            case ExceptionReason.InvalidNullInputs:
                                exceptionMessageBuilder.Append(SR.RootArgumentViolationsFoundNoInputs);
                                break;
                        }
                    }

                    string activityName = null;

                    if (validationError.Source != null)
                    {
                        activityName = validationError.Source.DisplayName;
                    }
                    else
                    {
                        activityName = "<UnknownActivity>";
                    }

                    exceptionMessageBuilder.AppendLine();
                    exceptionMessageBuilder.Append(string.Format(/*SR.Culture,*/ "'{0}': {1}", activityName, validationError.Message));

                    if (exceptionMessageBuilder.Length > maxExceptionStringSize)
                    {
                        break;
                    }
                }
            }

            string exceptionString = null;

            if (exceptionMessageBuilder != null)
            {
                exceptionString = exceptionMessageBuilder.ToString();

                if (exceptionString.Length > maxExceptionStringSize)
                {
                    string snipNotification = SR.TooManyViolationsForExceptionMessage;

                    exceptionString = exceptionString.Substring(0, maxExceptionStringSize - snipNotification.Length);
                    exceptionString += snipNotification;
                }
            }

            return exceptionString;
        }

        static internal string GenerateValidationErrorPrefix(Activity toValidate, ActivityUtilities.ActivityCallStack parentChain, ProcessActivityTreeOptions options, out Activity source)
        {
            bool parentVisible = true;
            string prefix = "";
            source = toValidate;

            // Processing for implementation of activity  
            // during build time 
            if (options.SkipRootConfigurationValidation)
            {
                // Check if the activity is a implementation child
                if (toValidate.MemberOf.Parent != null)
                {
                    // Check if activity is an immediate implementation child
                    // of x:class activity. This means that the activity is 
                    // being designed and hence we do not want to add the 
                    // prefix at build time
                    if (toValidate.MemberOf.Parent.Parent == null)
                    {
                        prefix = "";
                        source = toValidate;
                    }
                    else
                    {
                        // This means the activity is a child of immediate implementation child
                        // of x:class activity which means the activity is not visible.
                        // The source points to the first visible parent activity in the 
                        // parent chain.
                        while (source.MemberOf.Parent.Parent != null)
                        {
                            source = source.Parent;
                        }
                        prefix = SR.ValidationErrorPrefixForHiddenActivity(source);
                    }
                    return prefix;
                }
            }

            // Find out if any of the parents of the activity are not publicly visible
            for (int i = 0; i < parentChain.Count; i++)
            {
                if (parentChain[i].Activity.MemberOf.Parent != null)
                {
                    parentVisible = false;
                    break;
                }
            }

            // Figure out the source of validation error:
            //    - For hidden activity - source will be closest visible public parent
            //    - For visible activity - source will be the activity itself
            // In current design an activity is visible only if it is in the root id space.
            // In future, if we provide a knob for the user to specify the
            // id spaces that are visible, then this check needs to be changed
            // to iterate over the parentChain and find the closest parent activity that
            // is in the visible id spaces.
            while (source.MemberOf.Parent != null)
            {
                source = source.Parent;
            }

            if (toValidate.MemberOf.Parent != null)
            {
                // Activity itself is hidden 
                prefix = SR.ValidationErrorPrefixForHiddenActivity(source);
            }
            else
            {
                if (!parentVisible)
                {
                    // Activity itself is public but has a private parent
                    prefix = SR.ValidationErrorPrefixForPublicActivityWithHiddenParent(source.Parent, source);
                }
            }
            return prefix;
        }

        internal static void RunConstraints(ActivityUtilities.ChildActivity childActivity, ActivityUtilities.ActivityCallStack parentChain, IList<Constraint> constraints, ProcessActivityTreeOptions options, bool suppressGetChildrenViolations, ref IList<ValidationError> validationErrors)
        {
            if (constraints != null)
            {
                Activity toValidate = childActivity.Activity;

                LocationReferenceEnvironment environment = toValidate.GetParentEnvironment();

                Dictionary<string, object> inputDictionary = new Dictionary<string, object>(2);

                for (int constraintIndex = 0; constraintIndex < constraints.Count; constraintIndex++)
                {
                    Constraint constraint = constraints[constraintIndex];

                    // there may be null entries here
                    if (constraint == null)
                    {
                        continue;
                    }

                    inputDictionary[Constraint.ToValidateArgumentName] = toValidate;
                    ValidationContext validationContext = new ValidationContext(childActivity, parentChain, options, environment);
                    inputDictionary[Constraint.ToValidateContextArgumentName] = validationContext;
                    IDictionary<string, object> results = null;

                    try
                    {
                        results = WorkflowInvoker.Invoke(constraint, inputDictionary);
                    }
                    catch (Exception e)
                    {
                        if (Fx.IsFatal(e))
                        {
                            throw;
                        }

                        ValidationError constraintExceptionValidationError = new ValidationError(SR.InternalConstraintException(constraint.DisplayName, toValidate.GetType().FullName, toValidate.DisplayName, e.ToString()), false)
                        {
                            Source = toValidate,
                            Id = toValidate.Id
                        };

                        ActivityUtilities.Add(ref validationErrors, constraintExceptionValidationError);
                    }

                    if (results != null)
                    {
                        object resultValidationErrors;
                        if (results.TryGetValue(Constraint.ValidationErrorListArgumentName, out resultValidationErrors))
                        {
                            IList<ValidationError> validationErrorList = (IList<ValidationError>)resultValidationErrors;

                            if (validationErrorList.Count > 0)
                            {
                                if (validationErrors == null)
                                {
                                    validationErrors = new List<ValidationError>();
                                }

                                Activity source;
                                string prefix = ActivityValidationServices.GenerateValidationErrorPrefix(childActivity.Activity, parentChain, options, out source);

                                for (int validationErrorIndex = 0; validationErrorIndex < validationErrorList.Count; validationErrorIndex++)
                                {
                                    ValidationError validationError = validationErrorList[validationErrorIndex];

                                    validationError.Source = source;
                                    validationError.Id = source.Id;
                                    if (!string.IsNullOrEmpty(prefix))
                                    {
                                        validationError.Message = prefix + validationError.Message;
                                    }
                                    validationErrors.Add(validationError);
                                }
                            }
                        }
                    }

                    if (!suppressGetChildrenViolations)
                    {
                        validationContext.AddGetChildrenErrors(ref validationErrors);
                    }
                }
            }
        }

        internal static bool HasErrors(IList<ValidationError> validationErrors)
        {
            if (validationErrors != null && validationErrors.Count > 0)
            {
                for (int i = 0; i < validationErrors.Count; i++)
                {
                    if (!validationErrors[i].IsWarning)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class InternalActivityValidationServices
        {
            private ValidationSettings _settings;
            private Activity _rootToValidate;
            private IList<ValidationError> _errors;
            private ProcessActivityTreeOptions _options;
            private Activity _expressionRoot;
            private LocationReferenceEnvironment _environment;

            internal InternalActivityValidationServices(ValidationSettings settings, Activity toValidate)
            {
                _settings = settings;
                _rootToValidate = toValidate;
                _environment = settings.Environment;
            }

            internal ValidationResults InternalValidate()
            {
                _options = ProcessActivityTreeOptions.GetValidationOptions(_settings);

                if (_settings.OnlyUseAdditionalConstraints)
                {
                    // We don't want the errors from CacheMetadata so we send those to a "dummy" list.
                    IList<ValidationError> suppressedErrors = null;
                    ActivityUtilities.CacheRootMetadata(_rootToValidate, _environment, _options, new ActivityUtilities.ProcessActivityCallback(ValidateElement), ref suppressedErrors);
                }
                else
                {
                    // We want to add the CacheMetadata errors to our errors collection
                    ActivityUtilities.CacheRootMetadata(_rootToValidate, _environment, _options, new ActivityUtilities.ProcessActivityCallback(ValidateElement), ref _errors);
                }

                return new ValidationResults(_errors);
            }

            private void ValidateElement(ActivityUtilities.ChildActivity childActivity, ActivityUtilities.ActivityCallStack parentChain)
            {
                Activity toValidate = childActivity.Activity;

                if (!_settings.SingleLevel || object.ReferenceEquals(toValidate, _rootToValidate))
                {
                    // 0. Open time violations are captured by the CacheMetadata walk.

                    // 1. Argument validations are done by the CacheMetadata walk.

                    // 2. Build constraints are done by the CacheMetadata walk.

                    // 3. Then do policy constraints
                    if (_settings.HasAdditionalConstraints && childActivity.CanBeExecuted && parentChain.WillExecute)
                    {
                        bool suppressGetChildrenViolations = _settings.OnlyUseAdditionalConstraints || _settings.SingleLevel;

                        Type currentType = toValidate.GetType();

                        while (currentType != null)
                        {
                            IList<Constraint> policyConstraints;
                            if (_settings.AdditionalConstraints.TryGetValue(currentType, out policyConstraints))
                            {
                                RunConstraints(childActivity, parentChain, policyConstraints, _options, suppressGetChildrenViolations, ref _errors);
                            }

                            if (currentType.GetTypeInfo().IsGenericType)
                            {
                                Type genericDefinitionType = currentType.GetGenericTypeDefinition();
                                if (genericDefinitionType != null)
                                {
                                    IList<Constraint> genericTypePolicyConstraints;
                                    if (_settings.AdditionalConstraints.TryGetValue(genericDefinitionType, out genericTypePolicyConstraints))
                                    {
                                        RunConstraints(childActivity, parentChain, genericTypePolicyConstraints, _options, suppressGetChildrenViolations, ref _errors);
                                    }
                                }
                            }
                            currentType = currentType.GetTypeInfo().BaseType;
                        }
                    }

                    //4. Validate if the argument expression subtree contains an activity that can induce idle.
                    if (childActivity.Activity.IsExpressionRoot)
                    {
                        if (childActivity.Activity.HasNonEmptySubtree)
                        {
                            _expressionRoot = childActivity.Activity;
                            // Back-compat: In Dev10 we always used ProcessActivityTreeOptions.FullCachingOptions here, and ignored this.options.
                            // So we need to continue to do that, unless the new Dev11 flag SkipRootConfigurationValidation is passed.
                            ProcessActivityTreeOptions options = _options.SkipRootConfigurationValidation ? _options : ProcessActivityTreeOptions.FullCachingOptions;
                            ActivityUtilities.FinishCachingSubtree(childActivity, parentChain, options, ValidateExpressionSubtree);
                            _expressionRoot = null;
                        }
                        else if (childActivity.Activity.InternalCanInduceIdle)
                        {
                            Activity activity = childActivity.Activity;
                            RuntimeArgument runtimeArgument = GetBoundRuntimeArgument(activity);
                            ValidationError error = new ValidationError(SR.CanInduceIdleActivityInArgumentExpression(runtimeArgument.Name, activity.Parent.DisplayName, activity.DisplayName), true, runtimeArgument.Name, activity.Parent);
                            ActivityUtilities.Add(ref _errors, error);
                        }
                    }
                }
            }

            private void ValidateExpressionSubtree(ActivityUtilities.ChildActivity childActivity, ActivityUtilities.ActivityCallStack parentChain)
            {
                Fx.Assert(_expressionRoot != null, "This callback should be called activities in the expression subtree only.");

                if (childActivity.Activity.InternalCanInduceIdle)
                {
                    Activity activity = childActivity.Activity;
                    Activity expressionRoot = _expressionRoot;

                    RuntimeArgument runtimeArgument = GetBoundRuntimeArgument(expressionRoot);
                    ValidationError error = new ValidationError(SR.CanInduceIdleActivityInArgumentExpression(runtimeArgument.Name, expressionRoot.Parent.DisplayName, activity.DisplayName), true, runtimeArgument.Name, expressionRoot.Parent);
                    ActivityUtilities.Add(ref _errors, error);
                }
            }
        }

        // Iterate through all runtime arguments on the configured activity
        // and find the one that binds to expressionActivity.
        private static RuntimeArgument GetBoundRuntimeArgument(Activity expressionActivity)
        {
            Activity configuredActivity = expressionActivity.Parent;
            Fx.Assert(configuredActivity != null, "Configured activity should not be null.");

            RuntimeArgument boundRuntimeArgument = null;
            for (int i = 0; i < configuredActivity.RuntimeArguments.Count; i++)
            {
                boundRuntimeArgument = configuredActivity.RuntimeArguments[i];
                if (object.ReferenceEquals(boundRuntimeArgument.BoundArgument.Expression, expressionActivity))
                {
                    break;
                }
            }
            Fx.Assert(boundRuntimeArgument != null, "We should always be able to find the runtime argument!");
            return boundRuntimeArgument;
        }

        // This method checks for duplicate evaluation order entries in a collection that is 
        // sorted in ascendng order of evaluation order values.
        internal static void ValidateEvaluationOrder(IList<RuntimeArgument> runtimeArguments, Activity referenceActivity, ref IList<ValidationError> validationErrors)
        {
            for (int i = 0; i < runtimeArguments.Count - 1; i++)
            {
                RuntimeArgument argument = runtimeArguments[i];
                RuntimeArgument nextArgument = runtimeArguments[i + 1];
                if (argument.IsEvaluationOrderSpecified && nextArgument.IsEvaluationOrderSpecified)
                {
                    if (argument.BoundArgument.EvaluationOrder == nextArgument.BoundArgument.EvaluationOrder)
                    {
                        ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.DuplicateEvaluationOrderValues(referenceActivity.DisplayName, argument.BoundArgument.EvaluationOrder), false, argument.Name, referenceActivity));
                    }
                }
            }
        }

        internal enum ExceptionReason
        {
            InvalidTree,
            InvalidNullInputs,
            InvalidNonNullInputs,
        }
    }
}

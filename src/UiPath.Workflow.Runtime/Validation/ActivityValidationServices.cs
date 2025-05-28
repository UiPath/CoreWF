// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime;
using System.Text;

namespace System.Activities.Validation;

public static class ActivityValidationServices
{
    private static readonly ValidationSettings defaultSettings = new();

    internal static readonly ReadOnlyCollection<Activity> EmptyChildren = new(Array.Empty<Activity>());
    internal static ReadOnlyCollection<ValidationError> EmptyValidationErrors = new(new List<ValidationError>(0));

    public static ValidationResults Validate(Activity toValidate) => Validate(toValidate, defaultSettings);

    public static ValidationResults Validate(Activity toValidate, ValidationSettings settings)
    {
        if (toValidate == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(toValidate));
        }

        if (settings == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(settings));
        }

        if (toValidate.HasBeenAssociatedWithAnInstance)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RootActivityAlreadyAssociatedWithInstance(toValidate.DisplayName)));
        }

        if (settings.PrepareForRuntime && (settings.SingleLevel || settings.SkipValidatingRootConfiguration || settings.OnlyUseAdditionalConstraints))
        {
            throw FxTrace.Exception.Argument(nameof(settings), SR.InvalidPrepareForRuntimeValidationSettings);
        }

        InternalActivityValidationServices validator = new(settings, toValidate);
        return validator.InternalValidate();
    }

    public static Activity Resolve(Activity root, string id) => WorkflowInspectionServices.Resolve(root, id);

    internal static void ThrowIfViolationsExist(IList<ValidationError> validationErrors, ExceptionReason reason = ExceptionReason.InvalidTree)
    {
        Exception exception = CreateExceptionFromValidationErrors(validationErrors, reason);

        if (exception != null)
        {
            throw FxTrace.Exception.AsError(exception);
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
        }

        return null;
    }

    internal static List<Activity> GetChildren(ActivityUtilities.ChildActivity root, ActivityUtilities.ActivityCallStack parentChain, ProcessActivityTreeOptions options)
    {
        ActivityUtilities.FinishCachingSubtree(root, parentChain, options);

        List<Activity> listOfChildren = new();

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
                        if (inputs.TryGetValue(key, out object inputArgumentValue))
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
                    unusedArguments ??= new List<string>();
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
                throw FxTrace.Exception.Argument(parameterName, exceptionString);
            }
        }
    }

    internal static void ValidateArguments(Activity activity, bool isRoot, ref IList<ValidationError> validationErrors)
    {
        Fx.Assert(activity != null, "Activity to validate should not be null.");
        if (ValidationHelper.GatherAndValidateOverloads(activity, out Dictionary<string, List<RuntimeArgument>> overloadGroups, out List<RuntimeArgument> requiredArgumentsNotInOverloadGroups, out ValidationHelper.OverloadGroupEquivalenceInfo equivalenceInfo, ref validationErrors))
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

                string activityName = validationError.Source == null ? "<UnknownActivity>" : validationError.Source.DisplayName;
                exceptionMessageBuilder.AppendLine();
                exceptionMessageBuilder.Append(string.Format("'{0}': {1}", activityName, validationError.Message));

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

                exceptionString = exceptionString[..(maxExceptionStringSize - snipNotification.Length)];
                exceptionString += snipNotification;
            }
        }

        return exceptionString;
    }

    internal static string GenerateValidationErrorPrefix(Activity toValidate, ActivityUtilities.ActivityCallStack parentChain, ProcessActivityTreeOptions options, out Activity source)
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

            Dictionary<string, object> inputDictionary = new(2);

            for (int constraintIndex = 0; constraintIndex < constraints.Count; constraintIndex++)
            {
                Constraint constraint = constraints[constraintIndex];

                // there may be null entries here
                if (constraint == null)
                {
                    continue;
                }

                inputDictionary[Constraint.ToValidateArgumentName] = toValidate;
                ValidationContext validationContext = new(childActivity, parentChain, options, environment);
                inputDictionary[Constraint.ToValidateContextArgumentName] = validationContext;
                IDictionary<string, object> results = null;

                try
                {
                    var invoker = new WorkflowInvoker(constraint);
                    var extensions = validationContext.Environment.Extensions?.All;
                    if (extensions != null)
                    foreach (var e in extensions)
                    {
                        invoker.Extensions.Add(e);
                    }
                    results = invoker.Invoke(inputDictionary);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    ValidationError constraintExceptionValidationError = new(SR.InternalConstraintException(constraint.DisplayName, toValidate.GetType().FullName, toValidate.DisplayName, e.ToString()), false)
                    {
                        Source = toValidate,
                        Id = toValidate.Id
                    };

                    ActivityUtilities.Add(ref validationErrors, constraintExceptionValidationError);
                }

                if (results != null && results.TryGetValue(Constraint.ValidationErrorListArgumentName, out object resultValidationErrors))
                {
                    IList<ValidationError> validationErrorList = (IList<ValidationError>)resultValidationErrors;

                    if (validationErrorList.Count > 0)
                    {
                        validationErrors ??= new List<ValidationError>();
                        string prefix = GenerateValidationErrorPrefix(childActivity.Activity, parentChain, options, out Activity source);

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
        private readonly ValidationSettings _settings;
        private readonly Activity _rootToValidate;
        private IList<ValidationError> _errors;
        private ProcessActivityTreeOptions _options;
        private Activity _expressionRoot;
        private readonly LocationReferenceEnvironment _environment;

        internal InternalActivityValidationServices(ValidationSettings settings, Activity toValidate)
        {
            _settings = settings;
            _rootToValidate = toValidate;
            _environment = settings.Environment ?? new ActivityLocationReferenceEnvironment();

            // Add the expression evaluation extension based on Validation settings
            var expressionSettings = new ExpressionEvaluationSettings
            {
                PreferExpressionInterpretation = settings.PreferExpressionInterpretation
            };
            _environment.Extensions.Add(expressionSettings);

            _environment.IsValidating = !settings.ForceExpressionCache;
            if (settings.SkipExpressionCompilation)
            {
                _environment.CompileExpressions = true;
                _environment.IsValidating = false;
            }
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

            return new ValidationResults(GetValidationExtensionResults().Concat(_errors ?? Array.Empty<ValidationError>()).ToList());

            IEnumerable<ValidationError> GetValidationExtensionResults() =>
                _environment.Extensions.All.OfType<IValidationExtension>().SelectMany(validationExtension => validationExtension.PostValidate(_rootToValidate, _settings));
        }

        private void ValidateElement(ActivityUtilities.ChildActivity childActivity, ActivityUtilities.ActivityCallStack parentChain)
        {
            Activity toValidate = childActivity.Activity;

            if (_settings.SingleLevel && !ReferenceEquals(toValidate, _rootToValidate))
            {
                return;
            }
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
                    if (_settings.AdditionalConstraints.TryGetValue(currentType, out IList<Constraint> policyConstraints))
                    {
                        RunConstraints(childActivity, parentChain, policyConstraints, _options, suppressGetChildrenViolations, ref _errors);
                    }

                    if (currentType.IsGenericType)
                    {
                        Type genericDefinitionType = currentType.GetGenericTypeDefinition();
                        if (genericDefinitionType != null)
                        {
                            if (_settings.AdditionalConstraints.TryGetValue(genericDefinitionType, out IList<Constraint> genericTypePolicyConstraints))
                            {
                                RunConstraints(childActivity, parentChain, genericTypePolicyConstraints, _options, suppressGetChildrenViolations, ref _errors);
                            }
                        }
                    }
                    currentType = currentType.BaseType;
                }
            }

            //4. Validate if the argument expression subtree contains an activity that can induce idle.
            if (childActivity.Activity.IsExpressionRoot)
            {
                if (childActivity.Activity.HasNonEmptySubtree)
                {
                    _expressionRoot = childActivity.Activity;
                    // Back-compat: In 4.0 we always used ProcessActivityTreeOptions.FullCachingOptions here, and ignored this.options.
                    // So we need to continue to do that, unless the new 4.5 flag SkipRootConfigurationValidation is passed.
                    ProcessActivityTreeOptions options = _options.SkipRootConfigurationValidation ? _options : ProcessActivityTreeOptions.FullCachingOptions;
                    ActivityUtilities.FinishCachingSubtree(childActivity, parentChain, options, ValidateExpressionSubtree);
                    _expressionRoot = null;
                }
                else if (childActivity.Activity.InternalCanInduceIdle)
                {
                    Activity activity = childActivity.Activity;
                    RuntimeArgument runtimeArgument = GetBoundRuntimeArgument(activity);
                    ValidationError error = new(SR.CanInduceIdleActivityInArgumentExpression(runtimeArgument.Name, activity.Parent.DisplayName, activity.DisplayName), true, runtimeArgument.Name, activity.Parent);
                    ActivityUtilities.Add(ref _errors, error);
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
                ValidationError error = new(SR.CanInduceIdleActivityInArgumentExpression(runtimeArgument.Name, expressionRoot.Parent.DisplayName, activity.DisplayName), true, runtimeArgument.Name, expressionRoot.Parent);
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
            if (ReferenceEquals(boundRuntimeArgument.BoundArgument.Expression, expressionActivity))
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
            if (argument.IsEvaluationOrderSpecified && nextArgument.IsEvaluationOrderSpecified
                && argument.BoundArgument.EvaluationOrder == nextArgument.BoundArgument.EvaluationOrder)
            {
                ActivityUtilities.Add(ref validationErrors, new ValidationError(SR.DuplicateEvaluationOrderValues(referenceActivity.DisplayName, argument.BoundArgument.EvaluationOrder), false, argument.Name, referenceActivity));
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

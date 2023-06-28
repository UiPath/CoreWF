// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace System.Activities.Validation;

public static class ActivityValidationServices
{
    private static readonly ValidationSettings defaultSettings = new();

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

            return new ValidationResults(_errors);
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
                        ActivityUtilities.RunConstraints(childActivity, parentChain, policyConstraints, _options, suppressGetChildrenViolations, ref _errors);
                    }

                    if (currentType.IsGenericType)
                    {
                        Type genericDefinitionType = currentType.GetGenericTypeDefinition();
                        if (genericDefinitionType != null)
                        {
                            if (_settings.AdditionalConstraints.TryGetValue(genericDefinitionType, out IList<Constraint> genericTypePolicyConstraints))
                            {
                                ActivityUtilities.RunConstraints(childActivity, parentChain, genericTypePolicyConstraints, _options, suppressGetChildrenViolations, ref _errors);
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
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System.Threading;

namespace Microsoft.CoreWf
{
    internal class ProcessActivityTreeOptions
    {
        private static ProcessActivityTreeOptions s_validationOptions;
        private static ProcessActivityTreeOptions s_validationAndPrepareForRuntimeOptions;
        private static ProcessActivityTreeOptions s_singleLevelValidationOptions;
        private static ProcessActivityTreeOptions s_fullCachingOptions;
        private static ProcessActivityTreeOptions s_dynamicUpdateOptions;
        private static ProcessActivityTreeOptions s_dynamicUpdateOptionsForImplementation;
        private static ProcessActivityTreeOptions s_finishCachingSubtreeOptionsWithCreateEmptyBindings;
        private static ProcessActivityTreeOptions s_finishCachingSubtreeOptionsWithoutCreateEmptyBindings;
        private static ProcessActivityTreeOptions s_skipRootFinishCachingSubtreeOptions;
        private static ProcessActivityTreeOptions s_skipRootConfigurationValidationOptions;
        private static ProcessActivityTreeOptions s_singleLevelSkipRootConfigurationValidationOptions;

        private ProcessActivityTreeOptions()
        {
        }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public bool SkipIfCached
        {
            get;
            private set;
        }

        public bool CreateEmptyBindings
        {
            get;
            private set;
        }

        public bool SkipPrivateChildren
        {
            get;
            private set;
        }

        public bool OnlyCallCallbackForDeclarations
        {
            get;
            private set;
        }

        public bool SkipConstraints
        {
            get;
            private set;
        }

        public bool OnlyVisitSingleLevel
        {
            get;
            private set;
        }

        public bool SkipRootConfigurationValidation
        {
            get;
            private set;
        }

        public bool StoreTempViolations
        {
            get;
            private set;
        }

        public bool IsRuntimeReadyOptions
        {
            get
            {
                // We don't really support progressive caching at runtime so we only set ourselves
                // as runtime ready if we cached the whole workflow and created empty bindings.
                // In order to support progressive caching we need to deal with the following
                // issues:
                //   * We need a mechanism for supporting activities which supply extensions
                //   * We need to understand when we haven't created empty bindings so that
                //     we can progressively create them
                return !this.SkipPrivateChildren && this.CreateEmptyBindings;
            }
        }

        public static ProcessActivityTreeOptions FullCachingOptions
        {
            get
            {
                if (s_fullCachingOptions == null)
                {
                    s_fullCachingOptions = new ProcessActivityTreeOptions
                    {
                        SkipIfCached = true,
                        CreateEmptyBindings = true,
                        OnlyCallCallbackForDeclarations = true
                    };
                }

                return s_fullCachingOptions;
            }
        }

        public static ProcessActivityTreeOptions ValidationOptions
        {
            get
            {
                if (s_validationOptions == null)
                {
                    s_validationOptions = new ProcessActivityTreeOptions
                    {
                        SkipPrivateChildren = false,
                        // We don't want to interfere with activities doing null-checks
                        // by creating empty bindings.
                        CreateEmptyBindings = false
                    };
                }

                return s_validationOptions;
            }
        }

        public static ProcessActivityTreeOptions ValidationAndPrepareForRuntimeOptions
        {
            get
            {
                if (s_validationAndPrepareForRuntimeOptions == null)
                {
                    s_validationAndPrepareForRuntimeOptions = new ProcessActivityTreeOptions
                    {
                        SkipIfCached = false,
                        SkipPrivateChildren = false,
                        CreateEmptyBindings = true,
                    };
                }

                return s_validationAndPrepareForRuntimeOptions;
            }
        }

        private static ProcessActivityTreeOptions SkipRootConfigurationValidationOptions
        {
            get
            {
                if (s_skipRootConfigurationValidationOptions == null)
                {
                    s_skipRootConfigurationValidationOptions = new ProcessActivityTreeOptions
                    {
                        SkipPrivateChildren = false,
                        // We don't want to interfere with activities doing null-checks
                        // by creating empty bindings.
                        CreateEmptyBindings = false,
                        SkipRootConfigurationValidation = true
                    };
                }

                return s_skipRootConfigurationValidationOptions;
            }
        }

        private static ProcessActivityTreeOptions SingleLevelSkipRootConfigurationValidationOptions
        {
            get
            {
                if (s_singleLevelSkipRootConfigurationValidationOptions == null)
                {
                    s_singleLevelSkipRootConfigurationValidationOptions = new ProcessActivityTreeOptions
                    {
                        SkipPrivateChildren = false,
                        // We don't want to interfere with activities doing null-checks
                        // by creating empty bindings.
                        CreateEmptyBindings = false,
                        SkipRootConfigurationValidation = true,
                        OnlyVisitSingleLevel = true
                    };
                }

                return s_singleLevelSkipRootConfigurationValidationOptions;
            }
        }

        private static ProcessActivityTreeOptions SingleLevelValidationOptions
        {
            get
            {
                if (s_singleLevelValidationOptions == null)
                {
                    s_singleLevelValidationOptions = new ProcessActivityTreeOptions
                    {
                        SkipPrivateChildren = false,
                        // We don't want to interfere with activities doing null-checks
                        // by creating empty bindings.
                        CreateEmptyBindings = false,
                        OnlyVisitSingleLevel = true
                    };
                }

                return s_singleLevelValidationOptions;
            }
        }

        private static ProcessActivityTreeOptions FinishCachingSubtreeOptionsWithoutCreateEmptyBindings
        {
            get
            {
                if (s_finishCachingSubtreeOptionsWithoutCreateEmptyBindings == null)
                {
                    // We don't want to run constraints and we only want to hit
                    // the public path.
                    s_finishCachingSubtreeOptionsWithoutCreateEmptyBindings = new ProcessActivityTreeOptions
                    {
                        SkipConstraints = true,
                        StoreTempViolations = true
                    };
                }

                return s_finishCachingSubtreeOptionsWithoutCreateEmptyBindings;
            }
        }

        private static ProcessActivityTreeOptions SkipRootFinishCachingSubtreeOptions
        {
            get
            {
                if (s_skipRootFinishCachingSubtreeOptions == null)
                {
                    // We don't want to run constraints and we only want to hit
                    // the public path.
                    s_skipRootFinishCachingSubtreeOptions = new ProcessActivityTreeOptions
                    {
                        SkipConstraints = true,
                        SkipRootConfigurationValidation = true,
                        StoreTempViolations = true
                    };
                }

                return s_skipRootFinishCachingSubtreeOptions;
            }
        }

        private static ProcessActivityTreeOptions FinishCachingSubtreeOptionsWithCreateEmptyBindings
        {
            get
            {
                if (s_finishCachingSubtreeOptionsWithCreateEmptyBindings == null)
                {
                    // We don't want to run constraints and we only want to hit
                    // the public path.
                    s_finishCachingSubtreeOptionsWithCreateEmptyBindings = new ProcessActivityTreeOptions
                    {
                        SkipConstraints = true,
                        CreateEmptyBindings = true,
                        StoreTempViolations = true
                    };
                }

                return s_finishCachingSubtreeOptionsWithCreateEmptyBindings;
            }
        }

        public static ProcessActivityTreeOptions DynamicUpdateOptions
        {
            get
            {
                if (s_dynamicUpdateOptions == null)
                {
                    s_dynamicUpdateOptions = new ProcessActivityTreeOptions
                    {
                        OnlyCallCallbackForDeclarations = true,
                        SkipConstraints = true,
                    };
                }

                return s_dynamicUpdateOptions;
            }
        }

        public static ProcessActivityTreeOptions DynamicUpdateOptionsForImplementation
        {
            get
            {
                if (s_dynamicUpdateOptionsForImplementation == null)
                {
                    s_dynamicUpdateOptionsForImplementation = new ProcessActivityTreeOptions
                    {
                        SkipRootConfigurationValidation = true,
                        OnlyCallCallbackForDeclarations = true,
                        SkipConstraints = true,
                    };
                }

                return s_dynamicUpdateOptionsForImplementation;
            }
        }

        public static ProcessActivityTreeOptions GetFinishCachingSubtreeOptions(ProcessActivityTreeOptions originalOptions)
        {
            ProcessActivityTreeOptions result;
            if (originalOptions.CreateEmptyBindings)
            {
                Fx.Assert(!originalOptions.SkipRootConfigurationValidation, "If we ever add code that uses this combination of options, " +
                    "we need a new predefined setting on ProcessActivityTreeOptions.");
                result = ProcessActivityTreeOptions.FinishCachingSubtreeOptionsWithCreateEmptyBindings;
            }
            else
            {
                if (originalOptions.SkipRootConfigurationValidation)
                {
                    result = ProcessActivityTreeOptions.SkipRootFinishCachingSubtreeOptions;
                }
                else
                {
                    result = ProcessActivityTreeOptions.FinishCachingSubtreeOptionsWithoutCreateEmptyBindings;
                }
            }

            if (originalOptions.CancellationToken == CancellationToken.None)
            {
                return result;
            }
            else
            {
                return AttachCancellationToken(result, originalOptions.CancellationToken);
            }
        }

        public static ProcessActivityTreeOptions GetValidationOptions(ValidationSettings settings)
        {
            ProcessActivityTreeOptions result = null;
            if (settings.SkipValidatingRootConfiguration && settings.SingleLevel)
            {
                result = ProcessActivityTreeOptions.SingleLevelSkipRootConfigurationValidationOptions;
            }
            else if (settings.SkipValidatingRootConfiguration)
            {
                result = ProcessActivityTreeOptions.SkipRootConfigurationValidationOptions;
            }
            else if (settings.SingleLevel)
            {
                result = ProcessActivityTreeOptions.SingleLevelValidationOptions;
            }
            else if (settings.PrepareForRuntime)
            {
                Fx.Assert(!settings.SkipValidatingRootConfiguration && !settings.SingleLevel && !settings.OnlyUseAdditionalConstraints, "PrepareForRuntime cannot be set at the same time any of the three is set.");
                result = ProcessActivityTreeOptions.ValidationAndPrepareForRuntimeOptions;
            }
            else
            {
                result = ProcessActivityTreeOptions.ValidationOptions;
            }
            if (settings.CancellationToken == CancellationToken.None)
            {
                return result;
            }
            else
            {
                return AttachCancellationToken(result, settings.CancellationToken);
            }
        }

        private static ProcessActivityTreeOptions AttachCancellationToken(ProcessActivityTreeOptions result, CancellationToken cancellationToken)
        {
            ProcessActivityTreeOptions clone = result.Clone();
            clone.CancellationToken = cancellationToken;
            return clone;
        }

        private ProcessActivityTreeOptions Clone()
        {
            return new ProcessActivityTreeOptions
            {
                CancellationToken = this.CancellationToken,
                SkipIfCached = this.SkipIfCached,
                CreateEmptyBindings = this.CreateEmptyBindings,
                SkipPrivateChildren = this.SkipPrivateChildren,
                OnlyCallCallbackForDeclarations = this.OnlyCallCallbackForDeclarations,
                SkipConstraints = this.SkipConstraints,
                OnlyVisitSingleLevel = this.OnlyVisitSingleLevel,
                SkipRootConfigurationValidation = this.SkipRootConfigurationValidation,
                StoreTempViolations = this.StoreTempViolations,
            };
        }
    }
}

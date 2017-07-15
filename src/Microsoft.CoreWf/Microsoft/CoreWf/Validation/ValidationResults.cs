// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoreWf.Validation
{
    [Fx.Tag.XamlVisible(false)]
    public class ValidationResults
    {
        private ReadOnlyCollection<ValidationError> _allValidationErrors;
        private ReadOnlyCollection<ValidationError> _errors;
        private ReadOnlyCollection<ValidationError> _warnings;
        private bool _processedAllValidationErrors;

        public ValidationResults(IList<ValidationError> allValidationErrors)
        {
            if (allValidationErrors == null)
            {
                _allValidationErrors = ActivityValidationServices.EmptyValidationErrors;
            }
            else
            {
                _allValidationErrors = new ReadOnlyCollection<ValidationError>(allValidationErrors);
            }
        }

        public ReadOnlyCollection<ValidationError> Errors
        {
            get
            {
                if (!_processedAllValidationErrors)
                {
                    ProcessAllValidationErrors();
                }

                return _errors;
            }
        }

        public ReadOnlyCollection<ValidationError> Warnings
        {
            get
            {
                if (!_processedAllValidationErrors)
                {
                    ProcessAllValidationErrors();
                }

                return _warnings;
            }
        }

        private void ProcessAllValidationErrors()
        {
            if (_allValidationErrors.Count == 0)
            {
                _errors = ActivityValidationServices.EmptyValidationErrors;
                _warnings = ActivityValidationServices.EmptyValidationErrors;
            }
            else
            {
                IList<ValidationError> warningsList = null;
                IList<ValidationError> errorsList = null;

                for (int i = 0; i < _allValidationErrors.Count; i++)
                {
                    ValidationError violation = _allValidationErrors[i];

                    if (violation.IsWarning)
                    {
                        if (warningsList == null)
                        {
                            warningsList = new Collection<ValidationError>();
                        }

                        warningsList.Add(violation);
                    }
                    else
                    {
                        if (errorsList == null)
                        {
                            errorsList = new Collection<ValidationError>();
                        }

                        errorsList.Add(violation);
                    }
                }

                if (warningsList == null)
                {
                    _warnings = ActivityValidationServices.EmptyValidationErrors;
                }
                else
                {
                    _warnings = new ReadOnlyCollection<ValidationError>(warningsList);
                }

                if (errorsList == null)
                {
                    _errors = ActivityValidationServices.EmptyValidationErrors;
                }
                else
                {
                    _errors = new ReadOnlyCollection<ValidationError>(errorsList);
                }
            }

            _processedAllValidationErrors = true;
        }
    }
}

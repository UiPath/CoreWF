// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Validation
{
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using CoreWf.Runtime;

    [Fx.Tag.XamlVisible(false)]
    public class ValidationResults
    {
        private readonly ReadOnlyCollection<ValidationError> allValidationErrors;
        private ReadOnlyCollection<ValidationError> errors;
        private ReadOnlyCollection<ValidationError> warnings;
        private bool processedAllValidationErrors;

        public ValidationResults(IList<ValidationError> allValidationErrors)
        {
            if (allValidationErrors == null)
            {
                this.allValidationErrors = ActivityValidationServices.EmptyValidationErrors;
            }
            else
            {
                this.allValidationErrors = new ReadOnlyCollection<ValidationError>(allValidationErrors);
            }
        }

        public ReadOnlyCollection<ValidationError> Errors
        {
            get
            {
                if (!this.processedAllValidationErrors)
                {
                    ProcessAllValidationErrors();
                }

                return this.errors;
            }
        }

        public ReadOnlyCollection<ValidationError> Warnings
        {
            get
            {
                if (!this.processedAllValidationErrors)
                {
                    ProcessAllValidationErrors();
                }

                return this.warnings;
            }
        }

        private void ProcessAllValidationErrors()
        {
            if (this.allValidationErrors.Count == 0)
            {
                this.errors = ActivityValidationServices.EmptyValidationErrors;
                this.warnings = ActivityValidationServices.EmptyValidationErrors;
            }
            else
            {
                IList<ValidationError> warningsList = null;
                IList<ValidationError> errorsList = null;

                for (int i = 0; i < this.allValidationErrors.Count; i++)
                {
                    ValidationError violation = this.allValidationErrors[i];

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
                    this.warnings = ActivityValidationServices.EmptyValidationErrors;
                }
                else
                {
                    this.warnings = new ReadOnlyCollection<ValidationError>(warningsList);
                }

                if (errorsList == null)
                {
                    this.errors = ActivityValidationServices.EmptyValidationErrors;
                }
                else
                {
                    this.errors = new ReadOnlyCollection<ValidationError>(errorsList);
                }
            }

            this.processedAllValidationErrors = true;
        }
    }
}

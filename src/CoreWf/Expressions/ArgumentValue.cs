// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using CoreWf.Runtime;

    public sealed class ArgumentValue<T> : EnvironmentLocationValue<T>
    {
        private RuntimeArgument targetArgument;

        public ArgumentValue()
        {
        }

        public ArgumentValue(string argumentName)
        {
            this.ArgumentName = argumentName;
        }

        public string ArgumentName
        {
            get;
            set;
        }

        public override LocationReference LocationReference
        {
            get { return this.targetArgument; }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            this.targetArgument = null;

            if (string.IsNullOrEmpty(this.ArgumentName))
            {
                metadata.AddValidationError(SR.ArgumentNameRequired);
            }
            else
            {
                this.targetArgument = ActivityUtilities.FindArgument(this.ArgumentName, this);

                if (this.targetArgument == null)
                {
                    metadata.AddValidationError(SR.ArgumentNotFound(this.ArgumentName));
                }
                else if (!TypeHelper.AreTypesCompatible(this.targetArgument.Type, typeof(T)))
                {
                    metadata.AddValidationError(SR.ArgumentTypeMustBeCompatible(this.ArgumentName, this.targetArgument.Type, typeof(T)));
                }
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(this.ArgumentName))
            {
                return this.ArgumentName;
            }

            return base.ToString();
        }
    }
}

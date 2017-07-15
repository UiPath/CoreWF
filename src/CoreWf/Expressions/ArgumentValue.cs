// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;

namespace CoreWf.Expressions
{
    public sealed class ArgumentValue<T> : EnvironmentLocationValue<T>
    {
        private RuntimeArgument _targetArgument;

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
            get { return _targetArgument; }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            _targetArgument = null;

            if (string.IsNullOrEmpty(this.ArgumentName))
            {
                metadata.AddValidationError(SR.ArgumentNameRequired);
            }
            else
            {
                _targetArgument = ActivityUtilities.FindArgument(this.ArgumentName, this);

                if (_targetArgument == null)
                {
                    metadata.AddValidationError(SR.ArgumentNotFound(this.ArgumentName));
                }
                else if (!TypeHelper.AreTypesCompatible(_targetArgument.Type, typeof(T)))
                {
                    metadata.AddValidationError(SR.ArgumentTypeMustBeCompatible(this.ArgumentName, _targetArgument.Type, typeof(T)));
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

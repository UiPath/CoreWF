// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions
{
    using Portable.Xaml.Markup;

    [ContentProperty("DelegateArgument")]
    public sealed class DelegateArgumentReference<T> : EnvironmentLocationReference<T>
    {
        public DelegateArgumentReference()
            : base()
        {
        }

        public DelegateArgumentReference(DelegateArgument delegateArgument)
            : this()
        {
            this.DelegateArgument = delegateArgument;
        }

        public DelegateArgument DelegateArgument
        {
            get;
            set;
        }

        public override LocationReference LocationReference
        {
            get { return this.DelegateArgument; }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (this.DelegateArgument == null)
            {
                metadata.AddValidationError(SR.DelegateArgumentMustBeSet);
            }
            else
            {
                if (!this.DelegateArgument.IsInTree)
                {
                    metadata.AddValidationError(SR.DelegateArgumentMustBeReferenced(this.DelegateArgument.Name));
                }

                if (!metadata.Environment.IsVisible(this.DelegateArgument))
                {
                    metadata.AddValidationError(SR.DelegateArgumentNotVisible(this.DelegateArgument.Name));
                }

                if (!(this.DelegateArgument is DelegateOutArgument<T>) && !(this.DelegateArgument is DelegateInArgument<T>))
                {
                    metadata.AddValidationError(SR.DelegateArgumentTypeInvalid(this.DelegateArgument, typeof(T), this.DelegateArgument.Type));
                }
            }
        }
    }
}

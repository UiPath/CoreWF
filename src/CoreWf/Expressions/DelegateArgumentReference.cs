// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace System.Activities.Expressions;

[ContentProperty("DelegateArgument")]
public sealed class DelegateArgumentReference<T> : EnvironmentLocationReference<T>
{
    public DelegateArgumentReference()
        : base() { }

    public DelegateArgumentReference(DelegateArgument delegateArgument)
        : this()
    {
        DelegateArgument = delegateArgument;
    }

    public DelegateArgument DelegateArgument { get; set; }

    public override LocationReference LocationReference => DelegateArgument;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        if (DelegateArgument == null)
        {
            metadata.AddValidationError(SR.DelegateArgumentMustBeSet);
        }
        else
        {
            if (!DelegateArgument.IsInTree)
            {
                metadata.AddValidationError(SR.DelegateArgumentMustBeReferenced(DelegateArgument.Name));
            }

            if (!metadata.Environment.IsVisible(DelegateArgument))
            {
                metadata.AddValidationError(SR.DelegateArgumentNotVisible(DelegateArgument.Name));
            }

            if (DelegateArgument is not DelegateOutArgument<T> && DelegateArgument is not DelegateInArgument<T>)
            {
                metadata.AddValidationError(SR.DelegateArgumentTypeInvalid(DelegateArgument, typeof(T), DelegateArgument.Type));
            }
        }
    }
}

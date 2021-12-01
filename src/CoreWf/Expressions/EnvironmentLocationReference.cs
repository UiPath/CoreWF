// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Expressions;

[Fx.Tag.XamlVisible(false)]
public class EnvironmentLocationReference<T> : CodeActivity<Location<T>>, IExpressionContainer, ILocationReferenceExpression
{
    private readonly LocationReference _locationReference;

    // Ctors are internal because we rely on validation from creator or descendant
    internal EnvironmentLocationReference()
    {
        UseOldFastPath = true;
    }

    internal EnvironmentLocationReference(LocationReference locationReference)
        : this()
    {
        _locationReference = locationReference;
    }

    public virtual LocationReference LocationReference => _locationReference;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        // the creator of this activity is expected to have checked visibility of LocationReference.
        // we override the base CacheMetadata to avoid unnecessary reflection overhead.
    }

    protected sealed override Location<T> Execute(CodeActivityContext context)
    {
        try
        {
            context.AllowChainedEnvironmentAccess = true;
            return context.GetLocation<T>(LocationReference);
        }
        finally
        {
            context.AllowChainedEnvironmentAccess = false;
        }
    }

    ActivityWithResult ILocationReferenceExpression.CreateNewInstance(LocationReference locationReference) => new EnvironmentLocationReference<T>(locationReference);
}

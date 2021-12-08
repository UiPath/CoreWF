// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Expressions;

[Fx.Tag.XamlVisible(false)]
public class EnvironmentLocationValue<T> : CodeActivity<T>, IExpressionContainer, ILocationReferenceExpression
{
    private readonly LocationReference _locationReference;

    // Ctors are internal because we rely on validation from creator or descendant
    internal EnvironmentLocationValue()
    {
        UseOldFastPath = true;
    }

    internal EnvironmentLocationValue(LocationReference locationReference)
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

    protected override T Execute(CodeActivityContext context)
    {
        try
        {
            context.AllowChainedEnvironmentAccess = true;
            return context.GetValue<T>(LocationReference);
        }
        finally
        {
            context.AllowChainedEnvironmentAccess = false;
        }
    }

    ActivityWithResult ILocationReferenceExpression.CreateNewInstance(LocationReference locationReference) => new EnvironmentLocationValue<T>(locationReference);
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Expressions;

[Fx.Tag.XamlVisible(false)]
internal sealed class LocationReferenceValue<T> : CodeActivity<T>, IExpressionContainer, ILocationReferenceWrapper, ILocationReferenceExpression
{
    private readonly LocationReference _locationReference;

    internal LocationReferenceValue(LocationReference locationReference)
    {
        UseOldFastPath = true;
        _locationReference = locationReference;
    }

    LocationReference ILocationReferenceWrapper.LocationReference => _locationReference;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        // the creator of this activity is expected to have checked visibility of LocationReference.
        // we override the base CacheMetadata to avoid unnecessary reflection overhead.
    }

    protected override T Execute(CodeActivityContext context)
    {
        using (context.InheritVariables())
        {
            return context.GetValue<T>(_locationReference);
        }
    }

    ActivityWithResult ILocationReferenceExpression.CreateNewInstance(LocationReference locationReference) => new LocationReferenceValue<T>(locationReference);
}

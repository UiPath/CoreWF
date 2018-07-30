// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using CoreWf.Runtime;

    [Fx.Tag.XamlVisible(false)]
    internal sealed class LocationReferenceValue<T> : CodeActivity<T>, IExpressionContainer, ILocationReferenceWrapper, ILocationReferenceExpression
    {
        private readonly LocationReference locationReference;

        internal LocationReferenceValue(LocationReference locationReference)
        {
            this.UseOldFastPath = true;
            this.locationReference = locationReference;
        }

        LocationReference ILocationReferenceWrapper.LocationReference
        {
            get
            {
                return this.locationReference;
            }
        }

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
                return context.GetValue<T>(this.locationReference);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
        }

        ActivityWithResult ILocationReferenceExpression.CreateNewInstance(LocationReference locationReference)
        {
            return new LocationReferenceValue<T>(locationReference);
        }
    }
}

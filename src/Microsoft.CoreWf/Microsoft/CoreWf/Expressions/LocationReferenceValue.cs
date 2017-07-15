// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;

namespace CoreWf.Expressions
{
    [Fx.Tag.XamlVisible(false)]
    internal sealed class LocationReferenceValue<T> : CodeActivity<T>, IExpressionContainer, ILocationReferenceWrapper, ILocationReferenceExpression
    {
        private LocationReference _locationReference;

        internal LocationReferenceValue() : this(null) { }

        internal LocationReferenceValue(LocationReference locationReference)
        {
            this.UseOldFastPath = true;
            _locationReference = locationReference;
        }

        LocationReference ILocationReferenceWrapper.LocationReference
        {
            get
            {
                return _locationReference;
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
                return context.GetValue<T>(_locationReference);
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

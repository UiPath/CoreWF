// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;

namespace CoreWf.Expressions
{
    [Fx.Tag.XamlVisible(false)]
    public class EnvironmentLocationReference<T> : CodeActivity<Location<T>>, IExpressionContainer, ILocationReferenceExpression
    {
        private LocationReference _locationReference;

        // Ctors are internal because we rely on validation from creator or descendant
        internal EnvironmentLocationReference()
        {
            this.UseOldFastPath = true;
        }

        internal EnvironmentLocationReference(LocationReference locationReference)
            : this()
        {
            _locationReference = locationReference;
        }

        public virtual LocationReference LocationReference
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

        protected sealed override Location<T> Execute(CodeActivityContext context)
        {
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                return context.GetLocation<T>(this.LocationReference);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
        }

        ActivityWithResult ILocationReferenceExpression.CreateNewInstance(LocationReference locationReference)
        {
            return new EnvironmentLocationReference<T>(locationReference);
        }
    }
}

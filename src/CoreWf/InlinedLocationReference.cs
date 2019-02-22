// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Activities.Expressions;
    using System.Activities.Internals;

    internal class InlinedLocationReference : LocationReference, ILocationReferenceWrapper
    {
        private readonly LocationReference innerReference;
        private readonly Activity validAccessor;
        private readonly bool allowReads;
        private readonly bool allowWrites;
        private readonly bool allowGetLocation;

        public InlinedLocationReference(LocationReference innerReference, Activity validAccessor, ArgumentDirection accessDirection)
        {
            this.innerReference = innerReference;
            this.validAccessor = validAccessor;
            this.allowReads = accessDirection != ArgumentDirection.Out;
            this.allowWrites = accessDirection != ArgumentDirection.In;
        }

        public InlinedLocationReference(LocationReference innerReference, Activity validAccessor)
        {
            this.innerReference = innerReference;
            this.validAccessor = validAccessor;
            this.allowReads = true;
            this.allowWrites = true;
            this.allowGetLocation = true;
        }

        protected override string NameCore
        {
            get
            {
                return this.innerReference.Name;
            }
        }
        
        protected override Type TypeCore
        {
            get
            {
                return this.innerReference.Type;
            }
        }

        public override Location GetLocation(ActivityContext context)
        {
            if (context == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(context));
            }
            ValidateAccessor(context);
            if (!this.allowGetLocation)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.GetLocationOnPublicAccessReference(context.Activity)));
            }
            return GetLocationCore(context);
        }

        internal override Location GetLocationForRead(ActivityContext context)
        {
            ValidateAccessor(context);
            if (!this.allowReads)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ReadAccessToWriteOnlyPublicReference(context.Activity)));
            }
            return GetLocationCore(context);
        }


        internal override Location GetLocationForWrite(ActivityContext context)
        {
            ValidateAccessor(context);
            if (!this.allowWrites)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteAccessToReadOnlyPublicReference(context.Activity)));
            }
            return GetLocationCore(context);
        }

        private void ValidateAccessor(ActivityContext context)
        {
            // We need to call ThrowIfDisposed explicitly since
            // context.Activity does not check isDisposed
            context.ThrowIfDisposed();

            if (!object.ReferenceEquals(context.Activity, this.validAccessor))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InlinedLocationReferenceOnlyAccessibleByOwner(context.Activity, this.validAccessor)));
            }
        }

        private Location GetLocationCore(ActivityContext context)
        {
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                return this.innerReference.GetLocation(context);
            }
            finally
            {
                context.AllowChainedEnvironmentAccess = false;
            }
        }

        LocationReference ILocationReferenceWrapper.LocationReference
        {
            get
            {
                return this.innerReference;
            }
        }
    }
}

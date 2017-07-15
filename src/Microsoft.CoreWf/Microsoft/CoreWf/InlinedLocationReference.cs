// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Expressions;
using System;

namespace CoreWf
{
    internal class InlinedLocationReference : LocationReference, ILocationReferenceWrapper
    {
        private LocationReference _innerReference;
        private Activity _validAccessor;
        private bool _allowReads;
        private bool _allowWrites;
        private bool _allowGetLocation;

        public InlinedLocationReference(LocationReference innerReference, Activity validAccessor, ArgumentDirection accessDirection)
        {
            _innerReference = innerReference;
            _validAccessor = validAccessor;
            _allowReads = accessDirection != ArgumentDirection.Out;
            _allowWrites = accessDirection != ArgumentDirection.In;
        }

        public InlinedLocationReference(LocationReference innerReference, Activity validAccessor)
        {
            _innerReference = innerReference;
            _validAccessor = validAccessor;
            _allowReads = true;
            _allowWrites = true;
            _allowGetLocation = true;
        }

        protected override string NameCore
        {
            get
            {
                return _innerReference.Name;
            }
        }

        protected override Type TypeCore
        {
            get
            {
                return _innerReference.Type;
            }
        }

        public override Location GetLocation(ActivityContext context)
        {
            if (context == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }
            ValidateAccessor(context);
            if (!_allowGetLocation)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.GetLocationOnPublicAccessReference(context.Activity)));
            }
            return GetLocationCore(context);
        }

        internal override Location GetLocationForRead(ActivityContext context)
        {
            ValidateAccessor(context);
            if (!_allowReads)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ReadAccessToWriteOnlyPublicReference(context.Activity)));
            }
            return GetLocationCore(context);
        }


        internal override Location GetLocationForWrite(ActivityContext context)
        {
            ValidateAccessor(context);
            if (!_allowWrites)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteAccessToReadOnlyPublicReference(context.Activity)));
            }
            return GetLocationCore(context);
        }

        private void ValidateAccessor(ActivityContext context)
        {
            // We need to call ThrowIfDisposed explicitly since
            // context.Activity does not check isDisposed
            context.ThrowIfDisposed();

            if (!object.ReferenceEquals(context.Activity, _validAccessor))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.InlinedLocationReferenceOnlyAccessibleByOwner(context.Activity, _validAccessor)));
            }
        }

        private Location GetLocationCore(ActivityContext context)
        {
            try
            {
                context.AllowChainedEnvironmentAccess = true;
                return _innerReference.GetLocation(context);
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
                return _innerReference;
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Expressions;
using Internals;

internal class InlinedLocationReference : LocationReference, ILocationReferenceWrapper
{
    private readonly LocationReference _innerReference;
    private readonly Activity _validAccessor;
    private readonly bool _allowReads;
    private readonly bool _allowWrites;
    private readonly bool _allowGetLocation;

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

    protected override string NameCore => _innerReference.Name;

    protected override Type TypeCore => _innerReference.Type;

    public override Location GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }
        ValidateAccessor(context);
        if (!_allowGetLocation)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.GetLocationOnPublicAccessReference(context.Activity)));
        }
        return GetLocationCore(context);
    }

    internal override Location GetLocationForRead(ActivityContext context)
    {
        ValidateAccessor(context);
        if (!_allowReads)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ReadAccessToWriteOnlyPublicReference(context.Activity)));
        }
        return GetLocationCore(context);
    }

    internal override Location GetLocationForWrite(ActivityContext context)
    {
        ValidateAccessor(context);
        if (!_allowWrites)
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

        if (!ReferenceEquals(context.Activity, _validAccessor))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InlinedLocationReferenceOnlyAccessibleByOwner(context.Activity, _validAccessor)));
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

    LocationReference ILocationReferenceWrapper.LocationReference => _innerReference;
}

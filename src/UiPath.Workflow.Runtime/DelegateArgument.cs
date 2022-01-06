// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;
using Validation;

public abstract class DelegateArgument : LocationReference
{
    private ArgumentDirection _direction;
    private RuntimeDelegateArgument _runtimeArgument;
    private string _name;
    private int _cacheId;

    internal DelegateArgument() => Id = -1;

    [DefaultValue(null)]
    public new string Name
    {
        get => _name;
        set => _name = value;
    }

    protected override string NameCore => _name;

    public ArgumentDirection Direction
    {
        get => _direction;
        internal set => _direction = value;
    }

    internal Activity Owner { get; private set; }

    internal bool IsInTree => Owner != null;

    internal void ThrowIfNotInTree()
    {
        if (!IsInTree)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentMustBeReferenced(Name)));
        }
    }

    internal void Bind(RuntimeDelegateArgument runtimeArgument) => _runtimeArgument = runtimeArgument;

    internal bool InitializeRelationship(Activity parent, ref IList<ValidationError> validationErrors)
    {
        if (_cacheId == parent.CacheId)
        {
            Fx.Assert(Owner != null, "must have an owner here");
            ValidationError validationError = new(SR.DelegateArgumentAlreadyInUseOnActivity(Name, parent.DisplayName, Owner.DisplayName), Owner);
            ActivityUtilities.Add(ref validationErrors, validationError);

            // Get out early since we've already initialized this argument.
            return false;
        }

        Owner = parent;
        _cacheId = parent.CacheId;

        return true;
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public object Get(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        return context.GetValue<object>((LocationReference)this);
    }

    public override Location GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        ThrowIfNotInTree();

        if (!context.AllowChainedEnvironmentAccess)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
        }

        if (!context.Environment.TryGetLocation(Id, Owner, out Location location))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
        }

        return location;
    }

    // Only used by the debugger
    internal Location InternalGetLocation(LocationEnvironment environment)
    {
        Fx.Assert(IsInTree, "DelegateArgument must be opened");

        if (!environment.TryGetLocation(Id, Owner, out Location location))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.DelegateArgumentDoesNotExist(_runtimeArgument.Name)));
        }
        return location;
    }

    internal abstract Location CreateLocation();
}

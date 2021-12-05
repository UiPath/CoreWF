// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Activities;
using Internals;
using Runtime;

public struct CodeActivityPublicEnvironmentAccessor
{
    private CodeActivityMetadata _metadata;
    private bool _withoutArgument;

    public CodeActivityMetadata ActivityMetadata => _metadata;

    public static CodeActivityPublicEnvironmentAccessor Create(CodeActivityMetadata metadata)
    {
        metadata.ThrowIfDisposed();
            
        AssertIsCodeActivity(metadata.CurrentActivity);

        CodeActivityPublicEnvironmentAccessor result = new()
        {
            _metadata = metadata
        };
        return result;
    }

    internal static CodeActivityPublicEnvironmentAccessor CreateWithoutArgument(CodeActivityMetadata metadata)
    {
        CodeActivityPublicEnvironmentAccessor toReturn = Create(metadata);
        toReturn._withoutArgument = true;
        return toReturn;
    }

    public static bool operator ==(CodeActivityPublicEnvironmentAccessor left, CodeActivityPublicEnvironmentAccessor right) => left.Equals(right);

    public static bool operator !=(CodeActivityPublicEnvironmentAccessor left, CodeActivityPublicEnvironmentAccessor right) => !left.Equals(right);

    public bool TryGetAccessToPublicLocation(LocationReference publicLocation,
        ArgumentDirection accessDirection, out LocationReference equivalentLocation)
    {
        if (publicLocation == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(publicLocation));
        }
        ThrowIfUninitialized();

        return TryGetAccessToPublicLocation(publicLocation, accessDirection, false, out equivalentLocation);
    }

    public bool TryGetReferenceToPublicLocation(LocationReference publicReference,
        out LocationReference equivalentReference)
    {
        if (publicReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(publicReference));
        }
        ThrowIfUninitialized();

        return TryGetReferenceToPublicLocation(publicReference, false, out equivalentReference);
    }

    public override bool Equals(object obj)
    {
        if (obj is not CodeActivityPublicEnvironmentAccessor)
        {
            return false;
        }

        CodeActivityPublicEnvironmentAccessor other = (CodeActivityPublicEnvironmentAccessor)obj;
        return other._metadata == _metadata;
    }

    public override int GetHashCode() => _metadata.GetHashCode();

    // In 4.0 the expression type for publicly inspectable auto-generated arguments was 
    // LocationReferenceValue<T>, whether the argument was actually used as an L-Value or R-Value.
    // We keep that for back-compat (useLocationReferenceValue == true), and only use the new
    // EnvironmentLocationValue/Reference classes for new activities.
    internal bool TryGetAccessToPublicLocation(LocationReference publicLocation,
        ArgumentDirection accessDirection, bool useLocationReferenceValue, out LocationReference equivalentLocation)
    {
        Fx.Assert(!useLocationReferenceValue || ActivityMetadata.CurrentActivity.UseOldFastPath, "useLocationReferenceValue should only be used for back-compat");

        if (_metadata.Environment.IsVisible(publicLocation))
        {
            if (!_withoutArgument)
            {
                CreateArgument(publicLocation, accessDirection, useLocationReferenceValue);
            }                
            equivalentLocation = new InlinedLocationReference(publicLocation, _metadata.CurrentActivity, accessDirection);
            return true;
        }

        equivalentLocation = null;
        return false;
    }

    internal bool TryGetReferenceToPublicLocation(LocationReference publicReference,
        bool useLocationReferenceValue, out LocationReference equivalentReference)
    {
        Fx.Assert(!useLocationReferenceValue || ActivityMetadata.CurrentActivity.UseOldFastPath, "useLocationReferenceValue should only be used for back-compat");

        if (_metadata.Environment.IsVisible(publicReference))
        {
            if (!_withoutArgument)
            {
                CreateLocationArgument(publicReference, useLocationReferenceValue);
            }
            equivalentReference = new InlinedLocationReference(publicReference, _metadata.CurrentActivity);
            return true;
        }

        equivalentReference = null;
        return false;
    }

    internal void CreateArgument(LocationReference sourceReference, ArgumentDirection accessDirection, bool useLocationReferenceValue = false)
    {
        ActivityWithResult expression = ActivityUtilities.CreateLocationAccessExpression(sourceReference, accessDirection != ArgumentDirection.In, useLocationReferenceValue);
        AddGeneratedArgument(sourceReference.Type, accessDirection, expression);
    }

    internal void CreateLocationArgument(LocationReference sourceReference, bool useLocationReferenceValue = false)
    {
        ActivityWithResult expression = ActivityUtilities.CreateLocationAccessExpression(sourceReference, true, useLocationReferenceValue);
        AddGeneratedArgument(expression.ResultType, ArgumentDirection.In, expression);
    }

    private void AddGeneratedArgument(Type argumentType, ArgumentDirection direction, ActivityWithResult expression)
    {
        Argument argument = ActivityUtilities.CreateArgument(argumentType, direction);
        argument.Expression = expression;
        RuntimeArgument runtimeArgument = _metadata.CurrentActivity.AddTempAutoGeneratedArgument(argumentType, direction);
        Argument.TryBind(argument, runtimeArgument, _metadata.CurrentActivity);
    }

    private void ThrowIfUninitialized()
    {
        if (_metadata.CurrentActivity == null)
        {
            // Using ObjectDisposedException for consistency with the other metadata structs
            throw FxTrace.Exception.AsError(new ObjectDisposedException(ToString()));
        }
    }

    [Conditional("DEBUG")]
    private static void AssertIsCodeActivity(Activity activity)
    {
        Type codeActivityOfTType = null;
        if (activity is ActivityWithResult activityWithResult)
        {
            codeActivityOfTType = typeof(CodeActivity<>).MakeGenericType(activityWithResult.ResultType);
        }
        Fx.Assert(activity is CodeActivity || (codeActivityOfTType != null && codeActivityOfTType.IsAssignableFrom(activity.GetType())), "Expected CodeActivity or CodeActivity<T>");
    }
}

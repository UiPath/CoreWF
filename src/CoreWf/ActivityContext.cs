// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
using System.Globalization;

namespace System.Activities;
using Internals;
using Runtime;
using Tracking;

[Fx.Tag.XamlVisible(false)]
public class ActivityContext
{
    private ActivityInstance _instance;
    private ActivityExecutor _executor;
    private bool _isDisposed;
    private long _instanceId;

    // Used by subclasses that are pooled.
    internal ActivityContext() { }

    // these can only be created by the WF Runtime
    internal ActivityContext(ActivityInstance instance, ActivityExecutor executor)
    {
        Fx.Assert(instance != null, "valid activity instance is required");

        _instance = instance;
        _executor = executor;
        Activity = _instance.Activity;
        _instanceId = instance.InternalId;
    }

    internal LocationEnvironment Environment
    {
        get
        {
            ThrowIfDisposed();
            return _instance.Environment;
        }
    }

    internal bool AllowChainedEnvironmentAccess { get; set; }

    internal Activity Activity { get; private set; }

    internal ActivityInstance CurrentInstance => _instance;

    internal ActivityExecutor CurrentExecutor => _executor;

    public string ActivityInstanceId
    {
        get
        {
            ThrowIfDisposed();
            return _instanceId.ToString(CultureInfo.InvariantCulture);
        }
    }

    public Guid WorkflowInstanceId
    {
        get
        {
            ThrowIfDisposed();
            return _executor.WorkflowInstanceId;
        }
    }

    public WorkflowDataContext DataContext
    {
        get
        {
            ThrowIfDisposed();

            // Argument expressions don't have visbility into public variables at the same scope.
            // However fast-path expressions use the parent's ActivityInstance instead of
            // creating their own, so we need to give them a DataContext without variables
            bool includeLocalVariables = !_instance.IsResolvingArguments;

            if (_instance.DataContext == null ||
                _instance.DataContext.IncludesLocalVariables != includeLocalVariables)
            {
                _instance.DataContext
                    = new WorkflowDataContext(_executor, _instance, includeLocalVariables);
            }

            return _instance.DataContext;
        }
    }

    internal bool IsDisposed => _isDisposed;

    public T GetExtension<T>()
        where T : class
    {
        ThrowIfDisposed();
        return _executor.GetExtension<T>();
    }

    internal Location GetIgnorableResultLocation(RuntimeArgument resultArgument)
    {
        return _executor.GetIgnorableResultLocation(resultArgument);
    }

    internal void Reinitialize(ActivityInstance instance, ActivityExecutor executor)
        => Reinitialize(instance, executor, instance.Activity, instance.InternalId);

    internal void Reinitialize(ActivityInstance instance, ActivityExecutor executor, Activity activity, long instanceId)
    {
        _isDisposed = false;
        _instance = instance;
        _executor = executor;
        Activity = activity;
        _instanceId = instanceId;
    }

    // extra insurance against misuse (if someone stashes away the execution context to use later)
    internal void Dispose()
    {
        _isDisposed = true;
        _instance = null;
        _executor = null;
        Activity = null;
        _instanceId = 0;
    }

    internal void DisposeDataContext()
    {
        if (_instance.DataContext != null)
        {
            _instance.DataContext.DisposeEnvironment();
            _instance.DataContext = null;
        }
    }

    public T GetValue<T>(string locationReferenceName) => GetLocation<T>(locationReferenceName).Value;

    internal Location<T> GetLocation<T>(string locationReferenceName)
    {
        var environment = Activity.GetParentEnvironment();
        if (!environment.TryGetLocationReference(locationReferenceName, out var locationReference))
        {
            throw new ArgumentOutOfRangeException(nameof(locationReferenceName), SR.LocationExpressionCouldNotBeResolved(locationReferenceName));
        }
        return GetLocation<T>(locationReference);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public Location<T> GetLocation<T>(LocationReference locationReference)
    {
        ThrowIfDisposed();

        if (locationReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(locationReference));
        }

        Location location = locationReference.GetLocation(this);


        if (location is Location<T> typedLocation)
        {
            return typedLocation;
        }
        else
        {
            Fx.Assert(location != null, "The contract of LocationReference is that GetLocation never returns null.");

            if (locationReference.Type == typeof(T))
            {
                return new TypedLocationWrapper<T>(location);
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LocationTypeMismatch(locationReference.Name, typeof(T), locationReference.Type)));
            }
        }
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public T GetValue<T>(LocationReference locationReference)
    {
        ThrowIfDisposed();

        if (locationReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(locationReference));
        }

        return GetValueCore<T>(locationReference);
    }

    internal T GetValueCore<T>(LocationReference locationReference)
    {
        Location location = locationReference.GetLocationForRead(this);


        if (location is Location<T> typedLocation)
        {
            // If we hit this path we can avoid boxing value types
            return typedLocation.Value;
        }
        else
        {
            Fx.Assert(location != null, "The contract of LocationReference is that GetLocation never returns null.");

            return TypeHelper.Convert<T>(location.Value);
        }
    }

    public void SetValue<T>(LocationReference locationReference, T value)
    {
        ThrowIfDisposed();

        if (locationReference == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(locationReference));
        }

        SetValueCore<T>(locationReference, value);
    }

    internal void SetValueCore<T>(LocationReference locationReference, T value)
    {
        Location location = locationReference.GetLocationForWrite(this);


        if (location is Location<T> typedLocation)
        {
            // If we hit this path we can avoid boxing value types
            typedLocation.Value = value;
        }
        else
        {

            if (!TypeHelper.AreTypesCompatible(value, locationReference.Type))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotSetValueToLocation(value != null ? value.GetType() : typeof(T), locationReference.Name, locationReference.Type)));
            }

            location.Value = value;
        }
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public T GetValue<T>(OutArgument<T> argument)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        argument.ThrowIfNotInTree();

        return GetValueCore<T>(argument.RuntimeArgument);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public T GetValue<T>(InOutArgument<T> argument)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        argument.ThrowIfNotInTree();

        return GetValueCore<T>(argument.RuntimeArgument);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public T GetValue<T>(InArgument<T> argument)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        argument.ThrowIfNotInTree();

        return GetValueCore<T>(argument.RuntimeArgument);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public object GetValue(Argument argument)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        argument.ThrowIfNotInTree();

        return GetValueCore<object>(argument.RuntimeArgument);
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "We explicitly provide a RuntimeArgument overload to avoid requiring the object type parameter.")]
    public object GetValue(RuntimeArgument runtimeArgument)
    {
        ThrowIfDisposed();

        if (runtimeArgument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(runtimeArgument));
        }

        return GetValueCore<object>(runtimeArgument);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public void SetValue<T>(OutArgument<T> argument, T value)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            // We want to shortcut if the argument is null
            return;
        }

        argument.ThrowIfNotInTree();

        SetValueCore(argument.RuntimeArgument, value);
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public void SetValue<T>(InOutArgument<T> argument, T value)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            // We want to shortcut if the argument is null
            return;
        }

        argument.ThrowIfNotInTree();

        SetValueCore(argument.RuntimeArgument, value);
    }
        
    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "Generic needed for type inference")]
    public void SetValue<T>(InArgument<T> argument, T value)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            // We want to shortcut if the argument is null
            return;
        }

        argument.ThrowIfNotInTree();

        SetValueCore(argument.RuntimeArgument, value);
    }

    public void SetValue(Argument argument, object value)
    {
        ThrowIfDisposed();

        if (argument == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(argument));
        }

        argument.ThrowIfNotInTree();

        SetValueCore(argument.RuntimeArgument, value);
    }

    internal void TrackCore(CustomTrackingRecord record)
    {
        Fx.Assert(!_isDisposed, "not usable if disposed");
        Fx.Assert(record != null, "expect non-null record");

        if (_executor.ShouldTrack)
        {
            record.Activity = new ActivityInfo(_instance);
            record.InstanceId = WorkflowInstanceId;
            _executor.AddTrackingRecord(record);
        }
    }

    internal void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw FxTrace.Exception.AsError(
                new ObjectDisposedException(GetType().FullName, SR.AECDisposed));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Tracking;
using System;
using System.Globalization;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public class ActivityContext
    {
        private ActivityInstance _instance;
        private ActivityExecutor _executor;
        private bool _isDisposed;
        private long _instanceId;

        // Used by subclasses that are pooled.
        internal ActivityContext()
        {
        }

        // these can only be created by the WF Runtime
        internal ActivityContext(ActivityInstance instance, ActivityExecutor executor)
        {
            Fx.Assert(instance != null, "valid activity instance is required");

            _instance = instance;
            _executor = executor;
            this.Activity = _instance.Activity;
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

        internal bool AllowChainedEnvironmentAccess
        {
            get;
            set;
        }

        internal Activity Activity
        {
            get;
            private set;
        }

        internal ActivityInstance CurrentInstance
        {
            get
            {
                return _instance;
            }
        }

        internal ActivityExecutor CurrentExecutor
        {
            get
            {
                return _executor;
            }
        }

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

        internal bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

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
        {
            Reinitialize(instance, executor, instance.Activity, instance.InternalId);
        }

        internal void Reinitialize(ActivityInstance instance, ActivityExecutor executor, Activity activity, long instanceId)
        {
            _isDisposed = false;
            _instance = instance;
            _executor = executor;
            this.Activity = activity;
            _instanceId = instanceId;
        }

        // extra insurance against misuse (if someone stashes away the execution context to use later)
        internal void Dispose()
        {
            _isDisposed = true;
            _instance = null;
            _executor = null;
            this.Activity = null;
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

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public Location<T> GetLocation<T>(LocationReference locationReference)
        {
            ThrowIfDisposed();

            if (locationReference == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationReference");
            }

            Location location = locationReference.GetLocation(this);

            Location<T> typedLocation = location as Location<T>;

            if (typedLocation != null)
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
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.LocationTypeMismatch(locationReference.Name, typeof(T), locationReference.Type)));
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationReference");
            }

            return GetValueCore<T>(locationReference);
        }

        internal T GetValueCore<T>(LocationReference locationReference)
        {
            Location location = locationReference.GetLocationForRead(this);

            Location<T> typedLocation = location as Location<T>;

            if (typedLocation != null)
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationReference");
            }

            SetValueCore<T>(locationReference, value);
        }

        internal void SetValueCore<T>(LocationReference locationReference, T value)
        {
            Location location = locationReference.GetLocationForWrite(this);

            Location<T> typedLocation = location as Location<T>;

            if (typedLocation != null)
            {
                // If we hit this path we can avoid boxing value types
                typedLocation.Value = value;
            }
            else
            {
                if (!TypeHelper.AreTypesCompatible(value, locationReference.Type))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotSetValueToLocation(value != null ? value.GetType() : typeof(T), locationReference.Name, locationReference.Type)));
                }

                location.Value = value;
            }
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Generic needed for type inference")]
        public T GetValue<T>(OutArgument<T> argument)
        {
            ThrowIfDisposed();

            if (argument == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("argument");
            }

            argument.ThrowIfNotInTree();

            return GetValueCore<T>(argument.RuntimeArgument);
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Generic needed for type inference")]
        public T GetValue<T>(InOutArgument<T> argument)
        {
            ThrowIfDisposed();

            if (argument == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("argument");
            }

            argument.ThrowIfNotInTree();

            return GetValueCore<T>(argument.RuntimeArgument);
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Generic needed for type inference")]
        public T GetValue<T>(InArgument<T> argument)
        {
            ThrowIfDisposed();

            if (argument == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("argument");
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("argument");
            }

            argument.ThrowIfNotInTree();

            return GetValueCore<object>(argument.RuntimeArgument);
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "We explicitly provide a RuntimeArgument overload to avoid requiring the object type parameter.")]
        public object GetValue(RuntimeArgument runtimeArgument)
        {
            ThrowIfDisposed();

            if (runtimeArgument == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("runtimeArgument");
            }

            return GetValueCore<object>(runtimeArgument);
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //Justification = "Generic needed for type inference")]
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
        //Justification = "Generic needed for type inference")]
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
        //Justification = "Generic needed for type inference")]
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
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("argument");
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
                record.InstanceId = this.WorkflowInstanceId;
                _executor.AddTrackingRecord(record);
            }
        }

        internal void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(
                    new ObjectDisposedException(this.GetType().FullName, SR.AECDisposed));
            }
        }
    }
}

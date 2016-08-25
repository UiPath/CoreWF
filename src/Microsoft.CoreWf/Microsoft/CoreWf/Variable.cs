// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
    [DebuggerDisplay("Name = {Name}, Type = {Type}")]
    public abstract class Variable : LocationReference
    {
        private VariableModifiers _modifiers;
        private string _name;
        private int _cacheId;

        internal Variable()
            : base()
        {
            this.Id = -1;
        }

        internal bool IsHandle
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public new string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        [DefaultValue(VariableModifiers.None)]
        public VariableModifiers Modifiers
        {
            get
            {
                return _modifiers;
            }
            set
            {
                VariableModifiersHelper.Validate(value, "value");
                _modifiers = value;
            }
        }

        [IgnoreDataMember] // this member is repeated by all subclasses, which we control
        [DefaultValue(null)]
        public ActivityWithResult Default
        {
            get
            {
                return this.DefaultCore;
            }
            set
            {
                this.DefaultCore = value;
            }
        }

        protected override string NameCore
        {
            get
            {
                return _name;
            }
        }

        internal int CacheId
        {
            get
            {
                return _cacheId;
            }
        }

        internal abstract ActivityWithResult DefaultCore
        {
            get;
            set;
        }

        internal bool IsPublic
        {
            get;
            set;
        }

        internal object Origin
        {
            get;
            set;
        }

        internal Activity Owner
        {
            get;
            private set;
        }

        internal bool IsInTree
        {
            get
            {
                return this.Owner != null;
            }
        }

        public static Variable Create(string name, Type type, VariableModifiers modifiers)
        {
            return ActivityUtilities.CreateVariable(name, type, modifiers);
        }

        internal bool InitializeRelationship(Activity parent, bool isPublic, ref IList<ValidationError> validationErrors)
        {
            if (_cacheId == parent.CacheId)
            {
                if (this.Owner != null)
                {
                    ValidationError validationError = new ValidationError(SR.VariableAlreadyInUseOnActivity(this.Name, parent.DisplayName, this.Owner.DisplayName), false, this.Name, parent);
                    ActivityUtilities.Add(ref validationErrors, validationError);

                    // Get out early since we've already initialized this variable.
                    return false;
                }
            }

            this.Owner = parent;
            _cacheId = parent.CacheId;
            this.IsPublic = isPublic;

            if (this.Default != null)
            {
                ActivityWithResult expression = this.Default;

                if (expression is Argument.IExpressionWrapper)
                {
                    expression = ((Argument.IExpressionWrapper)expression).InnerExpression;
                }

                if (expression.ResultType != this.Type)
                {
                    ActivityUtilities.Add(
                        ref validationErrors,
                        new ValidationError(SR.VariableExpressionTypeMismatch(this.Name, this.Type, expression.ResultType), false, this.Name, parent));
                }

                return this.Default.InitializeRelationship(this, isPublic, ref validationErrors);
            }

            return true;
        }

        internal void ThrowIfNotInTree()
        {
            if (!this.IsInTree)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableNotOpen(this.Name, this.Type)));
            }
        }

        internal void ThrowIfHandle()
        {
            if (this.IsHandle)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
            }
        }

        public override Location GetLocation(ActivityContext context)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            // No need to call context.ThrowIfDisposed explicitly since all
            // the methods/properties on the context will perform that check.

            ThrowIfNotInTree();

            Location location;
            if (!context.AllowChainedEnvironmentAccess)
            {
                if (this.IsPublic || !object.ReferenceEquals(this.Owner, context.Activity))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableOnlyAccessibleAtScopeOfDeclaration(context.Activity, this.Owner)));
                }

                if (!context.Environment.TryGetLocation(this.Id, out location))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(this.Name)));
                }
            }
            else
            {
                // No validations in the allow chained access case

                if (!context.Environment.TryGetLocation(this.Id, this.Owner, out location))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(this.Name)));
                }
            }

            return location;
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public object Get(ActivityContext context)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            return context.GetValue<object>((LocationReference)this);
        }

        public void Set(ActivityContext context, object value)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.SetValue((LocationReference)this, value);
        }

        internal abstract Location DeclareLocation(ActivityExecutor executor, ActivityInstance instance);

        // This method exists for debugger use
        internal Location InternalGetLocation(LocationEnvironment environment)
        {
            Fx.Assert(this.IsInTree, "Variable must be opened");

            Location location;
            if (!environment.TryGetLocation(this.Id, this.Owner, out location))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(this.Name)));
            }
            return location;
        }

        // optional "fast-path" for initial value expressions that can be resolved synchronously
        internal abstract void PopulateDefault(ActivityExecutor executor, ActivityInstance parentInstance, Location location);

        internal abstract void SetIsWaitingOnDefaultValue(Location location);

        internal abstract Location CreateLocation();
    }

    public sealed class Variable<T> : Variable
    {
        private Activity<T> _defaultExpression;

        public Variable()
            : base()
        {
            this.IsHandle = ActivityUtilities.IsHandle(typeof(T));
        }

        public Variable(Expression<Func<ActivityContext, T>> defaultExpression)
            : this()
        {
            if (defaultExpression != null)
            {
                this.Default = new LambdaValue<T>(defaultExpression);
            }
        }

        public Variable(string name, Expression<Func<ActivityContext, T>> defaultExpression)
            : this(defaultExpression)
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.Name = name;
            }
        }

        public Variable(string name)
            : this()
        {
            if (!string.IsNullOrEmpty(name))
            {
                this.Name = name;
            }
        }

        public Variable(string name, T defaultValue)
            : this(name)
        {
            this.Default = new Literal<T>(defaultValue);
        }

        protected override Type TypeCore
        {
            get
            {
                return typeof(T);
            }
        }

        [DefaultValue(null)]
        public new Activity<T> Default
        {
            get
            {
                return _defaultExpression;
            }
            set
            {
                ThrowIfHandle();

                _defaultExpression = value;
            }
        }

        internal override ActivityWithResult DefaultCore
        {
            get
            {
                return this.Default;
            }
            set
            {
                ThrowIfHandle();

                if (value == null)
                {
                    _defaultExpression = null;
                    return;
                }

                if (value is Activity<T>)
                {
                    _defaultExpression = (Activity<T>)value;
                }
                else
                {
                    _defaultExpression = new ActivityWithResultWrapper<T>(value);
                }
            }
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public new Location<T> GetLocation(ActivityContext context)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            return context.GetLocation<T>(this);
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public new T Get(ActivityContext context)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            return context.GetValue<T>((LocationReference)this);
        }

        public void Set(ActivityContext context, T value)
        {
            if (context == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            context.SetValue((LocationReference)this, value);
        }

        internal override Location DeclareLocation(ActivityExecutor executor, ActivityInstance instance)
        {
            VariableLocation variableLocation = new VariableLocation(Modifiers, this.IsHandle);

            if (this.IsHandle)
            {
                Fx.Assert(this.Default == null, "Default should be null");
                instance.Environment.DeclareHandle(this, variableLocation, instance);

                HandleInitializationContext context = new HandleInitializationContext(executor, instance);
                try
                {
                    variableLocation.SetInitialValue((T)context.CreateAndInitializeHandle(typeof(T)));
                }
                finally
                {
                    context.Dispose();
                }
            }
            else
            {
                instance.Environment.Declare(this, variableLocation, instance);
            }

            return variableLocation;
        }

        internal override void PopulateDefault(ActivityExecutor executor, ActivityInstance parentInstance, Location location)
        {
            Fx.Assert(this.Default.UseOldFastPath, "Should only be called for OldFastPath");
            VariableLocation variableLocation = (VariableLocation)location;

            T value = executor.ExecuteInResolutionContext<T>(parentInstance, Default);
            variableLocation.SetInitialValue(value);
        }

        internal override void SetIsWaitingOnDefaultValue(Location location)
        {
            ((VariableLocation)location).SetIsWaitingOnDefaultValue();
        }

        internal override Location CreateLocation()
        {
            return new VariableLocation(this.Modifiers, this.IsHandle);
        }

        [DataContract]
        internal sealed class VariableLocation : Location<T>, INotifyPropertyChanged
        {
            private VariableModifiers _modifiers;

            private bool _isHandle;

            private bool _isWaitingOnDefaultValue;

            private PropertyChangedEventHandler _propertyChanged;
            private NotifyCollectionChangedEventHandler _valueCollectionChanged;
            private PropertyChangedEventHandler _valuePropertyChanged;

            public VariableLocation(VariableModifiers modifiers, bool isHandle)
                : base()
            {
                _modifiers = modifiers;
                _isHandle = isHandle;
            }

            public event PropertyChangedEventHandler PropertyChanged
            {
                add
                {
                    _propertyChanged += value;
                    INotifyPropertyChanged notifyPropertyChanged = this.Value as INotifyPropertyChanged;
                    if (notifyPropertyChanged != null)
                    {
                        notifyPropertyChanged.PropertyChanged += ValuePropertyChangedHandler;
                    }
                    INotifyCollectionChanged notifyCollectionChanged = this.Value as INotifyCollectionChanged;
                    if (notifyCollectionChanged != null)
                    {
                        notifyCollectionChanged.CollectionChanged += ValueCollectionChangedHandler;
                    }
                }
                remove
                {
                    _propertyChanged -= value;
                    INotifyPropertyChanged notifyPropertyChanged = this.Value as INotifyPropertyChanged;
                    if (notifyPropertyChanged != null)
                    {
                        notifyPropertyChanged.PropertyChanged -= ValuePropertyChangedHandler;
                    }
                    INotifyCollectionChanged notifyCollectionChanged = this.Value as INotifyCollectionChanged;
                    if (notifyCollectionChanged != null)
                    {
                        notifyCollectionChanged.CollectionChanged -= ValueCollectionChangedHandler;
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "modifiers")]
            internal VariableModifiers SerializedModifiers
            {
                get { return _modifiers; }
                set { _modifiers = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "isHandle")]
            internal bool SerializedIsHandle
            {
                get { return _isHandle; }
                set { _isHandle = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "isWaitingOnDefaultValue")]
            internal bool SerializedIsWaitingOnDefaultValue
            {
                get { return _isWaitingOnDefaultValue; }
                set { _isWaitingOnDefaultValue = value; }
            }

            internal override bool CanBeMapped
            {
                get
                {
                    return VariableModifiersHelper.IsMappable(_modifiers);
                }
            }

            public override T Value
            {
                get
                {
                    return base.Value;
                }
                set
                {
                    if (_isHandle)
                    {
                        Handle currentValue = base.Value as Handle;

                        // We only allow sets on null or uninitialized handles
                        if (currentValue != null && currentValue.IsInitialized)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
                        }

                        // We only allow setting it to null
                        if (value != null)
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
                        }
                    }

                    if (VariableModifiersHelper.IsReadOnly(_modifiers))
                    {
                        if (_isWaitingOnDefaultValue)
                        {
                            _isWaitingOnDefaultValue = false;
                        }
                        else
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(
                                new InvalidOperationException(SR.ConstVariableCannotBeSet));
                        }
                    }

                    base.Value = value;
                    NotifyPropertyChanged();
                }
            }

            private NotifyCollectionChangedEventHandler ValueCollectionChangedHandler
            {
                get
                {
                    if (_valueCollectionChanged == null)
                    {
                        _valueCollectionChanged = new NotifyCollectionChangedEventHandler(this.NotifyValueCollectionChanged);
                    }
                    return _valueCollectionChanged;
                }
            }

            private PropertyChangedEventHandler ValuePropertyChangedHandler
            {
                get
                {
                    if (_valuePropertyChanged == null)
                    {
                        _valuePropertyChanged = new PropertyChangedEventHandler(this.NotifyValuePropertyChanged);
                    }
                    return _valuePropertyChanged;
                }
            }

            internal void SetInitialValue(T value)
            {
                base.Value = value;
            }

            internal void SetIsWaitingOnDefaultValue()
            {
                if (VariableModifiersHelper.IsReadOnly(_modifiers))
                {
                    _isWaitingOnDefaultValue = true;
                }
            }

            private void NotifyValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyPropertyChanged();
            }

            private void NotifyValuePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                PropertyChangedEventHandler handler = _propertyChanged;
                if (handler != null)
                {
                    handler(this, e);
                }
            }

            private void NotifyPropertyChanged()
            {
                PropertyChangedEventHandler handler = _propertyChanged;
                if (handler != null)
                {
                    handler(this, ActivityUtilities.ValuePropertyChangedEventArgs);
                }
            }
        }
    }
}

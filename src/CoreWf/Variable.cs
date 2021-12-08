// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;

namespace System.Activities;
using Expressions;
using Internals;
using Runtime;
using Validation;

[DebuggerDisplay("Name = {Name}, Type = {Type}")]
public abstract class Variable : LocationReference
{
    private VariableModifiers _modifiers;
    private string _name;
    private int _cacheId;

    internal Variable()
        : base()
    {
        Id = -1;
    }

    internal bool IsHandle { get; set; }

    [DefaultValue(null)]
    public new string Name
    {
        get => _name;
        set => _name = value;
    }

    [DefaultValue(VariableModifiers.None)]
    public VariableModifiers Modifiers
    {
        get => _modifiers;
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
        get => DefaultCore;
        set => DefaultCore = value;
    }

    protected override string NameCore => _name;

    internal int CacheId => _cacheId;

    internal abstract ActivityWithResult DefaultCore { get; set; }

    internal bool IsPublic { get; set; }

    internal object Origin { get; set; }

    internal Activity Owner { get; private set; }

    internal bool IsInTree => Owner != null;

    public static Variable Create(string name, Type type, VariableModifiers modifiers) => ActivityUtilities.CreateVariable(name, type, modifiers);

    internal bool InitializeRelationship(Activity parent, bool isPublic, ref IList<ValidationError> validationErrors)
    {
        if (_cacheId == parent.CacheId)
        {
            if (Owner != null)
            {
                ValidationError validationError = new(SR.VariableAlreadyInUseOnActivity(Name, parent.DisplayName, Owner.DisplayName), false, Name, parent);
                ActivityUtilities.Add(ref validationErrors, validationError);

                // Get out early since we've already initialized this variable.
                return false;
            }
        }

        Owner = parent;
        _cacheId = parent.CacheId;
        IsPublic = isPublic;

        if (Default != null)
        {
            ActivityWithResult expression = Default;

            if (expression is Argument.IExpressionWrapper wrapper)
            {
                expression = wrapper.InnerExpression;
            }

            if (expression.ResultType != Type)
            {
                ActivityUtilities.Add(
                    ref validationErrors,
                    new ValidationError(SR.VariableExpressionTypeMismatch(Name, Type, expression.ResultType), false, Name, parent));
            }

            return Default.InitializeRelationship(this, isPublic, ref validationErrors);
        }

        return true;
    }

    internal void ThrowIfNotInTree()
    {
        if (!IsInTree)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableNotOpen(Name, Type)));
        }
    }

    internal void ThrowIfHandle()
    {
        if (IsHandle)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
        }
    }

    public override Location GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        // No need to call context.ThrowIfDisposed explicitly since all
        // the methods/properties on the context will perform that check.

        ThrowIfNotInTree();

        Location location;
        if (!context.AllowChainedEnvironmentAccess)
        {
            if (IsPublic || !ReferenceEquals(Owner, context.Activity))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableOnlyAccessibleAtScopeOfDeclaration(context.Activity, Owner)));
            }

            if (!context.Environment.TryGetLocation(Id, out location))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(Name)));
            }
        }
        else
        {
            // No validations in the allow chained access case

            if (!context.Environment.TryGetLocation(Id, Owner, out location))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(Name)));
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
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        return context.GetValue<object>((LocationReference)this);
    }

    public void Set(ActivityContext context, object value)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.SetValue((LocationReference)this, value);
    }

    internal abstract Location DeclareLocation(ActivityExecutor executor, ActivityInstance instance);

    // This method exists for debugger use
    internal Location InternalGetLocation(LocationEnvironment environment)
    {
        Fx.Assert(IsInTree, "Variable must be opened");

        if (!environment.TryGetLocation(Id, Owner, out Location location))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.VariableDoesNotExist(Name)));
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
    private Activity<T> defaultExpression;

    public Variable()
        : base()
    {
        IsHandle = ActivityUtilities.IsHandle(typeof(T));
    }

    public Variable(Expression<Func<ActivityContext, T>> defaultExpression)
        : this()
    {
        if (defaultExpression != null)
        {
            Default = new LambdaValue<T>(defaultExpression);
        }
    }

    public Variable(string name, Expression<Func<ActivityContext, T>> defaultExpression)
        : this(defaultExpression)
    {
        if (!string.IsNullOrEmpty(name))
        {
            Name = name;
        }
    }

    public Variable(string name)
        : this()
    {
        if (!string.IsNullOrEmpty(name))
        {
            Name = name;
        }
    }

    public Variable(string name, T defaultValue)
        : this(name)
    {
        Default = new Literal<T>(defaultValue);
    }

    protected override Type TypeCore => typeof(T);

    [DefaultValue(null)]
    public new Activity<T> Default
    {
        get => defaultExpression;
        set
        {
            ThrowIfHandle();

            defaultExpression = value;
        }
    }

    internal override ActivityWithResult DefaultCore
    {
        get => Default;
        set
        {
            ThrowIfHandle();

            if (value == null)
            {
                defaultExpression = null;
                return;
            }

            defaultExpression = value is Activity<T> activity ? activity : new ActivityWithResultWrapper<T>(value);
        }
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public new Location<T> GetLocation(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
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
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        return context.GetValue<T>((LocationReference)this);
    }

    public void Set(ActivityContext context, T value)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.SetValue((LocationReference)this, value);
    }

    internal override Location DeclareLocation(ActivityExecutor executor, ActivityInstance instance)
    {
        VariableLocation variableLocation = new(Modifiers, IsHandle);

        if (IsHandle)
        {
            Fx.Assert(Default == null, "Default should be null");
            instance.Environment.DeclareHandle(this, variableLocation, instance);

            HandleInitializationContext context = new(executor, instance);
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
        Fx.Assert(Default.UseOldFastPath, "Should only be called for OldFastPath");
        VariableLocation variableLocation = (VariableLocation)location;

        T value = executor.ExecuteInResolutionContext(parentInstance, Default);
        variableLocation.SetInitialValue(value);
    }

    internal override void SetIsWaitingOnDefaultValue(Location location)
    {
        ((VariableLocation)location).SetIsWaitingOnDefaultValue();
    }

    internal override Location CreateLocation()
    {
        return new VariableLocation(Modifiers, IsHandle);
    }

    [DataContract]
    internal sealed class VariableLocation : Location<T>, INotifyPropertyChanged
    {
        private VariableModifiers modifiers;
        private bool isHandle;
        private bool isWaitingOnDefaultValue;
        private PropertyChangedEventHandler propertyChanged;
        private NotifyCollectionChangedEventHandler valueCollectionChanged;
        private PropertyChangedEventHandler valuePropertyChanged;

        public VariableLocation(VariableModifiers modifiers, bool isHandle)
            : base()
        {
            this.modifiers = modifiers;
            this.isHandle = isHandle;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged += value;
                if (Value is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += ValuePropertyChangedHandler;
                }
                if (Value is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged += ValueCollectionChangedHandler;
                }
            }
            remove
            {
                propertyChanged -= value;
                if (Value is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged -= ValuePropertyChangedHandler;
                }
                if (Value is INotifyCollectionChanged notifyCollectionChanged)
                {
                    notifyCollectionChanged.CollectionChanged -= ValueCollectionChangedHandler;
                }
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "modifiers")]
        internal VariableModifiers SerializedModifiers
        {
            get => modifiers;
            set => modifiers = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "isHandle")]
        internal bool SerializedIsHandle
        {
            get => isHandle;
            set => isHandle = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "isWaitingOnDefaultValue")]
        internal bool SerializedIsWaitingOnDefaultValue
        {
            get => isWaitingOnDefaultValue;
            set => isWaitingOnDefaultValue = value;
        }

        internal override bool CanBeMapped => VariableModifiersHelper.IsMappable(modifiers);

        public override T Value
        {
            get => base.Value;
            set
            {
                if (isHandle)
                {

                    // We only allow sets on null or uninitialized handles
                    if (base.Value is Handle currentValue && currentValue.IsInitialized)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
                    }

                    // We only allow setting it to null
                    if (value != null)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPerformOperationOnHandle));
                    }
                }

                if (VariableModifiersHelper.IsReadOnly(modifiers))
                {
                    if (isWaitingOnDefaultValue)
                    {
                        isWaitingOnDefaultValue = false;
                    }
                    else
                    {
                        throw FxTrace.Exception.AsError(
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
                valueCollectionChanged ??= new NotifyCollectionChangedEventHandler(NotifyValueCollectionChanged);
                return valueCollectionChanged;
            }
        }

        private PropertyChangedEventHandler ValuePropertyChangedHandler
        {
            get
            {
                valuePropertyChanged ??= new PropertyChangedEventHandler(NotifyValuePropertyChanged);
                return valuePropertyChanged;
            }
        }

        internal void SetInitialValue(T value) => base.Value = value;

        internal void SetIsWaitingOnDefaultValue()
        {
            if (VariableModifiersHelper.IsReadOnly(modifiers))
            {
                isWaitingOnDefaultValue = true;
            }
        }

        private void NotifyValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => NotifyPropertyChanged();

        private void NotifyValuePropertyChanged(object sender, PropertyChangedEventArgs e) => propertyChanged?.Invoke(this, e);

        private void NotifyPropertyChanged() => propertyChanged?.Invoke(this, ActivityUtilities.ValuePropertyChangedEventArgs);
    }
}

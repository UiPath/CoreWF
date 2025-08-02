// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;
using Internals;
using Runtime;
using Validation;

[Fx.Tag.XamlVisible(false)]
public sealed class RuntimeArgument : LocationReference
{
    private static InternalEvaluationOrderComparer evaluationOrderComparer;
    private static readonly ReadOnlyCollection<string> emptyOverloadGroups = new(Array.Empty<string>());

    private Argument _boundArgument;
    private readonly PropertyDescriptor _bindingProperty;
    private readonly object _bindingPropertyOwner;
    private List<string> _overloadGroupNames;
    private ReadOnlyCollection<string> _overloadGroupNamesReadOnly;
    private int _cacheId;
    private readonly string _name;
    private uint _nameHash;
    private bool _isNameHashSet;
    private readonly Type _type;

    public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction)
        : this(name, argumentType, direction, false) { }

    public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, List<string> overloadGroupNames)
        : this(name, argumentType, direction, false, overloadGroupNames) { }

    public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired)
        : this(name, argumentType, direction, isRequired, null) { }

    public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired, List<string> overloadGroupNames)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(name));
        }

        ArgumentDirectionHelper.Validate(direction, "direction");

        _name = name;
        _type = argumentType ?? throw FxTrace.Exception.ArgumentNull(nameof(argumentType));
        Direction = direction;
        IsRequired = isRequired;
        _overloadGroupNames = overloadGroupNames;
    }

    internal RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired, List<string> overloadGroups, PropertyDescriptor bindingProperty, object propertyOwner)
        : this(name, argumentType, direction, isRequired, overloadGroups)
    {
        _bindingProperty = bindingProperty;
        _bindingPropertyOwner = propertyOwner;
    }

    internal RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired, List<string> overloadGroups, Argument argument)
        : this(name, argumentType, direction, isRequired, overloadGroups)
    {
        Fx.Assert(argument != null, "This ctor is only for arguments discovered via reflection in an IDictionary and therefore cannot be null.");

        // Bind straightway since we're not dealing with a property and empty binding isn't an issue.
        Argument.Bind(argument, this);
    }

    internal static IComparer<RuntimeArgument> EvaluationOrderComparer
    {
        get
        {
            evaluationOrderComparer ??= new InternalEvaluationOrderComparer();
            return evaluationOrderComparer;
        }
    }

    protected override string NameCore => _name;

    protected override Type TypeCore => _type;

    public ArgumentDirection Direction { get; private set; }

    internal string DisplayName => _bindingProperty?.DisplayName ?? Name;

    public bool IsRequired { get; private set; }

    public ReadOnlyCollection<string> OverloadGroupNames
    {
        get
        {
            if (_overloadGroupNamesReadOnly == null)
            {
                if (_overloadGroupNames == null || _overloadGroupNames.Count == 0)
                {
                    _overloadGroupNamesReadOnly = emptyOverloadGroups;
                }
                else
                {
                    _overloadGroupNamesReadOnly = new ReadOnlyCollection<string>(_overloadGroupNames);
                }
            }

            return _overloadGroupNamesReadOnly;
        }
    }

    internal Activity Owner { get; private set; }

    internal bool IsInTree => Owner != null;

    internal bool IsBound => _boundArgument != null;

    internal bool IsEvaluationOrderSpecified => IsBound && BoundArgument.EvaluationOrder != Argument.UnspecifiedEvaluationOrder;

    internal Argument BoundArgument
    {
        get => _boundArgument;
        // We allow this to be set an unlimited number of times.  We also allow it
        // to be set back to null.  
        set => _boundArgument = value;
    }

    // returns true if this is the "Result" argument of an Activity<T>
    internal bool IsResult
    {
        get
        {
            Fx.Assert(Owner != null, "should only be called when argument is bound");
            return Owner.IsResultArgument(this);
        }
    }

    internal void SetupBinding(Activity owningElement, bool createEmptyBinding)
    {
        if (_bindingProperty != null)
        {
            Argument argument = (Argument)_bindingProperty.GetValue(_bindingPropertyOwner);

            if (argument == null)
            {
                Fx.Assert(_bindingProperty.PropertyType.IsGenericType, "We only support arguments that are generic types in our reflection walk.");

                argument = (Argument)Activator.CreateInstance(_bindingProperty.PropertyType);
                argument.WasDesignTimeNull = true;

                if (createEmptyBinding && !_bindingProperty.IsReadOnly)
                {
                    _bindingProperty.SetValue(_bindingPropertyOwner, argument);
                }
            }

            Argument.Bind(argument, this);
        }
        else if (!IsBound)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(owningElement);

            PropertyDescriptor targetProperty = null;

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyDescriptor property = properties[i];

                // We only support auto-setting the property
                // for generic types.  Otherwise we have no
                // guarantee that the argument returned by the
                // property still matches the runtime argument's
                // type.
                if (property.Name == Name && property.PropertyType.IsGenericType)
                {
                    if (ActivityUtilities.TryGetArgumentDirectionAndType(property.PropertyType, out ArgumentDirection direction, out Type argumentType))
                    {
                        if (Type == argumentType && Direction == direction)
                        {
                            targetProperty = property;
                            break;
                        }
                    }
                }
            }

            Argument argument = null;

            if (targetProperty != null)
            {
                argument = (Argument)targetProperty.GetValue(owningElement);
            }

            if (argument == null)
            {
                if (targetProperty != null)
                {
                    if (targetProperty.PropertyType.IsGenericType)
                    {
                        argument = (Argument)Activator.CreateInstance(targetProperty.PropertyType);
                    }
                    else
                    {
                        argument = ActivityUtilities.CreateArgument(Type, Direction);
                    }

                }
                else
                {
                    argument = ActivityUtilities.CreateArgument(Type, Direction);
                }

                argument.WasDesignTimeNull = true;

                if (targetProperty != null && createEmptyBinding && !targetProperty.IsReadOnly)
                {
                    targetProperty.SetValue(owningElement, argument);
                }
            }

            Argument.Bind(argument, this);
        }

        Fx.Assert(IsBound, "We should always be bound when exiting this method.");
    }

    internal bool InitializeRelationship(Activity parent, ref IList<ValidationError> validationErrors)
    {
        if (_cacheId == parent.CacheId)
        {
            // We're part of the same tree walk
            if (Owner == parent)
            {
                ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.ArgumentIsAddedMoreThanOnce(Name, Owner.DisplayName)));

                // Get out early since we've already initialized this argument.
                return false;
            }

            Fx.Assert(Owner != null, "We must have already assigned an owner.");

            ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.ArgumentAlreadyInUse(Name, Owner.DisplayName, parent.DisplayName)));

            // Get out early since we've already initialized this argument.
            return false;
        }

        if (_boundArgument != null && _boundArgument.RuntimeArgument != this)
        {
            ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.RuntimeArgumentBindingInvalid(Name, _boundArgument.RuntimeArgument.Name)));

            return false;
        }

        Owner = parent;
        _cacheId = parent.CacheId;

        if (_boundArgument != null)
        {
            _boundArgument.Validate(parent, ref validationErrors);

            if (!BoundArgument.IsEmpty)
            {
                return BoundArgument.Expression.InitializeRelationship(this, ref validationErrors);
            }
        }

        return true;
    }

    internal bool TryPopulateValue(LocationEnvironment targetEnvironment, ActivityInstance targetActivityInstance, ActivityExecutor executor, object argumentValueOverride, Location resultLocation, bool skipFastPath)
    {
        // We populate values in the following order:
        //   Override
        //   Binding
        //   Default

        Fx.Assert(IsBound, "We should ALWAYS be bound at runtime.");
        if (argumentValueOverride != null)
        {
            Fx.Assert(
                resultLocation == null,
                "We should never have both an override and a result location unless some day " +
                "we decide to allow overrides for argument expressions.  If that day comes, we " +
                "need to deal with potential issues around someone providing and override for " +
                "a result - with the current code it wouldn't end up in the resultLocation.");

            Location location = _boundArgument.CreateDefaultLocation();
            targetEnvironment.Declare(this, location, targetActivityInstance);
            location.Value = argumentValueOverride;
            return true;
        }
        else if (!_boundArgument.IsEmpty)
        {
            if (skipFastPath)
            {
                BoundArgument.Declare(targetEnvironment, targetActivityInstance);
                return false;
            }
            else
            {
                return _boundArgument.TryPopulateValue(targetEnvironment, targetActivityInstance, executor);
            }
        }
        else if (resultLocation != null && IsResult)
        {
            targetEnvironment.Declare(this, resultLocation, targetActivityInstance);
            return true;
        }
        else
        {
            Location location = _boundArgument.CreateDefaultLocation();
            targetEnvironment.Declare(this, location, targetActivityInstance);
            return true;
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
            if (!ReferenceEquals(Owner, context.Activity))
            {
                throw FxTrace.Exception.AsError(
                    new InvalidOperationException(SR.CanOnlyGetOwnedArguments(
                        context.Activity.DisplayName,
                        Name,
                        Owner.DisplayName)));

            }

            if (ReferenceEquals(context.Environment.Definition, context.Activity))
            {
                if (!context.Environment.TryGetLocation(Id, out location))
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(Name)));
                }
            }
            else
            {
                Fx.Assert(Owner.IsFastPath, "If an activity defines an argument, then it should define an environment, unless it's SkipArgumentResolution");
                Fx.Assert(IsResult, "The only user-accessible argument that a SkipArgumentResolution activity can have is its result");
                // We need to give the activity access to its result argument because, if it has
                // no other arguments, it might have been implicitly opted into SkipArgumentResolution
                location = context.GetIgnorableResultLocation(this);
            }
        }
        else
        {
            Fx.Assert(ReferenceEquals(Owner, context.Activity) || ReferenceEquals(Owner, context.Activity.MemberOf.Owner),
                "This should have been validated by the activity which set AllowChainedEnvironmentAccess.");

            if (!context.Environment.TryGetLocation(Id, Owner, out location))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(Name)));
            }
        }

        return location;
    }

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public object Get(ActivityContext context) => context.GetValue<object>(this);

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public T Get<T>(ActivityContext context) => context.GetValue<T>(this);

    public void Set(ActivityContext context, object value) => context.SetValue(this, value);

    // This method exists for the Debugger
    internal Location InternalGetLocation(LocationEnvironment environment)
    {
        Fx.Assert(IsInTree, "Argument must be opened");

        if (!environment.TryGetLocation(Id, Owner, out Location location))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(Name)));
        }
        return location;
    }

    private ValidationError ProcessViolation(Activity owner, string errorMessage)
    {
        return new ValidationError(errorMessage, false, Name)
        {
            Source = owner,
            Id = owner.Id
        };
    }

    internal void ThrowIfNotInTree()
    {
        if (!IsInTree)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeArgumentNotOpen(Name)));
        }
    }

    private void EnsureHash()
    {
        if (!_isNameHashSet)
        {
            _nameHash = CRCHashCode.Calculate(Name);
            _isNameHashSet = true;
        }
    }

    // This class implements iSCSI CRC-32 check outlined in IETF RFC 3720.
    // it's marked internal so that DataModel CIT can access it
    internal static class CRCHashCode
    {
        // Reflected value for iSCSI CRC-32 polynomial 0x1edc6f41
        private const uint Polynomial = 0x82f63b78;

        public unsafe static uint Calculate(string s)
        {
            uint result = 0xffffffff;
            int byteLength = s.Length * sizeof(char);

            fixed (char* pString = s)
            {
                byte* pbString = (byte*)pString;
                for (int i = 0; i < byteLength; i++)
                {
                    result ^= pbString[i];
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                    result = ((result & 1) * Polynomial) ^ (result >> 1);
                }
            }
            return ~result;
        }

    }

    private class InternalEvaluationOrderComparer : IComparer<RuntimeArgument>
    {
        public int Compare(RuntimeArgument x, RuntimeArgument y)
        {
            if (!x.IsEvaluationOrderSpecified)
            {
                return y.IsEvaluationOrderSpecified ? -1 : CompareNameHashes(x, y);
            }
            else
            {
                return y.IsEvaluationOrderSpecified ? x.BoundArgument.EvaluationOrder.CompareTo(y.BoundArgument.EvaluationOrder) : 1;
            }
        }

        private static int CompareNameHashes(RuntimeArgument x, RuntimeArgument y)
        {
            x.EnsureHash();
            y.EnsureHash();

            return x._nameHash != y._nameHash
                ? x._nameHash.CompareTo(y._nameHash)
                : string.Compare(x.Name, y.Name, StringComparison.CurrentCulture);
        }
    }
}

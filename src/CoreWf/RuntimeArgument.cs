// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Validation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class RuntimeArgument : LocationReference
    {
        private static InternalEvaluationOrderComparer s_evaluationOrderComparer;
        private Argument _boundArgument;
        //PropertyDescriptor bindingProperty;
        //object bindingPropertyOwner;        
        private List<string> _overloadGroupNames;
        private int _cacheId;
        private string _name;
        private UInt32 _nameHash;
        private bool _isNameHashSet;
        private Type _type;

        public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction)
            : this(name, argumentType, direction, false)
        {
        }

        public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, List<string> overloadGroupNames)
            : this(name, argumentType, direction, false, overloadGroupNames)
        {
        }

        public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired)
            : this(name, argumentType, direction, isRequired, null)
        {
        }

        public RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired, List<string> overloadGroupNames)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty("name");
            }

            if (argumentType == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("argumentType");
            }

            ArgumentDirectionHelper.Validate(direction, "direction");

            _name = name;
            _type = argumentType;
            this.Direction = direction;
            this.IsRequired = isRequired;
            _overloadGroupNames = overloadGroupNames;
        }

        //internal RuntimeArgument(string name, Type argumentType, ArgumentDirection direction, bool isRequired, List<string> overloadGroups, PropertyDescriptor bindingProperty, object propertyOwner)
        //    : this(name, argumentType, direction, isRequired, overloadGroups)
        //{
        //    this.bindingProperty = bindingProperty;
        //    this.bindingPropertyOwner = propertyOwner;
        //}

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
                if (RuntimeArgument.s_evaluationOrderComparer == null)
                {
                    RuntimeArgument.s_evaluationOrderComparer = new InternalEvaluationOrderComparer();
                }
                return RuntimeArgument.s_evaluationOrderComparer;
            }
        }

        protected override string NameCore
        {
            get
            {
                return _name;
            }
        }

        protected override Type TypeCore
        {
            get
            {
                return _type;
            }
        }

        public ArgumentDirection Direction
        {
            get;
            private set;
        }

        public bool IsRequired
        {
            get;
            private set;
        }

        public ReadOnlyCollection<string> OverloadGroupNames
        {
            get
            {
                if (_overloadGroupNames == null)
                {
                    _overloadGroupNames = new List<string>(0);
                }

                return new ReadOnlyCollection<string>(_overloadGroupNames);
            }
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

        internal bool IsBound
        {
            get
            {
                return _boundArgument != null;
            }
        }

        internal bool IsEvaluationOrderSpecified
        {
            get
            {
                return this.IsBound && this.BoundArgument.EvaluationOrder != Argument.UnspecifiedEvaluationOrder;
            }
        }

        internal Argument BoundArgument
        {
            get
            {
                return _boundArgument;
            }
            set
            {
                // We allow this to be set an unlimited number of times.  We also allow it
                // to be set back to null.                
                _boundArgument = value;
            }
        }

        // returns true if this is the "Result" argument of an Activity<T>
        internal bool IsResult
        {
            get
            {
                Fx.Assert(this.Owner != null, "should only be called when argument is bound");
                return this.Owner.IsResultArgument(this);
            }
        }

        internal void SetupBinding(Activity owningElement, bool createEmptyBinding)
        {
            if (!this.IsBound)
            {
                PropertyInfo targetProperty = null;
                foreach (var property in owningElement.GetType().GetProperties())
                {
                    if (property.Name == this.Name && property.PropertyType.GetTypeInfo().IsGenericType)
                    {
                        ArgumentDirection direction;
                        Type argumentType;
                        if (ActivityUtilities.TryGetArgumentDirectionAndType(property.PropertyType, out direction, out argumentType))
                        {
                            if (this.Type == argumentType && this.Direction == direction)
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
                        if (targetProperty.PropertyType.GetTypeInfo().IsGenericType)
                        {
                            argument = (Argument)Activator.CreateInstance(targetProperty.PropertyType);
                        }
                        else
                        {
                            argument = ActivityUtilities.CreateArgument(this.Type, this.Direction);
                        }
                    }
                    else
                    {
                        argument = ActivityUtilities.CreateArgument(this.Type, this.Direction);
                    }

                    argument.WasDesignTimeNull = true;

                    if (targetProperty != null && createEmptyBinding && targetProperty.CanWrite)
                    {
                        targetProperty.SetValue(owningElement, argument);
                    }
                }

                Argument.Bind(argument, this);
            }

            Fx.Assert(this.IsBound, "We should always be bound when exiting this method.");
        }

        internal bool InitializeRelationship(Activity parent, ref IList<ValidationError> validationErrors)
        {
            if (_cacheId == parent.CacheId)
            {
                // We're part of the same tree walk
                if (this.Owner == parent)
                {
                    ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.ArgumentIsAddedMoreThanOnce(this.Name, this.Owner.DisplayName)));

                    // Get out early since we've already initialized this argument.
                    return false;
                }

                Fx.Assert(this.Owner != null, "We must have already assigned an owner.");

                ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.ArgumentAlreadyInUse(this.Name, this.Owner.DisplayName, parent.DisplayName)));

                // Get out early since we've already initialized this argument.
                return false;
            }

            if (_boundArgument != null && _boundArgument.RuntimeArgument != this)
            {
                ActivityUtilities.Add(ref validationErrors, ProcessViolation(parent, SR.RuntimeArgumentBindingInvalid(this.Name, _boundArgument.RuntimeArgument.Name)));

                return false;
            }

            this.Owner = parent;
            _cacheId = parent.CacheId;

            if (_boundArgument != null)
            {
                _boundArgument.Validate(parent, ref validationErrors);

                if (!this.BoundArgument.IsEmpty)
                {
                    return this.BoundArgument.Expression.InitializeRelationship(this, ref validationErrors);
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

            Fx.Assert(this.IsBound, "We should ALWAYS be bound at runtime.");
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
                    this.BoundArgument.Declare(targetEnvironment, targetActivityInstance);
                    return false;
                }
                else
                {
                    return _boundArgument.TryPopulateValue(targetEnvironment, targetActivityInstance, executor);
                }
            }
            else if (resultLocation != null && this.IsResult)
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
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("context");
            }

            // No need to call context.ThrowIfDisposed explicitly since all
            // the methods/properties on the context will perform that check.

            ThrowIfNotInTree();

            Location location;
            if (!context.AllowChainedEnvironmentAccess)
            {
                if (!object.ReferenceEquals(this.Owner, context.Activity))
                {
                    throw CoreWf.Internals.FxTrace.Exception.AsError(
                        new InvalidOperationException(SR.CanOnlyGetOwnedArguments(
                            context.Activity.DisplayName,
                            this.Name,
                            this.Owner.DisplayName)));
                }

                if (object.ReferenceEquals(context.Environment.Definition, context.Activity))
                {
                    if (!context.Environment.TryGetLocation(this.Id, out location))
                    {
                        throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(this.Name)));
                    }
                }
                else
                {
                    Fx.Assert(this.Owner.IsFastPath, "If an activity defines an argument, then it should define an environment, unless it's SkipArgumentResolution");
                    Fx.Assert(this.IsResult, "The only user-accessible argument that a SkipArgumentResolution activity can have is its result");
                    // We need to give the activity access to its result argument because, if it has
                    // no other arguments, it might have been implicitly opted into SkipArgumentResolution
                    location = context.GetIgnorableResultLocation(this);
                }
            }
            else
            {
                Fx.Assert(object.ReferenceEquals(this.Owner, context.Activity) || object.ReferenceEquals(this.Owner, context.Activity.MemberOf.Owner),
                    "This should have been validated by the activity which set AllowChainedEnvironmentAccess.");

                if (!context.Environment.TryGetLocation(this.Id, this.Owner, out location))
                {
                    throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(this.Name)));
                }
            }

            return location;
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public object Get(ActivityContext context)
        {
            return context.GetValue<object>(this);
        }

        // Soft-Link: This method is referenced through reflection by
        // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
        // file if the signature changes.
        public T Get<T>(ActivityContext context)
        {
            return context.GetValue<T>(this);
        }

        public void Set(ActivityContext context, object value)
        {
            context.SetValue(this, value);
        }

        // This method exists for the Debugger
        internal Location InternalGetLocation(LocationEnvironment environment)
        {
            Fx.Assert(this.IsInTree, "Argument must be opened");

            Location location;
            if (!environment.TryGetLocation(this.Id, this.Owner, out location))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ArgumentDoesNotExistInEnvironment(this.Name)));
            }
            return location;
        }

        private ValidationError ProcessViolation(Activity owner, string errorMessage)
        {
            return new ValidationError(errorMessage, false, this.Name)
            {
                Source = owner,
                Id = owner.Id
            };
        }

        internal void ThrowIfNotInTree()
        {
            if (!this.IsInTree)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeArgumentNotOpen(this.Name)));
            }
        }

        private void EnsureHash()
        {
            if (!_isNameHashSet)
            {
                //this.nameHash = CRCHashCode.Calculate(this.Name);
                _nameHash = (uint)this.Name.GetHashCode();
                _isNameHashSet = true;
            }
        }

        //// This class implements iSCSI CRC-32 check outlined in IETF RFC 3720.
        //// it's marked internal so that DataModel CIT can access it
        //internal static class CRCHashCode
        //{
        //    // Reflected value for iSCSI CRC-32 polynomial 0x1edc6f41
        //    const UInt32 polynomial = 0x82f63b78;

        //    [Fx.Tag.SecurityNote(Critical = "Critical because it is marked unsafe.",
        //        Safe = "Safe because we aren't leaking anything. We are just using pointers to get into the string.")]
        //    [SecuritySafeCritical]
        //    public unsafe static UInt32 Calculate(string s)
        //    {
        //        UInt32 result = 0xffffffff;
        //        int byteLength = s.Length * sizeof(char);

        //        fixed (char* pString = s)
        //        {
        //            byte* pbString = (byte*)pString;
        //            for (int i = 0; i < byteLength; i++)
        //            {
        //                result ^= pbString[i];
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //                result = ((result & 1) * polynomial) ^ (result >> 1);
        //            }
        //        }
        //        return ~result;
        //    }

        //}

        private class InternalEvaluationOrderComparer : IComparer<RuntimeArgument>
        {
            public int Compare(RuntimeArgument x, RuntimeArgument y)
            {
                if (!x.IsEvaluationOrderSpecified)
                {
                    if (y.IsEvaluationOrderSpecified)
                    {
                        return -1;
                    }
                    else
                    {
                        return CompareNameHashes(x, y);
                    }
                }
                else
                {
                    if (y.IsEvaluationOrderSpecified)
                    {
                        return x.BoundArgument.EvaluationOrder.CompareTo(y.BoundArgument.EvaluationOrder);
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

            private int CompareNameHashes(RuntimeArgument x, RuntimeArgument y)
            {
                x.EnsureHash();
                y.EnsureHash();

                if (x._nameHash != y._nameHash)
                {
                    return x._nameHash.CompareTo(y._nameHash);
                }
                else
                {
                    return string.Compare(x.Name, y.Name, StringComparison.CurrentCulture);
                }
            }
        }
    }
}

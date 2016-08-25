// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CoreWf.Expressions
{
    public sealed class ValueTypePropertyReference<TOperand, TResult> : CodeActivity<Location<TResult>>
    {
        private PropertyInfo _propertyInfo;
        private Func<object, object[], object> _getFunc;
        private Func<object, object[], object> _setFunc;

        private MethodInfo _getMethod;
        private MethodInfo _setMethod;

        private static MruCache<MethodInfo, Func<object, object[], object>> s_funcCache =
            new MruCache<MethodInfo, Func<object, object[], object>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static ReaderWriterLockSlim s_locker = new ReaderWriterLockSlim();

        [DefaultValue(null)]
        public string PropertyName
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public InOutArgument<TOperand> OperandLocation
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            MethodInfo oldGetMethod = _getMethod;
            MethodInfo oldSetMethod = _setMethod;

            if (!typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TypeMustbeValueType(typeof(TOperand).Name));
            }

            if (typeof(TOperand).GetTypeInfo().IsEnum)
            {
                metadata.AddValidationError(SR.TargetTypeCannotBeEnum(this.GetType().Name, this.DisplayName));
            }
            else if (String.IsNullOrEmpty(this.PropertyName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("PropertyName", this.DisplayName));
            }
            else
            {
                _propertyInfo = typeof(TOperand).GetProperty(this.PropertyName);
                if (_propertyInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(PropertyName, typeof(TOperand).Name));
                }
            }

            bool isRequired = false;
            if (_propertyInfo != null)
            {
                _setMethod = _propertyInfo.GetSetMethod();
                _getMethod = _propertyInfo.GetGetMethod();

                if (_setMethod == null)
                {
                    metadata.AddValidationError(SR.MemberIsReadOnly(_propertyInfo.Name, typeof(TOperand)));
                }
                if (_setMethod != null && !_setMethod.IsStatic)
                {
                    isRequired = true;
                }
            }
            MemberExpressionHelper.AddOperandLocationArgument<TOperand>(metadata, this.OperandLocation, isRequired);

            if (_propertyInfo != null)
            {
                if (MethodCallExpressionHelper.NeedRetrieve(_getMethod, oldGetMethod, _getFunc))
                {
                    _getFunc = MethodCallExpressionHelper.GetFunc(metadata, _getMethod, s_funcCache, s_locker);
                }
                if (MethodCallExpressionHelper.NeedRetrieve(_setMethod, oldSetMethod, _setFunc))
                {
                    _setFunc = MethodCallExpressionHelper.GetFunc(metadata, _setMethod, s_funcCache, s_locker, true);
                }
            }
        }

        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            Location<TOperand> operandLocationValue = this.OperandLocation.GetLocation(context);
            Fx.Assert(operandLocationValue != null, "OperandLocation must not be null");
            Fx.Assert(_propertyInfo != null, "propertyInfo must not be null");
            return new PropertyLocation(_propertyInfo, _getFunc, _setFunc, operandLocationValue);
        }

        [DataContract]
        internal class PropertyLocation : Location<TResult>
        {
            private Location<TOperand> _ownerLocation;

            private PropertyInfo _propertyInfo;

            private Func<object, object[], object> _getFunc;
            private Func<object, object[], object> _setFunc;

            public PropertyLocation(PropertyInfo propertyInfo, Func<object, object[], object> getFunc,
                Func<object, object[], object> setFunc, Location<TOperand> ownerLocation)
                : base()
            {
                _propertyInfo = propertyInfo;
                _ownerLocation = ownerLocation;

                _getFunc = getFunc;
                _setFunc = setFunc;
            }

            public override TResult Value
            {
                get
                {
                    // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                    // from the other's perspective, not internal properties, so they're okay as a special case.
                    // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                    // at an implementation level it is.
                    if (_getFunc != null)
                    {
                        return (TResult)_getFunc(_ownerLocation.Value, new object[0]);
                    }
                    if (_propertyInfo.GetGetMethod() == null && TypeHelper.AreTypesCompatible(_propertyInfo.DeclaringType, typeof(Location)) == false)
                    {
                        throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteonlyPropertyCannotBeRead(_propertyInfo.DeclaringType, _propertyInfo.Name)));
                    }

                    return (TResult)_propertyInfo.GetValue(_ownerLocation.Value, null);
                }
                set
                {
                    object copy = _ownerLocation.Value;
                    if (_getFunc != null)
                    {
                        copy = _setFunc(copy, new object[] { value });
                    }
                    else
                    {
                        _propertyInfo.SetValue(copy, value, null);
                    }
                    if (copy != null)
                    {
                        _ownerLocation.Value = (TOperand)copy;
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "ownerLocation")]
            internal Location<TOperand> SerializedOwnerLocation
            {
                get { return _ownerLocation; }
                set { _ownerLocation = value; }
            }

            [DataMember(Name = "propertyInfo")]
            internal PropertyInfo SerializedPropertyInfo
            {
                get { return _propertyInfo; }
                set { _propertyInfo = value; }
            }
        }
    }
}

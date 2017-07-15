// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace CoreWf.Expressions
{
    public sealed class PropertyReference<TOperand, TResult> : CodeActivity<Location<TResult>>
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

        public InArgument<TOperand> Operand
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            MethodInfo oldGetMethod = _getMethod;
            MethodInfo oldSetMethod = _setMethod;

            bool isRequired = false;
            if (typeof(TOperand).GetTypeInfo().IsEnum)
            {
                metadata.AddValidationError(SR.TargetTypeCannotBeEnum(this.GetType().Name, this.DisplayName));
            }
            else if (typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TargetTypeIsValueType(this.GetType().Name, this.DisplayName));
            }

            if (string.IsNullOrEmpty(this.PropertyName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("PropertyName", this.DisplayName));
            }
            else
            {
                Type operandType = typeof(TOperand);
                _propertyInfo = operandType.GetProperty(this.PropertyName);

                if (_propertyInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(PropertyName, typeof(TOperand).Name));
                }
                else
                {
                    _getMethod = _propertyInfo.GetGetMethod();
                    _setMethod = _propertyInfo.GetSetMethod();

                    // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                    // from the other's perspective, not internal properties, so they're okay as a special case.
                    // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                    // at an implementation level it is.
                    if (_setMethod == null && TypeHelper.AreTypesCompatible(_propertyInfo.DeclaringType, typeof(Location)) == false)
                    {
                        metadata.AddValidationError(SR.ReadonlyPropertyCannotBeSet(_propertyInfo.DeclaringType, _propertyInfo.Name));
                    }

                    if ((_getMethod != null && !_getMethod.IsStatic) || (_setMethod != null && !_setMethod.IsStatic))
                    {
                        isRequired = true;
                    }
                }
            }
            MemberExpressionHelper.AddOperandArgument(metadata, this.Operand, isRequired);
            if (_propertyInfo != null)
            {
                if (MethodCallExpressionHelper.NeedRetrieve(_getMethod, oldGetMethod, _getFunc))
                {
                    _getFunc = MethodCallExpressionHelper.GetFunc(metadata, _getMethod, s_funcCache, s_locker);
                }
                if (MethodCallExpressionHelper.NeedRetrieve(_setMethod, oldSetMethod, _setFunc))
                {
                    _setFunc = MethodCallExpressionHelper.GetFunc(metadata, _setMethod, s_funcCache, s_locker);
                }
            }
        }
        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            Fx.Assert(_propertyInfo != null, "propertyInfo must not be null");
            return new PropertyLocation<TResult>(_propertyInfo, _getFunc, _setFunc, this.Operand.Get(context));
        }

        [DataContract]
        internal class PropertyLocation<T> : Location<T>
        {
            private object _owner;

            private PropertyInfo _propertyInfo;

            private Func<object, object[], object> _getFunc;
            private Func<object, object[], object> _setFunc;

            public PropertyLocation(PropertyInfo propertyInfo, Func<object, object[], object> getFunc,
                Func<object, object[], object> setFunc, object owner)
                : base()
            {
                _propertyInfo = propertyInfo;
                _owner = owner;
                _getFunc = getFunc;
                _setFunc = setFunc;
            }

            public override T Value
            {
                get
                {
                    // Only allow access to public properties, EXCEPT that Locations are top-level variables 
                    // from the other's perspective, not internal properties, so they're okay as a special case.
                    // E.g. "[N]" from the user's perspective is not accessing a nonpublic property, even though
                    // at an implementation level it is.
                    if (_getFunc != null)
                    {
                        if (!_propertyInfo.GetGetMethod().IsStatic && _owner == null)
                        {
                            throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.NullReferencedMemberAccess(_propertyInfo.DeclaringType.Name, _propertyInfo.Name)));
                        }

                        return (T)_getFunc(_owner, new object[0]);
                    }
                    if (_propertyInfo.GetGetMethod() == null && TypeHelper.AreTypesCompatible(_propertyInfo.DeclaringType, typeof(Location)) == false)
                    {
                        throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.WriteonlyPropertyCannotBeRead(_propertyInfo.DeclaringType, _propertyInfo.Name)));
                    }

                    return (T)_propertyInfo.GetValue(_owner, null);
                }
                set
                {
                    if (_setFunc != null)
                    {
                        if (!_propertyInfo.GetSetMethod().IsStatic && _owner == null)
                        {
                            throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.NullReferencedMemberAccess(_propertyInfo.DeclaringType.Name, _propertyInfo.Name)));
                        }

                        _setFunc(_owner, new object[] { value });
                    }
                    else
                    {
                        _propertyInfo.SetValue(_owner, value, null);
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "owner")]
            internal object SerializedOwner
            {
                get { return _owner; }
                set { _owner = value; }
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

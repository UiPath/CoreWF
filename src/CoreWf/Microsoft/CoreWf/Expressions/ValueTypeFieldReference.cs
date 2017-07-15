// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace CoreWf.Expressions
{
    public sealed class ValueTypeFieldReference<TOperand, TResult> : CodeActivity<Location<TResult>>
    {
        private FieldInfo _fieldInfo;

        public ValueTypeFieldReference()
            : base()
        {
        }

        [DefaultValue(null)]
        public string FieldName
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
            bool isRequired = false;
            if (!typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TypeMustbeValueType(typeof(TOperand).Name));
            }
            if (typeof(TOperand).GetTypeInfo().IsEnum)
            {
                metadata.AddValidationError(SR.TargetTypeCannotBeEnum(this.GetType().Name, this.DisplayName));
            }
            if (string.IsNullOrEmpty(this.FieldName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", this.DisplayName));
            }
            else
            {
                _fieldInfo = typeof(TOperand).GetField(this.FieldName);
                isRequired = _fieldInfo != null && !_fieldInfo.IsStatic;
                if (_fieldInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(this.FieldName, typeof(TOperand).Name));
                }
                else if (_fieldInfo.IsInitOnly)
                {
                    metadata.AddValidationError(SR.MemberIsReadOnly(this.FieldName, typeof(TOperand).Name));
                }
            }

            MemberExpressionHelper.AddOperandLocationArgument<TOperand>(metadata, this.OperandLocation, isRequired);
        }

        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            Location<TOperand> operandLocationValue = this.OperandLocation.GetLocation(context);
            Fx.Assert(operandLocationValue != null, "OperandLocation must not be null");
            Fx.Assert(_fieldInfo != null, "fieldInfo must not be null.");
            return new FieldLocation(_fieldInfo, operandLocationValue);
        }

        [DataContract]
        internal class FieldLocation : Location<TResult>
        {
            private FieldInfo _fieldInfo;

            private Location<TOperand> _ownerLocation;

            public FieldLocation(FieldInfo fieldInfo, Location<TOperand> ownerLocation)
                : base()
            {
                _fieldInfo = fieldInfo;
                _ownerLocation = ownerLocation;
            }

            public override TResult Value
            {
                get
                {
                    return (TResult)_fieldInfo.GetValue(_ownerLocation.Value);
                }
                set
                {
                    object copy = _ownerLocation.Value;
                    _fieldInfo.SetValue(copy, value);
                    _ownerLocation.Value = (TOperand)copy;
                }
            }

            [DataMember(Name = "fieldInfo")]
            internal FieldInfo SerializedFieldInfo
            {
                get { return _fieldInfo; }
                set { _fieldInfo = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "ownerLocation")]
            internal Location<TOperand> SerializedOwnerLocation
            {
                get { return _ownerLocation; }
                set { _ownerLocation = value; }
            }
        }
    }
}

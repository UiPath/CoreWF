// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace CoreWf.Expressions
{
    public sealed class FieldReference<TOperand, TResult> : CodeActivity<Location<TResult>>
    {
        private FieldInfo _fieldInfo;

        public FieldReference()
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
        public InArgument<TOperand> Operand
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            bool isRequired = false;
            if (typeof(TOperand).GetTypeInfo().IsEnum)
            {
                metadata.AddValidationError(SR.TargetTypeCannotBeEnum(this.GetType().Name, this.DisplayName));
            }
            else if (typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TargetTypeIsValueType(this.GetType().Name, this.DisplayName));
            }

            if (string.IsNullOrEmpty(this.FieldName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", this.DisplayName));
            }
            else
            {
                Type operandType = typeof(TOperand);
                _fieldInfo = operandType.GetField(this.FieldName);

                if (_fieldInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(this.FieldName, typeof(TOperand).Name));
                }
                else
                {
                    if (_fieldInfo.IsInitOnly)
                    {
                        metadata.AddValidationError(SR.MemberIsReadOnly(this.FieldName, typeof(TOperand).Name));
                    }
                    isRequired = !_fieldInfo.IsStatic;
                }
            }
            MemberExpressionHelper.AddOperandArgument(metadata, this.Operand, isRequired);
        }

        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            Fx.Assert(_fieldInfo != null, "fieldInfo must not be null.");
            return new FieldLocation(_fieldInfo, this.Operand.Get(context));
        }

        [DataContract]
        internal class FieldLocation : Location<TResult>
        {
            private FieldInfo _fieldInfo;

            private object _owner;

            public FieldLocation(FieldInfo fieldInfo, object owner)
                : base()
            {
                _fieldInfo = fieldInfo;
                _owner = owner;
            }

            public override TResult Value
            {
                get
                {
                    //if (!this.fieldInfo.IsStatic && this.owner == null)
                    //{
                    //    // The field is non-static, and obj is a null reference 
                    //    if (this.fieldInfo.DeclaringType != null)
                    //    {
                    //        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(this.fieldInfo.DeclaringType.Name, this.fieldInfo.Name)));
                    //    }
                    //    else
                    //    {
                    //        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(typeof(FieldInfo), "DeclaringType")));
                    //    }
                    //}
                    return (TResult)_fieldInfo.GetValue(_owner);
                }
                set
                {
                    //if (!this.fieldInfo.IsStatic && this.owner == null)
                    //{
                    //    if (this.fieldInfo.DeclaringType != null)
                    //    {
                    //        // The field is non-static, and obj is a null reference 
                    //        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(this.fieldInfo.DeclaringType.Name, this.fieldInfo.Name)));
                    //    }
                    //    else
                    //    {
                    //        throw CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(typeof(FieldInfo), "DeclaringType")));
                    //    }
                    //}
                    _fieldInfo.SetValue(_owner, value);
                }
            }

            [DataMember(Name = "fieldInfo")]
            internal FieldInfo SerializedFieldInfo
            {
                get { return _fieldInfo; }
                set { _fieldInfo = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "owner")]
            internal object SerializedOwner
            {
                get { return _owner; }
                set { _owner = value; }
            }
        }
    }
}

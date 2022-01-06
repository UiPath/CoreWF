// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Reflection;

namespace System.Activities.Expressions;

public sealed class FieldReference<TOperand, TResult> : CodeActivity<Location<TResult>>
{
    private FieldInfo _fieldInfo;

    public FieldReference()
        : base() { }

    [DefaultValue(null)]
    public string FieldName { get; set; }

    [DefaultValue(null)]
    public InArgument<TOperand> Operand { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        bool isRequired = false;
        if (typeof(TOperand).IsEnum)
        {
            metadata.AddValidationError(SR.TargetTypeCannotBeEnum(GetType().Name, DisplayName));
        }
        else if (typeof(TOperand).IsValueType)
        {
            metadata.AddValidationError(SR.TargetTypeIsValueType(GetType().Name, DisplayName));
        }

        if (string.IsNullOrEmpty(FieldName))
        {
            metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", DisplayName));
        }
        else
        {
            Type operandType = typeof(TOperand);
            _fieldInfo = operandType.GetField(FieldName);

            if (_fieldInfo == null)
            {
                metadata.AddValidationError(SR.MemberNotFound(FieldName, typeof(TOperand).Name));
            }
            else
            {
                if (_fieldInfo.IsInitOnly)
                {
                    metadata.AddValidationError(SR.MemberIsReadOnly(FieldName, typeof(TOperand).Name));
                }
                isRequired = !_fieldInfo.IsStatic;
            }
        }
        MemberExpressionHelper.AddOperandArgument(metadata, Operand, isRequired);
    }

    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        Fx.Assert(_fieldInfo != null, "fieldInfo must not be null.");
        return new FieldLocation(_fieldInfo, Operand.Get(context));
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
            get =>
                //if (!this.fieldInfo.IsStatic && this.owner == null)
                //{
                //    // The field is non-static, and obj is a null reference 
                //    if (this.fieldInfo.DeclaringType != null)
                //    {
                //        throw FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(this.fieldInfo.DeclaringType.Name, this.fieldInfo.Name)));
                //    }
                //    else
                //    {
                //        throw FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(typeof(FieldInfo), "DeclaringType")));
                //    }
                //}
                (TResult)_fieldInfo.GetValue(_owner);
            set =>
                //if (!this.fieldInfo.IsStatic && this.owner == null)
                //{
                //    if (this.fieldInfo.DeclaringType != null)
                //    {
                //        // The field is non-static, and obj is a null reference 
                //        throw FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(this.fieldInfo.DeclaringType.Name, this.fieldInfo.Name)));
                //    }
                //    else
                //    {
                //        throw FxTrace.Exception.AsError(new ValidationException(SR.NullReferencedMemberAccess(typeof(FieldInfo), "DeclaringType")));
                //    }
                //}
                _fieldInfo.SetValue(_owner, value);
        }

        [DataMember(Name = "fieldInfo")]
        internal FieldInfo SerializedFieldInfo
        {
            get => _fieldInfo;
            set => _fieldInfo = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "owner")]
        internal object SerializedOwner
        {
            get => _owner;
            set => _owner = value;
        }
    }
}

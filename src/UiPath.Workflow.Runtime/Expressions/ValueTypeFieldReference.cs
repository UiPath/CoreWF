// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Reflection;

namespace System.Activities.Expressions;

public sealed class ValueTypeFieldReference<TOperand, TResult> : CodeActivity<Location<TResult>>
{
    private FieldInfo _fieldInfo;

    public ValueTypeFieldReference()
        : base() { }

    [DefaultValue(null)]
    public string FieldName { get; set; }

    [DefaultValue(null)]
    public InOutArgument<TOperand> OperandLocation { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        bool isRequired = false;
        if (!typeof(TOperand).IsValueType)
        {
            metadata.AddValidationError(SR.TypeMustbeValueType(typeof(TOperand).Name));
        }
        if (typeof(TOperand).IsEnum)
        {
            metadata.AddValidationError(SR.TargetTypeCannotBeEnum(GetType().Name, DisplayName));
        }
        if (string.IsNullOrEmpty(FieldName))
        {
            metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", DisplayName));
        }
        else
        {
            _fieldInfo = typeof(TOperand).GetField(FieldName);
            isRequired = _fieldInfo != null && !_fieldInfo.IsStatic;
            if (_fieldInfo == null)
            {
                metadata.AddValidationError(SR.MemberNotFound(FieldName, typeof(TOperand).Name));
            }
            else if (_fieldInfo.IsInitOnly)
            {
                metadata.AddValidationError(SR.MemberIsReadOnly(FieldName, typeof(TOperand).Name));
            }
        }

        MemberExpressionHelper.AddOperandLocationArgument(metadata, OperandLocation, isRequired);
    }

    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        Location<TOperand> operandLocationValue = OperandLocation.GetLocation(context);
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
            get => (TResult)_fieldInfo.GetValue(_ownerLocation.Value);
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
            get => _fieldInfo;
            set => _fieldInfo = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "ownerLocation")]
        internal Location<TOperand> SerializedOwnerLocation
        {
            get => _ownerLocation;
            set => _ownerLocation = value;
        }
    }
}

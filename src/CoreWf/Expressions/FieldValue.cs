// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Validation;
using System.Reflection;

namespace System.Activities.Expressions;

public sealed class FieldValue<TOperand, TResult> : CodeActivity<TResult>
{
    private Func<TOperand, TResult> _operationFunction;
    private bool _isOperationFunctionStatic;

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

        if (string.IsNullOrEmpty(FieldName))
        {
            metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", DisplayName));
        }
        else
        {
            Type operandType = typeof(TOperand);
            FieldInfo fieldInfo = operandType.GetField(FieldName);

            if (fieldInfo == null)
            {
                metadata.AddValidationError(SR.MemberNotFound(FieldName, typeof(TOperand).Name));
            }
            else
            {
                _isOperationFunctionStatic = fieldInfo.IsStatic;
                isRequired = !_isOperationFunctionStatic;

                if (!MemberExpressionHelper.TryGenerateLinqDelegate(FieldName, true, _isOperationFunctionStatic, out _operationFunction, out ValidationError validationError))
                {
                    metadata.AddValidationError(validationError);
                }
            }
        }
        MemberExpressionHelper.AddOperandArgument(metadata, Operand, isRequired);
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        TOperand operandValue = Operand.Get(context);

        if (!_isOperationFunctionStatic && operandValue == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Operand", GetType().Name, DisplayName)));
        }

        return _operationFunction(operandValue);
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Reflection;

namespace System.Activities.Expressions;

public sealed class PropertyValue<TOperand, TResult> : CodeActivity<TResult>
{
    private Func<TOperand, TResult> _operationFunction;
    private bool _isOperationFunctionStatic;

    public InArgument<TOperand> Operand { get; set; }

    [DefaultValue(null)]
    public string PropertyName { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        bool isRequired = false;
        if (typeof(TOperand).IsEnum)
        {
            metadata.AddValidationError(SR.TargetTypeCannotBeEnum(GetType().Name, DisplayName));
        }

        if (string.IsNullOrEmpty(PropertyName))
        {
            metadata.AddValidationError(SR.ActivityPropertyMustBeSet("PropertyName", DisplayName));
        }
        else
        {
            Type operandType = typeof(TOperand);
            PropertyInfo propertyInfo = operandType.GetProperty(PropertyName);

            if (propertyInfo == null)
            {
                metadata.AddValidationError(SR.MemberNotFound(PropertyName, typeof(TOperand).Name));
            }
            else
            {
                Fx.Assert(propertyInfo.GetAccessors().Length > 0, "Property should have at least 1 accessor.");

                _isOperationFunctionStatic = propertyInfo.GetAccessors()[0].IsStatic;
                isRequired = !_isOperationFunctionStatic;

                if (!MemberExpressionHelper.TryGenerateLinqDelegate(PropertyName, false, _isOperationFunctionStatic, out _operationFunction, out ValidationError validationError))
                {
                    metadata.AddValidationError(validationError);
                }

                MethodInfo getMethod = propertyInfo.GetGetMethod();
                MethodInfo setMethod = propertyInfo.GetSetMethod();

                if ((getMethod != null && !getMethod.IsStatic) || (setMethod != null && !setMethod.IsStatic))
                {
                    isRequired = true;
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

        TResult result = _operationFunction(operandValue);
        return result;
    }
}

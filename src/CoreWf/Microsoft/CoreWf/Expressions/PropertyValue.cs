// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Validation;
using System;
using System.ComponentModel;
using System.Reflection;

namespace CoreWf.Expressions
{
    public sealed class PropertyValue<TOperand, TResult> : CodeActivity<TResult>
    {
        private Func<TOperand, TResult> _operationFunction;
        private bool _isOperationFunctionStatic;

        public InArgument<TOperand> Operand
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public string PropertyName
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

            if (string.IsNullOrEmpty(this.PropertyName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("PropertyName", this.DisplayName));
            }
            else
            {
                PropertyInfo propertyInfo = null;
                Type operandType = typeof(TOperand);
                propertyInfo = operandType.GetProperty(this.PropertyName);

                if (propertyInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(this.PropertyName, typeof(TOperand).Name));
                }
                else
                {
                    Fx.Assert(propertyInfo.GetAccessors().Length > 0, "Property should have at least 1 accessor.");

                    _isOperationFunctionStatic = propertyInfo.GetAccessors()[0].IsStatic;
                    isRequired = !_isOperationFunctionStatic;

                    ValidationError validationError;
                    if (!MemberExpressionHelper.TryGenerateLinqDelegate(this.PropertyName, false, _isOperationFunctionStatic, out _operationFunction, out validationError))
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
            MemberExpressionHelper.AddOperandArgument(metadata, this.Operand, isRequired);
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            TOperand operandValue = this.Operand.Get(context);

            if (!_isOperationFunctionStatic && operandValue == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Operand", this.GetType().Name, this.DisplayName)));
            }

            TResult result = _operationFunction(operandValue);
            return result;
        }
    }
}

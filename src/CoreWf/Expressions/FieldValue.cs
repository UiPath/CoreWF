// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Validation;
using System;
using System.ComponentModel;
using System.Reflection;

namespace CoreWf.Expressions
{
    public sealed class FieldValue<TOperand, TResult> : CodeActivity<TResult>
    {
        private Func<TOperand, TResult> _operationFunction;
        private bool _isOperationFunctionStatic;

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

            if (string.IsNullOrEmpty(this.FieldName))
            {
                metadata.AddValidationError(SR.ActivityPropertyMustBeSet("FieldName", this.DisplayName));
            }
            else
            {
                FieldInfo fieldInfo = null;
                Type operandType = typeof(TOperand);
                fieldInfo = operandType.GetField(this.FieldName);

                if (fieldInfo == null)
                {
                    metadata.AddValidationError(SR.MemberNotFound(this.FieldName, typeof(TOperand).Name));
                }
                else
                {
                    _isOperationFunctionStatic = fieldInfo.IsStatic;
                    isRequired = !_isOperationFunctionStatic;

                    ValidationError validationError;
                    if (!MemberExpressionHelper.TryGenerateLinqDelegate(this.FieldName, true, _isOperationFunctionStatic, out _operationFunction, out validationError))
                    {
                        metadata.AddValidationError(validationError);
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

            return _operationFunction(operandValue);
        }
    }
}

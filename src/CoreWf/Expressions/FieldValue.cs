// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using CoreWf;
    using CoreWf.Internals;
    using CoreWf.Validation;
    using System;
    using System.ComponentModel;
    using System.Reflection;

    public sealed class FieldValue<TOperand, TResult> : CodeActivity<TResult>
    {
        private Func<TOperand, TResult> operationFunction;
        private bool isOperationFunctionStatic;

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

            if (typeof(TOperand).IsEnum)
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
                    this.isOperationFunctionStatic = fieldInfo.IsStatic;
                    isRequired = !this.isOperationFunctionStatic;

                    if (!MemberExpressionHelper.TryGenerateLinqDelegate(this.FieldName, true, this.isOperationFunctionStatic, out this.operationFunction, out ValidationError validationError))
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

            if (!this.isOperationFunctionStatic && operandValue == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Operand", this.GetType().Name, this.DisplayName)));
            }

            return this.operationFunction(operandValue);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Validation;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Microsoft.CoreWf.Expressions
{
    public sealed class Cast<TOperand, TResult> : CodeActivity<TResult>
    {
        //Lock is not needed for operationFunction here. The reason is that delegates for a given Cast<TLeft, TRight, TResult> are the same.
        //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
        private static Func<TOperand, TResult> s_checkedOperationFunction;
        private static Func<TOperand, TResult> s_uncheckedOperationFunction;
        private bool _checkedOperation = true;

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TOperand> Operand
        {
            get;
            set;
        }

        [DefaultValue(true)]
        public bool Checked
        {
            get { return _checkedOperation; }
            set { _checkedOperation = value; }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            UnaryExpressionHelper.OnGetArguments(metadata, this.Operand);

            if (_checkedOperation)
            {
                EnsureOperationFunction(metadata, ref s_checkedOperationFunction, ExpressionType.ConvertChecked);
            }
            else
            {
                EnsureOperationFunction(metadata, ref s_uncheckedOperationFunction, ExpressionType.Convert);
            }
        }

        private void EnsureOperationFunction(CodeActivityMetadata metadata,
            ref Func<TOperand, TResult> operationFunction,
            ExpressionType operatorType)
        {
            if (operationFunction == null)
            {
                ValidationError validationError;
                if (!UnaryExpressionHelper.TryGenerateLinqDelegate(
                            operatorType,
                            out operationFunction,
                            out validationError))
                {
                    metadata.AddValidationError(validationError);
                }
            }
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            TOperand operandValue = this.Operand.Get(context);

            //if user changed Checked flag between Open and Execution, 
            //a NRE may be thrown and that's by design
            if (_checkedOperation)
            {
                return s_checkedOperationFunction(operandValue);
            }
            else
            {
                return s_uncheckedOperationFunction(operandValue);
            }
        }
    }
}

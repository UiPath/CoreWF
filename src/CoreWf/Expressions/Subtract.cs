// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Validation;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace CoreWf.Expressions
{
    public sealed class Subtract<TLeft, TRight, TResult> : CodeActivity<TResult>
    {
        //Lock is not needed for operationFunction here. The reason is that delegates for a given Subtract<TLeft, TRight, TResult> are the same.
        //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
        private static Func<TLeft, TRight, TResult> s_checkedOperationFunction;
        private static Func<TLeft, TRight, TResult> s_uncheckedOperationFunction;
        private bool _checkedOperation = true;

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TLeft> Left
        {
            get;
            set;
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TRight> Right
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
            BinaryExpressionHelper.OnGetArguments(metadata, this.Left, this.Right);

            if (_checkedOperation)
            {
                EnsureOperationFunction(metadata, ref s_checkedOperationFunction, ExpressionType.SubtractChecked);
            }
            else
            {
                EnsureOperationFunction(metadata, ref s_uncheckedOperationFunction, ExpressionType.Subtract);
            }
        }

        private void EnsureOperationFunction(CodeActivityMetadata metadata,
            ref Func<TLeft, TRight, TResult> operationFunction,
            ExpressionType operatorType)
        {
            if (operationFunction == null)
            {
                ValidationError validationError;
                if (!BinaryExpressionHelper.TryGenerateLinqDelegate(
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
            TLeft leftValue = this.Left.Get(context);
            TRight rightValue = this.Right.Get(context);

            //if user changed Checked flag between Open and Execution, 
            //a NRE may be thrown and that's by design
            if (_checkedOperation)
            {
                return s_checkedOperationFunction(leftValue, rightValue);
            }
            else
            {
                return s_uncheckedOperationFunction(leftValue, rightValue);
            }
        }
    }
}

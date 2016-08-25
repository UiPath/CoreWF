// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Validation;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Microsoft.CoreWf.Expressions
{
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords,
    //Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [Not])")]
    public sealed class Not<TOperand, TResult> : CodeActivity<TResult>
    {
        //Lock is not needed for operationFunction here. The reason is that delegates for a given Not<TLeft, TRight, TResult> are the same.
        //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
        private static Func<TOperand, TResult> s_operationFunction;

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TOperand> Operand
        {
            get;
            set;
        }


        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            UnaryExpressionHelper.OnGetArguments(metadata, this.Operand);

            if (s_operationFunction == null)
            {
                ValidationError validationError;
                if (!UnaryExpressionHelper.TryGenerateLinqDelegate(ExpressionType.Not, out s_operationFunction, out validationError))
                {
                    metadata.AddValidationError(validationError);
                }
            }
        }
        protected override TResult Execute(CodeActivityContext context)
        {
            Fx.Assert(s_operationFunction != null, "OperationFunction must exist.");
            TOperand operandValue = this.Operand.Get(context);
            return s_operationFunction(operandValue);
        }
    }
}

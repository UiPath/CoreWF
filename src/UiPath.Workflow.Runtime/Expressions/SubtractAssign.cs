//
//  SubtractAssign.cs
//
//  Author:
//       Devin Duanne <dduanne@tafs.com>
//
//  Copyright (c) TAFS, LLC.
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Activities.Validation;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Numerics;

namespace System.Activities.Expressions
{
    /// <summary>
    /// A code activity which deccrementally assigns a numeral.
    /// </summary>
    /// <example>
    /// int myNum = 5;
    /// myNum += 5; // 10.
    /// </example>
    /// <typeparam name="TLeft">The operand to modify.</typeparam>
    /// <typeparam name="TRight">The operand with which to modify <typeparamref name="TLeft"/>.</typeparam>
    /// <typeparam name="TResult">The resulting type of hte operation.</typeparam>
    public sealed class SubtractAssign<TLeft, TRight, TResult> : CodeActivity<TResult>
#if NET7_0_OR_GREATER
        where TLeft : INumber<TLeft>
        where TRight : INumber<TRight>
        where TResult : INumber<TResult>
#endif
    {
        private static Func<TLeft, TRight, TResult>? checkedOperationFunction = null;
        private static Func<TLeft, TRight, TResult>? uncheckedOperationFunction = null;

        /// <summary>
        /// Gets or sets the left operand which will be modified by this operation.
        /// </summary>
        [RequiredArgument]
        public InArgument<TLeft> Left { get; set; } = new();

        /// <summary>
        /// Gets or sets the right operand which will be the modifier for this operation.
        /// </summary>
        [RequiredArgument]
        public InArgument<TRight> Right { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether this operation will happen in a checked or unchecked context.
        /// </summary>
        [DefaultValue(true)]
        public bool IsChecked { get; set; } = true;

        /// <inheritdoc/>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            BinaryExpressionHelper.OnGetArguments(metadata, Left, Right);

            if (IsChecked)
            {
                EnsureOperationFunction(metadata, ref checkedOperationFunction, ExpressionType.SubtractAssignChecked);
            }
            else
            {
                EnsureOperationFunction(metadata, ref uncheckedOperationFunction, ExpressionType.SubtractAssign);
            }
        }

        private static void EnsureOperationFunction
        (
            CodeActivityMetadata metadata,
            ref Func<TLeft, TRight, TResult>? operationFunction,
            ExpressionType operatorType
        )
        {
            if (operationFunction == null)
            {
                if (!BinaryExpressionHelper.TryGenerateLinqDelegate(
                    operatorType,
                    out operationFunction,
                    out ValidationError? validationError))
                {
                    metadata.AddValidationError(validationError);
                }
            }
        }

        /// <inheritdoc/>
        protected override TResult Execute(CodeActivityContext context)
        {
            TLeft leftValue = Left.Get(context);
            TRight rightValue = Right.Get(context);

            // If user changed checked flag between open and execution, an NRE may be thrown.
            // This is by design.
            // Nullability check silenced because the corresponding value is guaranteed to be
            // non-null
            return IsChecked
                ? checkedOperationFunction!(leftValue, rightValue)
                : uncheckedOperationFunction!(leftValue, rightValue);
        }
    }
}

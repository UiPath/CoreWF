//
//  Increment.cs
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
using System.Linq.Expressions;
using System.Numerics;

namespace System.Activities.Expressions
{
    /// <summary>
    /// A code activity which increments a numeral.
    /// </summary>
    /// <typeparam name="TNumeral">A numeric value, such as <see langword="int" /> or <see langword="long" />.</typeparam>
    public sealed class Increment<TNumeral> : CodeActivity<TNumeral>
#if NET7_0_OR_GREATER
        where TNumeral : IIncrementOperators<TNumeral>
#endif
    {
        private static Func<TNumeral, TNumeral>? operationFunction = null!;

        /// <summary>
        /// Gets or sets the numeral value that will be incremented.
        /// </summary>
        [RequiredArgument]
        public InOutArgument<TNumeral> Numeral { get; set; } = new();

        /// <inheritdoc/>
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            UnaryExpressionHelper.OnGetArguments<TNumeral>(metadata, Numeral);
            EnsureOperationFunction(metadata, ref operationFunction!, ExpressionType.Increment);
        }

        private static void EnsureOperationFunction
        (
            CodeActivityMetadata metadata,
            ref Func<TNumeral, TNumeral> operationFunction,
            ExpressionType operatorType
        )
        {
            if (operationFunction is null)
            {
                if (!UnaryExpressionHelper.TryGenerateLinqDelegate(operatorType, out operationFunction!, out ValidationError? validationError))
                {
                    metadata.AddValidationError(validationError);
                }
            }
        }

        /// <inheritdoc/>
        protected override TNumeral Execute(CodeActivityContext context)
        {
            TNumeral value = Numeral.Get(context);
            return operationFunction!.Invoke(value);
        }
    }
}

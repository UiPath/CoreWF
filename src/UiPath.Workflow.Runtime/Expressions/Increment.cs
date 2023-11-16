// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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

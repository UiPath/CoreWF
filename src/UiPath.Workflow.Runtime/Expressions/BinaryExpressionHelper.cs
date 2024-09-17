// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

/// <summary>
/// Helpers for binary expressions.
/// </summary>
public static class BinaryExpressionHelper
{
    /// <summary>
    /// Binds metadata when getting arguments.
    /// </summary>
    /// <typeparam name="TLeft">The type of the left argument.</typeparam>
    /// <typeparam name="TRight">The type of the right argument.</typeparam>
    /// <param name="metadata">The metadata.</param>
    /// <param name="left">The left argument.</param>
    /// <param name="right">The right argument.</param>
    public static void OnGetArguments<TLeft, TRight>(CodeActivityMetadata metadata, InArgument<TLeft> left, InArgument<TRight> right)
    {
        RuntimeArgument rightArgument = new("Right", typeof(TRight), ArgumentDirection.In, true);
        metadata.Bind(right, rightArgument);

        RuntimeArgument leftArgument = new("Left", typeof(TLeft), ArgumentDirection.In, true);
        metadata.Bind(left, leftArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                rightArgument,
                leftArgument
            });
    }

    /// <summary>
    /// Generates a <see cref="System.Linq"/> delegate.
    /// </summary>
    /// <typeparam name="TLeft">The type of the left argument.</typeparam>
    /// <typeparam name="TRight">The type of the right argument.</typeparam>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="operatorType">The type of expression.</param>
    /// <param name="function">The resulting <see cref="Func{T1, T2, TResult}"/>.</param>
    /// <param name="validationError">If the operation failed, the error.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryGenerateLinqDelegate<TLeft, TRight, TResult>(ExpressionType operatorType, out Func<TLeft, TRight, TResult> function, out ValidationError validationError)
    {
        function = null;
        validationError = null;

        ParameterExpression leftParameter = Expression.Parameter(typeof(TLeft), "left");
        ParameterExpression rightParameter = Expression.Parameter(typeof(TRight), "right");

        try
        {
            BinaryExpression binaryExpression = Expression.MakeBinary(operatorType, leftParameter, rightParameter);
            Expression<Func<TLeft, TRight, TResult>> lambdaExpression = Expression.Lambda<Func<TLeft, TRight, TResult>>(binaryExpression, leftParameter, rightParameter);
            function = lambdaExpression.Compile();

            return true;
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            validationError = new ValidationError(e.Message);
            return false;
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

/// <summary>
/// Helpers for unary expressions.
/// </summary>
public static class UnaryExpressionHelper
{
    /// <summary>
    /// Binds the metadata for the argument.
    /// </summary>
    /// <typeparam name="TOperand">The type of the argument.</typeparam>
    /// <param name="metadata">The metadata.</param>
    /// <param name="operand">The argument.</param>
    public static void OnGetArguments<TOperand>(CodeActivityMetadata metadata, InArgument<TOperand> operand)
    {
        RuntimeArgument operandArgument = new("Operand", typeof(TOperand), ArgumentDirection.In, true);
        metadata.Bind(operand, operandArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                operandArgument
            });
    }

    /// <summary>
    /// Binds the metadata for the argument.
    /// </summary>
    /// <typeparam name="TOperand">The type of the argument.</typeparam>
    /// <param name="metadata">The metadata.</param>
    /// <param name="operand">The argument.</param>
    public static void OnGetArguments<TOperand>(CodeActivityMetadata metadata, InOutArgument<TOperand> operand)
    {
        RuntimeArgument operandArgument = new("Operand", typeof(TOperand), ArgumentDirection.InOut, true);
        metadata.Bind(operand, operandArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                operandArgument
            });
    }

    /// <summary>
    /// Generates a <see cref="System.Linq"/> delegate.
    /// </summary>
    /// <typeparam name="TOperand">The type of the argument.</typeparam>
    /// <typeparam name="TResult">The return type of the operation.</typeparam>
    /// <param name="operatorType">The type of expression.</param>
    /// <param name="function">The resulting <see cref="Func{T1, T2, TResult}"/>.</param>
    /// <param name="validationError">If the operation failed, the error.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryGenerateLinqDelegate<TOperand, TResult>(ExpressionType operatorType, out Func<TOperand, TResult> operation, out ValidationError validationError)
    {
        operation = null;
        validationError = null;

        ParameterExpression operandParameter = Expression.Parameter(typeof(TOperand), "operand");
        try
        {
            UnaryExpression unaryExpression = Expression.MakeUnary(operatorType, operandParameter, typeof(TResult));
            Expression<Func<TOperand, TResult>> lambdaExpression = Expression.Lambda<Func<TOperand, TResult>>(unaryExpression, operandParameter);
            operation = lambdaExpression.Compile();
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

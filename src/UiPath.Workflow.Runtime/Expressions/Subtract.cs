// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

public sealed class Subtract<TLeft, TRight, TResult> : CodeActivity<TResult>
{
    //Lock is not needed for operationFunction here. The reason is that delegates for a given Subtract<TLeft, TRight, TResult> are the same.
    //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
    private static Func<TLeft, TRight, TResult> checkedOperationFunction;
    private static Func<TLeft, TRight, TResult> uncheckedOperationFunction;
    private bool _checkedOperation = true;

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TLeft> Left { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TRight> Right { get; set; }

    [DefaultValue(true)]
    public bool Checked
    {
        get => _checkedOperation;
        set => _checkedOperation = value;
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        BinaryExpressionHelper.OnGetArguments(metadata, Left, Right);

        if (_checkedOperation)
        {
            EnsureOperationFunction(metadata, ref checkedOperationFunction, ExpressionType.SubtractChecked);
        }
        else
        {
            EnsureOperationFunction(metadata, ref uncheckedOperationFunction, ExpressionType.Subtract);
        }
    }

    private static void EnsureOperationFunction(CodeActivityMetadata metadata,
        ref Func<TLeft, TRight, TResult> operationFunction,
        ExpressionType operatorType)
    {
        if (operationFunction == null)
        {
            if (!BinaryExpressionHelper.TryGenerateLinqDelegate(
                        operatorType,
                        out operationFunction,
                        out ValidationError validationError))
            {
                metadata.AddValidationError(validationError);
            }
        }
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        TLeft leftValue = Left.Get(context);
        TRight rightValue = Right.Get(context);

        //if user changed Checked flag between Open and Execution, 
        //a NRE may be thrown and that's by design
        if (_checkedOperation)
        {
            return checkedOperationFunction(leftValue, rightValue);
        }
        else
        {
            return uncheckedOperationFunction(leftValue, rightValue);
        }
    }
}

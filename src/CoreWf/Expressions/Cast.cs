// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

public sealed class Cast<TOperand, TResult> : CodeActivity<TResult>
{
    //Lock is not needed for operationFunction here. The reason is that delegates for a given Cast<TLeft, TRight, TResult> are the same.
    //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
    private static Func<TOperand, TResult> checkedOperationFunction;
    private static Func<TOperand, TResult> uncheckedOperationFunction;
    private bool _checkedOperation = true;

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TOperand> Operand { get; set; }

    [DefaultValue(true)]
    public bool Checked
    {
        get => _checkedOperation;
        set => _checkedOperation = value;
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        UnaryExpressionHelper.OnGetArguments(metadata, Operand);

        if (_checkedOperation)
        {
            EnsureOperationFunction(metadata, ref checkedOperationFunction, ExpressionType.ConvertChecked);
        }
        else
        {
            EnsureOperationFunction(metadata, ref uncheckedOperationFunction, ExpressionType.Convert);
        }
    }

    private static void EnsureOperationFunction(CodeActivityMetadata metadata,
        ref Func<TOperand, TResult> operationFunction,
        ExpressionType operatorType)
    {
        if (operationFunction == null)
        {
            if (!UnaryExpressionHelper.TryGenerateLinqDelegate(
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
        TOperand operandValue = Operand.Get(context);
            
        //if user changed Checked flag between Open and Execution, 
        //a NRE may be thrown and that's by design
        if (_checkedOperation)
        {
            return checkedOperationFunction(operandValue);
        }
        else
        {
            return uncheckedOperationFunction(operandValue);
        }
    }
}

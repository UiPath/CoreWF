// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords,
//    Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [Not])")]
public sealed class Not<TOperand, TResult> : CodeActivity<TResult>
{
    //Lock is not needed for operationFunction here. The reason is that delegates for a given Not<TLeft, TRight, TResult> are the same.
    //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
    private static Func<TOperand, TResult> operationFunction;

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TOperand> Operand { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        UnaryExpressionHelper.OnGetArguments(metadata, Operand);

        if (operationFunction == null)
        {
            if (!UnaryExpressionHelper.TryGenerateLinqDelegate(ExpressionType.Not, out operationFunction, out ValidationError validationError))
            {
                metadata.AddValidationError(validationError);
            }
        }
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        Fx.Assert(operationFunction != null, "OperationFunction must exist.");
        TOperand operandValue = Operand.Get(context);
        return operationFunction(operandValue);
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities.Expressions;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords,
//    Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [Or])")]
public sealed class Or<TLeft, TRight, TResult> : CodeActivity<TResult>
{
    //Lock is not needed for operationFunction here. The reason is that delegates for a given Or<TLeft, TRight, TResult> are the same.
    //It's possible that 2 threads are assigning the operationFucntion at the same time. But it's okay because the compiled codes are the same.
    private static Func<TLeft, TRight, TResult> operationFunction;

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TLeft> Left { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TRight> Right { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        BinaryExpressionHelper.OnGetArguments(metadata, Left, Right);

        if (operationFunction == null)
        {
            if (!BinaryExpressionHelper.TryGenerateLinqDelegate(ExpressionType.Or, out operationFunction, out ValidationError validationError))
            {
                metadata.AddValidationError(validationError);
            }
        }
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        Fx.Assert(operationFunction != null, "OperationFunction must exist.");
        TLeft leftValue = Left.Get(context);
        TRight rightValue = Right.Get(context);
        return operationFunction(leftValue, rightValue);
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.Activities.Validation;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Markup;

namespace System.Activities.Expressions;

// consciously not XAML-friendly since Linq Expressions aren't create-set-use
[Fx.Tag.XamlVisible(false)]
[Diagnostics.DebuggerStepThrough]
public sealed class LambdaValue<TResult> : CodeActivity<TResult>, IExpressionContainer, IValueSerializableExpression
{
    private Func<ActivityContext, TResult> _compiledLambdaValue;
    private readonly Expression<Func<ActivityContext, TResult>> _lambdaValue;
    private Expression<Func<ActivityContext, TResult>> _rewrittenTree;

    public LambdaValue(Expression<Func<ActivityContext, TResult>> lambdaValue)
    {
        _lambdaValue = lambdaValue ?? throw FxTrace.Exception.ArgumentNull(nameof(lambdaValue));
        UseOldFastPath = true;
    }

    // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
    internal Expression LambdaExpression => _lambdaValue;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

        // We need to rewrite the tree.
        if (ExpressionUtilities.TryRewriteLambdaExpression(_lambdaValue, out Expression newTree, publicAccessor))
        {
            _rewrittenTree = (Expression<Func<ActivityContext, TResult>>)newTree;
        }
        else
        {
            _rewrittenTree = _lambdaValue;
        }
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        var settings = context.GetExtension<ExpressionEvaluationSettings>();
        _compiledLambdaValue ??= _rewrittenTree.Compile(settings?.PreferExpressionInterpretation ?? false);
        return _compiledLambdaValue(context);
    }

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) =>
        // This workflow contains lambda expressions specified in code. 
        // These expressions are not XAML serializable. 
        // In order to make your workflow XAML-serializable, 
        // use either VisualBasicValue/Reference or ExpressionServices.Convert 
        // This will convert your lambda expressions into expression activities.
        throw FxTrace.Exception.AsError(new LambdaSerializationException());
}

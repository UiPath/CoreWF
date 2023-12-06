// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;
using ActivityContext = System.Activities.ActivityContext;

namespace Microsoft.CSharp.Activities;

[DebuggerStepThrough]
[ContentProperty("ExpressionText")]
public sealed class CSharpValue<TResult> : TextExpressionBase<TResult, TResult>
{
    private Func<ActivityContext, TResult> _compiledExpression;

    public CSharpValue() => UseOldFastPath = true;

    public CSharpValue(string expressionText) : this() => ExpressionText = expressionText;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Language => CSharpHelper.Language;

    public override string ExpressionText { get; set; }

    public override void UpdateExpressionText(string expressionText)
    {
        base.UpdateExpressionText(expressionText);
        _compiledExpression = null;
    }

    public override Expression GetExpressionTree() => ResolveExpressionTree();

    protected override TResult ExecuteCompiledExpression(CodeActivityContext context)
    {
        _compiledExpression ??= _expressionTree.Compile();
        return _compiledExpression(context);
    }

    protected override string GetContextCompilationError(
        CodeActivityPublicEnvironmentAccessor publicAccessor) => null;
}
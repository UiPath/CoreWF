// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;
using ActivityContext = System.Activities.ActivityContext;

namespace Microsoft.VisualBasic.Activities;

[DebuggerStepThrough]
public sealed class VisualBasicValue<TResult>
    : TextExpressionBase<TResult, TResult>, IValueSerializableExpression, IExpressionContainer
{
    private Func<ActivityContext, TResult> _compiledExpression;

    public VisualBasicValue() => UseOldFastPath = true;

    public VisualBasicValue(string expressionText) : this() => ExpressionText = expressionText;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Language => VisualBasicHelper.Language;

    public override string ExpressionText { get; set; }

    public override void UpdateExpressionText(string expressionText)
    {
        base.UpdateExpressionText(expressionText);
        _compiledExpression = null;
    }

    public override Expression GetExpressionTree() => ResolveExpressionTree();

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) => "[" + ExpressionText + "]";

    protected override TResult ExecuteCompiledExpression(CodeActivityContext context)
    {
        _compiledExpression ??= _expressionTree.Compile();
        return _compiledExpression(context);
    }

    protected override string GetContextCompilationError(
        CodeActivityPublicEnvironmentAccessor publicAccessor) => null;

}
// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Expressions;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace Microsoft.CSharp.Activities;

[DebuggerStepThrough]
[ContentProperty("ExpressionText")]
public class CSharpReference<TResult>
    : TextExpressionBase<TResult, Location<TResult>>, ITextExpression
{
    private LocationFactory<TResult> _locationFactory;

    public CSharpReference() => UseOldFastPath = true;

    public CSharpReference(string expressionText) : this() => ExpressionText = expressionText;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Language => CSharpHelper.Language;

    public override string ExpressionText { get; set; }

    public override Expression GetExpressionTree() => ResolveExpressionTree();

    public override void UpdateExpressionText(string expressionText)
    {
        base.UpdateExpressionText(expressionText);
        _locationFactory = null;
    }

    protected override Location<TResult> ExecuteCompiledExpression(CodeActivityContext context)
    {
        _locationFactory ??= ExpressionUtilities.CreateLocationFactory<TResult>(_expressionTree);
        return _locationFactory.CreateLocation(context);
    }
}
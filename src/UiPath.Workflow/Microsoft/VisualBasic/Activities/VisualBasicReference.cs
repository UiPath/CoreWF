// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.XamlIntegration;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace Microsoft.VisualBasic.Activities;

[DebuggerStepThrough]
public sealed class VisualBasicReference<TResult> 
    : TextExpressionBase<TResult, Location<TResult>>, IValueSerializableExpression, IExpressionContainer
{
    private LocationFactory<TResult> _locationFactory;

    public VisualBasicReference() => UseOldFastPath = true;

    public VisualBasicReference(string expressionText) : this() => ExpressionText = expressionText;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Language => VisualBasicHelper.Language;

    public override string ExpressionText { get; set; }

    public override void UpdateExpressionText(string expressionText)
    {
        base.UpdateExpressionText(expressionText);
        _locationFactory = null;
    }

    public override Expression GetExpressionTree() => ResolveExpressionTree();

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) => "[" + ExpressionText + "]";

    protected override Location<TResult> ExecuteCompiledExpression(CodeActivityContext context)
    {
        _locationFactory ??= ExpressionUtilities.CreateLocationFactory<TResult>(_expressionTree);
        return _locationFactory.CreateLocation(context);
    }
}
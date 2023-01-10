// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;
using ActivityContext = System.Activities.ActivityContext;

namespace Microsoft.VisualBasic.Activities;

[DebuggerStepThrough]
public sealed class VisualBasicValue<TResult> : CodeActivity<TResult>, IValueSerializableExpression,
    IExpressionContainer, ITextExpression
{
    private CompiledExpressionInvoker _invoker;

    public VisualBasicValue()
    {
        UseOldFastPath = true;
    }

    public VisualBasicValue(string expressionText)
        : this()
    {
        ExpressionText = expressionText;
    }

    public string ExpressionText { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Language => VisualBasicHelper.Language;

    public bool RequiresCompilation => true;

    public Expression GetExpressionTree()
    {
        if (IsMetadataCached)
        {
            return _invoker.GetExpressionTree();
        }

        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
    }

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) => "[" + ExpressionText + "]";

    protected override TResult Execute(CodeActivityContext context)
    {
        return (TResult)_invoker.InvokeExpression(context);
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _invoker = new CompiledExpressionInvoker(this, false, metadata);
        if (metadata.Environment.CompileExpressions)
        {
            return;
        }

        if (metadata.Environment.IsValidating)
        {
            foreach (var validationError in VbExpressionValidator.Instance.Validate<TResult>(this, metadata.Environment,
                         ExpressionText))
            {
                AddTempValidationError(validationError);
            }
        }
    }
}

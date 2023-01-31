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

namespace Microsoft.VisualBasic.Activities;

[DebuggerStepThrough]
public sealed class VisualBasicReference<TResult> : CodeActivity<Location<TResult>>, IValueSerializableExpression,
    IExpressionContainer, ITextExpression
{
    private CompiledExpressionInvoker _invoker;

    public VisualBasicReference()
    {
        UseOldFastPath = true;
    }

    public VisualBasicReference(string expressionText)
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

    public bool CanConvertToString(IValueSerializerContext context)
    {
        // we can always convert to a string 
        return true;
    }

    public string ConvertToString(IValueSerializerContext context)
    {
        // Return our bracket-escaped text
        return "[" + ExpressionText + "]";
    }

    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        var value = (Location<TResult>)_invoker.InvokeExpression(context);

        return value;
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _invoker = new CompiledExpressionInvoker(this, true, metadata);

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

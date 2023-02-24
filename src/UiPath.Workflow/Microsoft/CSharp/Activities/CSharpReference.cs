// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace Microsoft.CSharp.Activities;

[DebuggerStepThrough]
[ContentProperty("ExpressionText")]
public class CSharpReference<TResult> : CodeActivity<Location<TResult>>, ITextExpression
{
    private CompiledExpressionInvoker _invoker;

    public CSharpReference() => UseOldFastPath = true;

    public CSharpReference(string expressionText) : this() => ExpressionText = expressionText;

    public string ExpressionText { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Language => "C#";

    public bool RequiresCompilation => true;

    public Expression GetExpressionTree() => IsMetadataCached ? _invoker.GetExpressionTree() : throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _invoker = new CompiledExpressionInvoker(this, true, metadata);
        CsExpressionValidator.Instance.TryValidate<TResult>(this, metadata, ExpressionText);
    }

    protected override Location<TResult> Execute(CodeActivityContext context) => (Location<TResult>)_invoker.InvokeExpression(context);
}
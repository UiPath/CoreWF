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
    private Func<ActivityContext, TResult> _compiledExpression;
    private Expression<Func<ActivityContext, TResult>> _expressionTree;
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
        if (!IsMetadataCached)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
        }

        if (_expressionTree == null)
        {
            if (_invoker != null)
            {
                return _invoker.GetExpressionTree();
            }

            // it's safe to create this CodeActivityMetadata here,
            // because we know we are using it only as lookup purpose.
            var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), false);
            var publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
            try
            {
                _expressionTree = VisualBasicHelper.Compile<TResult>(ExpressionText, publicAccessor, false);
            }
            catch (SourceExpressionException e)
            {
                throw FxTrace.Exception.AsError(
                    new InvalidOperationException(SR.ExpressionTamperedSinceLastCompiled(e.Message)));
            }
            finally
            {
                metadata.Dispose();
            }
        }

        Fx.Assert(_expressionTree.NodeType == ExpressionType.Lambda, "Lambda expression required");
        return ExpressionUtilities.RewriteNonCompiledExpressionTree(_expressionTree);

    }

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) => "[" + ExpressionText + "]";

    protected override TResult Execute(CodeActivityContext context)
    {
        if (_expressionTree == null)
        {
            return (TResult) _invoker.InvokeExpression(context);
        }

        _compiledExpression ??= _expressionTree.Compile();

        return _compiledExpression(context);
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _expressionTree = null;
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
        else
        {
            try
            {
                var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
                _expressionTree = VisualBasicHelper.Compile<TResult>(ExpressionText, publicAccessor, false);
            }
            catch (SourceExpressionException e)
            {
                metadata.AddValidationError(e.Message);
            }
        }
    }
}

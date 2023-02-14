// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace Microsoft.CSharp.Activities;

[DebuggerStepThrough]
[ContentProperty("ExpressionText")]
public class CSharpValue<TResult> : CodeActivity<TResult>, ITextExpression
{
    private CompiledExpressionInvoker _invoker;
    private Expression<Func<System.Activities.ActivityContext, TResult>> _lambdaExpression;

    private Expression<Func<System.Activities.ActivityContext, TResult>> LambdaExpression
        => _lambdaExpression ??= Compile();

    public CSharpValue()
    {
        UseOldFastPath = true;
    }

    public CSharpValue(string expressionText) :
        this()
    {
        ExpressionText = expressionText;
    }

    public string ExpressionText { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Language => "C#";

    public bool RequiresCompilation => true;

    public Expression GetExpressionTree()
    {
        if (!IsMetadataCached)
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));

        return _invoker.GetExpressionTree() ?? ExpressionUtilities.RewriteNonCompiledExpressionTree(LambdaExpression);
    }

    public object ExecuteInContext(CodeActivityContext context)
    {
        var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), true);
        try
        {
            context.Reinitialize(context.CurrentInstance, context.CurrentExecutor, this, context.CurrentInstance.InternalId);

            var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
            var lambda = CSharpHelper.Compile<TResult>(ExpressionText, publicAccessor, false);
            return lambda.Compile()(context);
        }
        finally
        {
            metadata.Dispose();
        }
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _lambdaExpression = null;
        _invoker = new CompiledExpressionInvoker(this, false, metadata);
        if (metadata.Environment.CompileExpressions)
        {
            return;
        }

        if (metadata.Environment.IsValidating)
        {
            foreach (var validationError in CsExpressionValidator.Instance.Validate<TResult>(this, metadata.Environment,
                         ExpressionText))
            {
                AddTempValidationError(validationError);
            }
        }
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        return _invoker.IsExpressionCompiled(context)
            ? (TResult)_invoker.InvokeExpression(context)
            : LambdaExpression.Compile()(context);
    }

    private Expression<Func<System.Activities.ActivityContext, TResult>> Compile()
    {
        var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), false);
        var publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
        try
        {
            return CSharpHelper.Compile<TResult>(ExpressionText, publicAccessor, false);
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
}

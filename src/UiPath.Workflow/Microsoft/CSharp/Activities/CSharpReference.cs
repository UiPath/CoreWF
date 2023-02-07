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

    public CSharpReference()
    {
        UseOldFastPath = true;
    }

    public CSharpReference(string expressionText) :
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

        return _invoker.GetExpressionTree() ?? ExpressionUtilities.RewriteNonCompiledExpressionTree(Compile());
    }

    public object ExecuteInContext(CodeActivityContext context)
    {
        var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), true);
        try
        {
            context.Reinitialize(context.CurrentInstance, context.CurrentExecutor, this, context.CurrentInstance.InternalId);

            var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
            var expressionTree = CompileLocationExpression(publicAccessor, out _);

            var locationFactory = ExpressionUtilities.CreateLocationFactory<TResult>(expressionTree);
            return locationFactory.CreateLocation(context);
        }
        finally
        {
            metadata.Dispose();
        }
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

    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        var value = (Location<TResult>)_invoker.InvokeExpression(context);

        return value;
    }

    private Expression<Func<System.Activities.ActivityContext, TResult>> CompileLocationExpression(
        CodeActivityPublicEnvironmentAccessor publicAccessor, out string validationError)
    {
        var expressionTreeToReturn = CSharpHelper.Compile<TResult>(ExpressionText, publicAccessor, true);
        validationError = null;
        string extraErrorMessage = null;
        // inspect the expressionTree to see if it is a valid location expression(L-value)
        if (!publicAccessor.ActivityMetadata.HasViolations && (expressionTreeToReturn == null ||
            !ExpressionUtilities.IsLocation(expressionTreeToReturn, typeof(TResult), out extraErrorMessage)))
        {
            var errorMessage = SR.InvalidLValueExpression;

            if (extraErrorMessage != null)
            {
                errorMessage += ":" + extraErrorMessage;
            }

            expressionTreeToReturn = null;
            validationError = SR.CompilerErrorSpecificExpression(ExpressionText, errorMessage);
        }

        return expressionTreeToReturn;
    }

    private LambdaExpression Compile()
    {
        var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), false);
        var publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
        try
        {
            var expressionTree = CompileLocationExpression(publicAccessor, out var validationError);

            if (validationError != null)
            {
                throw FxTrace.Exception.AsError(
                    new InvalidOperationException(SR.ExpressionTamperedSinceLastCompiled(validationError)));
            }
            return expressionTree;
        }
        finally
        {
            metadata.Dispose();
        }
    }
}

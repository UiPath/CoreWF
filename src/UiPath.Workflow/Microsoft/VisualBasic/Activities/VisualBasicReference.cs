// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Internals;
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

    private Expression<Func<System.Activities.ActivityContext, TResult>> CompileLocationExpression(
        CodeActivityPublicEnvironmentAccessor publicAccessor, out string validationError)
    {
        var expressionTreeToReturn = VisualBasicHelper.Compile<TResult>(ExpressionText, publicAccessor, true);
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

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
public sealed class VisualBasicReference<TResult> : TextExpressionBase<Location<TResult>>, IValueSerializableExpression,
    IExpressionContainer
{
    private Expression<Func<ActivityContext, TResult>> _expressionTree;
    private CompiledExpressionInvoker _invoker;
    private LocationFactory<TResult> _locationFactory;

    public VisualBasicReference() => UseOldFastPath = true;

    public VisualBasicReference(string expressionText) : this() => ExpressionText = expressionText;

    public override string ExpressionText { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Language => VisualBasicHelper.Language;

    public override Expression GetExpressionTree()
    {
        if (IsMetadataCached)
        {
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
                    _expressionTree = CompileLocationExpression(publicAccessor, out var validationError);
                    if (validationError != null)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ExpressionTamperedSinceLastCompiled(validationError)));
                    }
                }
                finally
                {
                    metadata.Dispose();
                }
            }
            Fx.Assert(_expressionTree.NodeType == ExpressionType.Lambda, "Lambda expression required");
            return ExpressionUtilities.RewriteNonCompiledExpressionTree(_expressionTree);
        }
        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
    }

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) => "[" + ExpressionText + "]";

    protected override Location<TResult> Execute(CodeActivityContext context)
    {
        if (_expressionTree == null)
        {
            return (Location<TResult>)_invoker.InvokeExpression(context);
        }
        _locationFactory ??= ExpressionUtilities.CreateLocationFactory<TResult>(_expressionTree);
        return _locationFactory.CreateLocation(context);
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _expressionTree = null;
        _invoker = new CompiledExpressionInvoker(this, true, metadata);

        if (metadata.Environment.CompileExpressions)
        {
            return;
        }

        if (VbExpressionValidator.Instance.TryValidate<TResult>(this, metadata, ExpressionText, true))
        {
            return;
        }

        if (QueueForValidation<TResult>(metadata, true))
        {
            return;
        }
        // If ICER is not implemented that means we haven't been compiled
        var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
        _expressionTree = CompileLocationExpression(publicAccessor, out var validationError);
        if (validationError != null)
        {
            metadata.AddValidationError(validationError);
        }
    }

    private Expression<Func<ActivityContext, TResult>> CompileLocationExpression(CodeActivityPublicEnvironmentAccessor publicAccessor, out string validationError)
    {
        Expression<Func<ActivityContext, TResult>> expressionTreeToReturn = null;
        validationError = null;
        try
        {
            expressionTreeToReturn = VisualBasicHelper.Compile<TResult>(ExpressionText, publicAccessor, true);
            // inspect the expressionTree to see if it is a valid location expression(L-value)
            string extraErrorMessage = null;
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
        }
        catch (SourceExpressionException e)
        {
            validationError = e.Message;
        }
        return expressionTreeToReturn;
    }
}
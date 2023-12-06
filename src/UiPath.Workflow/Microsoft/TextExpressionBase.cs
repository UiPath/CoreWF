using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Linq.Expressions;

namespace System.Activities;

public abstract class TextExpressionBase<TTree, TResult> : CodeActivity<TResult>, ITextExpression
{
    private static readonly Func<ValidationExtension> _validationFunc = () => new();
    protected Expression<Func<ActivityContext, TTree>> _expressionTree;
    protected CompiledExpressionInvoker _invoker;

    public abstract string ExpressionText { get; set; }

    public abstract string Language { get; }

    public abstract Expression GetExpressionTree();

    public virtual void UpdateExpressionText(string expressionText)
    {
        CheckIsMetadataCached();
        ExpressionText = expressionText;
        CreateExpressionTree();
    }

    protected override TResult Execute(CodeActivityContext context)
    {
        if (_expressionTree == null)
            return (TResult)_invoker.InvokeExpression(context);

        return ExecuteCompiledExpression(context);
    }

    protected abstract TResult ExecuteCompiledExpression(CodeActivityContext context);

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        _expressionTree = null;
        _invoker = new CompiledExpressionInvoker(this, true, metadata);

        if (QueueForValidation<TResult>(metadata, true))
            return;

        // If ICER is not implemented that means we haven't been compiled
        var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
        var validationError = CompileExpressionTreeWithValidationError(publicAccessor);
        if (validationError != null)
        {
            metadata.AddValidationError(validationError);
        }
    }

    protected Expression ResolveExpressionTree()
    {
        CheckIsMetadataCached();

        if (_expressionTree == null)
        {
            if (_invoker != null)
                return _invoker.GetExpressionTree();

            // it's safe to create this CodeActivityMetadata here,
            // because we know we are using it only as lookup purpose.
            CreateExpressionTree();
        }
        Fx.Assert(_expressionTree.NodeType == ExpressionType.Lambda, "Lambda expression required");
        return ExpressionUtilities.RewriteNonCompiledExpressionTree(_expressionTree);
    }

    protected bool QueueForValidation<T>(CodeActivityMetadata metadata, bool isLocation)
    {
        if (metadata.Environment.CompileExpressions)
            return true;

        if (metadata.Environment.IsValidating)
        {
            var extension = metadata.Environment.Extensions.GetOrAdd(_validationFunc);
            extension.QueueExpressionForValidation<T>(new()
            {
                Activity = this,
                ExpressionText = ExpressionText,
                IsLocation = isLocation,
                ResultType = typeof(T),
                Environment = metadata.Environment
            }, Language);

            return true;
        }
        return false;
    }

    private void CheckIsMetadataCached()
    {
        if (!IsMetadataCached)
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
    }

    private void CreateExpressionTree()
    {
        var metadata = new CodeActivityMetadata(this, GetParentEnvironment(), false);
        try
        {
            var publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
            var validationError = CompileExpressionTreeWithValidationError(publicAccessor);
            if (validationError != null)
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ExpressionTamperedSinceLastCompiled(validationError)));
        }
        finally
        {
            metadata.Dispose();
        }
    }

    private string CompileExpressionTreeWithValidationError(CodeActivityPublicEnvironmentAccessor publicAccessor)
    {
        try
        {
            _expressionTree = CompileExpressionTree(publicAccessor);
            var result = GetContextCompilationError(publicAccessor);
            if (!string.IsNullOrEmpty(result))
            {
                _expressionTree = null;
            }
            return result;
        }
        catch (SourceExpressionException e)
        {
            return e.Message;
        }
    }

    protected virtual string GetContextCompilationError(
        CodeActivityPublicEnvironmentAccessor publicAccessor)
    {
        // inspect the expressionTree to see if it is a valid location expression(L-value)
        string extraErrorMessage = null;
        if (!publicAccessor.ActivityMetadata.HasViolations
            && (_expressionTree == null ||
                !ExpressionUtilities.IsLocation(_expressionTree, typeof(TResult), out extraErrorMessage)))
        {
            var errorMessage = SR.InvalidLValueExpression;
            if (extraErrorMessage != null)
            {
                errorMessage += ":" + extraErrorMessage;
            }
            return SR.CompilerErrorSpecificExpression(ExpressionText, errorMessage);
        }
        return null;
    }

    private Expression<Func<ActivityContext, TTree>> CompileExpressionTree(CodeActivityPublicEnvironmentAccessor publicAccessor)
    {
        switch (Language)
        {
            case CSharpHelper.Language:
                return CSharpHelper.Compile<TTree>(ExpressionText, publicAccessor, false);
            default:
                return VisualBasicHelper.Compile<TTree>(ExpressionText, publicAccessor, false);
        }
    }
}

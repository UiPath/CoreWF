
namespace Microsoft.Common
{
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Expressions;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Linq.Expressions;

    public abstract class Value<TResult> : CodeActivity<TResult>, IExpressionContainer, ITextExpression
    {
        private Expression<Func<ActivityContext, TResult>> expressionTree;
        private Func<ActivityContext, TResult> compiledExpression;
        private CompiledExpressionInvoker invoker;

        public string ExpressionText { get; set; }

        public abstract string Language { get; }

        public bool RequiresCompilation => true;

        protected Value() { }

        protected Value(string expressionText)
        {
            this.ExpressionText = expressionText;
        }

        public Expression GetExpressionTree()
        {
            if (this.IsMetadataCached)
            {
                if (this.expressionTree == null)
                {
                    // it's safe to create this CodeActivityMetadata here,
                    // because we know we are using it only as lookup purpose.
                    CodeActivityMetadata metadata = new CodeActivityMetadata(this, this.GetParentEnvironment(), false);
                    CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
                    try
                    {
                        expressionTree = Compile<TResult>(this.ExpressionText, publicAccessor, false);
                    }
                    catch (SourceExpressionException e)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ExpressionTamperedSinceLastCompiled(e.Message)));
                    }
                    finally
                    {
                        metadata.Dispose();
                    }
                }

                Fx.Assert(this.expressionTree.NodeType == ExpressionType.Lambda, "Lambda expression required");
                return ExpressionUtilities.RewriteNonCompiledExpressionTree(expressionTree);
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
            }
        }

        protected abstract Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression);

        protected override TResult Execute(CodeActivityContext context)
        {
            if (expressionTree == null)
            {
                return (TResult)invoker.InvokeExpression(context);
            }
            if (compiledExpression == null)
            {
                compiledExpression = expressionTree.Compile();
            }
            return compiledExpression(context);
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            expressionTree = null;
            invoker = new CompiledExpressionInvoker(this, false, metadata);
            if (metadata.Environment.CompileExpressions)
            {
                return;
            }
            // If ICER is not implemented that means we haven't been compiled
            var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
            try
            {
                expressionTree = Compile<TResult>(this.ExpressionText, publicAccessor, false);
            }
            catch (SourceExpressionException e)
            {
                metadata.AddValidationError(e.Message);
            }
        }
    }
}

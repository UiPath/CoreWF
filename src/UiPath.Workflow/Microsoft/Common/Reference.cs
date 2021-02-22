
namespace Microsoft.Common
{
    using System;
    using System.Activities;
    using System.Activities.ExpressionParser;
    using System.Activities.Expressions;
    using System.Activities.Internals;
    using System.Activities.Runtime;
    using System.Linq.Expressions;

    public abstract class Reference<TResult> : CodeActivity<Location<TResult>>, IExpressionContainer, ITextExpression
    {
        private Expression<Func<ActivityContext, TResult>> expressionTree;
        private LocationFactory<TResult> locationFactory;
        private CompiledExpressionInvoker invoker;

        public string ExpressionText { get; set; }

        public abstract string Language { get; }

        public bool RequiresCompilation => true;

        protected Reference() { }

        protected Reference(string expressionText)
        {
            this.ExpressionText = expressionText;
        }

        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            if (expressionTree == null)
            {
                return (Location<TResult>)invoker.InvokeExpression(context);
            }
            if (locationFactory == null)
            {
                locationFactory = ExpressionUtilities.CreateLocationFactory<TResult>(this.expressionTree);
            }
            return locationFactory.CreateLocation(context);
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            expressionTree = null;
            invoker = new CompiledExpressionInvoker(this, true, metadata);
            if (metadata.Environment.CompileExpressions)
            {
                return;
            }
            string validationError;
            // If ICER is not implemented that means we haven't been compiled
            var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
            expressionTree = CompileLocationExpression(publicAccessor, out validationError);
            if (validationError != null)
            {
                metadata.AddValidationError(validationError);
            }
        }

        public Expression GetExpressionTree()
        {
            if (this.IsMetadataCached)
            {
                if (this.expressionTree == null)
                {
                    string validationError;

                    // it's safe to create this CodeActivityMetadata here,
                    // because we know we are using it only as lookup purpose.
                    CodeActivityMetadata metadata = new CodeActivityMetadata(this, this.GetParentEnvironment(), false);
                    CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
                    try
                    {
                        this.expressionTree = this.CompileLocationExpression(publicAccessor, out validationError);

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

                Fx.Assert(this.expressionTree.NodeType == ExpressionType.Lambda, "Lambda expression required");
                return ExpressionUtilities.RewriteNonCompiledExpressionTree(expressionTree);
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
            }
        }

        protected abstract Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression);

        private Expression<Func<ActivityContext, TResult>> CompileLocationExpression(CodeActivityPublicEnvironmentAccessor publicAccessor, out string validationError)
        {
            Expression<Func<ActivityContext, TResult>> expressionTreeToReturn = null;
            validationError = null;
            try
            {
                expressionTreeToReturn = Compile<TResult>(this.ExpressionText, publicAccessor, true);
                // inspect the expressionTree to see if it is a valid location expression(L-value)
                string extraErrorMessage = null;
                if (!publicAccessor.ActivityMetadata.HasViolations && (expressionTreeToReturn == null || !ExpressionUtilities.IsLocation(expressionTreeToReturn, typeof(TResult), out extraErrorMessage)))
                {
                    string errorMessage = SR.InvalidLValueExpression;

                    if (extraErrorMessage != null)
                    {
                        errorMessage += ":" + extraErrorMessage;
                    }
                    expressionTreeToReturn = null;
                    validationError = SR.CompilerErrorSpecificExpression(this.ExpressionText, errorMessage);
                }
            }
            catch (SourceExpressionException e)
            {
                validationError = e.Message;
            }

            return expressionTreeToReturn;
        }
    }
}

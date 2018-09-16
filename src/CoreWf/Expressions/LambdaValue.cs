// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using System;
    using CoreWf.XamlIntegration;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Portable.Xaml.Markup;
    using CoreWf.Runtime;
    using CoreWf.Internals;

#if NET45
    using CoreWf.ExpressionParser; 
#endif

    // consciously not XAML-friendly since Linq Expressions aren't create-set-use
    [Fx.Tag.XamlVisible(false)]
    [DebuggerStepThrough]
    public sealed class LambdaValue<TResult> : CodeActivity<TResult>, IExpressionContainer, IValueSerializableExpression
    {
        private Func<ActivityContext, TResult> compiledLambdaValue;
        private readonly Expression<Func<ActivityContext, TResult>> lambdaValue;
        private Expression<Func<ActivityContext, TResult>> rewrittenTree;

        public LambdaValue(Expression<Func<ActivityContext, TResult>> lambdaValue)
        {
            this.lambdaValue = lambdaValue ?? throw FxTrace.Exception.ArgumentNull(nameof(lambdaValue));
            this.UseOldFastPath = true;
        }

        // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
        internal Expression LambdaExpression
        {
            get
            {
                return this.lambdaValue;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

            // We need to rewrite the tree.
            if (ExpressionUtilities.TryRewriteLambdaExpression(this.lambdaValue, out Expression newTree, publicAccessor))
            {
                this.rewrittenTree = (Expression<Func<ActivityContext, TResult>>)newTree;
            }
            else
            {
                this.rewrittenTree = this.lambdaValue;
            }
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            if (this.compiledLambdaValue == null)
            {
                this.compiledLambdaValue = this.rewrittenTree.Compile();
            }
            return this.compiledLambdaValue(context);
        }

        public bool CanConvertToString(IValueSerializerContext context)
        {
            return true;
        }

        public string ConvertToString(IValueSerializerContext context)
        {
            // This workflow contains lambda expressions specified in code. 
            // These expressions are not XAML serializable. 
            // In order to make your workflow XAML-serializable, 
            // use either VisualBasicValue/Reference or ExpressionServices.Convert 
            // This will convert your lambda expressions into expression activities.
            throw FxTrace.Exception.AsError(new LambdaSerializationException());
        }
    }
}

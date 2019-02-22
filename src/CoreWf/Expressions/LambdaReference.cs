// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Expressions
{
    using System;
    using System.Activities.XamlIntegration;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using Portable.Xaml.Markup;
    using System.Activities.Runtime;
    using System.Activities.Internals;

#if NET45
    using System.Activities.ExpressionParser; 
#endif

    // consciously not XAML-friendly since Linq Expressions aren't create-set-use
    [Fx.Tag.XamlVisible(false)]
    [DebuggerStepThrough]
    public sealed class LambdaReference<T> : CodeActivity<Location<T>>, IExpressionContainer, IValueSerializableExpression
    {
        private readonly Expression<Func<ActivityContext, T>> locationExpression;
        private Expression<Func<ActivityContext, T>> rewrittenTree;
        private LocationFactory<T> locationFactory;

        public LambdaReference(Expression<Func<ActivityContext, T>> locationExpression)
        {
            this.locationExpression = locationExpression ?? throw FxTrace.Exception.ArgumentNull(nameof(locationExpression));
            this.UseOldFastPath = true;
        }

        // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
        internal Expression LambdaExpression
        {
            get
            {
                return this.locationExpression;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

            // We need to rewrite the tree.
            if (ExpressionUtilities.TryRewriteLambdaExpression(this.locationExpression, out Expression newTree, publicAccessor, true))
            {
                this.rewrittenTree = (Expression<Func<ActivityContext, T>>)newTree;
            }
            else
            {
                this.rewrittenTree = this.locationExpression;
            }

            // inspect the expressionTree to see if it is a valid location expression(L-value)
            if (!ExpressionUtilities.IsLocation(this.rewrittenTree, typeof(T), out string extraErrorMessage))
            {
                string errorMessage = SR.InvalidLValueExpression;
                if (extraErrorMessage != null)
                {
                    errorMessage += ":" + extraErrorMessage;
                }
                metadata.AddValidationError(errorMessage);
            }
        }

        protected override Location<T> Execute(CodeActivityContext context)
        {
            if (this.locationFactory == null)
            {
                this.locationFactory = ExpressionUtilities.CreateLocationFactory<T>(this.rewrittenTree);
            }
            return this.locationFactory.CreateLocation(context);
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

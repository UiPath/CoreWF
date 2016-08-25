// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.CoreWf.Expressions
{
    // consciously not XAML-friendly since Linq Expressions aren't create-set-use
    [Fx.Tag.XamlVisible(false)]
    [DebuggerStepThrough]
    public sealed class LambdaValue<TResult> : CodeActivity<TResult>, IExpressionContainer/*, IValueSerializableExpression*/
    {
        private Func<ActivityContext, TResult> _compiledLambdaValue;
        private Expression<Func<ActivityContext, TResult>> _lambdaValue;
        private Expression<Func<ActivityContext, TResult>> _rewrittenTree;

        public LambdaValue(Expression<Func<ActivityContext, TResult>> lambdaValue)
        {
            if (lambdaValue == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("lambdaValue");
            }
            _lambdaValue = lambdaValue;
            this.UseOldFastPath = true;
        }

        // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
        internal Expression LambdaExpression
        {
            get
            {
                return _lambdaValue;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

            // We need to rewrite the tree.
            Expression newTree;
            if (ExpressionUtilities.TryRewriteLambdaExpression(_lambdaValue, out newTree, publicAccessor))
            {
                _rewrittenTree = (Expression<Func<ActivityContext, TResult>>)newTree;
            }
            else
            {
                _rewrittenTree = _lambdaValue;
            }
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            if (_compiledLambdaValue == null)
            {
                _compiledLambdaValue = _rewrittenTree.Compile();
            }
            return _compiledLambdaValue(context);
        }

        //public bool CanConvertToString(IValueSerializerContext context)
        //{
        //    return true;
        //}

        //public string ConvertToString(IValueSerializerContext context)
        //{
        //    // This workflow contains lambda expressions specified in code. 
        //    // These expressions are not XAML serializable. 
        //    // In order to make your workflow XAML-serializable, 
        //    // use either VisualBasicValue/Reference or ExpressionServices.Convert 
        //    // This will convert your lambda expressions into expression activities.
        //    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new LambdaSerializationException());
        //}
    }
}

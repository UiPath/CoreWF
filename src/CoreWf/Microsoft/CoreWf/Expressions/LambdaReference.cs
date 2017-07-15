// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace CoreWf.Expressions
{
    // consciously not XAML-friendly since Linq Expressions aren't create-set-use
    [Fx.Tag.XamlVisible(false)]
    [DebuggerStepThrough]
    public sealed class LambdaReference<T> : CodeActivity<Location<T>>, IExpressionContainer/*, IValueSerializableExpression*/
    {
        private Expression<Func<ActivityContext, T>> _locationExpression;
        private Expression<Func<ActivityContext, T>> _rewrittenTree;
        private LocationFactory<T> _locationFactory;

        public LambdaReference(Expression<Func<ActivityContext, T>> locationExpression)
        {
            if (locationExpression == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("locationExpression");
            }
            _locationExpression = locationExpression;
            this.UseOldFastPath = true;
        }

        // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
        internal Expression LambdaExpression
        {
            get
            {
                return _locationExpression;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

            // We need to rewrite the tree.
            Expression newTree;
            if (ExpressionUtilities.TryRewriteLambdaExpression(_locationExpression, out newTree, publicAccessor, true))
            {
                _rewrittenTree = (Expression<Func<ActivityContext, T>>)newTree;
            }
            else
            {
                _rewrittenTree = _locationExpression;
            }

            // inspect the expressionTree to see if it is a valid location expression(L-value)
            string extraErrorMessage = null;
            if (!ExpressionUtilities.IsLocation(_rewrittenTree, typeof(T), out extraErrorMessage))
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
            if (_locationFactory == null)
            {
                _locationFactory = ExpressionUtilities.CreateLocationFactory<T>(_rewrittenTree);
            }
            return _locationFactory.CreateLocation(context);
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
        //    throw CoreWf.Internals.FxTrace.Exception.AsError(new LambdaSerializationException());
        //}
    }
}

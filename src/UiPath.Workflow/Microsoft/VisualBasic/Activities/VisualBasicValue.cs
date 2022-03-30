﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.VisualBasic.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.ExpressionParser;
    using System.Activities.XamlIntegration;
    using System.Linq.Expressions;
    using System.Windows.Markup;
    using System.ComponentModel;
    using System.Activities.Runtime;
    using System.Activities.Internals;

    [System.Diagnostics.DebuggerStepThrough]
    public sealed class VisualBasicValue<TResult> : CodeActivity<TResult>, IValueSerializableExpression, IExpressionContainer, ITextExpression
    {
        Expression<Func<ActivityContext, TResult>> expressionTree;
        Func<ActivityContext, TResult> compiledExpression;
        CompiledExpressionInvoker invoker; 

        public VisualBasicValue() 
            : base()
        {
            this.UseOldFastPath = true;
        }

        public VisualBasicValue(string expressionText)
            : this()
        {
            this.ExpressionText = expressionText;
        }

        public string ExpressionText
        {
            get;
            set;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Language
        {
            get
            {
                return VisualBasicHelper.Language;
            }
        }

        public bool RequiresCompilation
        {
            get
            {
                return true;
            }
        }

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

            if (metadata.Environment.IsValidating)
            {
                foreach (var validationError in VbExpressionValidator.Instance.Validate<TResult>(this, metadata.Environment, ExpressionText))
                {
                    AddTempValidationError(validationError);
                }
            }
            else
            {
                try
                {
                    var publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
                    expressionTree = VisualBasicHelper.Compile<TResult>(ExpressionText, publicAccessor, false);
                }
                catch (SourceExpressionException e)
                {
                    metadata.AddValidationError(e.Message);
                }
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
            return "[" + this.ExpressionText + "]";
        }

        public Expression GetExpressionTree()
        {            
            if (this.IsMetadataCached)
            {
                if (this.expressionTree == null)
                {
                    if (invoker != null)
                    {
                        return invoker.GetExpressionTree();
                    }
                    // it's safe to create this CodeActivityMetadata here,
                    // because we know we are using it only as lookup purpose.
                    CodeActivityMetadata metadata = new CodeActivityMetadata(this, this.GetParentEnvironment(), false);
                    CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.CreateWithoutArgument(metadata);
                    try
                    {                                                
                        expressionTree = VisualBasicHelper.Compile<TResult>(this.ExpressionText, publicAccessor, false);
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
                return ExpressionUtilities.RewriteNonCompiledExpressionTree((LambdaExpression)this.expressionTree);
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached)); 
            }
        }
    }
}

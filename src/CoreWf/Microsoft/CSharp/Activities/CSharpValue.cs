//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.CSharp.Activities
{
    using System;
    using CoreWf;
    using CoreWf.Expressions;
    using CoreWf.Validation;
    using CoreWf.XamlIntegration;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using CoreWf.Runtime;
    using Portable.Xaml.Markup;

    [DebuggerStepThrough]
    [ContentProperty("ExpressionText")]
    public class CSharpValue<TResult> : CodeActivity<TResult>, ITextExpression
    {
        CompiledExpressionInvoker invoker;
          
        public CSharpValue()
        {
            this.UseOldFastPath = true;
        }

        public CSharpValue(string expressionText) :
            this()
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
                return "C#";
            }
        }

        public bool RequiresCompilation
        {
            get
            {
                return true;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            this.invoker = new CompiledExpressionInvoker(this, false, metadata);
        }

        protected override TResult Execute(CodeActivityContext context)
        {
            return (TResult)this.invoker.InvokeExpression(context);
        }

        public Expression GetExpressionTree()
        {
            if (this.IsMetadataCached)
            {
                return this.invoker.GetExpressionTree();
            }
            else
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ActivityIsUncached));
            }
        }
    }
}

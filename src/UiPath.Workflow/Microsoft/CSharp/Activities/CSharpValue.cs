﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.Validation;
    using System.Activities.XamlIntegration;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Activities.Runtime;
    using System.Windows.Markup;
    using System.Activities.Internals;

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
            invoker = new CompiledExpressionInvoker(this, false, metadata);
            if (metadata.Environment.CompileExpressions)
            {
                return;
            }

            if (metadata.Environment.IsValidating)
            {
                foreach (var validationError in CsExpressionValidator.Default.Validate<TResult>(this, metadata.Environment, ExpressionText))
                {
                    AddTempValidationError(validationError);
                }
            }
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

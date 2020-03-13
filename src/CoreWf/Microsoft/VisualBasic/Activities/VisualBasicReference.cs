//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.VisualBasic.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Activities;
    using System.Activities.Expressions;
    using System.Activities.ExpressionParser;
    using System.Activities.XamlIntegration;
    using System.Linq.Expressions;
    using System.Windows.Markup;
    using System.ComponentModel;
    using System.Activities.Runtime;
    using System.Activities.Internals;

    [DebuggerStepThrough]
    public sealed class VisualBasicReference<TResult> : CodeActivity<Location<TResult>>, IValueSerializableExpression, /*IExpressionContainer, */ITextExpression
    {
        CompiledExpressionInvoker invoker;

        public VisualBasicReference()
            : base()
        {
            this.UseOldFastPath = true;
        }

        public VisualBasicReference(string expressionText)
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
                return true; /* TODO false */
            }
        }

        protected override Location<TResult> Execute(CodeActivityContext context)
        {
            /*
            if (!this.invoker.IsStaticallyCompiled)
            {
                if (this.expressionTree != null)
                {
                    if (this.locationFactory == null)
                    {
                        this.locationFactory = ExpressionUtilities.CreateLocationFactory<TResult>(this.expressionTree);
                    }

                    return this.locationFactory.CreateLocation(context);
                }
                else
                {
                    return null;
                }
            }
            else
            */
            {
                return (Location<TResult>) this.invoker.InvokeExpression(context);
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            this.invoker = new CompiledExpressionInvoker(this, true, metadata);

            /*
            if (this.invoker.IsStaticallyCompiled)
            {
                return;
            }

            string validationError;

            // If ICER is not implemented that means we haven't been compiled
            CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);
            this.expressionTree = this.CompileLocationExpression(publicAccessor, out validationError);

            if (validationError != null)
            {
                metadata.AddValidationError(validationError);
            }
            */
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
            return this.invoker.GetExpressionTree();
        }
    }
}

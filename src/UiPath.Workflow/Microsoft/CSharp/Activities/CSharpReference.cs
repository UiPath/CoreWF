// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.Common;
using System;
using System.Activities;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Microsoft.CSharp.Activities
{
    [System.Diagnostics.DebuggerStepThrough]
    [System.Windows.Markup.ContentProperty("ExpressionText")]
    public class CSharpReference<TResult> : Reference<TResult>
    {
        public CSharpReference()
        {
            this.UseOldFastPath = true;
        }

        public CSharpReference(string expressionText) : base(expressionText) { }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Language
        {
            get
            {
                return CSharpHelper.Language;
            }
        }

        protected override Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
            => CSharpHelper.Compile<T>(expressionText, publicAccessor, isLocationExpression);
    }
}


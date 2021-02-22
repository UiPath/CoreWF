// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.Activities
{
    using Microsoft.Common;
    using System;
    using System.Activities;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Windows.Markup;

    [System.Diagnostics.DebuggerStepThrough]
    [ContentProperty("ExpressionText")]
    public class CSharpValue<TResult> : Value<TResult>
    {
        public CSharpValue()
        {
            this.UseOldFastPath = true;
        }

        public CSharpValue(string expressionText) : base(expressionText) { }

        protected override Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
            => CSharpHelper.Compile<T>(expressionText, publicAccessor, isLocationExpression);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Language
        {
            get
            {
                return CSharpHelper.Language;
            }
        }
    }
}

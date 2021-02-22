// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.VisualBasic.Activities
{
    using Microsoft.Common;
    using System;
    using System.Activities;
    using System.Activities.XamlIntegration;
    using System.ComponentModel;
    using System.Linq.Expressions;
    using System.Windows.Markup;

    [System.Diagnostics.DebuggerStepThrough]
    public sealed class VisualBasicReference<TResult> : Reference<TResult>, IValueSerializableExpression
    {
        public VisualBasicReference()
        {
            this.UseOldFastPath = true;
        }

        public VisualBasicReference(string expressionText) : base(expressionText) { }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Language
        {
            get
            {
                return VisualBasicHelper.Language;
            }
        }

        protected override Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
            => VisualBasicHelper.Compile<T>(expressionText, publicAccessor, isLocationExpression);

        #region IValueSerializableExpression

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

        #endregion IValueSerializableExpression
    }
}
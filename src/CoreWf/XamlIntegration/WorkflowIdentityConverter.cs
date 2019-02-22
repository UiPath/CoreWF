// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    public class WorkflowIdentityConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString)
            {
                return WorkflowIdentity.Parse(valueString);
            }
            return base.ConvertFrom(context, culture, value);
        }

        // No need to override [Can]ConvertTo, it automatically calls ToString
    }
}

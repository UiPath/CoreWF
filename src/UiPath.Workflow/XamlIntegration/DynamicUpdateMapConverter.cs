// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Activities.DynamicUpdate;
    using System.ComponentModel;
    using System.Globalization;
    using System.Xaml;
    using System.Windows.Markup;

    public class DynamicUpdateMapConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(MarkupExtension);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            DynamicUpdateMap map = value as DynamicUpdateMap;
            if (destinationType == typeof(MarkupExtension) && map != null)
            {
                return new DynamicUpdateMapExtension(map);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

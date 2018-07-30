// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using CoreWf.DynamicUpdate;
    using System.ComponentModel;
    using System.Globalization;    

    public class ObjectMatchInfoConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue = value as string;
            int result;
            if (int.TryParse(stringValue, NumberStyles.Integer, culture, out result))
            {
                return new ObjectMatchInfo(result);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            ObjectMatchInfo objectInfo = value as ObjectMatchInfo;
            if (destinationType == typeof(string) && objectInfo != null)
            {
                return objectInfo.OriginalId.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

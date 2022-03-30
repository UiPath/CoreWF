// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;

namespace System.Activities.XamlIntegration;

public class ImplementationVersionConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string stringValue && Version.TryParse(stringValue, out var deserializedVersion))
        {
            return deserializedVersion;
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
        destinationType == typeof(string);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        var implementationVersion = value as Version;
        if (destinationType == typeof(string) && implementationVersion != null)
        {
            return implementationVersion.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}

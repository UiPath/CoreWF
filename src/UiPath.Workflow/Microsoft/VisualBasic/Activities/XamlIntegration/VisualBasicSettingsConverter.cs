// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualBasic.Activities.XamlIntegration;

// this class is necessary in order for our value serializer to get called by XAML,
// even though the functionality is a no-op
public sealed class VisualBasicSettingsConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
        sourceType == TypeHelper.StringType || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
        destinationType != TypeHelper.StringType && base.CanConvertTo(context, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string sourceString)
        {
            return base.ConvertFrom(context, culture, value);
        }

        if (sourceString.Equals(VisualBasicSettingsValueSerializer.ImplementationVisualBasicSettingsValue))
        {
            // this is the VBSettings for the internal implementation
            // suppress its Xaml serialization
            var settings = CollectXmlNamespacesAndAssemblies(context);
            if (settings != null)
            {
                settings.SuppressXamlSerialization = true;
            }

            return settings;
        }

        if (!(sourceString.Equals(string.Empty) ||
            sourceString.Equals(VisualBasicSettingsValueSerializer.VisualBasicSettingsValue)))
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidVisualBasicSettingsValue));
        }

        return CollectXmlNamespacesAndAssemblies(context);
    }

    private static VisualBasicSettings CollectXmlNamespacesAndAssemblies(ITypeDescriptorContext context) =>
        VisualBasicExpressionConverter.CollectXmlNamespacesAndAssemblies(context);
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.Activities.XamlIntegration
{
    using System;
    using System.ComponentModel;
    using System.Activities.Runtime;
    using System.Globalization;
    using System.Activities;
    using System.Activities.Internals;
    using Microsoft.VisualBasic.Activities;
    using Microsoft.VisualBasic.Activities.XamlIntegration;

    // this class is necessary in order for our value serializer to get called by XAML,
    // even though the functionality is a no-op
    public sealed class CSharpSettingsConverter : TypeConverter
    {        
        public CSharpSettingsConverter()
            : base()
        {
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == TypeHelper.StringType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == TypeHelper.StringType)
            {
                return false;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string sourceString = value as string;
            if (sourceString != null)
            {
                if (sourceString.Equals(VisualBasicSettingsValueSerializer.ImplementationVisualBasicSettingsValue))
                {
                    // this is the VBSettings for the internal implementation
                    // suppress its Xaml serialization
                    VisualBasicSettings settings = CollectXmlNamespacesAndAssemblies(context);
                    if (settings != null)
                    {
                        settings.SuppressXamlSerialization = true;
                    }
                    return settings;
                }

                if (!(sourceString.Equals(String.Empty) || sourceString.Equals(VisualBasicSettingsValueSerializer.VisualBasicSettingsValue)))
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidSettingsValue));
                }

                return CollectXmlNamespacesAndAssemblies(context);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        VisualBasicSettings CollectXmlNamespacesAndAssemblies(ITypeDescriptorContext context)
        {
            return CSharpExpressionConverter.CollectXmlNamespacesAndAssemblies(context);
        }

    }
}

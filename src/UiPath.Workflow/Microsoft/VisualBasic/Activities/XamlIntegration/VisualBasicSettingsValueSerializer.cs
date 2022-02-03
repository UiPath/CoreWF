// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace Microsoft.VisualBasic.Activities.XamlIntegration;

// this value serializer always returns false for CanConvertToString, but
// needs to add namespace declarations to the context
// 
public sealed class VisualBasicSettingsValueSerializer : ValueSerializer
{
    internal const string VisualBasicSettingsValue =
        "Assembly references and imported namespaces serialized as XML namespaces";

    internal const string ImplementationVisualBasicSettingsValue =
        "Assembly references and imported namespaces for internal implementation";

    public override bool CanConvertToString(object value, IValueSerializerContext context)
    {
        var settings = value as VisualBasicSettings;

        // promote settings to xmlns declarations
        settings?.GenerateXamlReferences(context);

        return true;
    }

    public override string ConvertToString(object value, IValueSerializerContext context) =>
        value is VisualBasicSettings {SuppressXamlSerialization: true}
            ? ImplementationVisualBasicSettingsValue
            : VisualBasicSettingsValue;
}

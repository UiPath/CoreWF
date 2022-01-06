// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime;

[Fx.Tag.XamlVisible(false)]
internal static class PersistenceMetadataNamespace
{
    private const string baseNamespace = "urn:schemas-microsoft-com:System.Runtime.DurableInstancing/4.0/metadata";
    private static readonly XNamespace s_persistenceMetadataNamespace = XNamespace.Get(baseNamespace);

    private static XName s_instanceType;
    private static XName s_activationType;

    public static XName InstanceType
    {
        get
        {
            s_instanceType ??= s_persistenceMetadataNamespace.GetName("InstanceType");
            return s_instanceType;
        }
    }

    public static XName ActivationType
    {
        get
        {
            s_activationType ??= s_persistenceMetadataNamespace.GetName("ActivationType");
            return s_activationType;
        }
    }
}

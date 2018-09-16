// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace CoreWf.Runtime
{
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
                if (s_instanceType == null)
                {
                    s_instanceType = s_persistenceMetadataNamespace.GetName("InstanceType");
                }

                return s_instanceType;
            }
        }

        public static XName ActivationType
        {
            get
            {
                if (s_activationType == null)
                {
                    s_activationType = s_persistenceMetadataNamespace.GetName("ActivationType");
                }

                return s_activationType;
            }
        }

        public static class ActivationTypes
        {
            private const string baseNamespace = "urn:schemas-microsoft-com:System.ServiceModel.Activation";
            private static readonly XNamespace s_activationNamespace = XNamespace.Get(baseNamespace);

            private static XName s_was;

            public static XName WAS
            {
                get
                {
                    if (s_was == null)
                    {
                        s_was = s_activationNamespace.GetName("WindowsProcessActivationService");
                    }

                    return s_was;
                }
            }
        }
    }
}

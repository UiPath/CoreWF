// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace LegacyTest.Test.Common.TestObjects.Utilities
{
    public static class WorkflowNamespace
    {
        private static XNamespace s_workflowNameSpace = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties");

        public static XName WorkflowHostTypeName
        {
            get
            {
                return s_workflowNameSpace.GetName("WorkflowHostType");
            }
        }
    }

    public static class Workflow45Namespace
    {
        private static XNamespace s_workflow45NameSpace = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.5/properties");

        public static XName DefinitionIdentity
        {
            get
            {
                return s_workflow45NameSpace.GetName("DefinitionIdentity");
            }
        }

        public static XName DefinitionIdentities
        {
            get
            {
                return s_workflow45NameSpace.GetName("DefinitionIdentities");
            }
        }

        public static XName DefinitionIdentityFilter
        {
            get
            {
                return s_workflow45NameSpace.GetName("DefinitionIdentityFilter");
            }
        }

        public static XName WorkflowApplication
        {
            get
            {
                return s_workflow45NameSpace.GetName("WorkflowApplication");
            }
        }
    }

    public static class PersistenceNamespace
    {
        private static XNamespace s_persistenceNamespace = XNamespace.Get("urn:schemas-microsoft-com:System.Runtime.DurableInstancing/4.0/metadata");
        private static XNamespace s_activationNameSpace = XNamespace.Get("urn:schemas-microsoft-com:System.ServiceModel.Activation");

        public static XName ActivationType
        {
            get
            {
                return s_persistenceNamespace.GetName("ActivationType");
            }
        }

        public static XName WASActivation
        {
            get
            {
                return s_activationNameSpace.GetName("WindowsProcessActivationService");
            }
        }
    }
}

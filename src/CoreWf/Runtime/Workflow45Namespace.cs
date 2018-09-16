// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace CoreWf.Runtime
{
    [Fx.Tag.XamlVisible(false)]
    internal static class Workflow45Namespace
    {
        private const string baseNamespace = "urn:schemas-microsoft-com:CoreWf/4.5/properties";
        private static readonly XNamespace s_workflow45Namespace = XNamespace.Get(baseNamespace);

        private static XName s_definitionIdentity;
        private static XName s_definitionIdentities;
        private static XName s_definitionIdentityFilter;
        private static XName s_workflowApplication;


        public static XName DefinitionIdentity
        {
            get
            {
                if (s_definitionIdentity == null)
                {
                    s_definitionIdentity = s_workflow45Namespace.GetName("DefinitionIdentity");
                }

                return s_definitionIdentity;
            }
        }

        public static XName DefinitionIdentities
        {
            get
            {
                if (s_definitionIdentities == null)
                {
                    s_definitionIdentities = s_workflow45Namespace.GetName("DefinitionIdentities");
                }

                return s_definitionIdentities;
            }
        }

        public static XName DefinitionIdentityFilter
        {
            get
            {
                if (s_definitionIdentityFilter == null)
                {
                    s_definitionIdentityFilter = s_workflow45Namespace.GetName("DefinitionIdentityFilter");
                }

                return s_definitionIdentityFilter;
            }
        }

        public static XName WorkflowApplication
        {
            get
            {
                if (s_workflowApplication == null)
                {
                    s_workflowApplication = s_workflow45Namespace.GetName("WorkflowApplication");
                }

                return s_workflowApplication;
            }
        }
    }
}

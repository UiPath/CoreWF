// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace CoreWf.Runtime
{
    [Fx.Tag.XamlVisible(false)]
    internal static class WorkflowNamespace
    {
        private const string baseNamespace = "urn:schemas-microsoft-com:CoreWf/4.0/properties";
        private static readonly XNamespace s_workflowNamespace = XNamespace.Get(baseNamespace);
        private static readonly XNamespace s_variablesNamespace = XNamespace.Get(baseNamespace + "/variables");
        private static readonly XNamespace s_outputNamespace = XNamespace.Get(baseNamespace + "/output");

        private static XName s_workflowHostType;
        private static XName s_status;
        private static XName s_bookmarks;
        private static XName s_lastUpdate;
        private static XName s_exception;
        private static XName s_workflow;
        private static XName s_keyProvider;

        public static XNamespace VariablesPath
        {
            get
            {
                return s_variablesNamespace;
            }
        }

        public static XNamespace OutputPath
        {
            get
            {
                return s_outputNamespace;
            }
        }

        public static XName WorkflowHostType
        {
            get
            {
                if (s_workflowHostType == null)
                {
                    s_workflowHostType = s_workflowNamespace.GetName("WorkflowHostType");
                }

                return s_workflowHostType;
            }
        }

        public static XName Status
        {
            get
            {
                if (s_status == null)
                {
                    s_status = s_workflowNamespace.GetName("Status");
                }
                return s_status;
            }
        }

        public static XName Bookmarks
        {
            get
            {
                if (s_bookmarks == null)
                {
                    s_bookmarks = s_workflowNamespace.GetName("Bookmarks");
                }
                return s_bookmarks;
            }
        }

        public static XName LastUpdate
        {
            get
            {
                if (s_lastUpdate == null)
                {
                    s_lastUpdate = s_workflowNamespace.GetName("LastUpdate");
                }
                return s_lastUpdate;
            }
        }

        public static XName Exception
        {
            get
            {
                if (s_exception == null)
                {
                    s_exception = s_workflowNamespace.GetName("Exception");
                }
                return s_exception;
            }
        }

        public static XName Workflow
        {
            get
            {
                if (s_workflow == null)
                {
                    s_workflow = s_workflowNamespace.GetName("Workflow");
                }
                return s_workflow;
            }
        }

        public static XName KeyProvider
        {
            get
            {
                if (s_keyProvider == null)
                {
                    s_keyProvider = s_workflowNamespace.GetName("KeyProvider");
                }
                return s_keyProvider;
            }
        }
    }
}

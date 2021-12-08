// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime;

[Fx.Tag.XamlVisible(false)]
internal static class WorkflowServiceNamespace
{
    private const string baseNamespace = "urn:schemas-microsoft-com:System.ServiceModel.Activities/4.0/properties";
    private static readonly XNamespace s_workflowServiceNamespace = XNamespace.Get(baseNamespace);
    private static readonly XNamespace s_endpointsNamespace = XNamespace.Get(baseNamespace + "/endpoints");

    private static XName s_controlEndpoint;
    private static XName s_suspendException;
    private static XName s_suspendReason;
    private static XName s_siteName;
    private static XName s_relativeApplicationPath;
    private static XName s_relativeServicePath;
    private static XName s_creationContext;
    private static XName s_service;
    private static XName s_requestReplyCorrelation;
    private static XName s_messageVersionForReplies;

    public static XNamespace EndpointsPath => s_endpointsNamespace;

    public static XName ControlEndpoint
    {
        get
        {
            s_controlEndpoint ??= s_workflowServiceNamespace.GetName("ControlEndpoint");
            return s_controlEndpoint;
        }
    }

    public static XName MessageVersionForReplies
    {
        get
        {
            s_messageVersionForReplies ??= s_workflowServiceNamespace.GetName("MessageVersionForReplies");
            return s_messageVersionForReplies;
        }
    }

    public static XName RequestReplyCorrelation
    {
        get
        {
            s_requestReplyCorrelation ??= s_workflowServiceNamespace.GetName("RequestReplyCorrelation");
            return s_requestReplyCorrelation;
        }
    }

    public static XName SuspendReason
    {
        get
        {
            s_suspendReason ??= s_workflowServiceNamespace.GetName("SuspendReason");
            return s_suspendReason;
        }
    }

    public static XName SiteName
    {
        get
        {
            s_siteName ??= s_workflowServiceNamespace.GetName("SiteName");
            return s_siteName;
        }
    }

    public static XName SuspendException
    {
        get
        {
            s_suspendException ??= s_workflowServiceNamespace.GetName("SuspendException");
            return s_suspendException;
        }
    }

    public static XName RelativeApplicationPath
    {
        get
        {
            s_relativeApplicationPath ??= s_workflowServiceNamespace.GetName("RelativeApplicationPath");
            return s_relativeApplicationPath;
        }
    }

    public static XName RelativeServicePath
    {
        get
        {
            s_relativeServicePath ??= s_workflowServiceNamespace.GetName("RelativeServicePath");
            return s_relativeServicePath;
        }
    }

    public static XName CreationContext
    {
        get
        {
            s_creationContext ??= s_workflowServiceNamespace.GetName("CreationContext");
            return s_creationContext;
        }
    }

    public static XName Service
    {
        get
        {
            s_service ??= s_workflowServiceNamespace.GetName("Service");
            return s_service;
        }
    }
}

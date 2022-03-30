// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing;

[Serializable]
public class InstanceCompleteException : InstancePersistenceCommandException
{
    public InstanceCompleteException()
        : this(SR.InstanceCompleteDefault, null)
    {
    }

    public InstanceCompleteException(string message)
        : this(message, null)
    {
    }

    public InstanceCompleteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InstanceCompleteException(XName commandName, Guid instanceId)
        : this(commandName, instanceId, null)
    {
    }

    public InstanceCompleteException(XName commandName, Guid instanceId, Exception innerException)
        : this(commandName, instanceId, ToMessage(instanceId), innerException)
    {
    }

    public InstanceCompleteException(XName commandName, Guid instanceId, string message, Exception innerException)
        : base(commandName, instanceId, message, innerException)
    {
    }

    protected InstanceCompleteException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    private static string ToMessage(Guid instanceId)
    {
        if (instanceId != Guid.Empty)
        {
            return SR.InstanceCompleteSpecific(instanceId);
        }
        return SR.InstanceCompleteDefault;
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing;

[Serializable]
public class InstanceHandleConflictException : InstancePersistenceCommandException
{
    public InstanceHandleConflictException()
        : this(SR.InstanceHandleConflictDefault, null)
    {
    }

    public InstanceHandleConflictException(string message)
        : this(message, null)
    {
    }

    public InstanceHandleConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InstanceHandleConflictException(XName commandName, Guid instanceId)
        : this(commandName, instanceId, null)
    {
    }

    public InstanceHandleConflictException(XName commandName, Guid instanceId, Exception innerException)
        : this(commandName, instanceId, ToMessage(instanceId), innerException)
    {
    }

    public InstanceHandleConflictException(XName commandName, Guid instanceId, string message, Exception innerException)
        : base(commandName, instanceId, message, innerException)
    {
    }

    protected InstanceHandleConflictException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    private static string ToMessage(Guid instanceId)
    {
        if (instanceId != Guid.Empty)
        {
            return SR.InstanceHandleConflictSpecific(instanceId);
        }
        return SR.InstanceHandleConflictDefault;
    }
}

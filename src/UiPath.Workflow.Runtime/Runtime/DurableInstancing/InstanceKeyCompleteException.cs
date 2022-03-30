// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing;

[Serializable]
public class InstanceKeyCompleteException : InstancePersistenceCommandException
{
    private const string InstanceKeyName = "instancePersistenceInstanceKey";

    public InstanceKeyCompleteException()
        : this(SR.KeyNotReadyDefault, null)
    {
    }

    public InstanceKeyCompleteException(string message)
        : this(message, null)
    {
    }

    public InstanceKeyCompleteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InstanceKeyCompleteException(XName commandName, InstanceKey instanceKey)
        : this(commandName, instanceKey, null)
    {
    }

    public InstanceKeyCompleteException(XName commandName, InstanceKey instanceKey, Exception innerException)
        : this(commandName, Guid.Empty, instanceKey, ToMessage(instanceKey), innerException)
    {
    }

    public InstanceKeyCompleteException(XName commandName, Guid instanceId, InstanceKey instanceKey, string message, Exception innerException)
        : base(commandName, instanceId, message, innerException)
    {
        InstanceKey = instanceKey;
    }

    protected InstanceKeyCompleteException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Guid guid = (Guid)info.GetValue(InstanceKeyName, typeof(Guid));
        InstanceKey = guid == Guid.Empty ? null : new InstanceKey(guid);
    }

    public InstanceKey InstanceKey { get; private set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(InstanceKeyName, (InstanceKey != null && InstanceKey.IsValid) ? InstanceKey.Value : Guid.Empty, typeof(Guid));
    }

    private static string ToMessage(InstanceKey instanceKey)
    {
        if (instanceKey != null && instanceKey.IsValid)
        {
            return SR.KeyCompleteSpecific(instanceKey.Value);
        }
        return SR.KeyCompleteDefault;
    }
}

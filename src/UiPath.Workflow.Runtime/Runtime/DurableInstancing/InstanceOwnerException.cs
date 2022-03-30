// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing;

[Serializable]
public class InstanceOwnerException : InstancePersistenceException
{
    private const string InstanceOwnerIdName = "instancePersistenceInstanceOwnerId";

    public InstanceOwnerException()
        : base(SR.InstanceOwnerDefault)
    {
    }

    public InstanceOwnerException(string message)
        : base(message)
    {
    }

    public InstanceOwnerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public InstanceOwnerException(XName commandName, Guid instanceOwnerId)
        : this(commandName, instanceOwnerId, null)
    {
    }

    public InstanceOwnerException(XName commandName, Guid instanceOwnerId, Exception innerException)
        : this(commandName, instanceOwnerId, ToMessage(instanceOwnerId), innerException)
    {
    }

    public InstanceOwnerException(XName commandName, Guid instanceOwnerId, string message, Exception innerException)
        : base(commandName, message, innerException)
    {
        InstanceOwnerId = instanceOwnerId;
    }

    protected InstanceOwnerException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        InstanceOwnerId = (Guid)info.GetValue(InstanceOwnerIdName, typeof(Guid));
    }

    public Guid InstanceOwnerId { get; private set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(InstanceOwnerIdName, InstanceOwnerId, typeof(Guid));
    }

    private static string ToMessage(Guid instanceOwnerId)
    {
        if (instanceOwnerId == Guid.Empty)
        {
            return SR.InstanceOwnerDefault;
        }
        return SR.InstanceOwnerSpecific(instanceOwnerId);
    }
}

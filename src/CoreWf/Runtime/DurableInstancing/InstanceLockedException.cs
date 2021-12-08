// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceLockedException : InstancePersistenceCommandException
    {
        private const string InstanceOwnerIdName = "instancePersistenceInstanceOwnerId";
        private const string SerializableInstanceOwnerMetadataName = "instancePersistenceSerializableInstanceOwnerMetadata";

        public InstanceLockedException()
            : this(SR.CannotAcquireLockDefault, null)
        {
        }

        public InstanceLockedException(string message)
            : this(message, null)
        {
        }

        public InstanceLockedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InstanceLockedException(XName commandName, Guid instanceId)
            : this(commandName, instanceId, null)
        {
        }

        public InstanceLockedException(XName commandName, Guid instanceId, Exception innerException)
            : this(commandName, instanceId, ToMessage(instanceId), innerException)
        {
        }

        public InstanceLockedException(XName commandName, Guid instanceId, string message, Exception innerException)
            : this(commandName, instanceId, Guid.Empty, null, message, innerException)
        {
        }

        public InstanceLockedException(XName commandName, Guid instanceId, Guid instanceOwnerId, IDictionary<XName, object> serializableInstanceOwnerMetadata)
            : this(commandName, instanceId, instanceOwnerId, serializableInstanceOwnerMetadata, null)
        {
        }

        public InstanceLockedException(XName commandName, Guid instanceId, Guid instanceOwnerId, IDictionary<XName, object> serializableInstanceOwnerMetadata, Exception innerException)
            : this(commandName, instanceId, instanceOwnerId, serializableInstanceOwnerMetadata, ToMessage(instanceId, instanceOwnerId), innerException)
        {
        }

        // Copying the dictionary snapshots it and makes sure the IDictionary implementation is serializable.
        public InstanceLockedException(XName commandName, Guid instanceId, Guid instanceOwnerId, IDictionary<XName, object> serializableInstanceOwnerMetadata, string message, Exception innerException)
            : base(commandName, instanceId, message, innerException)
        {
            InstanceOwnerId = instanceOwnerId;
            if (serializableInstanceOwnerMetadata != null)
            {
                Dictionary<XName, object> copy = new Dictionary<XName, object>(serializableInstanceOwnerMetadata);
                SerializableInstanceOwnerMetadata = new ReadOnlyDictionary<XName, object>(copy);
            }
        }

        //[SecurityCritical]
        //protected InstanceLockedException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    InstanceOwnerId = (Guid)info.GetValue(InstanceOwnerIdName, typeof(Guid));
        //    SerializableInstanceOwnerMetadata = (ReadOnlyDictionary<XName, object>)info.GetValue(SerializableInstanceOwnerMetadataName, typeof(ReadOnlyDictionary<XName, object>));
        //}

        public Guid InstanceOwnerId { get; private set; }

        public IDictionary<XName, object> SerializableInstanceOwnerMetadata { get; private set; }

        //[Fx.Tag.SecurityNote(Critical = "Overrides critical inherited method")]
        //[SecurityCritical]
        ////[SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureGetObjectDataOverrides,
        //    //Justification = "Method is SecurityCritical")]
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    base.GetObjectData(info, context);
        //    info.AddValue(InstanceOwnerIdName, InstanceOwnerId, typeof(Guid));
        //    info.AddValue(SerializableInstanceOwnerMetadataName, SerializableInstanceOwnerMetadata, typeof(ReadOnlyDictionary<XName, object>));
        //}

        private static string ToMessage(Guid instanceId)
        {
            if (instanceId == Guid.Empty)
            {
                return SR.CannotAcquireLockDefault;
            }
            return SR.CannotAcquireLockSpecific(instanceId);
        }

        private static string ToMessage(Guid instanceId, Guid instanceOwnerId)
        {
            if (instanceId == Guid.Empty)
            {
                return SR.CannotAcquireLockDefault;
            }
            if (instanceOwnerId == Guid.Empty)
            {
                return SR.CannotAcquireLockSpecific(instanceId);
            }
            return SR.CannotAcquireLockSpecificWithOwner(instanceId, instanceOwnerId);
        }
    }
}

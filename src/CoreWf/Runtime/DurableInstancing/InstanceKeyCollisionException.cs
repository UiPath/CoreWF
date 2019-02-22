// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceKeyCollisionException : InstancePersistenceCommandException
    {
        private const string ConflictingInstanceIdName = "instancePersistenceConflictingInstanceId";
        private const string InstanceKeyName = "instancePersistenceInstanceKey";

        public InstanceKeyCollisionException()
            : this(SR.KeyCollisionDefault, null)
        {
        }

        public InstanceKeyCollisionException(string message)
            : this(message, null)
        {
        }

        public InstanceKeyCollisionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InstanceKeyCollisionException(XName commandName, Guid instanceId, InstanceKey instanceKey, Guid conflictingInstanceId)
            : this(commandName, instanceId, instanceKey, conflictingInstanceId, null)
        {
        }

        public InstanceKeyCollisionException(XName commandName, Guid instanceId, InstanceKey instanceKey, Guid conflictingInstanceId, Exception innerException)
            : this(commandName, instanceId, instanceKey, conflictingInstanceId, ToMessage(instanceId, instanceKey, conflictingInstanceId), innerException)
        {
        }

        public InstanceKeyCollisionException(XName commandName, Guid instanceId, InstanceKey instanceKey, Guid conflictingInstanceId, string message, Exception innerException)
            : base(commandName, instanceId, message, innerException)
        {
            ConflictingInstanceId = conflictingInstanceId;
            InstanceKey = instanceKey;
        }

        //[SecurityCritical]
        //protected InstanceKeyCollisionException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    ConflictingInstanceId = (Guid)info.GetValue(ConflictingInstanceIdName, typeof(Guid));
        //    Guid guid = (Guid)info.GetValue(InstanceKeyName, typeof(Guid));
        //    InstanceKey = guid == Guid.Empty ? null : new InstanceKey(guid);
        //}

        public Guid ConflictingInstanceId { get; private set; }

        public InstanceKey InstanceKey { get; private set; }

        //[Fx.Tag.SecurityNote(Critical = "Overrides critical inherited method")]
        //[SecurityCritical]
        ////[SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureGetObjectDataOverrides,
        //    //Justification = "Method is SecurityCritical")]
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    base.GetObjectData(info, context);
        //    info.AddValue(ConflictingInstanceIdName, ConflictingInstanceId, typeof(Guid));
        //    info.AddValue(InstanceKeyName, (InstanceKey != null && InstanceKey.IsValid) ? InstanceKey.Value : Guid.Empty, typeof(Guid));
        //}

        private static string ToMessage(Guid instanceId, InstanceKey instanceKey, Guid conflictingInstanceId)
        {
            if (instanceKey != null && instanceKey.IsValid)
            {
                if (instanceId != Guid.Empty && conflictingInstanceId != Guid.Empty)
                {
                    return SR.KeyCollisionSpecific(instanceId, instanceKey.Value, conflictingInstanceId);
                }
                return SR.KeyCollisionSpecificKeyOnly(instanceKey.Value);
            }
            return SR.KeyCollisionDefault;
        }
    }
}

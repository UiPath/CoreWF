// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceLockLostException : InstancePersistenceCommandException
    {
        public InstanceLockLostException()
            : this(SR.InstanceLockLostDefault, null)
        {
        }

        public InstanceLockLostException(string message)
            : this(message, null)
        {
        }

        public InstanceLockLostException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InstanceLockLostException(XName commandName, Guid instanceId)
            : this(commandName, instanceId, null)
        {
        }

        public InstanceLockLostException(XName commandName, Guid instanceId, Exception innerException)
            : this(commandName, instanceId, ToMessage(instanceId), innerException)
        {
        }

        public InstanceLockLostException(XName commandName, Guid instanceId, string message, Exception innerException)
            : base(commandName, instanceId, message, innerException)
        {
        }

        //[SecurityCritical]
        //protected InstanceLockLostException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}

        private static string ToMessage(Guid instanceId)
        {
            if (instanceId != Guid.Empty)
            {
                return SR.InstanceLockLostSpecific(instanceId);
            }
            return SR.InstanceLockLostDefault;
        }
    }
}

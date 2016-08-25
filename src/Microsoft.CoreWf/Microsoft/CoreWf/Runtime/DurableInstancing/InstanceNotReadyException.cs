// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceNotReadyException : InstancePersistenceCommandException
    {
        public InstanceNotReadyException()
            : this(SRCore.InstanceNotReadyDefault, null)
        {
        }

        public InstanceNotReadyException(string message)
            : this(message, null)
        {
        }

        public InstanceNotReadyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InstanceNotReadyException(XName commandName, Guid instanceId)
            : this(commandName, instanceId, null)
        {
        }

        public InstanceNotReadyException(XName commandName, Guid instanceId, Exception innerException)
            : this(commandName, instanceId, ToMessage(instanceId), innerException)
        {
        }

        public InstanceNotReadyException(XName commandName, Guid instanceId, string message, Exception innerException)
            : base(commandName, instanceId, message, innerException)
        {
        }

        //[SecurityCritical]
        //protected InstanceNotReadyException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}

        private static string ToMessage(Guid instanceId)
        {
            if (instanceId != Guid.Empty)
            {
                return SRCore.InstanceNotReadySpecific(instanceId);
            }
            return SRCore.InstanceNotReadyDefault;
        }
    }
}

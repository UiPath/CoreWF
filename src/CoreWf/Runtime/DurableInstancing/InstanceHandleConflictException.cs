// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceHandleConflictException : InstancePersistenceCommandException
    {
        public InstanceHandleConflictException()
            : this(SRCore.InstanceHandleConflictDefault, null)
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

        //[SecurityCritical]
        //protected InstanceHandleConflictException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //}

        private static string ToMessage(Guid instanceId)
        {
            if (instanceId != Guid.Empty)
            {
                return SRCore.InstanceHandleConflictSpecific(instanceId);
            }
            return SRCore.InstanceHandleConflictDefault;
        }
    }
}

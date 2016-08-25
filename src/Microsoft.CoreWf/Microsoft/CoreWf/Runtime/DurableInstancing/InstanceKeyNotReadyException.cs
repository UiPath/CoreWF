// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    //[Serializable]
    public class InstanceKeyNotReadyException : InstancePersistenceCommandException
    {
        private const string InstanceKeyName = "instancePersistenceInstanceKey";

        public InstanceKeyNotReadyException()
            : this(SRCore.KeyNotReadyDefault, null)
        {
        }

        public InstanceKeyNotReadyException(string message)
            : this(message, null)
        {
        }

        public InstanceKeyNotReadyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InstanceKeyNotReadyException(XName commandName, InstanceKey instanceKey)
            : this(commandName, instanceKey, null)
        {
        }

        public InstanceKeyNotReadyException(XName commandName, InstanceKey instanceKey, Exception innerException)
            : this(commandName, Guid.Empty, instanceKey, ToMessage(instanceKey), innerException)
        {
        }

        public InstanceKeyNotReadyException(XName commandName, Guid instanceId, InstanceKey instanceKey, string message, Exception innerException)
            : base(commandName, instanceId, message, innerException)
        {
            InstanceKey = instanceKey;
        }

        //[SecurityCritical]
        //protected InstanceKeyNotReadyException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    Guid guid = (Guid)info.GetValue(InstanceKeyName, typeof(Guid));
        //    InstanceKey = guid == Guid.Empty ? null : new InstanceKey(guid);
        //}

        public InstanceKey InstanceKey { get; private set; }

        //[Fx.Tag.SecurityNote(Critical = "Overrides critical inherited method")]
        //[SecurityCritical]
        ////[SuppressMessage(FxCop.Category.Security, FxCop.Rule.SecureGetObjectDataOverrides,
        //    //Justification = "Method is SecurityCritical")]
        //public override void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    base.GetObjectData(info, context);
        //    info.AddValue(InstanceKeyName, (InstanceKey != null && InstanceKey.IsValid) ? InstanceKey.Value : Guid.Empty, typeof(Guid));
        //}

        private static string ToMessage(InstanceKey instanceKey)
        {
            if (instanceKey != null && instanceKey.IsValid)
            {
                return SRCore.KeyNotReadySpecific(instanceKey.Value);
            }
            return SRCore.KeyNotReadyDefault;
        }
    }
}

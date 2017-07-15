// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    public abstract class InstancePersistenceEvent : IEquatable<InstancePersistenceEvent>
    {
        internal InstancePersistenceEvent(XName name)
        {
            if (name == null)
            {
                throw Fx.Exception.ArgumentNull("name");
            }
            Name = name;
        }

        public XName Name { get; private set; }

        public bool Equals(InstancePersistenceEvent persistenceEvent)
        {
            return !object.ReferenceEquals(persistenceEvent, null) && persistenceEvent.Name == Name;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InstancePersistenceEvent);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static bool operator ==(InstancePersistenceEvent left, InstancePersistenceEvent right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }
            else if (object.ReferenceEquals(left, null))
            {
                return false;
            }
            else
            {
                return left.Equals(right);
            }
        }

        public static bool operator !=(InstancePersistenceEvent left, InstancePersistenceEvent right)
        {
            return !(left == right);
        }
    }

    public abstract class InstancePersistenceEvent<T> : InstancePersistenceEvent
        where T : InstancePersistenceEvent<T>, new()
    {
        private static T s_instance;

        protected InstancePersistenceEvent(XName name)
            : base(name)
        {
        }

        public static T Value
        {
            get
            {
                if (s_instance == null)
                {
                    Interlocked.CompareExchange<T>(ref s_instance, new T(), null);
                }
                return s_instance;
            }
        }
    }
}

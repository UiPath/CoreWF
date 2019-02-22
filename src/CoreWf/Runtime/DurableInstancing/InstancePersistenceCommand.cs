// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    public abstract class InstancePersistenceCommand
    {
        protected InstancePersistenceCommand(XName name)
        {
            Name = name ?? throw Fx.Exception.ArgumentNull(nameof(name));
        }

        public XName Name { get; private set; }

        protected internal virtual bool IsTransactionEnlistmentOptional
        {
            get
            {
                return false;
            }
        }

        // For now, only support registering to bind once the owner is established.  (Can't create an owner and take a lock in one command.)
        protected internal virtual bool AutomaticallyAcquiringLock
        {
            get
            {
                return false;
            }
        }

        protected internal virtual void Validate(InstanceView view)
        {
        }

        internal virtual IEnumerable<InstancePersistenceCommand> Reduce(InstanceView view)
        {
            return null;
        }
    }
}

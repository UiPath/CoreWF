// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    // InstanceStore owns the synchronization of this class.
    internal class InstanceNormalEvent : InstancePersistenceEvent
    {
        private HashSet<InstanceHandle> _boundHandles = new HashSet<InstanceHandle>();
        private HashSet<InstanceHandle> _pendingHandles = new HashSet<InstanceHandle>();

        internal InstanceNormalEvent(InstancePersistenceEvent persistenceEvent)
            : base(persistenceEvent.Name)
        {
        }

        internal bool IsSignaled { get; set; }

        internal HashSet<InstanceHandle> BoundHandles
        {
            get
            {
                return _boundHandles;
            }
        }

        internal HashSet<InstanceHandle> PendingHandles
        {
            get
            {
                return _pendingHandles;
            }
        }
    }
}

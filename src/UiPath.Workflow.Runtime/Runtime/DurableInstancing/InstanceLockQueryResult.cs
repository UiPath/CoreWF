// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceLockQueryResult : InstanceStoreQueryResult
    {
        private static readonly ReadOnlyDictionary<Guid, Guid> s_emptyQueryResult = new ReadOnlyDictionary<Guid, Guid>(new Dictionary<Guid, Guid>(0));

        // Zero
        public InstanceLockQueryResult()
        {
            InstanceOwnerIds = s_emptyQueryResult;
        }

        // One
        public InstanceLockQueryResult(Guid instanceId, Guid instanceOwnerId)
        {
            Dictionary<Guid, Guid> owners = new Dictionary<Guid, Guid>(1);
            owners.Add(instanceId, instanceOwnerId);
            InstanceOwnerIds = new ReadOnlyDictionary<Guid, Guid>(owners);
        }

        // N
        public InstanceLockQueryResult(IDictionary<Guid, Guid> instanceOwnerIds)
        {
            Dictionary<Guid, Guid> copy = new Dictionary<Guid, Guid>(instanceOwnerIds);
            InstanceOwnerIds = new ReadOnlyDictionary<Guid, Guid>(copy);
        }

        public IDictionary<Guid, Guid> InstanceOwnerIds { get; private set; }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceOwnerQueryResult : InstanceStoreQueryResult
    {
        private static readonly ReadOnlyDictionary<Guid, IDictionary<XName, InstanceValue>> s_emptyQueryResult = new ReadOnlyDictionary<Guid, IDictionary<XName, InstanceValue>>(new Dictionary<Guid, IDictionary<XName, InstanceValue>>(0));
        private static readonly ReadOnlyDictionary<XName, InstanceValue> s_emptyMetadata = new ReadOnlyDictionary<XName, InstanceValue>(new Dictionary<XName, InstanceValue>(0));

        // Zero
        public InstanceOwnerQueryResult()
        {
            InstanceOwners = s_emptyQueryResult;
        }

        // One
        public InstanceOwnerQueryResult(Guid instanceOwnerId, IDictionary<XName, InstanceValue> metadata)
        {
            Dictionary<Guid, IDictionary<XName, InstanceValue>> owners = new Dictionary<Guid, IDictionary<XName, InstanceValue>>(1);
            IDictionary<XName, InstanceValue> safeMetadata; // if metadata is not readonly, copy it.
            if (metadata == null || metadata.IsReadOnly)
                safeMetadata = metadata;
            else
            {
                // Copy dictionary & make a readonly wrapper.
                IDictionary<XName, InstanceValue> copy = new Dictionary<XName, InstanceValue>(metadata);
                safeMetadata = new ReadOnlyDictionary<XName, InstanceValue>(copy);
            }
            owners.Add(instanceOwnerId, metadata == null ? s_emptyMetadata : safeMetadata);
            InstanceOwners = new ReadOnlyDictionary<Guid, IDictionary<XName, InstanceValue>>(owners);
        }

        // N
        public InstanceOwnerQueryResult(IDictionary<Guid, IDictionary<XName, InstanceValue>> instanceOwners)
        {
            Dictionary<Guid, IDictionary<XName, InstanceValue>> owners = new Dictionary<Guid, IDictionary<XName, InstanceValue>>(instanceOwners.Count);
            foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> metadata in instanceOwners)
            {
                IDictionary<XName, InstanceValue> safeMetadata; // if metadata is not readonly, copy it.
                if (metadata.Value == null || metadata.Value.IsReadOnly)
                    safeMetadata = metadata.Value;
                else
                {
                    // Copy dictionary & make a readonly wrapper.
                    IDictionary<XName, InstanceValue> copy = new Dictionary<XName, InstanceValue>(metadata.Value);
                    safeMetadata = new ReadOnlyDictionary<XName, InstanceValue>(copy);
                }
                owners.Add(metadata.Key, metadata.Value == null ? s_emptyMetadata : safeMetadata);
            }
            InstanceOwners = new ReadOnlyDictionary<Guid, IDictionary<XName, InstanceValue>>(owners);
        }

        public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceOwners { get; private set; }
    }
}

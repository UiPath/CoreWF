// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceKeyView
    {
        private static readonly ReadOnlyDictionary<XName, InstanceValue> s_emptyProperties = new ReadOnlyDictionary<XName, InstanceValue>(new Dictionary<XName, InstanceValue>(0));

        private IDictionary<XName, InstanceValue> _metadata;
        private Dictionary<XName, InstanceValue> _accumulatedMetadataWrites;

        internal InstanceKeyView(Guid key)
        {
            InstanceKey = key;
            InstanceKeyMetadataConsistency = InstanceValueConsistency.InDoubt | InstanceValueConsistency.Partial;
        }

        private InstanceKeyView(InstanceKeyView source)
        {
            InstanceKey = source.InstanceKey;
            InstanceKeyState = source.InstanceKeyState;

            InstanceKeyMetadata = source.InstanceKeyMetadata;
            InstanceKeyMetadataConsistency = source.InstanceKeyMetadataConsistency;
        }

        public Guid InstanceKey { get; private set; }
        public InstanceKeyState InstanceKeyState { get; internal set; }

        public InstanceValueConsistency InstanceKeyMetadataConsistency { get; internal set; }
        public IDictionary<XName, InstanceValue> InstanceKeyMetadata
        {
            get
            {
                IDictionary<XName, InstanceValue> pendingWrites = _accumulatedMetadataWrites;
                _accumulatedMetadataWrites = null;
                _metadata = pendingWrites.ReadOnlyMergeInto(_metadata ?? InstanceKeyView.s_emptyProperties, true);
                return _metadata;
            }
            internal set
            {
                _accumulatedMetadataWrites = null;
                _metadata = value;
            }
        }
        internal Dictionary<XName, InstanceValue> AccumulatedMetadataWrites
        {
            get
            {
                if (_accumulatedMetadataWrites == null)
                {
                    _accumulatedMetadataWrites = new Dictionary<XName, InstanceValue>();
                }
                return _accumulatedMetadataWrites;
            }
        }

        internal InstanceKeyView Clone()
        {
            return new InstanceKeyView(this);
        }
    }
}

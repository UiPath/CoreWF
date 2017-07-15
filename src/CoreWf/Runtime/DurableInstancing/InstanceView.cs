// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Xml.Linq;

namespace CoreWf.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceView
    {
        private static readonly ReadOnlyDictionary<XName, InstanceValue> s_emptyProperties = new ReadOnlyDictionary<XName, InstanceValue>(new Dictionary<XName, InstanceValue>(0));
        private static readonly ReadOnlyDictionary<Guid, InstanceKeyView> s_emptyKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(new Dictionary<Guid, InstanceKeyView>(0));

        private IDictionary<XName, InstanceValue> _data;
        private IDictionary<XName, InstanceValue> _metadata;
        private IDictionary<XName, InstanceValue> _ownerMetadata;
        private IDictionary<Guid, InstanceKeyView> _keys;
        private ReadOnlyCollection<InstanceStoreQueryResult> _queryResults;

        private Dictionary<XName, InstanceValue> _accumulatedMetadataWrites;
        private Dictionary<XName, InstanceValue> _accumulatedOwnerMetadataWrites;
        private Collection<InstanceStoreQueryResult> _queryResultsBackingCollection;

        private long _instanceVersion;

        internal InstanceView(InstanceOwner owner)
            : this()
        {
            InstanceOwner = owner;
        }

        internal InstanceView(InstanceOwner owner, Guid instanceId)
            : this()
        {
            Fx.Assert(instanceId != Guid.Empty, "Null instanceId passed to InstanceView ctor.");

            InstanceOwner = owner;
            InstanceId = instanceId;
            IsBoundToInstance = true;
        }

        private InstanceView()
        {
            _instanceVersion = -1;
            InstanceDataConsistency = InstanceValueConsistency.InDoubt | InstanceValueConsistency.Partial;
            InstanceMetadataConsistency = InstanceValueConsistency.InDoubt | InstanceValueConsistency.Partial;
            InstanceOwnerMetadataConsistency = InstanceValueConsistency.InDoubt | InstanceValueConsistency.Partial;
            InstanceKeysConsistency = InstanceValueConsistency.InDoubt | InstanceValueConsistency.Partial;
        }

        private InstanceView(InstanceView source)
        {
            _instanceVersion = source._instanceVersion;

            InstanceOwner = source.InstanceOwner;
            InstanceId = source.InstanceId;
            IsBoundToInstance = source.IsBoundToInstance;

            InstanceState = source.InstanceState;

            InstanceDataConsistency = source.InstanceDataConsistency;
            InstanceMetadataConsistency = source.InstanceMetadataConsistency;
            InstanceOwnerMetadataConsistency = source.InstanceOwnerMetadataConsistency;
            InstanceKeysConsistency = source.InstanceKeysConsistency;

            InstanceData = source.InstanceData;
            InstanceMetadata = source.InstanceMetadata;
            InstanceOwnerMetadata = source.InstanceOwnerMetadata;

            InstanceStoreQueryResults = source.InstanceStoreQueryResults;

            Dictionary<Guid, InstanceKeyView> keys = null;
            if (source.InstanceKeys.Count > 0)
            {
                keys = new Dictionary<Guid, InstanceKeyView>(source.InstanceKeys.Count);
                foreach (KeyValuePair<Guid, InstanceKeyView> key in source.InstanceKeys)
                {
                    keys.Add(key.Key, key.Value.Clone());
                }
            }
            InstanceKeys = keys == null ? null : new ReadOnlyDictionary<Guid, InstanceKeyView>(keys);
        }

        public Guid InstanceId { get; private set; }
        public bool IsBoundToInstance { get; private set; }

        public InstanceOwner InstanceOwner { get; private set; }
        public bool IsBoundToInstanceOwner
        {
            get
            {
                return InstanceOwner != null;
            }
        }

        public bool IsBoundToLock
        {
            get
            {
                return _instanceVersion >= 0;
            }
        }

        public InstanceState InstanceState { get; internal set; }

        // All dictionaries are always read-only.

        public InstanceValueConsistency InstanceDataConsistency { get; internal set; }
        public IDictionary<XName, InstanceValue> InstanceData
        {
            get
            {
                return _data ?? InstanceView.s_emptyProperties;
            }
            internal set
            {
                Fx.AssertAndThrow(!IsViewFrozen, "Setting Data on frozen View.");
                _data = value;
            }
        }

        public InstanceValueConsistency InstanceMetadataConsistency { get; internal set; }
        public IDictionary<XName, InstanceValue> InstanceMetadata
        {
            get
            {
                IDictionary<XName, InstanceValue> pendingWrites = _accumulatedMetadataWrites;
                _accumulatedMetadataWrites = null;
                _metadata = pendingWrites.ReadOnlyMergeInto(_metadata ?? InstanceView.s_emptyProperties, true);
                return _metadata;
            }
            internal set
            {
                Fx.AssertAndThrow(!IsViewFrozen, "Setting Metadata on frozen View.");
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

        public InstanceValueConsistency InstanceOwnerMetadataConsistency { get; internal set; }
        public IDictionary<XName, InstanceValue> InstanceOwnerMetadata
        {
            get
            {
                IDictionary<XName, InstanceValue> pendingWrites = _accumulatedOwnerMetadataWrites;
                _accumulatedOwnerMetadataWrites = null;
                _ownerMetadata = pendingWrites.ReadOnlyMergeInto(_ownerMetadata ?? InstanceView.s_emptyProperties, true);
                return _ownerMetadata;
            }
            internal set
            {
                Fx.AssertAndThrow(!IsViewFrozen, "Setting OwnerMetadata on frozen View.");
                _accumulatedOwnerMetadataWrites = null;
                _ownerMetadata = value;
            }
        }
        internal Dictionary<XName, InstanceValue> AccumulatedOwnerMetadataWrites
        {
            get
            {
                if (_accumulatedOwnerMetadataWrites == null)
                {
                    _accumulatedOwnerMetadataWrites = new Dictionary<XName, InstanceValue>();
                }
                return _accumulatedOwnerMetadataWrites;
            }
        }

        public InstanceValueConsistency InstanceKeysConsistency { get; internal set; }
        public IDictionary<Guid, InstanceKeyView> InstanceKeys
        {
            get
            {
                return _keys ?? InstanceView.s_emptyKeys;
            }
            internal set
            {
                Fx.AssertAndThrow(!IsViewFrozen, "Setting Keys on frozen View.");
                _keys = value;
            }
        }

        // Arch prefers ReadOnlyCollection here to ICollection.   
        //[SuppressMessage(FxCop.Category.Usage, FxCop.Rule.CollectionPropertiesShouldBeReadOnly,
        //Justification = "property is of ReadOnlyCollection type")]
        public ReadOnlyCollection<InstanceStoreQueryResult> InstanceStoreQueryResults
        {
            get
            {
                if (_queryResults == null)
                {
                    _queryResults = new ReadOnlyCollection<InstanceStoreQueryResult>(QueryResultsBacking);
                }
                return _queryResults;
            }
            internal set
            {
                Fx.AssertAndThrow(!IsViewFrozen, "Setting InstanceStoreQueryResults on frozen View.");
                _queryResults = null;
                if (value == null)
                {
                    _queryResultsBackingCollection = null;
                }
                else
                {
                    _queryResultsBackingCollection = new Collection<InstanceStoreQueryResult>();
                    foreach (InstanceStoreQueryResult queryResult in value)
                    {
                        _queryResultsBackingCollection.Add(queryResult);
                    }
                }
            }
        }
        internal Collection<InstanceStoreQueryResult> QueryResultsBacking
        {
            get
            {
                if (_queryResultsBackingCollection == null)
                {
                    _queryResultsBackingCollection = new Collection<InstanceStoreQueryResult>();
                }
                return _queryResultsBackingCollection;
            }
        }

        internal void BindOwner(InstanceOwner owner)
        {
            Fx.AssertAndThrow(!IsViewFrozen, "BindOwner called on read-only InstanceView.");
            Fx.Assert(owner != null, "Null owner passed to BindOwner.");
            if (IsBoundToInstanceOwner)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextAlreadyBoundToOwner));
            }
            InstanceOwner = owner;
        }

        internal void BindInstance(Guid instanceId)
        {
            Fx.AssertAndThrow(!IsViewFrozen, "BindInstance called on read-only InstanceView.");
            Fx.Assert(instanceId != Guid.Empty, "Null instanceId passed to BindInstance.");
            if (IsBoundToInstance)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextAlreadyBoundToInstance));
            }
            InstanceId = instanceId;
            IsBoundToInstance = true;
        }

        internal void BindLock(long instanceVersion)
        {
            Fx.AssertAndThrow(!IsViewFrozen, "BindLock called on read-only InstanceView.");
            Fx.Assert(instanceVersion >= 0, "Negative instanceVersion passed to BindLock.");
            if (!IsBoundToInstanceOwner)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextMustBeBoundToOwner));
            }
            if (!IsBoundToInstance)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextMustBeBoundToInstance));
            }
            if (Interlocked.CompareExchange(ref _instanceVersion, instanceVersion, -1) != -1)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextAlreadyBoundToLock));
            }
        }

        // This method is called when IPC.BindReclaimedLock is called.  It reserves the lock, so that future calls to this or BindLock can be prevented.
        // We set the version to -(instanceVersion + 2) so that 0 maps to -2 (since -1 is special).
        internal void StartBindLock(long instanceVersion)
        {
            Fx.AssertAndThrow(!IsViewFrozen, "StartBindLock called on read-only InstanceView.");
            Fx.Assert(instanceVersion >= 0, "Negative instanceVersion passed to StartBindLock.");
            if (!IsBoundToInstanceOwner)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextMustBeBoundToOwner));
            }
            if (!IsBoundToInstance)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextMustBeBoundToInstance));
            }
            if (Interlocked.CompareExchange(ref _instanceVersion, checked(-instanceVersion - 2), -1) != -1)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SRCore.ContextAlreadyBoundToLock));
            }
        }

        // This completes the bind started in StartBindLock.
        internal void FinishBindLock(long instanceVersion)
        {
            Fx.AssertAndThrow(!IsViewFrozen, "FinishBindLock called on read-only InstanceView.");
            Fx.Assert(IsBoundToInstanceOwner, "Must be bound to owner, checked in StartBindLock.");
            Fx.Assert(IsBoundToInstance, "Must be bound to instance, checked in StartBindLock.");

            long result = Interlocked.CompareExchange(ref _instanceVersion, instanceVersion, -instanceVersion - 2);
            Fx.AssertAndThrow(result == -instanceVersion - 2, "FinishBindLock called with mismatched instance version.");
        }

        internal void MakeReadOnly()
        {
            IsViewFrozen = true;
        }

        internal InstanceView Clone()
        {
            return new InstanceView(this);
        }

        private bool IsViewFrozen { get; set; }
    }
}

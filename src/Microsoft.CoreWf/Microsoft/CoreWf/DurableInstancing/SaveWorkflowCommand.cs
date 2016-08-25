// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Microsoft.CoreWf.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class SaveWorkflowCommand : InstancePersistenceCommand
    {
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> _keysToAssociate;
        private Collection<Guid> _keysToComplete;
        private Collection<Guid> _keysToFree;

        private Dictionary<XName, InstanceValue> _instanceData;

        private Dictionary<XName, InstanceValue> _instanceMetadataChanges;
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> _keyMetadataChanges;

        public SaveWorkflowCommand()
            : base(InstancePersistence.ActivitiesCommandNamespace.GetName("SaveWorkflow"))
        {
        }

        public bool UnlockInstance { get; set; }
        public bool CompleteInstance { get; set; }

        public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeysToAssociate
        {
            get
            {
                if (_keysToAssociate == null)
                {
                    _keysToAssociate = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
                }
                return _keysToAssociate;
            }
        }

        public ICollection<Guid> InstanceKeysToComplete
        {
            get
            {
                if (_keysToComplete == null)
                {
                    _keysToComplete = new Collection<Guid>();
                }
                return _keysToComplete;
            }
        }

        public ICollection<Guid> InstanceKeysToFree
        {
            get
            {
                if (_keysToFree == null)
                {
                    _keysToFree = new Collection<Guid>();
                }
                return _keysToFree;
            }
        }

        public IDictionary<XName, InstanceValue> InstanceMetadataChanges
        {
            get
            {
                if (_instanceMetadataChanges == null)
                {
                    _instanceMetadataChanges = new Dictionary<XName, InstanceValue>();
                }
                return _instanceMetadataChanges;
            }
        }

        public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeyMetadataChanges
        {
            get
            {
                if (_keyMetadataChanges == null)
                {
                    _keyMetadataChanges = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
                }
                return _keyMetadataChanges;
            }
        }

        public IDictionary<XName, InstanceValue> InstanceData
        {
            get
            {
                if (_instanceData == null)
                {
                    _instanceData = new Dictionary<XName, InstanceValue>();
                }
                return _instanceData;
            }
        }

        //protected internal override bool IsTransactionEnlistmentOptional
        //{
        //    get
        //    {
        //        return !CompleteInstance &&
        //            (this.instanceData == null || this.instanceData.Count == 0) &&
        //            (this.keyMetadataChanges == null || this.keyMetadataChanges.Count == 0) &&
        //            (this.instanceMetadataChanges == null || this.instanceMetadataChanges.Count == 0) &&
        //            (this.keysToFree == null || this.keysToFree.Count == 0) &&
        //            (this.keysToComplete == null || this.keysToComplete.Count == 0) &&
        //            (this.keysToAssociate == null || this.keysToAssociate.Count == 0);
        //    }
        //}

        protected internal override bool AutomaticallyAcquiringLock
        {
            get
            {
                return true;
            }
        }

        protected internal override void Validate(InstanceView view)
        {
            if (!view.IsBoundToInstance)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.InstanceRequired));
            }

            if (!view.IsBoundToInstanceOwner)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.OwnerRequired));
            }

            if (_keysToAssociate != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in _keysToAssociate)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value);
                }
            }

            if (_keyMetadataChanges != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in _keyMetadataChanges)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value, true);
                }
            }

            if (this.CompleteInstance && !this.UnlockInstance)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.ValidateUnlockInstance));
            }

            InstancePersistence.ValidatePropertyBag(_instanceMetadataChanges, true);
            InstancePersistence.ValidatePropertyBag(_instanceData);
        }
    }
}

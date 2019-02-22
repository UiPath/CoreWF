// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.DurableInstancing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Activities.Runtime;
    using System.Activities.Runtime.DurableInstancing;
    using System.Activities.Internals;
    using System.Xml.Linq;

    [Fx.Tag.XamlVisible(false)]
    public sealed class SaveWorkflowCommand : InstancePersistenceCommand
    {
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> keysToAssociate;
        private Collection<Guid> keysToComplete;
        private Collection<Guid> keysToFree;
        private Dictionary<XName, InstanceValue> instanceData;
        private Dictionary<XName, InstanceValue> instanceMetadataChanges;
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> keyMetadataChanges;

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
                if (this.keysToAssociate == null)
                {
                    this.keysToAssociate = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
                }
                return this.keysToAssociate;
            }
        }

        public ICollection<Guid> InstanceKeysToComplete
        {
            get
            {
                if (this.keysToComplete == null)
                {
                    this.keysToComplete = new Collection<Guid>();
                }
                return this.keysToComplete;
            }
        }

        public ICollection<Guid> InstanceKeysToFree
        {
            get
            {
                if (this.keysToFree == null)
                {
                    this.keysToFree = new Collection<Guid>();
                }
                return this.keysToFree;
            }
        }

        public IDictionary<XName, InstanceValue> InstanceMetadataChanges
        {
            get
            {
                if (this.instanceMetadataChanges == null)
                {
                    this.instanceMetadataChanges = new Dictionary<XName, InstanceValue>();
                }
                return this.instanceMetadataChanges;
            }
        }

        public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeyMetadataChanges
        {
            get
            {
                if (this.keyMetadataChanges == null)
                {
                    this.keyMetadataChanges = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
                }
                return this.keyMetadataChanges;
            }
        }

        public IDictionary<XName, InstanceValue> InstanceData
        {
            get
            {
                if (this.instanceData == null)
                {
                    this.instanceData = new Dictionary<XName, InstanceValue>();
                }
                return this.instanceData;
            }
        }

        protected internal override bool IsTransactionEnlistmentOptional
        {
            get
            {
                return !CompleteInstance &&
                    (this.instanceData == null || this.instanceData.Count == 0) &&
                    (this.keyMetadataChanges == null || this.keyMetadataChanges.Count == 0) &&
                    (this.instanceMetadataChanges == null || this.instanceMetadataChanges.Count == 0) &&
                    (this.keysToFree == null || this.keysToFree.Count == 0) &&
                    (this.keysToComplete == null || this.keysToComplete.Count == 0) &&
                    (this.keysToAssociate == null || this.keysToAssociate.Count == 0);
            }
        }

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
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InstanceRequired));
            }

            if (!view.IsBoundToInstanceOwner)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OwnerRequired));
            }

            if (this.keysToAssociate != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in this.keysToAssociate)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value);
                }
            }

            if (this.keyMetadataChanges != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in this.keyMetadataChanges)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value, true);
                }
            }

            if (this.CompleteInstance && !this.UnlockInstance)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ValidateUnlockInstance));
            }
            
            InstancePersistence.ValidatePropertyBag(this.instanceMetadataChanges, true);
            InstancePersistence.ValidatePropertyBag(this.instanceData);
        }
    }
}

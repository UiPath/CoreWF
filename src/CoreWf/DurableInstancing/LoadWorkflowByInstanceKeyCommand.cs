// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.DurableInstancing
{
    using System;
    using System.Collections.Generic;
    using CoreWf.Runtime;
    using CoreWf.Runtime.DurableInstancing;
    using CoreWf.Internals;
    using System.Xml.Linq;

    [Fx.Tag.XamlVisible(false)]
    public sealed class LoadWorkflowByInstanceKeyCommand : InstancePersistenceCommand
    {
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> keysToAssociate;

        public LoadWorkflowByInstanceKeyCommand()
            : base(InstancePersistence.ActivitiesCommandNamespace.GetName("LoadWorkflowByInstanceKey"))
        {
        }

        public bool AcceptUninitializedInstance { get; set; }

        public Guid LookupInstanceKey { get; set; }
        public Guid AssociateInstanceKeyToInstanceId { get; set; }

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

        protected internal override bool IsTransactionEnlistmentOptional
        {
            get
            {
                return (this.keysToAssociate == null || this.keysToAssociate.Count == 0) && AssociateInstanceKeyToInstanceId == Guid.Empty;
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
            if (!view.IsBoundToInstanceOwner)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OwnerRequired));
            }
            if (view.IsBoundToInstance)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AlreadyBoundToInstance));
            }

            if (LookupInstanceKey == Guid.Empty)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadOpKeyMustBeValid));
            }

            if (AssociateInstanceKeyToInstanceId == Guid.Empty)
            {
                if (InstanceKeysToAssociate.ContainsKey(LookupInstanceKey))
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadOpAssociateKeysCannotContainLookupKey));
                }
            }
            else
            {
                if (!AcceptUninitializedInstance)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.LoadOpFreeKeyRequiresAcceptUninitialized));
                }
            }

            if (this.keysToAssociate != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in this.keysToAssociate)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value);
                }
            }
        }
    }
}

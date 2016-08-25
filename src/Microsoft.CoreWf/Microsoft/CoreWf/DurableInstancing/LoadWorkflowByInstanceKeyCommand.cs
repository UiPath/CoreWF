// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.CoreWf.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class LoadWorkflowByInstanceKeyCommand : InstancePersistenceCommand
    {
        private Dictionary<Guid, IDictionary<XName, InstanceValue>> _keysToAssociate;

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
                if (_keysToAssociate == null)
                {
                    _keysToAssociate = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
                }
                return _keysToAssociate;
            }
        }

        //protected internal override bool IsTransactionEnlistmentOptional
        //{
        //    get
        //    {
        //        return (this.keysToAssociate == null || this.keysToAssociate.Count == 0) && AssociateInstanceKeyToInstanceId == Guid.Empty;
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
            if (!view.IsBoundToInstanceOwner)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.OwnerRequired));
            }
            if (view.IsBoundToInstance)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.AlreadyBoundToInstance));
            }

            if (LookupInstanceKey == Guid.Empty)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.LoadOpKeyMustBeValid));
            }

            if (AssociateInstanceKeyToInstanceId == Guid.Empty)
            {
                if (InstanceKeysToAssociate.ContainsKey(LookupInstanceKey))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.LoadOpAssociateKeysCannotContainLookupKey));
                }
            }
            else
            {
                if (!AcceptUninitializedInstance)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.LoadOpFreeKeyRequiresAcceptUninitialized));
                }
            }

            if (_keysToAssociate != null)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in _keysToAssociate)
                {
                    InstancePersistence.ValidatePropertyBag(key.Value);
                }
            }
        }
    }
}

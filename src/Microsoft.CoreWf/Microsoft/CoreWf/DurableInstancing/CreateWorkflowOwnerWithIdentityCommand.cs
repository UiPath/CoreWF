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
    public sealed class CreateWorkflowOwnerWithIdentityCommand : InstancePersistenceCommand
    {
        private Dictionary<XName, InstanceValue> _instanceOwnerMetadata;

        public CreateWorkflowOwnerWithIdentityCommand()
            : base(InstancePersistence.ActivitiesCommandNamespace.GetName("CreateWorkflowOwnerWithIdentity"))
        {
        }

        public IDictionary<XName, InstanceValue> InstanceOwnerMetadata
        {
            get
            {
                if (_instanceOwnerMetadata == null)
                {
                    _instanceOwnerMetadata = new Dictionary<XName, InstanceValue>();
                }

                return _instanceOwnerMetadata;
            }
        }

        //protected internal override bool IsTransactionEnlistmentOptional
        //{
        //    get
        //    {
        //        return this.instanceOwnerMetadata == null || this.instanceOwnerMetadata.Count == 0;
        //    }
        //}

        protected internal override void Validate(InstanceView view)
        {
            if (view.IsBoundToInstanceOwner)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.AlreadyBoundToOwner));
            }

            InstancePersistence.ValidatePropertyBag(_instanceOwnerMetadata);
        }
    }
}

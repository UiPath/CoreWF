// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.DurableInstancing
{
    using System;
    using System.Collections.Generic;
    using System.Activities.Runtime.DurableInstancing;
    using System.Xml.Linq;
    using System.Activities.Runtime;
    using System.Activities.Internals;

    [Fx.Tag.XamlVisible(false)]
    public sealed class CreateWorkflowOwnerCommand : InstancePersistenceCommand
    {
        private Dictionary<XName, InstanceValue> instanceOwnerMetadata;

        public CreateWorkflowOwnerCommand()
            : base(InstancePersistence.ActivitiesCommandNamespace.GetName("CreateWorkflowOwner"))
        {
        }

        public IDictionary<XName, InstanceValue> InstanceOwnerMetadata
        {
            get
            {
                if (this.instanceOwnerMetadata == null)
                {
                    this.instanceOwnerMetadata = new Dictionary<XName, InstanceValue>();
                }
                return this.instanceOwnerMetadata;
            }
        }

        protected internal override bool IsTransactionEnlistmentOptional
        {
            get
            {
                return this.instanceOwnerMetadata == null || this.instanceOwnerMetadata.Count == 0;
            }
        }

        protected internal override void Validate(InstanceView view)
        {
            if (view.IsBoundToInstanceOwner)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AlreadyBoundToOwner));
            }
            InstancePersistence.ValidatePropertyBag(this.instanceOwnerMetadata);
        }
    }
}

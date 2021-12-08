// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;
using System.Xml.Linq;

namespace System.Activities.DurableInstancing;

[Fx.Tag.XamlVisible(false)]
public sealed class CreateWorkflowOwnerCommand : InstancePersistenceCommand
{
    private Dictionary<XName, InstanceValue> _instanceOwnerMetadata;

    public CreateWorkflowOwnerCommand()
        : base(InstancePersistence.ActivitiesCommandNamespace.GetName("CreateWorkflowOwner")) { }

    public IDictionary<XName, InstanceValue> InstanceOwnerMetadata
    {
        get
        {
            _instanceOwnerMetadata ??= new Dictionary<XName, InstanceValue>();
            return _instanceOwnerMetadata;
        }
    }

    protected internal override bool IsTransactionEnlistmentOptional => _instanceOwnerMetadata == null || _instanceOwnerMetadata.Count == 0;

    protected internal override void Validate(InstanceView view)
    {
        if (view.IsBoundToInstanceOwner)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.AlreadyBoundToOwner));
        }
        InstancePersistence.ValidatePropertyBag(_instanceOwnerMetadata);
    }
}

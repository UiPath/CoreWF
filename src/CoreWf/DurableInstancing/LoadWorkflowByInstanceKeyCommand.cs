// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;
using System.Xml.Linq;

namespace System.Activities.DurableInstancing;

[Fx.Tag.XamlVisible(false)]
public sealed class LoadWorkflowByInstanceKeyCommand : InstancePersistenceCommand
{
    private Dictionary<Guid, IDictionary<XName, InstanceValue>> keysToAssociate;

    public LoadWorkflowByInstanceKeyCommand()
        : base(InstancePersistence.ActivitiesCommandNamespace.GetName("LoadWorkflowByInstanceKey")) { }

    public bool AcceptUninitializedInstance { get; set; }

    public Guid LookupInstanceKey { get; set; }
    public Guid AssociateInstanceKeyToInstanceId { get; set; }

    public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeysToAssociate
    {
        get
        {
            keysToAssociate ??= new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
            return keysToAssociate;
        }
    }

    protected internal override bool IsTransactionEnlistmentOptional => (keysToAssociate == null || keysToAssociate.Count == 0) && AssociateInstanceKeyToInstanceId == Guid.Empty;

    protected internal override bool AutomaticallyAcquiringLock => true;

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

        if (keysToAssociate != null)
        {
            foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> key in keysToAssociate)
            {
                InstancePersistence.ValidatePropertyBag(key.Value);
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace System.Activities.DurableInstancing;

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
        : base(InstancePersistence.ActivitiesCommandNamespace.GetName("SaveWorkflow")) { }

    public bool UnlockInstance { get; set; }
    public bool CompleteInstance { get; set; }

    public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeysToAssociate
    {
        get
        {
            _keysToAssociate ??= new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
            return _keysToAssociate;
        }
    }

    public ICollection<Guid> InstanceKeysToComplete
    {
        get
        {
            _keysToComplete ??= new Collection<Guid>();
            return _keysToComplete;
        }
    }

    public ICollection<Guid> InstanceKeysToFree
    {
        get
        {
            _keysToFree ??= new Collection<Guid>();
            return _keysToFree;
        }
    }

    public IDictionary<XName, InstanceValue> InstanceMetadataChanges
    {
        get
        {
            _instanceMetadataChanges ??= new Dictionary<XName, InstanceValue>();
            return _instanceMetadataChanges;
        }
    }

    public IDictionary<Guid, IDictionary<XName, InstanceValue>> InstanceKeyMetadataChanges
    {
        get
        {
            _keyMetadataChanges ??= new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
            return _keyMetadataChanges;
        }
    }

    public IDictionary<XName, InstanceValue> InstanceData
    {
        get
        {
            _instanceData ??= new Dictionary<XName, InstanceValue>();
            return _instanceData;
        }
    }

    protected internal override bool IsTransactionEnlistmentOptional => !CompleteInstance &&
                (_instanceData == null || _instanceData.Count == 0) &&
                (_keyMetadataChanges == null || _keyMetadataChanges.Count == 0) &&
                (_instanceMetadataChanges == null || _instanceMetadataChanges.Count == 0) &&
                (_keysToFree == null || _keysToFree.Count == 0) &&
                (_keysToComplete == null || _keysToComplete.Count == 0) &&
                (_keysToAssociate == null || _keysToAssociate.Count == 0);

    protected internal override bool AutomaticallyAcquiringLock => true;

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

        if (CompleteInstance && !UnlockInstance)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ValidateUnlockInstance));
        }

        InstancePersistence.ValidatePropertyBag(_instanceMetadataChanges, true);
        InstancePersistence.ValidatePropertyBag(_instanceData);
    }
}

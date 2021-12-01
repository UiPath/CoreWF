// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;

namespace System.Activities.DurableInstancing;

[Fx.Tag.XamlVisible(false)]
public sealed class TryLoadRunnableWorkflowCommand : InstancePersistenceCommand
{
    public TryLoadRunnableWorkflowCommand()
        : base(InstancePersistence.ActivitiesCommandNamespace.GetName("TryLoadRunnableWorkflow")) { }

    protected internal override bool IsTransactionEnlistmentOptional => true;

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
    }
}

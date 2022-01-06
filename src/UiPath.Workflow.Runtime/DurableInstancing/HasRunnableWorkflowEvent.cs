// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.DurableInstancing;

namespace System.Activities.DurableInstancing;

[Fx.Tag.XamlVisible(false)]
public sealed class HasRunnableWorkflowEvent : InstancePersistenceEvent<HasRunnableWorkflowEvent>
{
    public HasRunnableWorkflowEvent()
        : base(InstancePersistence.ActivitiesEventNamespace.GetName("HasRunnableWorkflow")) { }
}

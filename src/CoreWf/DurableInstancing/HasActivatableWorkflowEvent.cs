// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.DurableInstancing
{
    using CoreWf.Runtime;
    using CoreWf.Runtime.DurableInstancing;

    [Fx.Tag.XamlVisible(false)]   
    public sealed class HasActivatableWorkflowEvent : InstancePersistenceEvent<HasActivatableWorkflowEvent>
    {
        public HasActivatableWorkflowEvent()
            : base(InstancePersistence.ActivitiesEventNamespace.GetName("HasActivatableWorkflow"))
        {
        }
    }
}

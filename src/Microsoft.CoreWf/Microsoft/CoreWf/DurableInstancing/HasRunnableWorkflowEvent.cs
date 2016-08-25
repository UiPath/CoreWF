// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.DurableInstancing;

namespace Microsoft.CoreWf.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class HasRunnableWorkflowEvent : InstancePersistenceEvent<HasRunnableWorkflowEvent>
    {
        public HasRunnableWorkflowEvent()
            : base(InstancePersistence.ActivitiesEventNamespace.GetName("HasRunnableWorkflow"))
        {
        }
    }
}

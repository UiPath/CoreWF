// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Runtime.DurableInstancing;

namespace CoreWf.DurableInstancing
{
    //using System.Diagnostics.CodeAnalysis;

    [Fx.Tag.XamlVisible(false)]
    public sealed class HasActivatableWorkflowEvent : InstancePersistenceEvent<HasActivatableWorkflowEvent>
    {
        public HasActivatableWorkflowEvent()
            : base(InstancePersistence.ActivitiesEventNamespace.GetName("HasActivatableWorkflow"))
        {
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Runtime.DurableInstancing;
using System;

namespace CoreWf.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class TryLoadRunnableWorkflowCommand : InstancePersistenceCommand
    {
        public TryLoadRunnableWorkflowCommand()
            : base(InstancePersistence.ActivitiesCommandNamespace.GetName("TryLoadRunnableWorkflow"))
        {
        }

        //protected internal override bool IsTransactionEnlistmentOptional
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        protected internal override bool AutomaticallyAcquiringLock
        {
            get
            {
                return true;
            }
        }

        protected internal override void Validate(InstanceView view)
        {
            if (!view.IsBoundToInstanceOwner)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.OwnerRequired));
            }
            if (view.IsBoundToInstance)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SRCore.AlreadyBoundToInstance));
            }
        }
    }
}

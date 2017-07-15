// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Threading;

namespace CoreWf
{
    internal static class SynchronizationContextHelper
    {
        private static WFDefaultSynchronizationContext s_defaultContext;

        public static SynchronizationContext GetDefaultSynchronizationContext()
        {
            if (SynchronizationContextHelper.s_defaultContext == null)
            {
                SynchronizationContextHelper.s_defaultContext = new WFDefaultSynchronizationContext();
            }
            return SynchronizationContextHelper.s_defaultContext;
        }

        public static SynchronizationContext CloneSynchronizationContext(SynchronizationContext context)
        {
            Fx.Assert(context != null, "null context parameter");
            WFDefaultSynchronizationContext wfDefaultContext = context as WFDefaultSynchronizationContext;
            if (wfDefaultContext != null)
            {
                Fx.Assert(SynchronizationContextHelper.s_defaultContext != null, "We must have set the static member by now!");
                return SynchronizationContextHelper.s_defaultContext;
            }
            else
            {
                return context.CreateCopy();
            }
        }

        private class WFDefaultSynchronizationContext : SynchronizationContext
        {
            public WFDefaultSynchronizationContext()
            {
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                ActionItem.Schedule(delegate (object s) { d(s); }, state);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                d(state);
            }
        }
    }
}

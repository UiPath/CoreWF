// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using CoreWf.Runtime;
    using System.Threading;

    internal static class SynchronizationContextHelper
    {
        private static WFDefaultSynchronizationContext defaultContext;

        public static SynchronizationContext GetDefaultSynchronizationContext()
        {
            if (SynchronizationContextHelper.defaultContext == null)
            {
                SynchronizationContextHelper.defaultContext = new WFDefaultSynchronizationContext();
            }
            return SynchronizationContextHelper.defaultContext;
        }

        public static SynchronizationContext CloneSynchronizationContext(SynchronizationContext context)
        {
            Fx.Assert(context != null, "null context parameter");
            if (context is WFDefaultSynchronizationContext wfDefaultContext)
            {
                Fx.Assert(SynchronizationContextHelper.defaultContext != null, "We must have set the static member by now!");
                return SynchronizationContextHelper.defaultContext;
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
                ActionItem.Schedule(delegate(object s) { d(s); }, state);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                d(state);
            }
        }
    }
}

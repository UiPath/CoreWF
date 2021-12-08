// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities;
using Runtime;

internal static class SynchronizationContextHelper
{
    private static WFDefaultSynchronizationContext defaultContext;

    public static SynchronizationContext GetDefaultSynchronizationContext()
    {
        defaultContext ??= new WFDefaultSynchronizationContext();
        return defaultContext;
    }

    public static SynchronizationContext CloneSynchronizationContext(SynchronizationContext context)
    {
        Fx.Assert(context != null, "null context parameter");
        if (context is WFDefaultSynchronizationContext wfDefaultContext)
        {
            Fx.Assert(defaultContext != null, "We must have set the static member by now!");
            return defaultContext;
        }
        else
        {
            return context.CreateCopy();
        }
    }

    private class WFDefaultSynchronizationContext : SynchronizationContext
    {
        public WFDefaultSynchronizationContext() { }

        public override void Post(SendOrPostCallback d, object state) => ActionItem.Schedule(delegate (object s) { d(s); }, state);

        public override void Send(SendOrPostCallback d, object state) => d(state);
    }
}

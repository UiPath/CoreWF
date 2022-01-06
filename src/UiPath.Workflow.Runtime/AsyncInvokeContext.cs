// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities;

internal class AsyncInvokeContext
{
    public AsyncInvokeContext(object userState, WorkflowInvoker invoker)
    {
        UserState = userState;
        SynchronizationContext syncContext = SynchronizationContext.Current ?? WorkflowApplication.SynchronousSynchronizationContext.Value;
        Operation = new AsyncInvokeOperation(syncContext);
        Invoker = invoker;
    }

    public object UserState { get; private set; }

    public AsyncInvokeOperation Operation { get; private set; }

    public WorkflowApplication WorkflowApplication { get; set; }

    public WorkflowInvoker Invoker { get; private set; }

    public IDictionary<string, object> Outputs { get; set; }
}

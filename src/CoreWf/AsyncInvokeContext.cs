// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Collections.Generic;
    using System.Threading;

    internal class AsyncInvokeContext
    {
        public AsyncInvokeContext(object userState, WorkflowInvoker invoker)
        {
            this.UserState = userState;                      
            SynchronizationContext syncContext = SynchronizationContext.Current ?? WorkflowApplication.SynchronousSynchronizationContext.Value;
            this.Operation = new AsyncInvokeOperation(syncContext);
            this.Invoker = invoker;
        }

        public object UserState
        {
            get;
            private set;
        }

        public AsyncInvokeOperation Operation
        {
            get;
            private set;
        }

        public WorkflowApplication WorkflowApplication
        {
            get;
            set;
        }

        public WorkflowInvoker Invoker
        {
            get;
            private set;
        }

        public IDictionary<string, object> Outputs
        {
            get;
            set;
        }
    }
}

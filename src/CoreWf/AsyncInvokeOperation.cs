// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System.Threading;
    using CoreWf.Runtime;

    internal class AsyncInvokeOperation
    {
        private readonly object thisLock;

        public AsyncInvokeOperation(SynchronizationContext syncContext)
        {
            Fx.Assert(syncContext != null, "syncContext cannot be null");
            this.SyncContext = syncContext;
            thisLock = new object();
        }

        private SynchronizationContext SyncContext
        {
            get;
            set;
        }

        private bool Completed
        {
            get;
            set;
        }

        public void OperationStarted()
        {
            this.SyncContext.OperationStarted();
        }

        public void OperationCompleted()
        {
            lock (thisLock)
            {
                Fx.AssertAndThrowFatal(!this.Completed, "Async operation has already been completed");
                this.Completed = true;
            }
            this.SyncContext.OperationCompleted();
        }

        public void PostOperationCompleted(SendOrPostCallback callback, object arg)
        {
            lock (thisLock)
            {
                Fx.AssertAndThrowFatal(!this.Completed, "Async operation has already been completed");
                this.Completed = true;
            }
            Fx.Assert(callback != null, "callback cannot be null");
            this.SyncContext.Post(callback, arg);
            this.SyncContext.OperationCompleted();
        }
    }
}

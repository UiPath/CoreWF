// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Activities;
using Runtime;

internal class AsyncInvokeOperation
{
    private readonly object _thisLock;

    public AsyncInvokeOperation(SynchronizationContext syncContext)
    {
        Fx.Assert(syncContext != null, "syncContext cannot be null");
        SyncContext = syncContext;
        _thisLock = new object();
    }

    private SynchronizationContext SyncContext { get; set; }

    private bool Completed { get; set; }

    public void OperationStarted() => SyncContext.OperationStarted();

    public void OperationCompleted()
    {
        lock (_thisLock)
        {
            Fx.AssertAndThrowFatal(!Completed, "Async operation has already been completed");
            Completed = true;
        }
        SyncContext.OperationCompleted();
    }

    public void PostOperationCompleted(SendOrPostCallback callback, object arg)
    {
        lock (_thisLock)
        {
            Fx.AssertAndThrowFatal(!Completed, "Async operation has already been completed");
            Completed = true;
        }
        Fx.Assert(callback != null, "callback cannot be null");
        SyncContext.Post(callback, arg);
        SyncContext.OperationCompleted();
    }
}

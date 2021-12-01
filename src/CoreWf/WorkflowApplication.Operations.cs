// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Hosting;
using Runtime;

public partial class WorkflowApplication
{
    private class InstanceOperation
    {
        private AsyncWaitHandle _waitHandle;

        public InstanceOperation()
        {
            InterruptsScheduler = true;
            RequiresInitialized = true;
        }

        public bool Notified { get; set; }

        public int ActionId { get; set; }

        public bool InterruptsScheduler { get; protected set; }

        public bool RequiresInitialized { get; set; }

        public void OnEnqueued() => _waitHandle = new AsyncWaitHandle();

        public virtual bool CanRun(WorkflowApplication instance) => true;

        public void NotifyTurn()
        {
            Fx.Assert(_waitHandle != null, "We must have a wait handle.");

            _waitHandle.Set();
        }

        public bool WaitForTurn(TimeSpan timeout)
        {
            if (_waitHandle != null)
            {
                return _waitHandle.Wait(timeout);
            }

            return true;
        }

        public bool WaitForTurnAsync(TimeSpan timeout, Action<object, TimeoutException> callback, object state)
        {
            if (_waitHandle != null)
            {
                return _waitHandle.WaitAsync(callback, state, timeout);
            }

            return true;
        }
    }

    private class RequiresIdleOperation : InstanceOperation
    {
        private readonly bool _requiresRunnableInstance;

        public RequiresIdleOperation()
            : this(false) { }

        public RequiresIdleOperation(bool requiresRunnableInstance)
        {
            InterruptsScheduler = false;
            _requiresRunnableInstance = requiresRunnableInstance;
        }

        public override bool CanRun(WorkflowApplication instance)
        {
            if (_requiresRunnableInstance && instance._state != WorkflowApplicationState.Runnable)
            {
                return false;
            }

            return instance.Controller.State == WorkflowInstanceState.Idle || instance.Controller.State == WorkflowInstanceState.Complete;
        }
    }

    private class DeferredRequiresIdleOperation : InstanceOperation
    {
        public DeferredRequiresIdleOperation()
        {
            InterruptsScheduler = false;
        }

        public override bool CanRun(WorkflowApplication instance)
        {
            return (ActionId != instance._actionCount && instance.Controller.State == WorkflowInstanceState.Idle) || instance.Controller.State == WorkflowInstanceState.Complete;
        }
    }

    private class RequiresPersistenceOperation : InstanceOperation
    {
        public override bool CanRun(WorkflowApplication instance)
        {
            if (!instance.Controller.IsPersistable && instance.Controller.State != WorkflowInstanceState.Complete)
            {
                instance.Controller.PauseWhenPersistable();
                return false;
            }
            return true;
        }
    }
}

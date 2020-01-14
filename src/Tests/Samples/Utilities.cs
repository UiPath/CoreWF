// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Runtime.DurableInstancing;
using System.Collections.Generic;
using System.Threading;

namespace Samples
{
    public static class Utilities
    {
        public static WorkflowApplication CreateWorkflowApplication(
            Activity wfDefinition,
            InstanceStore store = null,
            AutoResetEvent idleEvent = null,
            AutoResetEvent unloadedEvent = null,
            AutoResetEvent completedEvent = null,
            AutoResetEvent abortedEvent = null,
            Dictionary<string, object> wfArguments = null,
            Action<WorkflowApplicationIdleEventArgs> idleDelegate = null,
            Action<WorkflowApplicationEventArgs> unloadedDelegate = null,
            Action<WorkflowApplicationCompletedEventArgs> completedDelegate = null,
            Action<WorkflowApplicationAbortedEventArgs> abortedDelegate = null,
            Func<WorkflowApplicationIdleEventArgs, PersistableIdleAction> persistableIdleDelegate = null)
        {
            WorkflowApplication wfApp = new WorkflowApplication(wfDefinition);
            wfApp.InstanceStore = store;

            if (idleDelegate == null)
            {
                wfApp.Idle = delegate (WorkflowApplicationIdleEventArgs e)
                {
                    if (idleEvent != null)
                    {
                        idleEvent.Set();
                    }
                };
            }
            else
            {
                wfApp.Idle = idleDelegate;
            }

            if (unloadedDelegate == null)
            {
                wfApp.Unloaded = delegate (WorkflowApplicationEventArgs e)
                {
                    if (unloadedEvent != null)
                    {
                        unloadedEvent.Set();
                    }
                };
            }
            else
            {
                wfApp.Unloaded = unloadedDelegate;
            }

            if (completedDelegate == null)
            {
                wfApp.Completed = delegate (WorkflowApplicationCompletedEventArgs e)
                {
                    if (completedEvent != null)
                    {
                        completedEvent.Set();
                    }
                };
            }
            else
            {
                wfApp.Completed = completedDelegate;
            }

            if (abortedDelegate == null)
            {
                wfApp.Aborted = delegate (WorkflowApplicationAbortedEventArgs e)
                {
                    if (abortedEvent != null)
                    {
                        abortedEvent.Set();
                    }
                };
            }
            else
            {
                wfApp.Aborted = abortedDelegate;
            }

            if (persistableIdleDelegate == null)
            {
                wfApp.PersistableIdle = delegate (WorkflowApplicationIdleEventArgs e)
                {
                    if (store != null)
                    {
                        return PersistableIdleAction.Unload;
                    }
                    else
                    {
                        return PersistableIdleAction.None;
                    }
                };
            }
            else
            {
                wfApp.PersistableIdle = persistableIdleDelegate;
            }

            return wfApp;
        }
    }
}

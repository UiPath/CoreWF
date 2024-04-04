// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;

namespace LegacyTest.Test.Common.TestObjects.Runtime.DynamicUpdates
{
    public static class DynamicUpdates
    {
        public static void PersistAndUpdate(this TestWorkflowRuntime twRuntime, WorkflowIdentity updatedIdentity)
        {
            twRuntime.PersistWorkflow();
            twRuntime.UnloadWorkflow();
            if (null != updatedIdentity)
            {
                twRuntime.LoadAndUpdateWorkflow(updatedIdentity.ToString());
            }
            else
            {
                twRuntime.LoadWorkflow();
            }
            twRuntime.ResumeWorkflow();
        }
    }
}

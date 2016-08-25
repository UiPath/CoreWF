// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;

namespace Test.Common.TestObjects.Runtime.DynamicUpdates
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

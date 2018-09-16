// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;

namespace Test.Common.TestObjects.Runtime
{
    public interface IWorkflowRuntimeAdapter
    {
        void OnInstanceCreate(WorkflowApplication workflowInstance);
        void OnInstanceLoad(WorkflowApplication workflowInstance);
    }

    public interface IWorkflowRuntimeAdapter2 : IWorkflowRuntimeAdapter
    {
        WorkflowApplication CreateInstance(Activity activity);
        void LoadInstance(WorkflowApplication application, Guid instanceId);
    }
}

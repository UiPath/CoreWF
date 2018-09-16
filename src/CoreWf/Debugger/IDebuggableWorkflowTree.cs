// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Debugger
{
    // Interface to implement in serializable object containing Workflow
    // to be debuggable with Workflow debugger.
    public interface IDebuggableWorkflowTree
    {
        // Return the root of the workflow tree.
        Activity GetWorkflowRoot(); 
    }

}

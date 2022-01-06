// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

internal interface IFlowSwitch
{
    bool Execute(NativeActivityContext context, Flowchart parent);
    FlowNode GetNextNode(object value);
}

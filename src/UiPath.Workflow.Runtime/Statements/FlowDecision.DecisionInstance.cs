// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public sealed partial class FlowDecision
{
    public class DecisionInstance : Flowchart.NodeInstance<FlowDecision, bool>
    {

        protected override void Execute()
        {
            Flowchart.ScheduleWithCallback(Node.Condition);
        }
        protected override void OnCompletionCallback(bool result)
        {
            Flowchart.EnqueueNodeExecution(result ? Node.True : Node.False);
        }
    }
}

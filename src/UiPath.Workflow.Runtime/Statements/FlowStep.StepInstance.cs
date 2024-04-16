// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public sealed partial class FlowStep
{
    public class StepInstance : NodeInstance<FlowStep>
    {
        protected override void Execute()
        {
            if (Node.Next == null)
            {
                if (TD.FlowchartNextNullIsEnabled())
                {
                    TD.FlowchartNextNull(Flowchart.DisplayName);
                }
            }
            if (Node.Action == null)
            {
                OnCompletionCallback();
            }
            else
            {
                Flowchart.ScheduleWithCallback(Node.Action);
            }
        }

        protected override void OnCompletionCallback()
        {
            Flowchart.EnqueueNodeExecution(Node.Next);
        }
    }
}

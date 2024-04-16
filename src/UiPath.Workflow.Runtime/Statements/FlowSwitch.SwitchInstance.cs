// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

public sealed partial class FlowSwitch<T>
{
    public class SwitchInstance : NodeInstance<FlowSwitch<T>, T>
    {

        protected override void Execute()
        {
            Flowchart.ScheduleWithCallback(Node.Expression);
        }

        protected override void OnCompletionCallback(T value)
        {
            if (Node.Cases.TryGetValue(value, out FlowNode result))
            {
                if (TD.FlowchartSwitchCaseIsEnabled())
                {
                    TD.FlowchartSwitchCase(Flowchart.DisplayName, value?.ToString());
                }
                Flowchart.EnqueueNodeExecution(result);
            }
            else
            {
                if (Node.Default != null)
                {
                    if (TD.FlowchartSwitchDefaultIsEnabled())
                    {
                        TD.FlowchartSwitchDefault(Flowchart.DisplayName);
                    }
                }
                else
                {
                    if (TD.FlowchartSwitchCaseNotFoundIsEnabled())
                    {
                        TD.FlowchartSwitchCaseNotFound(Flowchart.DisplayName);
                    }
                }
                Flowchart.EnqueueNodeExecution(Node.Default);
            }
        }
    }
}

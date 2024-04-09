// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Action")]
public sealed class FlowStep : FlowNode
{
    public FlowStep() { }

    [DefaultValue(null)]
    public Activity Action { get; set; }

    [DefaultValue(null)]
    [DependsOn("Action")]
    public FlowNode Next { get; set; }

    internal override IReadOnlyList<FlowNode> GetSuccessors()
    {
        if (Next != null)
        {
            return new[] { Next };
        }
        return Array.Empty<FlowNode>();
    }

    internal override void Execute()
    {
        if (Next == null)
        {
            if (TD.FlowchartNextNullIsEnabled())
            {
                TD.FlowchartNextNull(Owner.DisplayName);
            }
        }
        if (Action == null)
        {
            OnCompletionCallback();
        }
        else
        {
            Owner.ScheduleWithCallback(Action);
        }
    }

    protected override void OnCompletionCallback()
    {
        Owner.EnqueueNodeExecution(Next);
    }

    internal override IEnumerable<Activity> GetChildActivities()
    => Action!= null ? new[] { Action } : null;

    public override string ToString()
    {
        return Action?.DisplayName ?? $"{GetType().Name}.{Index}";
    }
}

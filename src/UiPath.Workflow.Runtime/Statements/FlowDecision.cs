// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities.Statements;

public sealed class FlowDecision : FlowNode
{
    private const string DefaultDisplayName = "Decision";
    private string _displayName;

    public FlowDecision()
    {
        _displayName = DefaultDisplayName;
    }

    public FlowDecision(Expression<Func<ActivityContext, bool>> condition)
        : this()
    {
        if (condition == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(condition));
        }

        Condition = new LambdaValue<bool>(condition);
    }

    public FlowDecision(Activity<bool> condition)
        : this()
    {
        Condition = condition ?? throw FxTrace.Exception.ArgumentNull(nameof(condition));
    }

    [DefaultValue(null)]
    public Activity<bool> Condition { get; set; }

    [DefaultValue(null)]
    [DependsOn("Condition")]
    public FlowNode True { get; set; }

    [DefaultValue(null)]
    [DependsOn("True")]
    public FlowNode False { get; set; }

    [DefaultValue(DefaultDisplayName)]
    public string DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    protected override void OnCacheMetadata()
    {
        if (Condition == null)
        {
            Metadata.AddValidationError(SR.FlowDecisionRequiresCondition(Flowchart.DisplayName));
        }
    }

    internal override IReadOnlyList<FlowNode> GetSuccessors() => new[] { True, False };

    internal override IEnumerable<Activity> GetChildActivities() => new[] { Condition };

    internal override void Execute()
    {
        Flowchart.ScheduleWithCallback(Condition);
    }
    protected override void OnCompletionCallback(bool result)
    {
        Flowchart.EnqueueNodeExecution(result ? True : False);
    }
}

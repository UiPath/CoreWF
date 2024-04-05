// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Statements.Interfaces;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities.Statements;

public sealed class FlowDecision : FlowNode, IFlowDecision
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

    IFlowNode IFlowDecision.True { get => this.True; set => this.True = value as FlowNode; }

    IFlowNode IFlowDecision.False { get => this.False; set => this.False = value as FlowNode; }

    [DefaultValue(DefaultDisplayName)]
    public string DisplayName
    {
        get => _displayName;
        set => _displayName = value;
    }

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        if (Condition == null)
        {
            metadata.AddValidationError(SR.FlowDecisionRequiresCondition(owner.DisplayName));
        }
    }

    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        if (True != null)
        {
            connections.Add(True);
        }

        if (False != null)
        {
            connections.Add(False);
        }
    }

    internal override Activity ChildActivity => Condition;

    internal bool Execute(NativeActivityContext context, CompletionCallback<bool> onConditionCompleted)
    {
        context.ScheduleActivity(Condition, onConditionCompleted);
        return false;
    }
}

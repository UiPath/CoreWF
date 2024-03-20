using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;
namespace System.Activities.Statements;

public class FlowSplitBranch
{
    internal FlowNode RuntimeNode {get; set;}
    internal FlowSplit SplitNode { get; set; }
    private string _displayName;

    public FlowNode StartNode { get; set; }
    [DefaultValue(null)]
    public Activity<bool> Condition { get; set; }
    [DefaultValue(null)]
    [DependsOn(nameof(Condition))]
    public string DisplayName
    {
        get => Condition?.DisplayName ?? _displayName;
        set
        {
            if (Condition is not null)
                Condition.DisplayName = value;

            _displayName = value;
        }
    }
}

public class FlowSplit : FlowNode
{
    [DefaultValue(null)]
    public Collection<FlowSplitBranch> Branches
        => _branches ??= new ValidatingCollection<FlowSplitBranch>
        {
            OnAddValidationCallback = item =>
            {
                if (item == null)
                {
                    throw FxTrace.Exception.ArgumentNull(nameof(item));
                }
                if (item.SplitNode != null)
                    throw FxTrace.Exception.Argument(nameof(item), "Cannot add same branch to multiple Split nodes");
                item.SplitNode = this;
                if (item.StartNode == null)
                    throw FxTrace.Exception.Argument(nameof(item.StartNode), "StartNode must not be null.");
            }
        };

    private ValidatingCollection<FlowSplitBranch> _branches;
    internal override Activity ChildActivity => null;

    private List<StartBranch> RuntimeBranchesNodes { get; set; }

    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        RuntimeBranchesNodes ??= Branches.Select(b => new StartBranch(b)).ToList();
        connections.AddRange(RuntimeBranchesNodes);
    }

    internal override void Execute(FlowNode predecessorNode)
    {
        for (int i = RuntimeBranchesNodes.Count - 1; i >= 0; i--)
        {
            var branch = RuntimeBranchesNodes[i];
                Owner.EnqueueNodeExecution(node: branch);
        }
    }
    private class StartBranch : FlowNode
    {

        public StartBranch(FlowSplitBranch flowSplitBranch)
        {
            FlowSplitBranch = flowSplitBranch;
        }

        private FlowSplitBranch FlowSplitBranch { get; }

        internal override Activity ChildActivity => null;

        internal override void Execute(FlowNode predecessorNode)
        {
            Owner.StartBranch(FlowSplitBranch);
            Owner.EnqueueNodeExecution(FlowSplitBranch.RuntimeNode, FlowSplitBranch);
        }

        internal override void GetConnectedNodes(IList<FlowNode> connections)
        {
            Owner.AddStaticBranches(this, new[] { FlowSplitBranch });
            FlowSplitBranch.RuntimeNode ??= (FlowSplitBranch.Condition is null)
                ? FlowSplitBranch.StartNode
                : new FlowDecision()
                {
                    Condition = FlowSplitBranch.Condition,
                    DisplayName = FlowSplitBranch.DisplayName,
                    True = FlowSplitBranch.StartNode,
                };
            connections.Add(FlowSplitBranch.RuntimeNode);
        }
    }
}
using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using static System.Activities.Statements.Flowchart;
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

    private List<FlowNode> RuntimeBranchesNodes { get; set; }

    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {

        RuntimeBranchesNodes ??= Branches.Select(GetRuntimeNode).ToList();
        connections.AddRange(RuntimeBranchesNodes);

        FlowNode GetRuntimeNode(FlowSplitBranch splitBranch)
        {
            splitBranch.RuntimeNode ??= (splitBranch.Condition is null)
            ? splitBranch.StartNode
            : new FlowDecision()
            {
                Condition = splitBranch.Condition,
                DisplayName = splitBranch.DisplayName,
                True = splitBranch.StartNode,
            };
            connections.Add(splitBranch.RuntimeNode);
            var splitBranches = Owner.GetStaticBranches(splitBranch.SplitNode);
            Owner.GetStaticBranches(splitBranch.RuntimeNode).Push(splitBranch, splitBranches);

            return splitBranch.RuntimeNode;
        }
    }

    internal override void Execute()
    {
        for (int i = RuntimeBranchesNodes.Count - 1; i >= 0; i--)
        {
            var branch = RuntimeBranchesNodes[i];
            Owner.EnqueueNodeExecution(branch, Owner.Current.BranchInstance.Push(
                        branchId: $"{Owner.Current.NodeInstanceId}_{branch.Index}",
                        splitInstanceId: Owner.Current.NodeInstanceId));
        }
    }
}
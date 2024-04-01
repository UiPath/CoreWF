using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
namespace System.Activities.Statements;

public class FlowSplitBranch
{
    internal FlowSplit SplitNode { get; set; }
    public FlowNode StartNode { get; set; }
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

        RuntimeBranchesNodes ??= Branches.Select(SetupRuntimeNode).ToList();
        connections.AddRange(RuntimeBranchesNodes);

        FlowNode SetupRuntimeNode(FlowSplitBranch splitBranch)
        {
            var runtimeNode = splitBranch.StartNode;

            connections.Add(runtimeNode);
            var splitBranches = Owner.GetStaticBranches(splitBranch.SplitNode);
            Owner.GetStaticBranches(runtimeNode).Push(splitBranch, splitBranches);

            return runtimeNode;
        }
    }

    protected override void OnEndCacheMetadata()
    {
        var merges = Branches.SelectMany(b => Owner.GetMerges(b.StartNode)).Distinct().ToList();
        if (merges.Count != 1)
            AddValidationError("Split branches should end in exactly one Merge node.", merges);
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
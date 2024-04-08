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

    private List<FlowNode> RuntimeBranchesNodes { get; set; }

    internal override IReadOnlyList<FlowNode> GetSuccessors()
    {
        RuntimeBranchesNodes ??= Branches.Select(SetupRuntimeNode).ToList();
        return RuntimeBranchesNodes;

        FlowNode SetupRuntimeNode(FlowSplitBranch splitBranchOut)
        {
            var runtimeNode = splitBranchOut.StartNode;

            var splitIncomingBranches = Owner.GetStaticBranches(splitBranchOut.SplitNode);
            Owner.GetStaticBranches(runtimeNode).Push(splitBranchOut, splitIncomingBranches);

            return runtimeNode;
        }
    }

    protected override void OnEndCacheMetadata()
    {
        HashSet<FlowMerge> allMerges = new();
        foreach (var branch in Branches)
        {
            var merges = Owner.GetMerges(branch.StartNode).Distinct().ToList();
            allMerges.AddRange(merges);
            if (merges.Count >= 1)
                AddValidationError("Split branch should end in only one Merge node.", new[] { branch.StartNode }.Concat(merges));
        }
        if (allMerges.Count == 0)
            AddValidationError("Split should end in one Merge node.");
        if (allMerges.Count > 1)
            AddValidationError("All split branches should end in only one Merge node.", allMerges);
    }

    internal override void Execute()
    {
        for (int i = RuntimeBranchesNodes.Count - 1; i >= 0; i--)
        {
            var branch = RuntimeBranchesNodes[i];
            Owner.EnqueueNodeExecution(branch, Owner.CurrentBranch.Push(
                        branchId: $"{branch.Index}",
                        splitId: Owner.CurrentNodeId));
        }
    }
}
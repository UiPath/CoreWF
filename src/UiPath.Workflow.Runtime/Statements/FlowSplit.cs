using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
namespace System.Activities.Statements;

public class FlowSplit : FlowNode
{
    [DefaultValue(null)]
    public Collection<FlowNode> Branches
        => _branches ??= ValidatingCollection<FlowNode>.NullCheck();

    private ValidatingCollection<FlowNode> _branches;

    internal override IReadOnlyList<FlowNode> GetSuccessors()
    => Branches.ToList();

    protected override void OnEndCacheMetadata()
    {
        HashSet<FlowMerge> allMerges = new();
        foreach (var branch in Branches)
        {
            var merges = Owner.GetMerges(branch).Distinct().ToList();
            allMerges.AddRange(merges);
            if (merges.Count > 1)
                AddValidationError("Split branch should end in only one Merge node.", new[] { branch }.Concat(merges));
        }
        if (allMerges.Count == 0)
            AddValidationError("Split should end in one Merge node.");
        if (allMerges.Count > 1)
            AddValidationError("All split branches should end in only one Merge node.", allMerges);
    }

    internal override Flowchart.NodeInstance CreateInstance()
        => new SplitInstance();

    private class SplitInstance : Flowchart.NodeInstance<FlowSplit>
    {
        internal override void Execute(Flowchart Owner, FlowSplit Node)
        {
            for (int i = Node.Branches.Count - 1; i >= 0; i--)
            {
                var branch = Node.Branches[i];
                Owner.EnqueueNodeExecution(branch, Owner.CurrentBranch.Push(
                            branchId: $"{branch.Index}",
                            splitId: Owner.CurrentNodeId));
            }
        }
    }
}
using System.Activities.Validation;
using System.Linq;
namespace System.Activities.Statements;

public class FlowMerge : FlowNode
{
    [DefaultValue(null)]
    public Activity<bool> Completion { get; set; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }

    internal override Activity ChildActivity => Completion;
    private List<FlowSplitBranch> ConnectedBranches { get; set; }

    private record MergeState
    {
        public bool Done { get; set; }
        public HashSet<int> CompletedNodeIndeces { get; set; }
    }

    public FlowMerge()
    {
    }

    protected override void OnEndCacheMetadata()
    {
        var predecessors = Owner.GetPredecessors(this);
        ConnectedBranches = predecessors.SelectMany(p => Owner.GetStaticBranches(p)).Distinct().ToList();
        var outgoingBranches = ConnectedBranches.SelectMany(b => Owner.GetStaticBranches(b.SplitNode)).Distinct().ToList();
        Owner.AddStaticBranches(this, outgoingBranches);

        //ValidateAllBranches();

        void ValidateAllBranches()
        {
            var splits = ConnectedBranches.Select(bl => bl.SplitNode).Distinct().ToList();
            if (splits.Count() > 1)
            {
                Metadata.AddValidationError("All join branches should start in the same parallel node.");
            }
            var split = splits.FirstOrDefault();
            if (split is null)
                return;
            var branches = ConnectedBranches.Select(b => b.RuntimeNode.Index).Distinct().ToList();
            if (branches.Count != split.Branches.Count) 
                Metadata.AddValidationError("All parallel branches should end in same join node.");
        }
    }
    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }

    MergeState GetJoinState()
    {
        var key = $"{Index}";
        var joinStates = Owner.GetPersistableState<Dictionary<string, MergeState>>("FlowMerge");
        joinStates.TryGetValue(key, out var joinState);
        if (joinState is null)
        {
            joinState = new()
            {
                CompletedNodeIndeces = new HashSet<int>(),
            };
            joinStates.Add(key, joinState);
        }
        return joinState;
    }
    internal override void Execute(FlowNode predecessorNode)
    {
        var joinState = GetJoinState();
        var branch = Owner.GetBranch(predecessorNode);
        if (!ConnectedBranches.Contains(branch))
            return;
        joinState.CompletedNodeIndeces.Add(branch.RuntimeNode.Index);
        joinState.CompletedNodeIndeces
            .AddRange(Owner
            .GetCompletedBranches()
            .Select(b => b.RuntimeNode.Index));
        if (Completion is not null)
        {
            Owner.ScheduleWithCallback(Completion);
        }
        else
        {
            OnCompletionCallback(false);
        }
    }
    protected override void OnCompletionCallback(bool result)
    {
        var joinState = GetJoinState();
        var incompleteBranches = ConnectedBranches
            .Select(b => b.RuntimeNode.Index)
            .Except(joinState.CompletedNodeIndeces).ToList();
        if (result)
        {
            EndAllBranches();
        }

        if (incompleteBranches.Any())
        {
            Owner.MarkDoNotCompleteNode();
            return;
        }

        joinState.Done = true;
        Owner.EnqueueNodeExecution(Next);
        
        void EndAllBranches()
        {
            var toCancel = incompleteBranches;
            Cancel(toCancel);
        }

        void Cancel(List<int> toCancel)
        {
            foreach (var branchNode in toCancel)
            {
                if (branchNode == Index)
                    continue;
                if (Owner.Cancel(branchNode))
                {
                    var successors = Owner.GetSuccessors(branchNode)
                        .Select(p => p.Index).ToList();
                    Cancel(successors);
                }
            }
        }
    }

    internal void OnBranchEnded(FlowNode current)
    {
        var branch = Owner.GetBranch(current);
        
        if (ConnectedBranches.Contains(branch) && !GetJoinState().Done)
            Owner.EnqueueNodeExecution(this);
    }
}
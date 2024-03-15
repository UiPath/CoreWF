using System.Linq;
namespace System.Activities.Statements;

public class FlowMerge : FlowNode
{
    private FlowSplit _split;
    [DefaultValue(null)]
    public Activity<bool> Completion { get; set; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    public FlowSplit SplitNode
    {
        get => _split;
        init
        {
            if (value?.MergeNode is { } merge && merge != this)
                    throw new InvalidOperationException("Split and merge must be linked both ways.");
            _split = value;
        }
    }


    internal override Activity ChildActivity => Completion;

    private List<Flowchart.BranchLinks> ConnectedBranches { get; set; }

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
        ConnectedBranches = predecessors.Select(p => Owner.GetBranch(p)).ToList();

        ValidateAllBranches();

        void ValidateAllBranches()
        {
            HashSet<FlowNode> visited = new()
            {
                this,
                _split,
            };
            List<FlowNode> toVisit = new(1) { this };
            do
            {
                var predecessors = toVisit.SelectMany(v => Owner.GetPredecessors(v)).ToList();
                toVisit = new List<FlowNode>(predecessors.Count);
                foreach (var predecessor in predecessors)
                {
                    if (!visited.Add(predecessor))
                        continue;
                    if (predecessor == null)
                    {
                        Metadata.AddValidationError("All join branches should start in the parent parallel node.");
                        continue;
                    }
                    toVisit.Add(predecessor);
                }
            }
            while (toVisit.Any());
            var allBranchesJoined = _split.Branches.All(b => visited.Contains(b.StartNode));
            if (!allBranchesJoined)
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

    MergeState GetJoinState(Func<MergeState> add = null)
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
        joinState.CompletedNodeIndeces.Add(branch.NodeIndex);
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
            .Select(b => b.NodeIndex)
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
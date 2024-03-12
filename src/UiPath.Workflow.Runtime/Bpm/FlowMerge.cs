using System.Activities.Validation;
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


    private readonly FlowchartState.Of<Dictionary<string, MergeState>> _joinStates;
    internal override Activity ChildActivity => Completion;

    private List<int> ConnectedBranches { get; set; }

    private record MergeState
    {
        public bool Done { get; set; }
        public HashSet<int> CompletedNodeIndeces { get; set; }
    }

    public FlowMerge(FlowSplit split) : this()
    {
        SplitNode = split;
    }


    public FlowMerge()
    {
        _joinStates = new("FlowMerge", this, () => new());
    }
    protected override void OnEndCacheMetadata()
    {
        ConnectedBranches = SplitNode
            .RuntimeBranchesNodes
            .Select(b => b.Index).ToList();

        ValidateAllBranches();

        void ValidateAllBranches()
        {
            HashSet<FlowNode> visited = new()
            {
                this,
                SplitNode,
            };
            List<FlowNode> toVisit = new(1) { this };
            do
            {
                var predecessors = toVisit.SelectMany(v => Extension.GetPredecessors(v)).ToList();
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
            var allBranchesJoined = SplitNode.Branches.All(b => visited.Contains(b.StartNode));
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
        var joinStates = _joinStates.GetOrAdd();
        joinStates.TryGetValue(key, out var joinState);
        if (joinState is null)
        {
            joinState = add();
            joinStates.Add(key, joinState);
        }
        return joinState;
    }
    internal override void Execute(FlowNode predecessorNode)
    {
        var joinState = GetJoinState(() => new()
        {
            CompletedNodeIndeces = new HashSet<int>(),
        });
        joinState.CompletedNodeIndeces.Add(predecessorNode.Index);
        if (Completion is not null)
        {
            Extension.ScheduleWithCallback(Completion);
        }
        else
        {
            OnCompletionCallback(false);
        }
    }
    protected override void OnCompletionCallback(bool result)
    {
        var joinState = GetJoinState();
        var incompleteBranches = ConnectedBranches.Except(joinState.CompletedNodeIndeces).ToList();
        if (result)
        {
            EndAllBranches();
        }

        if (incompleteBranches.Any())
            return;

        joinState.Done = true;
        Extension.ExecuteNextNode(Next);
        
        void EndAllBranches()
        {
            var toCancel = incompleteBranches;
            if (Cancel(toCancel))
            {
                if (!Cancel(toCancel))
                {
                    joinState.CompletedNodeIndeces.AddRange(incompleteBranches);
                    OnCompletionCallback(false);
                }
            }

        }

        bool Cancel(IEnumerable<int> toCancel)
        {
            var result = false;
            foreach (var branch in toCancel)
            {
                if (Extension.Cancel(branch))
                {
                    result = true;
                    Cancel(Extension.GetSuccessors(branch).Select(p => p.Index));
                }
            }
            return result;
        }
    }
}
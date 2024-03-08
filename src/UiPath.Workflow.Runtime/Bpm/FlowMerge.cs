using System.Linq;
namespace System.Activities.Statements;

public class FlowMerge : FlowNodeBase
{
    private readonly FlowchartState.Of<Dictionary<string,MergeState>> _joinStates;
    [DefaultValue(null)]
    public Activity<bool> Completion { get; set; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    public FlowSplit Split { get; internal set; }
    internal override Activity ChildActivity => Completion;

    private record MergeState
    {
        public int WaitCount { get; set; }
        public bool Done { get; set; }
        public HashSet<int> CompletedNodeIndeces { get; set; }
        public List<int> ConnectedBranches { get; set; }
        public Dictionary<string, int> PendingCompletionsInstanceIdToPredecessorIndex { get; set; }
    }

    internal FlowMerge()
    {
        _joinStates = new("FlowMerge", this, () => new());
    }
    internal override void EndCacheMetadata()
    {
        ValidateAllBranches();

        void ValidateAllBranches()
        {
            HashSet<FlowNode> visited = new()
            {
                this,
                Split,
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
            var allBranchesJoined = Split.Branches.All(b => visited.Contains(b.StartNode));
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
            ConnectedBranches = new(Extension.GetPredecessors(this).Select(p => p.Index)),
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

        if (!result && joinState.CompletedNodeIndeces.Count < Split.Branches.Count)
            return;

        joinState.Done = true;
        var toCancel = joinState.ConnectedBranches.Except(joinState.CompletedNodeIndeces);
        Cancel(toCancel);
        Owner.ExecuteNextNode(Next);

        void Cancel(IEnumerable<int> toCancel)
        {
            foreach (var branch in toCancel)
            {
                if (Extension.Cancel(branch))
                {
                    Cancel(Extension.GetPredecessors(branch).Select(p => p.Index));
                }
            }
        }
    }
}
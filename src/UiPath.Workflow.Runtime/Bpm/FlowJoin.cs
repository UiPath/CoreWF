using System.Linq;
namespace System.Activities.Statements;

public class FlowJoin : FlowNodeBase
{
    private readonly FlowchartState.Of<Dictionary<string,JoinState>> _joinStates;
    [DefaultValue(null)]
    public Activity<bool> Completion { get; set; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    public FlowParallel Parallel { get; internal set; }
    internal override Activity ChildActivity => Completion;

    private record JoinState
    {
        public int WaitCount { get; set; }
        public bool Done { get; set; }
        public HashSet<int> CompletedNodeIndeces { get; set; }
        public List<int> ConnectedBranches { get; set; }
        public Dictionary<string, int> PendingCompletionsInstanceIdToPredecessorIndex { get; set; }
    }

    internal FlowJoin()
    {
        _joinStates = new("FlowJoin", this, () => new());
    }
    internal override void EndCacheMetadata()
    {
        ValidateAllBranches();

        void ValidateAllBranches()
        {
            HashSet<FlowNode> visited = new()
            {
                this,
                Parallel,
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
            var allBranchesJoined = Parallel.Branches.All(b => visited.Contains(b.StartNode));
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
    JoinState GetJoinState(NativeActivityContext context, Func<JoinState> add = null)
    {
        var key = $"{Index}";
        var joinStates = _joinStates.GetOrAdd(context);
        joinStates.TryGetValue(key, out var joinState);
        if (joinState is null)
        {
            joinState = add();
            joinStates.Add(key, joinState);
        }
        return joinState;
    }
    internal override void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode)
    {
        var joinState = GetJoinState(context, () => new()
        {
            ConnectedBranches = new(Extension.GetPredecessors(this).Select(p => p.Index)),
            CompletedNodeIndeces = new HashSet<int>(),
        });
        joinState.CompletedNodeIndeces.Add(predecessorNode.Index);
        if (Completion is not null)
        {
            ScheduleWithCallback(context, Completion);
        }
        else
        {
            OnCompletionCallback(context, completedInstance, false);
        }
    }
    protected override void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        var joinState = GetJoinState(context);

        if (!result && joinState.CompletedNodeIndeces.Count < Parallel.Branches.Count)
            return;

        joinState.Done = true;
        var toCancel = joinState.ConnectedBranches.Except(joinState.CompletedNodeIndeces);
        Cancel(toCancel);
        Owner.ExecuteNextNode(context, Next, completedInstance);

        void Cancel(IEnumerable<int> toCancel)
        {
            foreach (var branch in toCancel)
            {
                if (Extension.Cancel(context, branch))
                {
                    Cancel(Extension.GetPredecessors(branch).Select(p => p.Index));
                }
            }
        }
    }
}
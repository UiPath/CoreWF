using System.Activities.Statements;
using System.Linq;
namespace System.Activities.Bpm;

public class FlowJoin : FlowNodeExtensible
{
    private FlowchartState<Dictionary<string,JoinState>> _joinStates;
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    [DefaultValue(null)]
    public int? WaitCount { get; set; }

    internal override Activity ChildActivity => null;

    record JoinState
    {
        public int WaitCount { get; set; }
        public bool Done { get; set; }
        public HashSet<int> CompletedNodeIndeces { get; set; }
        public List<int> ConnectedBranches { get; set; }
    }

    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        _joinStates = new("FlowJoin", owner, () => new());
        base.OnOpen(owner, metadata);
    }

    internal override void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode)
    {
        var key = $"{Index}";
        var joinStates = _joinStates.GetOrAdd(context);
        joinStates.TryGetValue(key, out var joinState);
        if (joinState == null)
        {
            List<int> connectedBranches = new (Owner._extension.GetPredecessors(context, Index));
            joinState = new() 
            {
                ConnectedBranches = connectedBranches,
                CompletedNodeIndeces = new HashSet<int>(),
                WaitCount = Math.Min(connectedBranches.Count, WaitCount ?? connectedBranches.Count) 
            };
            joinStates.Add(key, joinState);
        }
        joinState.CompletedNodeIndeces.Add(predecessorNode.Index);

        if (joinState.CompletedNodeIndeces.Count < joinState.WaitCount || joinState.Done)
        {
            return;
        }
        joinState.Done = true;
        var toCancel = joinState.ConnectedBranches.Except(joinState.CompletedNodeIndeces);
        Cancel(toCancel);
        Owner.ExecuteNextNode(context, Next, completedInstance);

        void Cancel(IEnumerable<int> toCancel) 
        {
            foreach (var branch in toCancel)
            {
                if (Owner._extension.Cancel(context, branch))
                {
                    Cancel(Owner._extension.GetPredecessors(context, branch));
                }
            }
        }
    }
}
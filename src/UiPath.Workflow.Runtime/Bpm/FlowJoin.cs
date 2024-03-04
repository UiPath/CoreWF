using System.Activities.Statements;
namespace System.Activities.Bpm;

public class FlowJoin : FlowNodeExtensible
{
    private FlowchartState<JoinState> _joinStates;
    private HashSet<FlowNode> _connectedBranches = new();
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    [DefaultValue(null)]
    public int? WaitCount { get; set; }

    internal override Activity ChildActivity => null;

    record JoinState
    {
        public int Count;

        public int WaitCount { get; internal set; }
        public bool Done { get; internal set; }
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
        _joinStates = new("FlowJoin", owner);
        base.OnOpen(owner, metadata);
    }

    internal override void Execute(NativeActivityContext context)
    {
        var key = $"{Index}";
        var joinStates = _joinStates.GetOrAdd(context);
        joinStates.TryGetValue(key, out var joinState);
        if (joinState == null)
        {
            joinState = new() { Count = 1, WaitCount = Math.Min(_connectedBranches.Count, WaitCount ?? _connectedBranches.Count) };
            joinStates.Add(key, joinState);
        }
        else
        {
            joinState.Count++;
        }
        if (joinState.Count < joinState.WaitCount || joinState.Done)
        {
            return;
        }

        joinState.Done = true;
        context.CancelChildren();
        Owner.ExecuteNextNode(context, Next, context.CurrentInstance);
    }

    protected override void NotifyPredecessor(FlowNode predecessor)
    {
        _connectedBranches.Add(predecessor);
    }
}
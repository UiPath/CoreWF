using System.Activities.Validation;
using System.Linq;
using static System.Activities.Statements.Flowchart;
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
        public HashSet<BranchInstance> CompletedBranches { get; set; }
    }

    public FlowMerge()
    {
    }

    protected override void OnEndCacheMetadata()
    {
        var predecessors = Owner.GetPredecessors(this);
        ConnectedBranches = predecessors
            .Select(p => Owner.GetStaticBranches(p).GetTop())
            .Distinct().ToList();

        ValidateAllBranches();

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
            PopBranchesStacks();
        }

        void PopBranchesStacks()
        {
            var nextStacks = Owner.GetStaticBranches(this);
            Owner.GetStaticBranches(Next).AddPopFrom(nextStacks);
        }
    }

    MergeState GetJoinState(NodeInstance mergeInstance)
    {
        var key = $"{Index}.{mergeInstance.BranchInstance.SplitInstanceId}";
        var joinStates = Owner.GetPersistableState<Dictionary<string, MergeState>>("FlowMerge");
        joinStates.TryGetValue(key, out var joinState);
        if (joinState is null)
        {
            joinState = new()
            {
                CompletedBranches = new(),
            };
            joinStates.Add(key, joinState);
        }
        return joinState;
    }
    internal override void Execute()
    {
        var joinState = GetJoinState(Owner.Current);
        var branch = Owner.Previous.BranchInstance;
        joinState.CompletedBranches.Add(branch);

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
        var joinState = GetJoinState(Owner.Current);
        if (result)
        {
            EndAllBranches();
        }
        var runningBranches = Owner.GetOtherRunningBranches(joinState.CompletedBranches);

        if (runningBranches.Count > 0)
        {
            Owner.MarkDoNotCompleteNode();
            return;
        }
        if (joinState.Done)
        {
            return;
        }
        joinState.Done = true;

        Owner.EnqueueNodeExecution(Next, Owner.Current.BranchInstance.Pop()) ;
        
        void EndAllBranches()
        {
            Owner.CancelOtherBranches(joinState.CompletedBranches);
        }
    }
}
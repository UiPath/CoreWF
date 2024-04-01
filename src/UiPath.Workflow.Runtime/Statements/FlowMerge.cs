using System.Activities.Validation;
using System.Linq;
using static System.Activities.Statements.Flowchart;
namespace System.Activities.Statements;

public class FlowMergeAny : FlowMerge
{
    private protected override bool IsMergeAny => true;
}

public class FlowMergeAll : FlowMerge
{
    private protected override bool IsMergeAny => false;
}

public abstract class FlowMerge : FlowNode
{
    private protected abstract bool IsMergeAny { get; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }

    internal override Activity ChildActivity => null;

    private record MergeState
    {
        public bool Done { get; set; }
        public HashSet<BranchInstance> CompletedBranches { get; set; } = new();
    }

    protected override void OnEndCacheMetadata()
    {
        var predecessors = Owner.GetPredecessors(this);
        var connectedBranches = predecessors
            .Select(p => Owner.GetStaticBranches(p).GetTop())
            .Distinct().ToList();

        var splits = connectedBranches.Select(bl => bl.SplitNode).Distinct().ToList();
        if (splits.Count > 1)
            AddValidationError("All merge branches should start in the same Split node.", splits); 
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

    private MergeState GetJoinState()
    {
        var key = $"FlowMerge.{Index}.{Owner.Current.BranchInstance.SplitsStack}";
        var joinState = Owner.GetPersistableState<MergeState>(key);
        return joinState;
    }
    internal override void Execute()
    {
        var joinState = GetJoinState();
        if (joinState.Done)
        {
            return;
        }
        var branch = Owner.Current.BranchInstance;
        joinState.CompletedBranches.Add(branch);

        OnCompletionCallback(IsMergeAny);
    }

    protected override void OnCompletionCallback(bool result)
    {
        var joinState = GetJoinState();
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
        joinState.Done = true;

        Owner.EnqueueNodeExecution(Next, Owner.Current.BranchInstance.Pop()) ;
        
        void EndAllBranches()
        {
            Owner.CancelOtherBranches(joinState.CompletedBranches);
        }
    }
}
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
        public HashSet<BranchInstance> CompletedBranches { get; set; } = new();
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
                Metadata.AddValidationError(new ValidationError("All merge branches should start in the same Split node.") { SourceDetail = this });
            }
            var split = splits.FirstOrDefault();
            if (split is null)
                return;
            var branches = ConnectedBranches.Select(b => b.RuntimeNode.Index).Distinct().ToList();
            if (branches.Count != split.Branches.Count) 
                Metadata.AddValidationError(new ValidationError("Split branches should end in same Merge node.") { SourceDetail = this});
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
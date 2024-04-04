using System.Activities.Validation;
using System.Diagnostics;
using System.Linq;
using static System.Activities.Statements.Flowchart;
namespace System.Activities.Statements;

public class FlowMergeFirst : FlowMerge
{
    private protected override bool IsMergeFirst => true;
}

public class FlowMergeAll : FlowMerge
{
    private protected override bool IsMergeFirst => false;
}

public abstract class FlowMerge : FlowNode
{
    private const string DefaultDisplayName = nameof(FlowMerge);

    private protected abstract bool IsMergeFirst { get; }
    [DefaultValue(null)]
    public FlowNode Next { get; set; }
    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; }

    private class MergeInstance : NodeInstance<FlowMerge>
    {
        public bool Done { get; set; }
        bool set { get; set; }
        private FlowMerge _merge;
        public FlowMerge Merge 
        {
            get => _merge;
            set => _merge = value;
        }
        internal override void Execute(Flowchart Flowchart, FlowMerge node)
        {
            if (set)
            {
                Debug.Assert(Merge == node);
            }
            else
            {
                Merge = node;
                set = true;
            }
            if (Done)
            {
                return;
            }

            if (node.IsMergeFirst)
            {
                EndAllBranches();
            }
            var runningBranches = Flowchart.GetOtherNodes();

            if (runningBranches.Count > 0)
            {
                Flowchart.MarkDoNotCompleteNode();
                return;
            }

            Done = true;
            Flowchart.EnqueueNodeExecution(node.Next, Flowchart.CurrentBranch.Pop());

            void EndAllBranches()
            {
                Flowchart.CancelOtherBranches();
            }
        }
    }

    protected override void OnEndCacheMetadata()
    {
        var predecessors = Owner.GetPredecessors(this);
        var connectedBranches = predecessors
            .SelectMany(p => Owner.GetStaticBranches(p).GetTop())
            .Distinct().ToList();

        var splits = connectedBranches.Select(bl => bl.SplitNode).Distinct().ToList();
        if (splits.Count > 1)
            AddValidationError("All merge branches should start in the same Split node.", splits); 
    }
    internal override IReadOnlyList<FlowNode> GetSuccessors()
    {
        if (Next != null)
        {
            PopBranchesStacks();
            return new [] { Next };
        }
        return Array.Empty<FlowNode>();

        void PopBranchesStacks()
        {
            Owner.GetStaticBranches(Next).AddPop(Owner.GetStaticBranches(this));
        }
    }

    internal override NodeInstance CreateInstance()
    {
        return new MergeInstance();
    }
}
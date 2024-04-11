using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
using static System.Activities.Statements.Flowchart;
namespace System.Activities.Statements;


public abstract class MergeBehavior
{
    private protected MergeBehavior()
    {
        
    }
}

public class MergeFirstBehavior : MergeBehavior
{

}

public class MergeAllBehavior : MergeBehavior
{

}

[ContentProperty(nameof(Behavior))]
public class FlowMerge : FlowNode
{
    private const string DefaultDisplayName = nameof(FlowMerge);

    [DefaultValue(null)]
    public MergeBehavior Behavior { get; set; } = new MergeAllBehavior();
    [DefaultValue(null)]
    [DependsOn(nameof(Behavior))]
    public FlowNode Next { get; set; }
    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; } = DefaultDisplayName;

    private class MergeInstance : NodeInstance<FlowMerge>
    {
        public MergeInstance()
        {
            DoNotComplete = true;
        }
        public bool CancelExecuted { get; set; }
        internal override void Execute()
        {
            if (!DoNotComplete)
                return;
            var runningNodes = Flowchart.GetOtherNodes();
            if (Node.Behavior is MergeFirstBehavior && !CancelExecuted)
            {
                Flowchart.CancelNodes(runningNodes);
                CancelExecuted = true;
            }

            if (runningNodes.Count > 0)
            {
                Debug.WriteLine($"{Node}: DoNotComplete");
                return;
            }

            DoNotComplete = false;
            Debug.WriteLine($"{Node}: Next queued");
            Flowchart.EnqueueNodeExecution(Node.Next, EnqueueType.Pop);
        }
    }

    protected override void OnEndCacheMetadata()
    {
        var connectedBranches = Flowchart.GetStaticSplitsStack(this).GetTop();
        var splits = connectedBranches.Select(bl => bl).Distinct().ToList();
        if (splits.Count > 1)
            AddValidationError("All merge branches should start in the same Split node.", splits); 
    }
    internal override IReadOnlyList<FlowNode> GetSuccessors()
    {
        if (Next != null)
        {
            return new [] { Next };
        }
        return Array.Empty<FlowNode>();
    }

    internal override NodeInstance CreateInstance()
    {
        return new MergeInstance();
    }
}
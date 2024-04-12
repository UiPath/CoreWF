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
public partial class FlowMerge : FlowNode
{
    private const string DefaultDisplayName = nameof(FlowMerge);

    [DefaultValue(null)]
    public MergeBehavior Behavior { get; set; } = new MergeAllBehavior();
    [DefaultValue(null)]
    [DependsOn(nameof(Behavior))]
    public FlowNode Next { get; set; }
    [DefaultValue(DefaultDisplayName)]
    public string DisplayName { get; set; } = DefaultDisplayName;

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
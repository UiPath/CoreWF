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
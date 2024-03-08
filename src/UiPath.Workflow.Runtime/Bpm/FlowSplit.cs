using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;
namespace System.Activities.Statements;

public class FlowSplitBranch
{
    private string _displayName;

    public FlowNode StartNode { get; set; }
    [DefaultValue(null)]
    public Activity<bool> Condition { get; set; }
    [DefaultValue(null)]
    [DependsOn(nameof(Condition))]
    public string DisplayName
    {
        get => Condition?.DisplayName ?? _displayName;
        set
        {
            if (Condition is not null)
                Condition.DisplayName = value;

            _displayName = value;
        }
    }

    public static FlowSplitBranch New(FlowSplit splitNode)
    {
        return new()
        {
            StartNode = splitNode.MergeNode
        };
    }
}

[ContentProperty(nameof(Branches))]
public class FlowSplit : FlowNodeBase
{
    private FlowMerge _merge;
    public FlowMerge MergeNode
    {
        get => _merge;
        init
        {
            if (value?.SplitNode is { } split && split != this)
                throw new InvalidOperationException("Split and merge must be linked both ways.");
            _merge = value;
        }
    }

    [DefaultValue(null)]
    public Collection<FlowSplitBranch> Branches => _branches ??= ValidatingCollection<FlowSplitBranch>.NullCheck();


    private ValidatingCollection<FlowSplitBranch> _branches;
    internal override Activity ChildActivity => null;
 
    private List<FlowNode> _runtimeBranches;

    public FlowSplit()
    {
        MergeNode = new FlowMerge() { SplitNode = this };
    }
    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        _runtimeBranches = new (Branches.Select(b => (b.Condition is null) ? b.StartNode :
                    new FlowDecision()
                    {
                        Condition = b.Condition,
                        DisplayName = b.DisplayName,
                        True = b.StartNode,
                        False = MergeNode
                    }
            ));
        connections.AddRange(_runtimeBranches);
    }

    internal override void Execute(FlowNode predecessorNode)
    {
        for (int i = _runtimeBranches.Count - 1; i >= 0; i--)
        {
            var branch = _runtimeBranches[i];
                Owner.ExecuteNextNode(branch);
        }
    }
}
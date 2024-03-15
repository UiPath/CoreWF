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

public class FlowSplit : FlowNode
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

    internal List<FlowNode> RuntimeBranchesNodes { get; private set; }

    public FlowSplit()
    {
        MergeNode = new FlowMerge() { SplitNode = this };
    }
    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        RuntimeBranchesNodes ??= GetRuntimeNodes();
        connections.AddRange(RuntimeBranchesNodes);
        List<FlowNode> GetRuntimeNodes()
        {
            var result = new List<FlowNode>();
            foreach (var splitBranch in Branches)
            {
                var node = (splitBranch.Condition is null)
                    ? splitBranch.StartNode
                    : new FlowDecision()
                    {
                        Condition = splitBranch.Condition,
                        DisplayName = splitBranch.DisplayName,
                        True = splitBranch.StartNode,
                        False = MergeNode
                    };
                result.Add(node);
                Owner.AddBranch(node, splitBranch, this);
            }
            return result;
        }
    }

    internal override void Execute(FlowNode predecessorNode)
    {
        for (int i = RuntimeBranchesNodes.Count - 1; i >= 0; i--)
        {
            var branch = RuntimeBranchesNodes[i];
                Owner.EnqueueNodeExecution(node: branch, isNewBranch: true);
        }
    }
}
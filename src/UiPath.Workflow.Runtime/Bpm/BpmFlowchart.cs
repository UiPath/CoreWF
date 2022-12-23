using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Markup;
namespace System.Activities.Statements;
[ContentProperty("Nodes")]
public sealed class BpmFlowchart : NativeActivity
{
    private Collection<Variable> _variables;
    private Collection<BpmNode> _nodes;
    private readonly Collection<BpmNode> _reachableNodes = new();
    [DefaultValue(false)]
    public bool ValidateUnconnectedNodes { get; set; }
    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }
    [DependsOn("Variables")]
    public BpmNode StartNode { get; set; }
    [DependsOn("StartNode")]
    public Collection<BpmNode> Nodes => _nodes ??= ValidatingCollection<BpmNode>.NullCheck();
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        Variables.Add(new Variable<Dictionary<string, object>>("flowchartState", c => new()));
        metadata.SetVariablesCollection(Variables);
        GatherReachableNodes(metadata);
        if (ValidateUnconnectedNodes && (_reachableNodes.Count < Nodes.Count))
        {
            metadata.AddValidationError(SR.FlowchartContainsUnconnectedNodes(DisplayName));
        }
        var childrenNodes = ValidateUnconnectedNodes ?Nodes.Distinct() : _reachableNodes;
        var children = new Collection<Activity>();
        children.AddRange(childrenNodes);
        metadata.SetChildrenCollection(children);
    }
    private void GatherReachableNodes(NativeActivityMetadata metadata)
    {
        // Clear out our cached list of all nodes
        _reachableNodes.Clear();
        if (StartNode == null && Nodes.Count > 0)
        {
            metadata.AddValidationError(SR.FlowchartMissingStartNode(DisplayName));
        }
        else
        {
            DepthFirstVisitNodes((n) => VisitNode(n, metadata), StartNode);
        }
    }
    // Returns true if we should visit connected nodes
    private bool VisitNode(BpmNode node, NativeActivityMetadata metadata)
    {
        if (node.Open(this, metadata))
        {
            Fx.Assert(node.Index == -1 && !_reachableNodes.Contains(node), "Corrupt BpmFlowchart.reachableNodes.");
            node.Index = _reachableNodes.Count;
            _reachableNodes.Add(node);
            return true;
        }
        return false;
    }
    private static void DepthFirstVisitNodes(Func<BpmNode, bool> visitNodeCallback, BpmNode start)
    {
        Fx.Assert(visitNodeCallback != null, "This must be supplied since it stops us from infinitely looping.");
        List<BpmNode> connected = new();
        Stack<BpmNode> stack = new();
        if (start == null)
        {
            return;
        }
        stack.Push(start);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == null)
            {
                continue;
            }
            if (visitNodeCallback(current))
            {
                connected.Clear();
                current.GetConnectedNodes(connected);
                for (int i = 0; i < connected.Count; i++)
                {
                    stack.Push(connected[i]);
                }
            }
        }
    }
    protected override void Execute(NativeActivityContext context)
    {
        if (StartNode != null)
        {
            if (TD.FlowchartStartIsEnabled())
            {
                TD.FlowchartStart(DisplayName);
            }
            StartNode.TryExecute(context, null, null);
        }
        else
        {
            if (TD.FlowchartEmptyIsEnabled())
            {
                TD.FlowchartEmpty(DisplayName);
            }
        }
    }
}
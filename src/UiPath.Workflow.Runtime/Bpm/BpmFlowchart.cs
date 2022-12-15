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
    private readonly Collection<BpmNode> _reachableNodes;
    private CompletionCallback _onStepCompleted;
    private CompletionCallback<bool> _onDecisionCompleted;
    private readonly Variable<int> _currentNode;

    public BpmFlowchart()
    {
        _currentNode = new Variable<int>();
        _reachableNodes = new Collection<BpmNode>();
    }

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
    public Collection<BpmNode> Nodes
    {
        get
        {
            _nodes ??= new ValidatingCollection<BpmNode>
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
            return _nodes;
        }
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetVariablesCollection(Variables);
        metadata.AddImplementationVariable(_currentNode);

        GatherReachableNodes(metadata);
        if (ValidateUnconnectedNodes && (_reachableNodes.Count < Nodes.Count))
        {
            metadata.AddValidationError(SR.FlowchartContainsUnconnectedNodes(DisplayName));
        }
        HashSet<Activity> uniqueChildren = new();
        IEnumerable<BpmNode> childrenNodes = ValidateUnconnectedNodes ? Nodes.Distinct() : _reachableNodes;
        foreach (BpmNode node in childrenNodes)
        {
            if (ValidateUnconnectedNodes)
            {
                node.OnOpen(this, metadata);
            }
            node.GetChildActivities(uniqueChildren);
        }

        List<Activity> children = new(uniqueChildren.Count);
        foreach (Activity child in uniqueChildren)
        {
            children.Add(child);
        }

        metadata.SetChildrenCollection(new Collection<Activity>(children));
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
            BpmNode current = stack.Pop();

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
            ExecuteNodeChain(context, StartNode, null);
        }
        else
        {
            if (TD.FlowchartEmptyIsEnabled())
            {
                TD.FlowchartEmpty(DisplayName);
            }
        }
    }

    private void ExecuteNodeChain(NativeActivityContext context, BpmNode node, ActivityInstance completedInstance)
    {
        if (node == null)
        {
            if (context.IsCancellationRequested)
            {
                Fx.Assert(completedInstance != null, "cannot request cancel if we never scheduled any children");
                // we are done but the last child didn't complete successfully
                if (completedInstance.State != ActivityInstanceState.Closed)
                {
                    context.MarkCanceled();
                }
            }

            return;
        }

        if (context.IsCancellationRequested)
        {
            // we're not done and cancel has been requested
            context.MarkCanceled();
            return;
        }


        Fx.Assert(node != null, "caller should validate");
        BpmNode current = node;
        do
        {
            if (ExecuteSingleNode(context, current, out BpmNode next))
            {
                current = next;
            }
            else
            {
                _currentNode.Set(context, current.Index);
                current = null;
            }
        }
        while (current != null);
    }

    private bool ExecuteSingleNode(NativeActivityContext context, BpmNode node, out BpmNode nextNode)
    {
        Fx.Assert(node != null, "caller should validate");
        if (node is BpmStep step)
        {
            _onStepCompleted ??= new CompletionCallback(OnStepCompleted);
            return step.Execute(context, _onStepCompleted, out nextNode);
        }

        nextNode = null;
        if (node is BpmDecision decision)
        {
            _onDecisionCompleted ??= new CompletionCallback<bool>(OnDecisionCompleted);
            return decision.Execute(context, _onDecisionCompleted);
        }

        IBpmSwitch switchNode = node as IBpmSwitch;
        Fx.Assert(switchNode != null, "unrecognized BpmNode");

        return switchNode.Execute(context, this);
    }

    private BpmNode GetCurrentNode(NativeActivityContext context)
    {
        int index = _currentNode.Get(context);
        BpmNode result = _reachableNodes[index];
        Fx.Assert(result != null, "corrupt internal state");
        return result;
    }

    private void OnStepCompleted(NativeActivityContext context, ActivityInstance completedInstance)
    {
        BpmStep step = GetCurrentNode(context) as BpmStep;
        Fx.Assert(step != null, "corrupt internal state");
        BpmNode next = step.Next;
        ExecuteNodeChain(context, next, completedInstance);
    }

    private void OnDecisionCompleted(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        BpmDecision decision = GetCurrentNode(context) as BpmDecision;
        Fx.Assert(decision != null, "corrupt internal state");
        BpmNode next = result ? decision.True : decision.False;
        ExecuteNodeChain(context, next, completedInstance);
    }

    internal void OnSwitchCompleted<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        IBpmSwitch switchNode = GetCurrentNode(context) as IBpmSwitch;
        Fx.Assert(switchNode != null, "corrupt internal state");
        BpmNode next = switchNode.GetNextNode(result);
        ExecuteNodeChain(context, next, completedInstance);
    }
}

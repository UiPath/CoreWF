using System.Activities.Runtime;
using System.Linq;
namespace System.Activities.Statements;

internal class FlowchartExtension
{
    private readonly Flowchart _flowchart;
    private readonly Dictionary<FlowNode, BranchLinks> _splitBranchesByNode = new();
    private readonly HashSet<FlowNode> _nodes = new();
    private readonly FlowchartState.Of<Dictionary<string, int>> _nodeIndexByActivityId;
    private readonly FlowchartState.Of<Dictionary<int, NodeState>> _nodesStatesByIndex;
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();
    private readonly Dictionary<Type, object> _completionCallbacks = new();
    private CompletionCallback _completionCallback;
    private ActivityInstance _completedInstance;
    private FlowNode _current;

    public NativeActivityContext ActivityContext { get; private set; }

    public FlowchartExtension(Flowchart flowchart)
    {
        _flowchart = flowchart;
        _nodeIndexByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
        _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
    }

    private IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (this._completedInstance is not null || this.ActivityContext is not null)
            throw new InvalidOperationException("Context already set.");
        _completedInstance = completedInstance;
        ActivityContext = context;
        _current = GetCurrentNode();
        return Disposable.Create(() =>
        {
            ActivityContext = null;
            _completedInstance = null;
            _current = null;
        });

        FlowNode GetCurrentNode()
        {
            if (completedInstance is null)
                return _nodes.FirstOrDefault(n => n.Index == 0);
            if (!_nodeIndexByActivityId.GetOrAdd().TryGetValue(completedInstance?.Activity.Id, out var index))
                return null;
            FlowNode result = _nodes.FirstOrDefault(n => n.Index == index);
            Fx.Assert(result != null, "corrupt internal state");
            return result;
        }
    }

    public void EndCacheMetadata(NativeActivityMetadata metadata)
    {
        if (_flowchart.StartNode is not null)
            _nodes.Add(_flowchart.StartNode);
        foreach (var node in _nodes.OfType<FlowNode>())
        {
            node.EndCacheMetadata(metadata);
        }
    }
    public void ScheduleWithCallback(Activity activity)
    {
        _completionCallback ??= new CompletionCallback(_flowchart.OnCompletionCallback);
        ActivityContext.ScheduleActivity(activity, _completionCallback);
    }

    public void ScheduleWithCallback<T>(Activity<T> activity)
    {
        if (!_completionCallbacks.TryGetValue(typeof(T), out var callback))
        {
            _completionCallbacks[typeof(T)] = callback = new CompletionCallback<T>(_flowchart.OnCompletionCallback);
        }
        ActivityContext.ScheduleActivity(activity, callback as CompletionCallback<T>);
    }

    public void OnExecute(NativeActivityContext context)
    {
        using var _ = WithContext(context, null);
        SaveLinks();
        SaveActivityIdToNodeIndex();
        ExecuteNodeChain(_flowchart.StartNode);

        void SaveActivityIdToNodeIndex()
        {
            var nodesByActivityId = _nodeIndexByActivityId.GetOrAdd();
            foreach (var activityWithNode in _nodes)
            {
                SaveNode(activityWithNode);
            }

            void SaveNode(FlowNode node)
            {
                var activityId = node?.ChildActivity?.Id;
                if (activityId != null)
                    nodesByActivityId[activityId] = node.Index;
            }
        }

        void SaveLinks()
        {
            var nodesStates = _nodesStatesByIndex.GetOrAdd();
            foreach (var node in _nodes)
            {
                nodesStates[node.Index] = new NodeState() { ActivityId = node.ChildActivity?.Id };
            }
        }
    }

    public void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
    {
        if (predecessor == null)
            return;
        foreach (var successor in successors.Where(s => s is not null))
        {
            _nodes.Add(predecessor);
            _nodes.Add(successor);
            if (!_predecessors.TryGetValue(successor, out var predecessors))
            {
                _predecessors[successor] = predecessors = new();
            }
            predecessors.Add(predecessor);
            if (!_successors.TryGetValue(predecessor, out var successorsSaved))
            {
                _successors[predecessor] = successorsSaved = new();
            }
            successorsSaved.Add(successor);
            PropagateBranchNode(predecessor, successor);
        }
    }

    public bool IsCancelRequested()
    {
        if (_completedInstance?.IsCancellationRequested == true)
            return true;
        var node = _current;
        if (node == null)
            return false;
        return GetNodeState(node.Index).IsCancelRequested;
    }

    private NodeState GetNodeState(int index) => _nodesStatesByIndex.GetOrAdd()[index];

    public bool Cancel(int branchNodeIndex)
    {
        var nodeState = GetNodeState(branchNodeIndex);
        var childToCancel = ActivityContext.GetChildren().FirstOrDefault(c => c.Activity.Id == nodeState.ActivityId);
        if (nodeState.IsCancelRequested
            && (childToCancel is null || childToCancel.IsCancellationRequested))
            return false;

        nodeState.IsCancelRequested = true;
        if (childToCancel is not null)
            ActivityContext.CancelChild(childToCancel);
        return true;
    }

    public void Install(NativeActivityMetadata metadata)
        => FlowchartState.Install(metadata, _flowchart);

    public List<FlowNode> GetSuccessors(int index)
        => _successors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

    public List<FlowNode> GetPredecessors(int index)
        => _predecessors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

    public List<FlowNode> GetPredecessors(FlowNode node)
    {
        _predecessors.TryGetValue(node, out var result);
        return result?.ToList() ?? new();
    }

    public void SaveBranch(FlowNode node, FlowSplitBranch splitBranch, FlowSplit parentSplit)
    {
        _splitBranchesByNode[node] = new BranchLinks
        {
            Split = parentSplit,
            Branch = splitBranch,
            RuntimeNode = node
        };
    }

    private void PropagateBranchNode(FlowNode predecessor, FlowNode successor)
    {
        if (!_splitBranchesByNode.TryGetValue(predecessor, out var pre))
            return;
        _splitBranchesByNode[successor] = pre;
    }

    public BranchLinks GetBranch(FlowNode predecessorNode)
    {
        return _splitBranchesByNode[predecessorNode];
    }

    public void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        using var _ = WithContext(context, completedInstance);
        var node = _current as FlowNode;
        node.OnCompletionCallback(result);
        OnNodeCompleted();
    }

    private void OnNodeCompleted()
    {
    }

    public void ExecuteNextNode(FlowNode next)
    {
        OnNodeCompleted();
        ExecuteNodeChain(next);
    }

    private void ExecuteNodeChain(FlowNode node)
    {
        if (IsCancelRequested() is true)
            return;


        if (node == null)
        {
            if (ActivityContext.IsCancellationRequested)
            {
                Fx.Assert(_completedInstance != null, "cannot request cancel if we never scheduled any children");
                // we are done but the last child didn't complete successfully
                if (_completedInstance.State != ActivityInstanceState.Closed)
                {
                    ActivityContext.MarkCanceled();
                }
            }

            return;
        }

        if (ActivityContext.IsCancellationRequested)
        {
            // we're not done and cancel has been requested
            ActivityContext.MarkCanceled();
            return;
        }


        Fx.Assert(node != null, "caller should validate");
        var previousNode = _current;
        _current = node;
        ((FlowNode)node).Execute(previousNode);
    }

    private class NodeState
    {
        public bool IsCancelRequested { get; set; }
        public string ActivityId { get; set; }
    }
    private class Disposable : IDisposable
    {
        private Action _onDispose;
        public void Dispose()
        {
            _onDispose();
        }

        public static IDisposable Create(Action onDispose)
            => new Disposable() { _onDispose = onDispose };
    }
    public class BranchLinks
    {
        public FlowSplit Split { get; set; }
        public FlowSplitBranch Branch { get; set; }
        public FlowNode RuntimeNode { get; set; }
    }
}

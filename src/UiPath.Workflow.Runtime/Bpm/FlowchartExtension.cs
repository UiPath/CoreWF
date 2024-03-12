using System.Activities.Runtime;
using System.Linq;
namespace System.Activities.Statements;

internal class FlowchartExtension
{
    private readonly Flowchart _flowchart;
    private readonly Queue<FlowNode> _executionQueue = new();
    private readonly Dictionary<FlowNode, BranchLinks> _splitBranchesByNode = new();
    private readonly HashSet<FlowNode> _nodes = new();
    private readonly FlowchartState.Of<Dictionary<string, int>> _nodeIndexByActivityInstanceId;
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
        _nodeIndexByActivityInstanceId = new("_nodeIndexByActivityId", flowchart, () => new());
        _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
    }

    private IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (_completedInstance is not null || ActivityContext is not null)
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
            if (!_nodeIndexByActivityInstanceId.GetOrAdd().TryGetValue(completedInstance?.Id, out var index))
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
    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        _nodeIndexByActivityInstanceId.GetOrAdd()[activityInstance.Id] = _current.Index;
        var nodeState = GetNodeState(_current.Index);
        nodeState.ActivityInstanceIds.Add(activityInstance.Id);
    }

    public void ScheduleWithCallback(Activity activity)
    {
        _completionCallback ??= new CompletionCallback(_flowchart.OnCompletionCallback);
        var activityInstance = ActivityContext.ScheduleActivity(activity, _completionCallback);
        SaveNodeActivityLink(activityInstance);
    }

    public void ScheduleWithCallback<T>(Activity<T> activity)
    {
        if (!_completionCallbacks.TryGetValue(typeof(T), out var callback))
        {
            _completionCallbacks[typeof(T)] = callback = new CompletionCallback<T>(_flowchart.OnCompletionCallback);
        }
        var activityInstance = ActivityContext.ScheduleActivity(activity, callback as CompletionCallback<T>);
        SaveNodeActivityLink(activityInstance);
    }

    public void OnExecute(NativeActivityContext context)
    {
        using var _ = WithContext(context, null);
        EnqueueNodeExecution(_flowchart.StartNode);
        ExecuteQueue();
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
        var node = _current;
        if (node == null)
            return false;
        return GetNodeState(node.Index).IsCancelRequested;
    }

    private NodeState GetNodeState(int index)
    {
        var nodesStatesByIndex = _nodesStatesByIndex.GetOrAdd();
        if (!nodesStatesByIndex.TryGetValue(index, out var state))
            nodesStatesByIndex[index] = state = new();

        return state;
    }

    public bool Cancel(int branchNodeIndex)
    {
        var nodeState = GetNodeState(branchNodeIndex);
        if (nodeState.IsCancelRequested || nodeState.IsCompleted)
            return false;

        nodeState.IsCancelRequested = true;
        var childrenToCancel = ActivityContext.GetChildren()
            .Where(c => nodeState.ActivityInstanceIds.Contains(c.Id))
            .ToList();
        foreach (var child in childrenToCancel)
            ActivityContext.CancelChild(child);
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
        _current.OnCompletionCallback(result);
        ExecuteQueue();
    }

    public void EnqueueNodeExecution(FlowNode next)
    {
        if (next is null)
            return;
        _executionQueue.Enqueue(next);
    }

    private void ExecuteQueue()
    {
        SetNodeCompleted();

        while (_executionQueue.TryDequeue(out var next))
        {
            var state = GetNodeState(next.Index);
            state.IsRunning = true;
            ExecuteNode(next);
        }
    }
    private void SetNodeCompleted()
    {
        var state = GetNodeState(_current.Index);
        state.ActivityInstanceIds.Remove(_completedInstance?.Id);
        if (!state.ActivityInstanceIds.Any())
        {
            state.IsCompleted = true;
            if (_completedInstance is not null)
            {
                state.IsCancelRequested = _completedInstance.IsCancellationRequested;
            }
        }
        _completedInstance = null;
    }

    private void ExecuteNode(FlowNode node)
    {
        var previousNode = _current;
        _current = node;
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
        if (IsCancelRequested())
        {
            OnCurrentBranchCancelled();
            return;
        }
        node.Execute(previousNode);
    }

    private void OnCurrentBranchCancelled()
    {
        var merge = GetBranch(_current)?.Split.MergeNode;
        if (merge is null)
            return;
        EnqueueNodeExecution(merge);
    }

    private class NodeState
    {
        public bool IsCancelRequested { get; set; }
        public bool IsRunning { get; set; }
        public bool IsCompleted { get; set; }
        public HashSet<string> ActivityInstanceIds { get; set; } = new();
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

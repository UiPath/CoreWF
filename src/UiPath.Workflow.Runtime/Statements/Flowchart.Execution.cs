// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Linq;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Statements;

partial class Flowchart
{
    private const string FlowChartStateVariableName = "flowchartState";
    private readonly Variable<Dictionary<string, object>> _flowchartState = new (FlowChartStateVariableName, c => new ());

    protected override void Execute(NativeActivityContext context)
    {
        if (StartNode != null)
        {
            if (TD.FlowchartStartIsEnabled())
            {
                TD.FlowchartStart(DisplayName);
            }
            OnExecute(context);
        }
        else
        {
            if (TD.FlowchartEmptyIsEnabled())
            {
                TD.FlowchartEmpty(DisplayName);
            }
        }
    }

    private readonly Dictionary<FlowNode, BranchLinks> _splitBranchesByNode = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();
    private CompletionCallback _completionCallback;
    private readonly Dictionary<Type, Delegate> _completionCallbacks = new();
    private ActivityInstance _completedInstance;
    private FlowNode _current;

    private Queue<FlowNode> _executionQueue = new();
    private Dictionary<string, int> NodeIndexByActivityInstanceId 
        => GetPersistableState<Dictionary<string, int>>("_nodeIndexByActivityId");
    private Dictionary<int, NodeState> NodesStatesByIndex 
        => GetPersistableState<Dictionary<int, NodeState>>("_nodesStatesByIndex");
    private NativeActivityContext _activeContext;
    private bool _doNotCompleteNode;

    private IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (_completedInstance is not null || _activeContext is not null)
            throw new InvalidOperationException("Context already set.");
        _completedInstance = completedInstance;
        _activeContext = context;
        _current = GetCurrentNode();
        return Disposable.Create(() =>
        {
            _activeContext = null;
            _completedInstance = null;
            _current = null;
        });

        FlowNode GetCurrentNode()
        {
            if (completedInstance is null)
                return _reachableNodes[0];
            if (!NodeIndexByActivityInstanceId.TryGetValue(completedInstance?.Id, out var index))
                return null;
            FlowNode result = _reachableNodes[index];
            Fx.Assert(result != null, "corrupt internal state");
            return result;
        }
    }

    private void EndCacheMetadata(NativeActivityMetadata metadata)
    {
        foreach (var node in _reachableNodes.OfType<FlowNode>())
        {
            foreach (var successor in GetSuccessors(node.Index))
            {
                PropagateBranch(node, successor);
            }
        }
        foreach (var node in _reachableNodes.OfType<FlowNode>())
        {
            node.EndCacheMetadata(metadata);
        }
        void PropagateBranch(FlowNode predecessor, FlowNode successor)
        {
            if (!_splitBranchesByNode.TryGetValue(predecessor, out var pre)
                || _splitBranchesByNode.ContainsKey(successor))
                return;

            _splitBranchesByNode[successor] = pre;
        }
    }
    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        NodeIndexByActivityInstanceId[activityInstance.Id] = _current.Index;
        var nodeState = GetNodeState(_current.Index);
        nodeState.ActivityInstanceIds.Add(activityInstance.Id);
    }

    internal void ScheduleWithCallback(Activity activity)
    {
        _completionCallback ??= new(OnCompletionCallback);
        var activityInstance = _activeContext.ScheduleActivity(activity, _completionCallback);
        SaveNodeActivityLink(activityInstance);
    }

    internal void ScheduleWithCallback<T>(Activity<T> activity)
    {
        if (!_completionCallbacks.TryGetValue(typeof(T), out var callback))
        {
            _completionCallbacks[typeof(T)] = callback = new CompletionCallback<T>(OnCompletionCallback);
        }
        var activityInstance = _activeContext.ScheduleActivity(activity, (CompletionCallback<T>)callback);
        SaveNodeActivityLink(activityInstance);
    }

    private void OnExecute(NativeActivityContext context)
    {
        using var _ = WithContext(context, null);
        EnqueueNodeExecution(StartNode);
        ExecuteQueue();
    }

    private void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
    {
        if (predecessor == null)
            return;
        foreach (var successor in successors.Where(s => s is not null))
        {
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
        }
    }

    private bool IsCancelRequested()
    {
        if (_current == null)
            return false;
        return GetNodeState(_current.Index).IsCancelRequested;
    }

    private NodeState GetNodeState(int index)
    {
        var nodesStatesByIndex = NodesStatesByIndex;
        if (!nodesStatesByIndex.TryGetValue(index, out var state))
            nodesStatesByIndex[index] = state = new();

        return state;
    }

    internal bool Cancel(int branchNodeIndex)
    {
        var nodeState = GetNodeState(branchNodeIndex);
        if (nodeState.IsCancelRequested || nodeState.IsCompleted)
            return false;

        nodeState.IsCancelRequested = true;
        var childrenToCancel = _activeContext.GetChildren()
            .Where(c => nodeState.ActivityInstanceIds.Contains(c.Id))
            .ToList();
        foreach (var child in childrenToCancel)
            _activeContext.CancelChild(child);
        return true;
    }

    internal List<FlowNode> GetSuccessors(int index)
        => _successors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

    internal List<FlowNode> GetPredecessors(FlowNode node)
    {
        _predecessors.TryGetValue(node, out var result);
        return result?.ToList() ?? new();
    }

    internal void AddBranch(FlowNode node, FlowSplitBranch splitBranch, FlowSplit parentSplit)
    {
        _splitBranchesByNode[node] = new BranchLinks
        {
            Split = parentSplit,
            Branch = splitBranch,
            RuntimeNode = node
        };
    }

    internal BranchLinks GetBranch(FlowNode predecessorNode)
    {
        return _splitBranchesByNode[predecessorNode];
    }
    private void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance)
    {
        OnCompletionCallback<object>(context, completedInstance, null);
    }

    private void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        using var _ = WithContext(context, completedInstance);
        _current.OnCompletionCallback(result);
        ExecuteQueue();
    }

    internal void EnqueueNodeExecution(FlowNode node, bool isNewBranch = false)
    {
        if (node is null)
            return;
        _executionQueue.Enqueue(node);
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
    internal void MarkDoNotCompleteNode()
    {
        _doNotCompleteNode = true;
    }
    private void SetNodeCompleted()
    {
        var state = GetNodeState(_current.Index);
        state.ActivityInstanceIds.Remove(_completedInstance?.Id);
        if (!state.ActivityInstanceIds.Any())
        {
            state.IsCompleted = !_doNotCompleteNode;
            if (_completedInstance is not null)
            {
                state.IsCancelRequested = _completedInstance.IsCancellationRequested;
            }
        }
        _completedInstance = null;
        _doNotCompleteNode = false;
    }
    private void ExecuteNode(FlowNode node)
    {
        var previousNode = _current;
        _current = node;
        if (node == null)
        {
            if (_activeContext.IsCancellationRequested)
            {
                Fx.Assert(_completedInstance != null, "cannot request cancel if we never scheduled any children");
                // we are done but the last child didn't complete successfully
                if (_completedInstance.State != ActivityInstanceState.Closed)
                {
                    _activeContext.MarkCanceled();
                }
            }

            return;
        }

        if (_activeContext.IsCancellationRequested)
        {
            // we're not done and cancel has been requested
            _activeContext.MarkCanceled();
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

    private List<FlowMerge> GetRunningMerges()
    {
        var runningNodes = _reachableNodes
            .OfType<FlowMerge>()
            .Where(n =>
        {
            var state = GetNodeState(n.Index);
            return state != null
            && state.IsRunning
            && !state.IsCompleted
            ;
        }).ToList();
        return runningNodes;
    }

    private void OnCurrentBranchCancelled()
    {
        var runningMerges = GetRunningMerges()
            .OfType<FlowMerge>();
        foreach (var runningMerge in runningMerges)
        {
            runningMerge.OnBranchEnded(_current);
        }
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
    internal class BranchLinks
    {
        public FlowSplit Split { get; set; }
        public FlowSplitBranch Branch { get; set; }
        public FlowNode RuntimeNode { private get; set; }
        public int NodeIndex => RuntimeNode.Index;
    }
    internal T GetPersistableState<T>(string key) where T: new()
    {
        var flowChartState = _flowchartState.Get(_activeContext);
        if (!flowChartState.TryGetValue(key, out var value))
        {
            value = new T();
            flowChartState[key] = value;
        }
        return (T)value;
    }
}

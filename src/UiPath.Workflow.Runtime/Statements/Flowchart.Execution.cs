// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Validation;
using System.Globalization;
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

    private readonly Dictionary<FlowNode, StaticBranchInfo> _staticBranchesByNode = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();
    private CompletionCallback _completionCallback;
    private readonly Dictionary<Type, Delegate> _completionCallbacks = new();
    private readonly Queue<NodeInstance> _executionQueue = new();

    private ActivityInstance _completedInstance;
    internal NodeInstance Current { get; private set; }
    internal NodeInstance Previous => NodesStatesByNodeInstanceId[Current.NodeInstanceId].NodeInstance;

    private Dictionary<string, NodeInstance> NodeInstanceByActivityInstanceId 
        => GetPersistableState<Dictionary<string, NodeInstance>>("_nodeIndexByActivityId");
    private Dictionary<string, NodeState> NodesStatesByNodeInstanceId 
        => GetPersistableState<Dictionary<string, NodeState>>("_nodesStatesByNodeInstanceId");
    private Dictionary<int, NodeState> RuntimeBranchesState 
        => GetPersistableState<Dictionary<int, NodeState>>("_runtimeBranchesState");
    private NativeActivityContext _activeContext;
    private bool _doNotCompleteNode;

    private IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (_completedInstance is not null || _activeContext is not null)
            throw new InvalidOperationException("Context already set.");
        _completedInstance = completedInstance;
        _activeContext = context;
        Current = GetCurrentNode();
        return Disposable.Create(() =>
        {
            _activeContext = null;
            _completedInstance = null;
            Current = default;
        });

        NodeInstance GetCurrentNode()
        {
            if (completedInstance is null)
                return default;
            if (!NodeInstanceByActivityInstanceId.TryGetValue(completedInstance?.Id, out var nodeInstance))
                return default;
            FlowNode result = _reachableNodes[nodeInstance.NodeIndex];
            Fx.Assert(result != null, "corrupt internal state");
            return nodeInstance;
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
            var predecessorBranches = GetStaticBranches(predecessor);
            var successorBranches = GetStaticBranches(successor);
            successorBranches.AddStack(predecessorBranches);
        }
    }
    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        NodeInstanceByActivityInstanceId[activityInstance.Id] = Current;
        var nodeState = GetNodeState(Current);
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
        EnqueueNodeExecution(StartNode, new BranchInstance(RuntimeNodeIndex: StartNode.Index.ToString(CultureInfo.InvariantCulture), SplitInstanceId: ""));
        ExecuteQueue();
    }

    private void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
    {
        if (predecessor == null)
            return;

        var preStacks = GetStaticBranches(predecessor);

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

            var successorStacks = GetStaticBranches(successor);
            successorStacks.AddStack(preStacks);
        }
    }

    private bool IsCancelRequested()
    {
        if (Current == default)
            return false;
        return GetNodeState(Current).IsCancelRequested;
    }

    private NodeState GetNodeState(NodeInstance nodeInstance)
    {
        if (!NodesStatesByNodeInstanceId.TryGetValue(nodeInstance.NodeInstanceId, out var state))
            NodesStatesByNodeInstanceId[nodeInstance.NodeInstanceId] = state = new() { NodeInstance = nodeInstance };
        return state;
    }
    private NodeState GetBranchState(int index)
    {
        var nodesStatesByIndex = RuntimeBranchesState;
        if (!nodesStatesByIndex.TryGetValue(index, out var state))
            nodesStatesByIndex[index] = state = new();

        return state;
    }

    internal bool CancelOtherBranches(IEnumerable<BranchInstance> branchInstances)
    {
        var splitNodesStates = GetOtherBranchesNodes(branchInstances);

        foreach (var nodeState in splitNodesStates)
        {
            var childrenToCancel = _activeContext.GetChildren()
                .Where(c => nodeState.ActivityInstanceIds.Contains(c.Id))
                .ToList();
            foreach (var child in childrenToCancel)
                _activeContext.CancelChild(child);
            nodeState.IsCancelRequested = true;
        }
        return splitNodesStates.Any();
    }
    internal HashSet<BranchInstance> GetOtherRunningBranches(IEnumerable<BranchInstance> completedBranches)
    {
        var runningNodes = GetOtherBranchesNodes(completedBranches);
        return new (runningNodes.Select(n => n.NodeInstance.BranchInstance));
    }
    private List<NodeState> GetOtherBranchesNodes(IEnumerable<BranchInstance> completedBranches)
    {
        return (
                from state in NodesStatesByNodeInstanceId.Values
                where state.NodeInstance.BranchInstance.SplitInstanceId.StartsWith(completedBranches.First().SplitInstanceId)
                where !completedBranches.Contains(state.NodeInstance.BranchInstance)
                where !(state.IsCancelRequested || state.IsCompleted)
                select state
            ).ToList();
    }

    internal List<FlowNode> GetSuccessors(int index)
        => _successors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

    internal List<FlowNode> GetPredecessors(FlowNode node)
    {
        _predecessors.TryGetValue(node, out var result);
        return result?.ToList() ?? new();
    }

    internal StaticBranchInfo GetStaticBranches(FlowNode node)
    {
        if (_staticBranchesByNode.ContainsKey(node))
            return _staticBranchesByNode[node];
        else
            return _staticBranchesByNode[node] = new();
    }

    private void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance)
    {
        OnCompletionCallback<object>(context, completedInstance, null);
    }

    private void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        using var _ = WithContext(context, completedInstance);
        var currentNode = _reachableNodes[Current.NodeIndex];
        currentNode.OnCompletionCallback(result);
        ExecuteQueue();
    }

    internal void EnqueueNodeExecution(FlowNode node, BranchInstance branchInstance = null)
    {
        if (node is null)
            return;
        NodeInstance nodeInstance = new(NodeIndex: node.Index,
                                        BranchInstance: branchInstance ?? Current.BranchInstance,
                                        PreviousNodeInstanceId: Current?.NodeInstanceId)
        {
            NodeName = $"{node.Index}.{node.GetType().Name}"
        };
        var nodeState = GetNodeState(nodeInstance);
        nodeState.IsQueued = true;
        _executionQueue.Enqueue(nodeInstance);
        if (Current == default)
        {
            Current = nodeInstance;
        }
    }

    private void ExecuteQueue()
    {
        while (_executionQueue.TryDequeue(out var nextInstance))
        {
            SetNodeCompleted();
            var state = GetNodeState(nextInstance);
            state.IsQueued = false;
            state.StartedRunning = true;
            ExecuteNode(nextInstance);
        }
    }
    internal void MarkDoNotCompleteNode()
    {
        GetNodeState(Current).DoNotCompleteOnce = true;
    }
    private void SetNodeCompleted()
    {
        if (Current == default)
            return;
        var state = GetNodeState(Current);
        state.ActivityInstanceIds.Remove(_completedInstance?.Id);
        if (!state.ActivityInstanceIds.Any())
        {
            state.IsCompleted = !state.DoNotCompleteOnce;
            if (_completedInstance is not null)
            {
                state.IsCancelRequested = _completedInstance.IsCancellationRequested;
            }
        }
        _completedInstance = null;
        state.DoNotCompleteOnce = false;
    }
    private void ExecuteNode(NodeInstance nodeInstance)
    {
        Current = nodeInstance;
        if (nodeInstance == default)
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

        Fx.Assert(nodeInstance != default, "caller should validate");
        if (IsCancelRequested())
        {
            OnCurrentBranchCancelled();
            return;
        }
        var node = _reachableNodes[nodeInstance.NodeIndex];
        node.Execute();
    }

    private void OnCurrentBranchEnded()
    {
        var runningMerges = from state in NodesStatesByNodeInstanceId.Values
                           let merge = _reachableNodes[state.NodeInstance.NodeIndex] as FlowMerge
                           where merge is not null
                           where state.StartedRunning && !state.IsCompleted
                           select new { state, merge };

        var result = runningMerges.ToList();
        foreach (var runningMerge in result)
        {
            var mergeInstance = runningMerge.state.NodeInstance;
            if (Current.BranchInstance.SplitInstanceId.Equals(mergeInstance.BranchInstance.SplitInstanceId))
                EnqueueNodeExecution(runningMerge.merge, mergeInstance.BranchInstance);
        }
    }

    private void OnCurrentBranchCancelled()
        => OnCurrentBranchEnded();
    

    public class StaticBranchInfo
    {
        private List<List<FlowSplitBranch>> _branches = new ();

        public FlowSplitBranch GetTop()
        {
            return _branches.FirstOrDefault()?.LastOrDefault();            
        }

        public void Push(FlowSplitBranch newBranch, StaticBranchInfo splitBranchInfo)
        {
            AddStack(splitBranchInfo._branches
                    .Select(b => Enumerable.Concat(b, new[] { newBranch }))
                    .Concat(new[] { new[] { newBranch } }));
        }

        public void AddStack(StaticBranchInfo preStacks)
        {
            AddStack(preStacks._branches);
        }

        private void AddStack(IEnumerable<IEnumerable<FlowSplitBranch>> stack)
        {
            if (_branches.Count == 0)
            {
                _branches = stack.Select(b => b.ToList()).ToList();
                return;
            }
            foreach (var pre in stack)
            {
                if (_branches.Any(b => pre.All(p => b.Contains(p))))
                    continue;
                _branches.Add(pre.ToList());
            }
        }

        public void AddPopFrom(StaticBranchInfo nextStacks)
        {
            var pops = nextStacks._branches.Select(b => Enumerable.Reverse(b).Skip(1).Reverse().ToList()).ToList();
            AddStack(pops);
        }

        public StaticBranchInfo() { }
    }

    internal record NodeInstance(int NodeIndex, BranchInstance BranchInstance, string PreviousNodeInstanceId)
    {
        public string NodeInstanceId { get; } = Guid.NewGuid().ToString();
        public string NodeName { get; internal set; }

        public override string ToString()
        {
            return NodeName + "/" + base.ToString();
        }
    }

    internal record BranchInstance(string RuntimeNodeIndex, string SplitInstanceId) 
    {
        public BranchInstance Push(string runtimeNodeIndex, string splitInstanceId)
        {
            return new
            (
                RuntimeNodeIndex: RuntimeNodeIndex + "." + runtimeNodeIndex,
                SplitInstanceId: SplitInstanceId + "." + splitInstanceId
            );
        }
        public BranchInstance Pop()
        {
            return new
            (
                RuntimeNodeIndex: Pop(RuntimeNodeIndex),
                SplitInstanceId: Pop(SplitInstanceId)
            );
        }

        private static string Pop(string stack)
        {
            var lastIndex = stack.LastIndexOf('.');
            return stack.Substring(0, lastIndex);
        }
    }


    private class NodeState
    {
        public bool DoNotCompleteOnce { get; internal set; }
        public bool IsCancelRequested { get; set; }
        public bool StartedRunning { get; set; }
        public bool IsCompleted { get; set; }
        public HashSet<string> ActivityInstanceIds { get; set; } = new();
        public NodeInstance NodeInstance { get; set; }
        public bool IsQueued { get; set; }
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

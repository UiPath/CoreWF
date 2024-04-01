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

    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
    private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();
    private CompletionCallback _completionCallback;
    private readonly Dictionary<Type, Delegate> _completionCallbacks = new();
    private readonly Queue<NodeState> _executionQueue = new();

    private ActivityInstance _completedInstance;
    private NodeState CurrentNodeState { get; set; }
    internal NodeInstance Current => CurrentNodeState.NodeInstance;

    private Dictionary<string, NodeState> NodesStatesByNodeInstanceId 
        => GetPersistableState<Dictionary<string, NodeState>>("_nodesStatesByNodeInstanceId");

    private NativeActivityContext _activeContext;

    private IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (_completedInstance is not null || _activeContext is not null)
            throw new InvalidOperationException("Context already set.");
        _completedInstance = completedInstance;
        _activeContext = context;
        SetCurrentNode();
        return Disposable.Create(() =>
        {
            _activeContext = null;
            _completedInstance = null;
            CurrentNodeState = null;
        });

        void SetCurrentNode()
        {
            CurrentNodeState = null;
            if (completedInstance is null)
                return;
            CurrentNodeState = NodesStatesByNodeInstanceId.Values
                .Where(n => n.ActivityInstanceIds.Contains(completedInstance?.Id))
                .FirstOrDefault();
        }
    }

    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        CurrentNodeState.ActivityInstanceIds.Add(activityInstance.Id);
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

    protected override void Execute(NativeActivityContext context)
    {
        if (StartNode != null)
        {
            if (TD.FlowchartStartIsEnabled())
            {
                TD.FlowchartStart(DisplayName);
            }
            using var _ = WithContext(context, null);
            EnqueueNodeExecution(StartNode, new BranchInstance(BranchesStack: $"__0", SplitsStack: "_"));
            ExecuteQueue();
        }
        else
        {
            if (TD.FlowchartEmptyIsEnabled())
            {
                TD.FlowchartEmpty(DisplayName);
            }
        }
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
                where state.NodeInstance.BranchInstance.SplitsStack.StartsWith(completedBranches.First().SplitsStack)
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

    internal List<FlowMerge> GetMerges(FlowNode flowNode)
    {
        var staticBranches = GetStaticBranches(flowNode);

        var merges = (
        from nodeInfo in _staticBranchesByNode
        where nodeInfo.Key is FlowMerge
        where nodeInfo.Value.IsOn(staticBranches)
        select nodeInfo.Key as FlowMerge
        ).Take(1).ToList();

        return merges;
    }


    private void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance)
    {
        OnCompletionCallback<object>(context, completedInstance, null);
    }

    private void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        using var _ = WithContext(context, completedInstance);
        var currentNode = _reachableNodes[CurrentNodeState.NodeInstance.NodeIndex];
        currentNode.OnCompletionCallback(result);
        CurrentNodeState.ActivityInstanceIds.Remove(completedInstance.Id);
        SetNodeCompleted();
        ExecuteQueue();
    }
    private void SetNodeCompleted()
    {
        if (!CurrentNodeState.ActivityInstanceIds.Any())
        {
            CurrentNodeState.IsCompleted = !CurrentNodeState.DoNotCompleteOnce;
            CurrentNodeState.DoNotCompleteOnce = false;
            if (CurrentNodeState.IsCompleted)
                NodesStatesByNodeInstanceId.Remove(CurrentNodeState.NodeInstance.NodeInstanceId);
        }
    }

    internal void EnqueueNodeExecution(FlowNode node, BranchInstance branchInstance = null)
    {
        if (node is null)
            return;

        if (CurrentNodeState?.IsCancelRequested is true)
            return;

        NodeInstance nodeInstance = new(NodeIndex: node.Index,
                                        NodeInstanceId: $"{node.Index}.{node.GetType().Name}.{node.ChildActivity?.DisplayName}.{Guid.NewGuid()}",
                                        BranchInstance: branchInstance ?? CurrentNodeState.NodeInstance.BranchInstance);

        var nodeState = NodesStatesByNodeInstanceId[nodeInstance.NodeInstanceId] = new() 
        {
            NodeInstance = nodeInstance,
            IsQueued = true
        };

        _executionQueue.Enqueue(nodeState);
    }

    internal void MarkDoNotCompleteNode()
    {
        CurrentNodeState.DoNotCompleteOnce = true;
    }
    private void ExecuteQueue()
    {
        while (_executionQueue.TryDequeue(out var nextNode))
        {
            ExecuteNode(nextNode);
        }
        
        if (NodesStatesByNodeInstanceId.Values.All(ns => ns.IsCompleted))
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
        }

        void ExecuteNode(NodeState nextNode)
        {
            CurrentNodeState = nextNode;
            nextNode.IsQueued = false;
            nextNode.StartedRunning = true;

            if (_activeContext.IsCancellationRequested)
            {
                // we're not done and cancel has been requested
                _activeContext.MarkCanceled();
                _executionQueue.Clear();
                return;
            }

            if (nextNode.IsCancelRequested)
            {
                nextNode.IsCompleted = true;
                CheckBranchEnded();
            }
            else
            {
                var node = _reachableNodes[nextNode.NodeInstance.NodeIndex];
                node.Execute();
                SetNodeCompleted();
                CheckBranchEnded();
            }

            void CheckBranchEnded()
            {
                var sameBranchRunningNodes = NodesStatesByNodeInstanceId.Values
                    .Where(ns => ns.NodeInstance.BranchInstance.BranchesStack.StartsWith(nextNode.NodeInstance.BranchInstance.BranchesStack))
                    .Where(ns => !ns.IsCompleted)
                    .ToList();

                if (!sameBranchRunningNodes.Any())
                {
                    OnCurrentBranchEnded();
                }
            }

            void OnCurrentBranchEnded()
            {
                var runningMerges = (
                    from state in NodesStatesByNodeInstanceId.Values
                    where _reachableNodes[state.NodeInstance.NodeIndex] is FlowMerge
                    where state.StartedRunning && !state.IsCompleted
                    where nextNode.NodeInstance.BranchInstance.SplitsStack == state.NodeInstance.BranchInstance.SplitsStack
                    select state
                    ).ToList();

                foreach (var merge in runningMerges)
                {
                    _executionQueue.Enqueue(merge);
                }
            }
        }
    }

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

        internal bool IsOn(StaticBranchInfo staticBranches)
        {
            ///todo
            return true;
        }

        public StaticBranchInfo() { }
    }

    internal record NodeInstance(int NodeIndex, string NodeInstanceId, BranchInstance BranchInstance)
    {
        public override string ToString()
        {
            return $"{NodeInstanceId}/Branch={BranchInstance}";
        }
    }

    internal record BranchInstance(string BranchesStack, string SplitsStack) 
    {
        private const char StackDelimiter = ':';
        public BranchInstance Push(string branchId, string splitInstanceId)
        {
            return new
            (
                BranchesStack: BranchesStack + StackDelimiter + branchId,
                SplitsStack: SplitsStack + StackDelimiter + splitInstanceId
            );
        }
        public BranchInstance Pop()
        {
            return new
            (
                BranchesStack: Pop(BranchesStack),
                SplitsStack: Pop(SplitsStack)
            );
        }

        private static string Pop(string stack)
        {
            var lastIndex = stack.LastIndexOf(StackDelimiter);
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

        public override string ToString()
        {
            return NodeInstance.ToString();
        }
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

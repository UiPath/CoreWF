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

    internal ExecutionBranchId CurrentBranch => CurrentNodeState.ExecutionBranch;
    internal string CurrentNodeId => CurrentNodeState.ExecutionNodeId;

    private Dictionary<string, NodeState> NodesStates 
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
            CurrentNodeState = NodesStates.Values
                .Where(n => n.ActivityInstanceIds.Contains(completedInstance?.Id))
                .FirstOrDefault();
        }
    }

    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        CurrentNodeState.ActivityInstanceIds.Add(activityInstance.Id);
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
            EnqueueNodeExecution(StartNode, new ExecutionBranchId(BranchesStack: $"__0", SplitsStack: "_"));
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

    internal bool CancelOtherBranches(IEnumerable<ExecutionBranchId> branchInstances)
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
    internal HashSet<ExecutionBranchId> GetOtherRunningBranches(IEnumerable<ExecutionBranchId> completedBranches)
    {
        var runningNodes = GetOtherBranchesNodes(completedBranches);
        return new (runningNodes.Select(n => n.ExecutionBranch));
    }
    private List<NodeState> GetOtherBranchesNodes(IEnumerable<ExecutionBranchId> completedBranches)
    {
        return (
                from state in NodesStates.Values
                where state.ExecutionBranch.SplitsStack.StartsWith(completedBranches.First().SplitsStack)
                where !completedBranches.Contains(state.ExecutionBranch)
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
        where nodeInfo.Value.IsOnBranch(staticBranches)
        select nodeInfo
        ).ToList();

        return merges.Select(ni => ni.Key as FlowMerge).ToList() ;
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

    private void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance)
    {
        OnCompletionCallback<object>(context, completedInstance, null);
    }

    private void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        using var _ = WithContext(context, completedInstance);
        var currentNode = _reachableNodes[CurrentNodeState.StaticNodeIndex];
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
                NodesStates.Remove(CurrentNodeState.ExecutionNodeId);
        }
    }

    internal void EnqueueNodeExecution(FlowNode node, ExecutionBranchId branchInstance = null)
    {
        if (node is null)
            return;

        if (CurrentNodeState?.IsCancelRequested is true)
            return;

        var executionNodeId = $"{node.Index}"; //+ Guid.NewGuid().ToString();

        if (!NodesStates.TryGetValue(executionNodeId, out var nodeState))
        {
            nodeState = NodesStates[executionNodeId] = new()
            {
                ExecutionBranch = branchInstance ?? CurrentNodeState.ExecutionBranch,
                ExecutionNodeId = executionNodeId,
                StaticNodeIndex = node.Index,
            };
        }

        nodeState.IsQueued = true;
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
        
        if (NodesStates.Values.All(ns => ns.IsCompleted))
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
                var node = _reachableNodes[nextNode.StaticNodeIndex];
                node.Execute();
                SetNodeCompleted();
                CheckBranchEnded();
            }

            void CheckBranchEnded()
            {
                var sameBranchRunningNodes = NodesStates.Values
                    .Where(ns => ns.ExecutionBranch.BranchesStack.StartsWith(nextNode.ExecutionBranch.BranchesStack))
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
                    from state in NodesStates.Values
                    where _reachableNodes[state.StaticNodeIndex] is FlowMerge
                    where state.StartedRunning && !state.IsCompleted
                    where nextNode.ExecutionBranch.SplitsStack == state.ExecutionBranch.SplitsStack
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
        List<List<FlowSplitBranch>> _branches;
        private List<List<FlowSplitBranch>> Branches
        {
            get
            {
                if (_branches != null)
                    return _branches;
                _branches = new List<List<FlowSplitBranch>>();
                foreach(var inheritedStack in _inheritedStacks) 
                {
                    AddStack(inheritedStack.Branches);
                }
                foreach(var push in _pushes)
                {
                    var result = push.Item1.Branches.Select(
                                stack => stack.Concat(new[] { push.Item2 }).ToList()
                            ).ToList();
                    if (result.Count == 0)
                        result.Add(new() { push.Item2 });
                }
                if (_branches.Count == 0)
                    _branches.Add(new List<FlowSplitBranch>());
                return _branches;
            }
        }
        private readonly HashSet<(StaticBranchInfo, FlowSplitBranch)> _pushes = new();
        private readonly HashSet<StaticBranchInfo> _inheritedStacks = new();
        private bool _hasPop = false;

        public HashSet<FlowSplitBranch> GetTop()
        {
            return new (Branches.Select(b => b.LastOrDefault()).Where(b => b!= null));
        }

        public void Push(FlowSplitBranch newBranch, StaticBranchInfo splitBranchInfo)
        {
            _pushes.Add(new(splitBranchInfo, newBranch));
        }

        public void PropagateStack(StaticBranchInfo preStacks)
        {
            _inheritedStacks.Add(preStacks);
        }

        private void AddStack(List<List<FlowSplitBranch>> stack)
        {
            foreach (var pre in stack)
            {
                AddStack(pre);
            }

            void AddStack(IEnumerable<FlowSplitBranch> stack)
            {
                var pre = _hasPop 
                        ? stack.Reverse().Skip(1).Reverse().ToList()
                        : stack.ToList();
                if (HasBranch(pre))
                    return;

                _branches.Add(pre);
            }
        }

        public void AddPop()
        {
            _hasPop = true;
        }

        private bool HasBranch(List<FlowSplitBranch> branch)
            => Branches.Any(b => branch.Count == b.Count && branch.All(p => b.Contains(p)));

        internal bool IsOnBranch(StaticBranchInfo toConfirm)
        {
            foreach (var branchToConfirm in toConfirm.Branches)
            {
                if (HasBranch(branchToConfirm))
                    return true;
            }
            return false;
        }

        public StaticBranchInfo() { }
    }

    internal record ExecutionBranchId
    {
        private const char StackDelimiter = ':';
        public string BranchesStack { get; private init; }
        public string SplitsStack { get; private init; }

        public ExecutionBranchId(string BranchesStack, string SplitsStack)
        {
            this.BranchesStack = BranchesStack;
            this.SplitsStack = SplitsStack;
        }
        public ExecutionBranchId Push(string branchId, string splitId)
        {
            return this with
            {
                BranchesStack = $"{BranchesStack}{StackDelimiter}{splitId}_{branchId}",
                SplitsStack = $"{SplitsStack}{StackDelimiter}{splitId}"
            };
        }
        public ExecutionBranchId Pop()
        {
            return this with
            {
                BranchesStack = Pop(BranchesStack),
                SplitsStack = Pop(SplitsStack)
            };
        }
        private static string Pop(string stack)
        {
            var lastIndex = stack.LastIndexOf(StackDelimiter);
            return stack.Substring(0, lastIndex);
        }
    }


    private class NodeState
    {
        public ExecutionBranchId ExecutionBranch { get; init; }
        public int StaticNodeIndex { get; init; }
        public string ExecutionNodeId { get; init; }
        public bool DoNotCompleteOnce { get; internal set; }
        public bool IsCancelRequested { get; set; }
        public bool StartedRunning { get; set; }
        public bool IsCompleted { get; set; }
        public HashSet<string> ActivityInstanceIds { get; set; } = new();

        public bool IsQueued { get; set; }

        public override string ToString()
        {
            return $"{ExecutionNodeId}/{ExecutionBranch}";
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

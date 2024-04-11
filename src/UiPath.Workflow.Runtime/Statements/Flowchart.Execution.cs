// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Diagnostics;
using System.Linq;

namespace System.Activities.Statements;

partial class Flowchart
{
    private const string FlowChartStateVariableName = "flowchartState";
    private readonly Variable<State> _flowchartState = new(FlowChartStateVariableName, c => new()
    {
        Version = FileVersionInfo.GetVersionInfo(typeof(Flowchart).Assembly.Location).ProductVersion ?? "Empty"
    });

    private CompletionCallback _completionCallback;
    private readonly Dictionary<Type, Delegate> _completionCallbacks = new();
    private readonly Queue<NodeInstance> _executionQueue = new();

    private ActivityInstance _completedInstance;
    internal NodeInstance CurrentNode { get; set; }

    internal ExecutionBranchId CurrentBranch => CurrentNode.ExecutionBranch;
    internal string CurrentNodeId => CurrentNode.ExecutionNodeId;

    private Dictionary<string, NodeInstance> NodesInstances => _flowchartState.Get(_activeContext).NodesInstances;

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
            CurrentNode = null;
        });

        void SetCurrentNode()
        {
            CurrentNode = null;
            if (completedInstance is null)
                return;
            CurrentNode = NodesInstances.Values
                .Where(n => n.ActivityInstanceIds.Contains(completedInstance?.Id))
                .FirstOrDefault();
        }
    }

    private void SaveNodeActivityLink(ActivityInstance activityInstance)
    {
        CurrentNode.ActivityInstanceIds.Add(activityInstance.Id);
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

    internal bool CancelNodes(List<NodeInstance> otherNodes)
    {
        foreach (var nodeState in otherNodes)
        {
            var childrenToCancel = _activeContext.GetChildren()
                .Where(c => nodeState.ActivityInstanceIds.Contains(c.Id))
                .ToList();
            foreach (var child in childrenToCancel)
                _activeContext.CancelChild(child);
            nodeState.IsCancelRequested = true;
        }
        return otherNodes.Any();
    }
    internal List<NodeInstance> GetOtherNodes()
    {
        var currentBranch = CurrentNode.ExecutionBranch;
        return (
                from state in NodesInstances.Values
                where state != CurrentNode
                where state.ExecutionBranch.SplitsStack.StartsWith(currentBranch.SplitsStack)
                where state.ActivityInstanceIds.Any() || !(state.IsCancelRequested)
                select state
            ).ToList();
    }

    internal StaticNodeBranchInfo GetStaticBranches(FlowNode node)
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
        var currentNode = _reachableNodes[CurrentNode.StaticNodeIndex];
        Debug.WriteLine($"completionCallback:{currentNode}");
        currentNode.OnCompletionCallback(result);
        CurrentNode.ActivityInstanceIds.Remove(completedInstance.Id);
        SetNodeCompleted();
        ExecuteQueue();
    }
    private void SetNodeCompleted()
    {
        if (CurrentNode.DoNotComplete || CurrentNode.ActivityInstanceIds.Any())
            return;

        NodesInstances.Remove(CurrentNode.ExecutionNodeId);
        CheckBranchEnded();
    }

    void CheckBranchEnded()
    {
        var runningMerges = (
            from state in NodesInstances.Values
            where _reachableNodes[state.StaticNodeIndex] is FlowMerge
            where state != CurrentNode
            where state.StartedRunning
            where CurrentNode.ExecutionBranch.SplitsStack == state.ExecutionBranch.SplitsStack
            select state
            ).ToList();

        foreach (var merge in runningMerges)
        {
            Debug.WriteLine($"merging {merge}");
            _executionQueue.Enqueue(merge);
        }
    }

    internal void EnqueueNodeExecution(FlowNode node, ExecutionBranchId branchInstance = null)
    {
        if (node is null)
            return;

        if (CurrentNode?.IsCancelRequested is true)
        {
            Debug.WriteLine($"skipping queue:{node} because cancelled:{CurrentNode} ");
            return;
        }

        var executionNodeId = $"{node.Index}-{node}"; //+ Guid.NewGuid().ToString();

        if (!NodesInstances.TryGetValue(executionNodeId, out var nodeInstance))
        {
            nodeInstance = node.CreateInstance();
            nodeInstance.ExecutionBranch = branchInstance ?? CurrentNode.ExecutionBranch;
            nodeInstance.ExecutionNodeId = executionNodeId;
            nodeInstance.StaticNodeIndex = node.Index;
            NodesInstances[executionNodeId] = nodeInstance;
        }

        nodeInstance.IsQueued = true;
        _executionQueue.Enqueue(nodeInstance);
    }

    private void ExecuteQueue()
    {
        while (_executionQueue.TryDequeue(out var nextNode))
        {
            ExecuteNode(nextNode);
        }
        
        if (!NodesInstances.Values.Any())
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

        void ExecuteNode(NodeInstance nextNode)
        {
            CurrentNode = nextNode;
            nextNode.IsQueued = false;
            nextNode.StartedRunning = true;

            if (_activeContext.IsCancellationRequested)
            {
                Debug.WriteLine($"CancellAll");
                // we're not done and cancel has been requested
                _activeContext.MarkCanceled();
                _executionQueue.Clear();
                return;
            }

            if (nextNode.IsCancelRequested)
            {
                Debug.WriteLine($"skipping cancelled node {nextNode}");
                NodesInstances.Remove(nextNode.ExecutionNodeId);
                CheckBranchEnded();
            }
            else
            {
                Debug.WriteLine($"executing {nextNode}");
                var node = _reachableNodes[nextNode.StaticNodeIndex];
                nextNode.Execute(this, node);
                SetNodeCompleted();
            }
        }
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
    internal abstract class NodeInstance<TFlowNode> : NodeInstance where TFlowNode : FlowNode
    {
        protected Flowchart Flowchart { get; private set; }
        protected TFlowNode Node { get; private set; }

        internal sealed override void Execute(Flowchart flowchart, FlowNode node)
        {
            Flowchart = flowchart;
            Node = node as TFlowNode;

            Execute();
        }
        internal abstract void Execute();
    }
    internal class NodeInstance
    {
        public ExecutionBranchId ExecutionBranch { get; set; }
        public int StaticNodeIndex { get; set; }
        public string ExecutionNodeId { get; set; }
        public bool DoNotComplete { get; set; }
        public bool IsCancelRequested { get; set; }
        public bool StartedRunning { get; set; }
        public HashSet<string> ActivityInstanceIds { get; set; } = new();

        public bool IsQueued { get; set; }

        public override string ToString()
        {
            return $"{ExecutionNodeId}/{ExecutionBranch}";
        }
        internal virtual void Execute(Flowchart Owner, FlowNode Node) {
            Node.Execute();
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
    private class State
    {
        public string Version { get; init; }
        public Dictionary<string, NodeInstance> NodesInstances { get; set; } = new();
    }
}

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

    internal string CurrentNodeId => CurrentNode.ExecutionNodeId;

    private Dictionary<string, NodeInstance> NodesInstances => _flowchartState.Get(_activeContext).NodesInstances;
    private int NextExecutionNodeId => ++_flowchartState.Get(_activeContext).NextExecutionNodeId;

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
            EnqueueNodeExecution(StartNode);
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
    internal List<NodeInstance> GetSameStackNodes()
    {
        return (
                from state in NodesInstances.Values
                where state != CurrentNode
                where state.ExecutionStack.IsSameOrInnerStackOf(CurrentNode.ExecutionStack)
                select state
            ).ToList();
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
        CurrentNode.OnCompletionCallback(this, currentNode, result);
        CurrentNode.ActivityInstanceIds.Remove(completedInstance.Id);
        SetNodeCompleted();
        ExecuteQueue();
    }
    private void SetNodeCompleted()
    {
        if (CurrentNode is FlowMerge.MergeInstance merge && !merge.MergeCompleted || CurrentNode.ActivityInstanceIds.Any())
            return;

        NodesInstances.Remove(CurrentNode.ExecutionNodeId);
        CheckBranchEnded();
    }

    void CheckBranchEnded()
    {
        var runningMerges = (
            from nodeInstance in NodesInstances.Values
            where nodeInstance != CurrentNode
            where _reachableNodes[nodeInstance.StaticNodeIndex] is FlowMerge
            select nodeInstance
            ).ToList();

        foreach (var merge in runningMerges)
        {
            Debug.WriteLine($"merging {merge}");
            _executionQueue.Enqueue(merge);
        }
    }

    internal void EnqueueNodeExecution(FlowNode node, EnqueueType enqueueType = EnqueueType.Propagate)
    {
        if (node is null)
            return;

        if (CurrentNode?.IsCancelRequested is true)
        {
            Debug.WriteLine($"skipping queue for {node} because cancelled:{CurrentNode} ");
            return;
        }
        var executionStack = enqueueType switch
            {
                EnqueueType.Propagate => CurrentNode?.ExecutionStack ?? new() { SplitsStack = "_" },
                EnqueueType.Push => CurrentNode.ExecutionStack.Push(CurrentNodeId),
                EnqueueType.Pop => CurrentNode.ExecutionStack.Pop(),
                _ => throw new NotImplementedException()
            };
        
        var executionNodeId = node switch
        {
            FlowMerge => $"{node.Index}_{executionStack}",
            _ => $"{node.Index}_{NextExecutionNodeId}",
        };

        if (!NodesInstances.TryGetValue(executionNodeId, out var nodeInstance))
        {
            nodeInstance = node.CreateInstance();
            nodeInstance.ExecutionStack = executionStack;
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

    internal enum EnqueueType
    {
        Propagate,
        Push,
        Pop
    }

    public record ExecutionStackInfo
    {
        private const char StackDelimiter = ':';
        public string SplitsStack { get; init; }
        public ExecutionStackInfo() 
        {
        }

        public ExecutionStackInfo Push(string splitId)
        {
            return this with
            {
                SplitsStack = $"{SplitsStack}{StackDelimiter}{splitId}"
            };
        }
        public ExecutionStackInfo Pop()
        {
            return this with
            {
                SplitsStack = Pop(SplitsStack)
            };
        }
        private static string Pop(string stack) => stack[..stack.LastIndexOf(StackDelimiter)];

        internal bool IsSameOrInnerStackOf(ExecutionStackInfo outerStack)
            => SplitsStack.StartsWith(outerStack.SplitsStack);

        public override string ToString() => SplitsStack;
    }
    public abstract class NodeInstance
    {
        public ExecutionStackInfo ExecutionStack { get; set; }
        public int StaticNodeIndex { get; set; }
        public string ExecutionNodeId { get; set; }
        public bool IsCancelRequested { get; set; }
        public bool StartedRunning { get; set; }
        public HashSet<string> ActivityInstanceIds { get; set; } = new();

        public bool IsQueued { get; set; }

        public override string ToString()
        {
            return $"{ExecutionNodeId}/{ExecutionStack}";
        }
        internal abstract void Execute(Flowchart Owner, FlowNode Node);
        internal virtual void OnCompletionCallback<T>(Flowchart Owner, FlowNode Node, T result) { }
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
    public class State
    {
        public string Version { get; init; }
        public Dictionary<string, NodeInstance> NodesInstances { get; set; } = new();
        public int NextExecutionNodeId { get; set; } = 0;
    }
}

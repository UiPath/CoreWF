using System.Activities.Runtime;
using System.Linq;
namespace System.Activities.Statements;

public abstract partial class FlowNodeBase
{
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

    internal class FlowchartExtension
    {
        private readonly Dictionary<FlowNode, BranchLinks> _splitBranchesByNode = new();
        private readonly HashSet<FlowNode> _nodes = new();
        private readonly FlowchartState.Of<Dictionary<string,int>> _nodeIndexByActivityId;
        private readonly FlowchartState.Of<Dictionary<int, NodeState>> _nodesStatesByIndex;
        private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
        private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();
        private readonly Dictionary<Type,object> _completionCallbacks = new();
        private CompletionCallback _completionCallback;

        public ActivityInstance completedInstance { get; private set; }
        public NativeActivityContext context { get; private set; }
        public Flowchart Flowchart { get; }
        public FlowNode Current { get; set; }

        public bool HasState() => FlowchartState.HasState(Flowchart);

        public FlowchartExtension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodeIndexByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
            _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
        }

        public IDisposable WithContext(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (this.completedInstance is not null || this.context is not null)
                throw new InvalidOperationException("Context already set.");
            this.completedInstance = completedInstance;
            this.context = context;
            this.Current = GetCurrentNode();
            return Disposable.Create(() =>
            {
                this.context = null;
                this.completedInstance = null;
                this.Current = null;
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

        public void EndCacheMetadata()
        {
            if (Flowchart.StartNode is not null)
                _nodes.Add(Flowchart.StartNode);
            foreach(var node in _nodes.OfType<FlowNodeBase>())
            {
                node.EndCacheMetadata();
            }
        }
        public void ScheduleWithCallback(Activity activity)
        {
            _completionCallback ??= new CompletionCallback(Flowchart.OnCompletionCallback);
            context.ScheduleActivity(activity, _completionCallback);
        }

        public void ScheduleWithCallback<T>(Activity<T> activity)
        {
            if (!_completionCallbacks.TryGetValue(typeof(T), out var callback))
            {
                _completionCallbacks[typeof(T)] = callback = new CompletionCallback<T>(Flowchart.OnCompletionCallback);
            }
            context.ScheduleActivity(activity, callback as CompletionCallback<T>);
        }

        public void OnExecute()
        {
            SaveLinks();
            SaveActivityIdToNodeIndex();
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
                    nodesStates[node.Index] = new NodeState() { ActivityId = node.ChildActivity?.Id};
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
            if (completedInstance?.IsCancellationRequested == true)
                return true;
            var node = Current;
            if (node == null)
                return false;
            return GetNodeState(node.Index).IsCancelRequested;
        }

        private NodeState GetNodeState(int index) => _nodesStatesByIndex.GetOrAdd()[index];

        public bool Cancel(int branchNodeIndex)
        {
            var nodeState = GetNodeState(branchNodeIndex);
            var childToCancel = context.GetChildren().FirstOrDefault(c => c.Activity.Id == nodeState.ActivityId);
            if (nodeState.IsCancelRequested 
                && (childToCancel is null || childToCancel.IsCancellationRequested))
                return false;

            nodeState.IsCancelRequested = true;
            if (childToCancel is not null)
                context.CancelChild(childToCancel);
            return true;
        }

        internal void Install(NativeActivityMetadata metadata)
            => FlowchartState.Install(metadata, Flowchart);

        internal List<FlowNode> GetSuccessors(int index)
            => _successors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

        internal List<FlowNode> GetPredecessors(int index)
            => _predecessors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

        internal List<FlowNode> GetPredecessors(FlowNode node)
        {
            _predecessors.TryGetValue(node, out var result);
            return result?.ToList() ?? new();
        }

        internal void SaveBranch(FlowNode node, FlowSplitBranch splitBranch, FlowSplit parentSplit)
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

        internal BranchLinks GetBranch(FlowNode predecessorNode)
        {
            return _splitBranchesByNode[predecessorNode];
        }

        internal void OnCompletionCallback<T>(T result)
        {
            var node = Current as FlowNodeBase;
            node.OnCompletionCallback(result);
            OnNodeCompleted();
        }

        internal void OnNodeCompleted()
        {
        }

        internal class BranchLinks
        {
            public FlowSplit Split { get; set; }
            public FlowSplitBranch Branch { get; set; }
            public FlowNode RuntimeNode { get; set; }
        }

        private class NodeState
        {
            public bool IsCancelRequested { get; set; }
            public string ActivityId { get; set; }
        }
    }
}
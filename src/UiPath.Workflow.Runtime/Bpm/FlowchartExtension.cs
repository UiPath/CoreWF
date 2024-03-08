using System.Linq;
using static System.Activities.Runtime.Scheduler;
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
            return Disposable.Create(() =>
            {
                this.context = null;
                this.completedInstance = null;
            });
        }

        public void EndCacheMetadata()
        {
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

        public bool TryGetCurrentNode(out int index)
        {
            if (completedInstance == null)
            {
                index = 0;
                return false;
            }

            return _nodeIndexByActivityId.GetOrAdd().TryGetValue(completedInstance.Activity.Id, out index);
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
            }
        }

        public bool IsCancelRequested()
        {
            if (completedInstance?.IsCancellationRequested == true 
                || TryGetCurrentNode(out var index) && GetNodeState(index).IsCancelRequested)
            {
                return true;
            }
            return false;
        }

        private NodeState GetNodeState(int index) => _nodesStatesByIndex.GetOrAdd()[index];

        public bool Cancel(int branch)
        {
            var nodeState = GetNodeState(branch);
            if (nodeState.IsCancelRequested)
                return false;
            nodeState.IsCancelRequested = true;
            
            var childToCancel = context.GetChildren().FirstOrDefault(c => c.Activity.Id == nodeState.ActivityId);
            if (childToCancel is not null)
                context.CancelChild(childToCancel);

            return true;
        }

        internal void Install(NativeActivityMetadata metadata)
            => FlowchartState.Install(metadata, Flowchart);

        internal List<FlowNode> GetPredecessors(int index)
            => _predecessors.FirstOrDefault(l => l.Key.Index == index).Value?.ToList() ?? new();

        internal List<FlowNode> GetPredecessors(FlowNode node) 
            => _predecessors[node].ToList();

        private class NodeState
        {
            public bool IsCancelRequested { get; set; }
            public string ActivityId { get; set; }
        }
    }
}
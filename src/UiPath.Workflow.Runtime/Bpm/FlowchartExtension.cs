using System.Linq;
namespace System.Activities.Statements;

public abstract partial class FlowNodeBase
{
    internal class FlowchartExtension
    {
        private readonly HashSet<FlowNode> _nodes = new();
        private readonly FlowchartState.Of<Dictionary<string,int>> _nodeIndexByActivityId;
        private readonly FlowchartState.Of<Dictionary<int, NodeState>> _nodesStatesByIndex;
        private readonly Dictionary<FlowNode, HashSet<FlowNode>> _successors = new();
        private readonly Dictionary<FlowNode, HashSet<FlowNode>> _predecessors = new();

        public Flowchart Flowchart { get; }
        public bool HasState(NativeActivityContext context) => FlowchartState.HasState(Flowchart, context);

        public FlowchartExtension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodeIndexByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
            _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
        }

        public void EndCacheMetadata()
        {
            foreach(var node in _nodes.OfType<FlowNodeBase>())
            {
                node.EndCacheMetadata();
            }
        }

        public bool TryGetCurrentNode(NativeActivityContext context, ActivityInstance completedInstance, out int index)
        {
            if (completedInstance == null)
            {
                index = 0;
                return false;
            }

            return _nodeIndexByActivityId.GetOrAdd(context).TryGetValue(completedInstance.Activity.Id, out index);
        }

        public void OnExecute(NativeActivityContext context)
        {
            SaveLinks();
            SaveActivityIdToNodeIndex();
            void SaveActivityIdToNodeIndex()
            {
                var nodesByActivityId = _nodeIndexByActivityId.GetOrAdd(context);
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
                var nodesStates = _nodesStatesByIndex.GetOrAdd(context);
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

        public bool IsCancelRequested(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (completedInstance?.IsCancellationRequested == true 
                || TryGetCurrentNode(context, completedInstance, out var index) && GetNodeState(context, index).IsCancelRequested)
            {
                return true;
            }
            return false;
        }

        private NodeState GetNodeState(NativeActivityContext context, int index) => _nodesStatesByIndex.GetOrAdd(context)[index];

        public bool Cancel(NativeActivityContext context, int branch)
        {
            var nodeState = GetNodeState(context, branch);
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
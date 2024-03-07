using System.Linq;
namespace System.Activities.Statements;

public abstract partial class FlowNodeBase
{
    internal class FlowchartExtension
    {
        private readonly List<(FlowNode predecessor, FlowNode successor)> _links = new();
        private readonly FlowchartState.Of<Dictionary<string,int>> _nodeIndexByActivityId;
        private readonly FlowchartState.Of<Dictionary<int, NodeState>> _nodesStatesByIndex;
        public Flowchart Flowchart { get; }
        public bool HasState(NativeActivityContext context) => FlowchartState.HasState(Flowchart, context);

        public FlowchartExtension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodeIndexByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
            _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
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
                foreach (var activityWithNode in _links)
                {
                    SaveNode(activityWithNode.predecessor);
                    SaveNode(activityWithNode.successor);
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
                foreach (var item in _links)
                {
                    if (!nodesStates.TryGetValue(item.predecessor.Index, out var predecessorState))
                    {
                        predecessorState = new NodeState() { ActivityId = item.predecessor.ChildActivity?.Id};
                        nodesStates[item.predecessor.Index] = predecessorState;
                    }
                   
                    predecessorState.Successors.Add(item.successor.Index);

                    if (!nodesStates.TryGetValue(item.successor.Index, out var successorState))
                    {
                        successorState = new NodeState() { ActivityId = item.successor.ChildActivity?.Id };
                        nodesStates[item.successor.Index] = successorState;
                    }
                    successorState.Predecessors.Add(item.predecessor.Index);
                }
            }
        }

        public void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
        {
            if (predecessor == null)
                return;
            foreach (var successor in successors.Where(s => s is not null))
            {
                _links.Add(new(predecessor, successor));
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

        public IEnumerable<int> GetPredecessors(NativeActivityContext context, int index)
        => GetNodeState(context, index).Predecessors;

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
        {
            FlowchartState.Install(metadata, Flowchart);
        }

        private class NodeState
        {
            public bool IsCancelRequested { get; set; }
            public HashSet<int> Successors { get; } = new ();
            public HashSet<int> Predecessors { get; } = new ();
            public string ActivityId { get; set; }
        }
    }
}
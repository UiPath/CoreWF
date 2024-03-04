using System.Activities.Statements;
using System.Linq;
using System.Reflection;
namespace System.Activities.Bpm;

public abstract class FlowNodeExtensible : FlowNode
{
    public const string FlowChartStateVariableName = "flowchartState";
    internal abstract void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode);

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        Install(metadata, owner);
    }

    private static void Install(NativeActivityMetadata metadata, Flowchart owner)
    {
        if (owner.ImplementationVariables.Any( v => v.Name == FlowChartStateVariableName))
            return;
        
        metadata.AddImplementationVariable(new Variable<Dictionary<string, object>>(FlowChartStateVariableName, c => new()));
    }

    internal class FlowchartState<T>
    {
        private readonly string _key;
        private readonly Flowchart _owner;
        private readonly Func<T> _addValue;

        public FlowchartState(string key, Flowchart owner, Func<T> addValue)
        {
            _key = key;
            _owner = owner;
            _addValue = addValue;
        }

        public T GetOrAdd(ActivityContext context)
        {
            var variable = _owner.ImplementationVariables.Single(v => v.Name == FlowChartStateVariableName);
            var flowChartState = (Dictionary<string, object>)variable.Get(context);
            if (!flowChartState.TryGetValue(_key, out var value))
            {
                value = _addValue();
                flowChartState[_key] = value;
            }
            return (T)value;
        }
    }

    internal class Extension
    {
        public bool TryGetCurrentNode(NativeActivityContext context, ActivityInstance completedInstance, out int index)
        {
            if (completedInstance == null)
            {
                index = -1;
                return false;
            }    

            return _nodesByActivityId.GetOrAdd(context).TryGetValue(completedInstance.Activity.Id, out index);
        }
        private readonly List<(FlowNode predecessor, FlowNode successor)> _links = new();
        private readonly FlowchartState<Dictionary<string,int>> _nodesByActivityId;
        private readonly FlowchartState<Dictionary<int, NodeState>> _nodesStatesByIndex;

        public Extension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodesByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
            _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
        }
        public Flowchart Flowchart { get; }

        public void OnExecute(NativeActivityContext context)
        {
            if (Flowchart.IsLegacyFlowchart)
                return;

            SaveLinks();
            SaveActivityIdToNodeIndex();
            void SaveActivityIdToNodeIndex()
            {
                var nodesByActivityId = _nodesByActivityId.GetOrAdd(context);
                foreach (var activityWithNode in _links)
                {
                    SaveNode(activityWithNode.predecessor);
                    SaveNode(activityWithNode.successor);
                }

                void SaveNode(FlowNode node)
                {
                    var activityId = node.ChildActivity?.Id;
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

        internal void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
        {
            successors.ForEach(s => _links.Add(new(predecessor, s)));
        }

        internal bool IsCancelRequested(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (completedInstance?.IsCancellationRequested == true 
                || TryGetCurrentNode(context, completedInstance, out var index) && GetNodeState(context, index).IsCancelRequested)
            {
                return true;
            }
            return false;

        }

        private NodeState GetNodeState(NativeActivityContext context, int index) => _nodesStatesByIndex.GetOrAdd(context)[index];

        internal IEnumerable<int> GetPredecessors(NativeActivityContext context, int index)
        => GetNodeState(context, index).Predecessors;

        internal bool Cancel(NativeActivityContext context, int branch)
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

        private class NodeState
        {
            public bool IsCancelRequested { get; set; }
            public HashSet<int> Successors { get; } = new ();
            public HashSet<int> Predecessors { get; } = new ();
            public string ActivityId { get; set; }
        }
    }

}
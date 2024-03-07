using System.Activities.Statements;
using System.Linq;
namespace System.Activities.Bpm;

public abstract class FlowNodeExtensible : FlowNode
{
    public const string FlowChartStateVariableName = "flowchartState";
    internal FlowchartExtension Extension => Owner._extension;

    internal abstract void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode);

    internal override void OnOpen(Flowchart owner, NativeActivityMetadata metadata)
    {
        FlowchartState.Install(metadata, owner);
    }

    internal virtual void OnCompletionCallback<T>(NativeActivityContext context, ActivityInstance completedInstance, T result)
    {
        if (result is bool b)
            OnCompletionCallback(context, completedInstance, b);
    }

    protected virtual void OnCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
    }


    internal abstract class FlowchartState
    {
        private readonly string _key;
        private Flowchart _owner;
        private readonly Func<Flowchart> _getowner;
        private readonly Func<object> _addValue;

        public FlowchartState(string key, Func<Flowchart> getOwner, Func<object> addValue)
        {
            _key = key;
            _getowner = getOwner;
            _addValue = addValue;
        }

        public static void Install(NativeActivityMetadata metadata, Flowchart owner)
        {
            if (owner.ImplementationVariables.Any(v => v.Name == FlowChartStateVariableName))
                return;

            metadata.AddImplementationVariable(new Variable<Dictionary<string, object>>(FlowChartStateVariableName, c => new()));
        }
        public object GetOrAdd(ActivityContext context)
        {
            _owner ??= _getowner();
            var variable = _owner.ImplementationVariables.Single(v => v.Name == FlowChartStateVariableName);
            var flowChartState = (Dictionary<string, object>)variable.Get(context);
            if (!flowChartState.TryGetValue(_key, out var value))
            {
                value = _addValue();
                flowChartState[_key] = value;
            }
            return value;
        }
    }
    internal class FlowchartState<T> : FlowchartState
    {
        public FlowchartState(string key, Flowchart owner, Func<T> addValue) : base(key, () => owner, () => addValue())
        {
        }
        public FlowchartState(string key, FlowNodeExtensible node, Func<T> addValue) : base(key, () => node.Owner, () => addValue())
        {
        }

        public new T GetOrAdd(ActivityContext context) => (T)base.GetOrAdd(context);
    }

    internal class FlowchartExtension
    {
        public bool TryGetCurrentNode(NativeActivityContext context, ActivityInstance completedInstance, out int index)
        {
            if (completedInstance == null)
            {
                index = -1;
                return false;
            }    

            return _nodeIndexByActivityId.GetOrAdd(context).TryGetValue(completedInstance.Activity.Id, out index);
        }
        private readonly List<(FlowNode predecessor, FlowNode successor)> _links = new();
        private readonly FlowchartState<Dictionary<string,int>> _nodeIndexByActivityId;
        private readonly FlowchartState<Dictionary<int, NodeState>> _nodesStatesByIndex;

        public FlowchartExtension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodeIndexByActivityId = new("_nodeIndexByActivityId", flowchart, () => new());
            _nodesStatesByIndex = new("_nodesStatesByIndex", flowchart, () => new());
        }
        public Flowchart Flowchart { get; }

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

        internal void ScheduleWithCallback<T>(NativeActivityContext context, Activity<T> activity)
        {
            context.ScheduleActivity(activity, new CompletionCallback<T>(Flowchart.OnCompletionCallback));
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
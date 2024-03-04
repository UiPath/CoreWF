using System.Activities.Statements;
using System.Linq;
namespace System.Activities.Bpm;

public abstract class FlowNodeExtensible : FlowNode
{
    public const string FlowChartStateVariableName = "flowchartState";
    internal abstract void Execute(NativeActivityContext context);

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

    protected virtual void NotifyPredecessor(FlowNode predecesor)
    {
    }

    internal class FlowchartState<T>
    {
        private readonly string _key;
        private readonly Flowchart _owner;

        public FlowchartState(string key, Flowchart owner)
        {
            _key = key;
            _owner = owner;
        }

        public Dictionary<string, T> GetOrAdd(ActivityContext context)
        {
            var variable = _owner.ImplementationVariables.Single(v => v.Name == FlowChartStateVariableName);
            var flowChartState = (Dictionary<string, object>)variable.Get(context);
            if (!flowChartState.TryGetValue(_key, out var value))
            {
                value = new Dictionary<string, T>();
                flowChartState[_key] = value;
            }
            return (Dictionary<string, T>)value;
        }
    }

    internal class Extension
    {
        public bool TryGetCurrentNode(NativeActivityContext context, ActivityInstance completedInstance, out int index)
        {
            return _nodesByActivityId.GetOrAdd(context).TryGetValue(completedInstance.Activity.Id, out index);
        }
        private readonly List<(Activity activity, FlowNode node)> _nodesByActivity = new();
        private readonly FlowchartState<int> _nodesByActivityId;

        public Extension(Flowchart flowchart)
        {
            Flowchart = flowchart;
            _nodesByActivityId = new("_nodeIndexByActivityId", flowchart);
        }
        public Flowchart Flowchart { get; }

        public void OnExecute(NativeActivityContext context)
        {
            if (Flowchart.IsLegacyFlowchart)
                return;

            SaveActivityIdToNodeIndex();
            void SaveActivityIdToNodeIndex()
            {
                var nodesByActivityId = _nodesByActivityId.GetOrAdd(context);
                foreach (var activityWithNode in _nodesByActivity)
                {
                    nodesByActivityId
                        .Add(activityWithNode.activity.Id, activityWithNode.node.Index);
                }
            }
        }

        public void NotifyNode(FlowNode node)
        {
            if (node.ChildActivity is not null)
                _nodesByActivity.Add(new(node.ChildActivity, node));
        }

        internal void RecordLinks(FlowNode predecessor, List<FlowNode> successors)
        {
            foreach (var node in successors.OfType<FlowNodeExtensible>())
            {
                node.NotifyPredecessor(predecessor);
            }
        }

    }

}
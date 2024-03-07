using System.Activities.Runtime.Collections;
using System.Activities.Validation;
using System.Collections.ObjectModel;
using System.Linq;
namespace System.Activities.Statements;

public class FlowParallel : FlowNodeBase
{
    public class Branch
    {
        public static Branch New(FlowParallel parallel)
        {
            return new()
            {
                StartNode = parallel.JoinNode
            };
        }

        private Branch()
        {
            
        }

        public FlowNode StartNode { get; set; }
        public Activity<bool> Condition { get; set; }
        public string DisplayName {  get; set; }
    }
    public FlowJoin JoinNode { get; }
    private ValidatingCollection<Branch> _branches;
    internal override Activity ChildActivity => null;
    [DefaultValue(null)]
    public Collection<Branch> Branches => _branches ??= ValidatingCollection<Branch>.NullCheck();

    private List<FlowNode> _runtimeBranches;

    public FlowParallel()
    {
        JoinNode = new FlowJoin() { Parallel = this };
    }
    internal override void GetConnectedNodes(IList<FlowNode> connections)
    {
        _runtimeBranches = new (Branches.Select(b => (b.Condition is null) ? b.StartNode :
                    new FlowDecision()
                    {
                        Condition = b.Condition ?? new Expressions.LambdaValue<bool>(c => true),
                        DisplayName = b.DisplayName,
                        True = b.StartNode,
                        False = JoinNode
                    }
            ));
        connections.AddRange(_runtimeBranches);
    }

    internal override void Execute(NativeActivityContext context, ActivityInstance completedInstance, FlowNode predecessorNode)
    {
        for (int i = _runtimeBranches.Count - 1; i >= 0; i--)
        {
            var branch = _runtimeBranches[i];
                Owner.ExecuteNextNode(context, branch, completedInstance);
        }
    }
}
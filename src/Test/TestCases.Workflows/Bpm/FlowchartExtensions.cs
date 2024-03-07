using System.Activities;
using System.Activities.Validation;
using System.Activities.Statements;
using System.Activities.Bpm;
using System.Linq;

namespace TestCases.Activitiess.Bpm;

public static class FlowchartExtensions
{
    public static FlowParallel SplitTo(this FlowParallel parallel, params Activity[] nodes)
    {
        foreach(var node in nodes)
        {
            var branch = System.Activities.Bpm.FlowParallel.Branch.New(parallel);
            branch.StartNode = new FlowStep() { Action = node, Next = branch.StartNode };
            parallel.Branches.Add(branch);
        }
        return parallel;
    }
    public static FlowParallel SplitTo(this FlowParallel parallel, params FlowNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var branch = System.Activities.Bpm.FlowParallel.Branch.New(parallel);
            node.FlowTo(parallel.JoinNode);
            branch.StartNode = node;
            parallel.Branches.Add(branch);
        }
        return parallel;
    }
    public static FlowStep FlowTo(this Activity predeccessor, FlowNode successor)
    {
        return new FlowStep { Action = predeccessor }.FlowTo(successor);
    }
    public static FlowStep FlowTo(this Activity predeccessor, Activity successor)
    {
        return new FlowStep { Action = predeccessor }.FlowTo(successor);
    }
    public static T FlowTo<T>(this T predeccessor, Activity successor)
        where T : FlowNode
    {
        return predeccessor.FlowTo(new FlowStep { Action = successor });
    }
    public static T FlowTo<T>(this T predeccessor, FlowNode successor)
        where T: FlowNode
    {
        FlowNode current = predeccessor;
        while (current != successor)
        {
            if (current is FlowStep step)
            {
                current = (step.Next ??= successor);
            }
            else if (current is FlowJoin join)
            {
                current = (join.Next ??= successor);
            }
            else if (current is FlowParallel parallel)
            {
                current = (parallel.JoinNode.Next ??= successor);
            }
        }
        return predeccessor;
    }
}

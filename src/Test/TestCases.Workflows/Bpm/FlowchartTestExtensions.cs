using System.Activities;
using System.Activities.Statements;
using System;

namespace TestCases.Activitiess.Bpm;

public static class FlowchartTestExtensions
{
    public static FlowParallel AddBranches(this FlowParallel parallel, params Activity[] nodes)
    {
        foreach(var node in nodes)
        {
            var branch = FlowParallel.Branch.New(parallel);
            branch.StartNode = new FlowStep() { Action = node, Next = branch.StartNode };
            parallel.Branches.Add(branch);
        }
        return parallel;
    }
    public static FlowParallel AddBranches(this FlowParallel parallel, params FlowNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var branch = FlowParallel.Branch.New(parallel);
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
        if (predeccessor == successor)
            return predeccessor;
        switch (predeccessor)
        {
            case FlowStep step:
                (step.Next ??= successor).FlowTo(successor);
                break;
            case FlowJoin join:
                (join.Next ??= successor).FlowTo(successor);
                break;
            case FlowParallel parallel:
                (parallel.JoinNode.Next ??= successor).FlowTo(successor);
                break;
            case FlowDecision decision:
                (decision.True ??= successor).FlowTo(successor);
                (decision.False ??= successor).FlowTo(successor);
                break;
            default:
                throw new NotSupportedException(predeccessor.GetType().Name);
        }
        return predeccessor;
    }
}

using System.Activities;
using System.Activities.Statements;
using System;
using System.Linq;

namespace TestCases.Activitiess.Bpm;

public static class FlowchartTestExtensions
{
    public static FlowSplit AddBranches(this FlowSplit split, params Activity[] nodes)
    {
        return split.AddBranches(nodes.Select(a => new FlowStep { Action = a }).ToArray());
    }
    public static FlowSplit AddBranches(this FlowSplit split, params FlowNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var branch = new FlowSplitBranch()
            {
                StartNode = node,
            };
            split.Branches.Add(branch);
        }
        return split;
    }
    public static FlowStep Step(this Activity activity)
    {
        return new FlowStep { Action = activity };
    }
    public static FlowMerge Merge(this Activity activity)
    {
        return new () { Next = new FlowStep() { Action = activity } };
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
            case FlowMerge join:
                (join.Next ??= successor).FlowTo(successor);
                break;
            case FlowSplit split:
                foreach (var branch in split.Branches)
                {
                    branch.StartNode.FlowTo(successor);
                }
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

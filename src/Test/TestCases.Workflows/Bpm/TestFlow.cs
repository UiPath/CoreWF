using System.Activities;
using System.Activities.Statements;
using System;
using System.Collections.Generic;
using System.Activities.Validation;
using Shouldly;
using System.Linq;
using WorkflowApplicationTestExtensions;

namespace TestCases.Activitiess.Bpm;

public class AddStringActivity : NativeActivity
{
    protected override bool CanInduceIdle => true;

    public string Item { get; set; }
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);
        metadata.AddImplementationVariable(new Variable<List<string>>("strings"));
    }

    protected override void Execute(NativeActivityContext context)
    {
        context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        using var _ = context.InheritVariables();
        var stringsLocation = context.GetInheritedLocation<List<string>>("strings");
        stringsLocation.Value ??= [];
        stringsLocation.Value.Add(Item);
    }
}

public static class TestFlow
{
    public static FlowNode Text(this FlowNode flowstep, string stringToAdd)
    => flowstep.FlowTo(Text(stringToAdd));

    public static FlowNode Delay(this FlowNode flowstep, TimeSpan delay)
    => flowstep.FlowTo(Delay(delay));

    public static FlowStep Text(string stringToAdd)
    => new AddStringActivity() { Item = stringToAdd, DisplayName = stringToAdd }.Step();

    public static FlowStep Delay(TimeSpan delay)
    => new Delay() { Duration = new InArgument<TimeSpan>(delay) }.Step();

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

    public static T FlowTo<T>(this T predeccessor, Activity activity)
        where T : FlowNode
    => predeccessor.FlowTo(activity.Step());
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

    public static ValidationResults Validate(FlowNode startNode)
    {
        return ActivityValidationServices.Validate(new Flowchart() { StartNode = startNode });
    }

    public static ValidationResults HasErrorFor(this ValidationResults results, FlowNode errorNode)
    {
        results.Errors.ShouldNotBeEmpty();
        results.Errors.Where(e => (e.SourceDetail as IList<FlowNode>).Contains(errorNode)).ShouldNotBeEmpty();
        return results;
    }

    public static List<string> Results(FlowNode startNode)
    {
        var flowchart = new Flowchart { StartNode = startNode };
        return Results(flowchart);
    }

    public static List<string> Results(Flowchart flowchart)
    {
        Variable<List<string>> _stringsVariable = new("strings", c => new());
        var root = new ActivityWithResult<List<string>> { Body = flowchart, In = _stringsVariable };
        var app = new WorkflowApplication(root);
        return (List<string>)app.RunUntilCompletion().Outputs["Result"];
    }

    private class ActivityWithResult<TResult> : NativeActivity<TResult>
    {
        public Variable<TResult> In { get; set; } = new();
        public Activity Body { get; set; }
        protected override void Execute(NativeActivityContext context) => context.ScheduleActivity(Body, (context, _) =>
        {
            TResult value;
            using (context.InheritVariables())
            {
                value = In.Get(context);
            }
            context.SetValue(Result, value);
        });
    }

}

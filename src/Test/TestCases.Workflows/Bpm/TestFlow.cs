using System.Activities;
using System.Activities.Statements;
using System;
using System.Collections.Generic;
using System.Activities.Validation;
using Shouldly;
using System.Linq;
using WorkflowApplicationTestExtensions;
using System.Threading;
using System.ComponentModel;

namespace TestCases.Activitiess.Bpm;

public class AddStringActivity : NativeActivity
{
    [DefaultValue(null)]
    public TimeSpan? Duration { get; set; }
    [DefaultValue(null)]
    public string Text { get; set; }
    [DefaultValue(null)]
    public string TextOnCancel { get; set; }
    private Variable<DateTime> StartTime { get; set; } = new Variable<DateTime>();

    protected override bool CanInduceIdle => true;

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);
        metadata.AddImplementationVariable(new Variable<List<string>>("strings"));
        metadata.AddImplementationVariable(StartTime);
    }

    protected override void Execute(NativeActivityContext context)
    {
        StartTime.Set(context, DateTime.Now);
        SuspendLoop(context);
    }

    private void SuspendLoop(NativeActivityContext context)
    {
        context.CreateBookmark(WorkflowApplicationTestExtensions.WorkflowApplicationTestExtensions.AutoResumedBookmarkNamePrefix + this.DisplayName + ":" + context.ActivityInstanceId, new BookmarkCallback(OnBookmarkResumed));
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        var starttime = StartTime.Get(context);
        var duration = Duration;
        if (starttime + duration > DateTime.Now)
        {
            Thread.Sleep(5);
            SuspendLoop(context);
            return;
        }
        AddString(context, Text);
    }

    protected override void Cancel(NativeActivityContext context)
    {
        if (TextOnCancel is not null)
        {
            AddString(context, TextOnCancel);
            base.Cancel(context);
        }
    }
    private void AddString(NativeActivityContext context, string text)
    {
        using var _ = context.InheritVariables();
        var stringsLocation = context.GetInheritedLocation<List<string>>("strings");
        stringsLocation.Value ??= [];
        stringsLocation.Value.Add(text);
    }
}

public static class TestFlow
{
    public static FlowNode Text(this FlowNode flowstep, string stringToAdd)
    => flowstep.FlowTo(Text(stringToAdd));

    public static FlowNode CancelableText(this FlowNode flowstep, TimeSpan delay, string textOnCancel = null)
    => flowstep.FlowTo(DelayedText(delay:delay, text: "[waited]" + textOnCancel, textOnCancel:textOnCancel));
    public static FlowNode DelayedText(this FlowNode flowstep, TimeSpan delay, string text)
    => flowstep.FlowTo(DelayedText(delay, text));

    public static FlowStep Text(string stringToAdd)
    => new AddStringActivity() { Text = stringToAdd, DisplayName = stringToAdd }.Step();

    public static FlowStep DelayedText(TimeSpan delay, string text, string textOnCancel = null)
    => new AddStringActivity() { Duration = delay, Text = text, TextOnCancel = textOnCancel, DisplayName = text }.Step();

    public static FlowSplit AddBranches(this FlowSplit split, params FlowNode[] nodes)
    {
        foreach (var node in nodes)
        {
            split.Branches.Add(node);
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
                    branch.FlowTo(successor);
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
        var flowchart = new Flowchart() { StartNode = startNode };
        //double validate to make sure CacheMetadata is reentrant
        var initial  = ActivityValidationServices.Validate(flowchart);
        var validationResult = ActivityValidationServices.Validate(flowchart);
        validationResult.ShouldBeEquivalentTo(initial);
        return validationResult;
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

    public static List<string> Results(Activity flowchart)
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

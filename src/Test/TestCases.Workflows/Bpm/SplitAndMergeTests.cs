using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using System.Linq;
using System.Activities.Expressions;
using TestObjects.XamlTestDriver;
using System.IO;
using System;

namespace TestCases.Activitiess.Bpm;

public class AddStringActivity : NativeActivity
{
    public string Item { get; set; }
    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        base.CacheMetadata(metadata);
        metadata.AddImplementationVariable(new Variable<List<string>>("strings"));
    }

    protected override void Execute(NativeActivityContext context)
    {
        using var _ = context.InheritVariables();
        var stringsLocation = context.GetInheritedLocation<List<string>>("strings");
        stringsLocation.Value ??= new();
        stringsLocation.Value.Add(Item);
    }
}

public class SplitAndMergeTests
{
    private List<string> Results { get; set; }
    private readonly Variable<List<string>> _stringsVariable = new("strings", c => new());

    private AddStringActivity AddString(string stringToAdd)
    {
        var act = new AddStringActivity() { Item = stringToAdd };
        return act;
    }

    private List<string> ExecuteFlowchart(FlowNode startNode)
    {
        var flowchart = new Flowchart { StartNode = startNode };
        return ExecuteFlowchart(flowchart);
    }

    private List<string> ExecuteFlowchart(Flowchart flowchart)
    {
        var root = new ActivityWithResult<List<string>> { Body = flowchart, In = _stringsVariable };
        return Results = WorkflowInvoker.Invoke(root);
    }

    [Fact]
    public void RoundTrip_xaml()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var split = new FlowSplit().AddBranches(AddString(branch1Str), AddString(branch2Str));
        var flowchart = new Flowchart { StartNode = split };
        var roundTrip = XamlRoundTrip(flowchart);
        ExecuteFlowchart(roundTrip);
        Results.ShouldBe(new() { branch1Str, branch2Str });

        static T XamlRoundTrip<T>(T obj)
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\testFlowchart.xaml";
            File.Delete(filePath);
            using (var stream = File.OpenWrite(filePath))
            {
                XamlTestDriver.Serialize(obj, stream);
            }
            using (var stream = File.OpenRead(filePath))
            {
                return (T)XamlTestDriver.Deserialize(stream);
            }
        }
    }

    [Fact]
    public void Should_execute_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var parallel = new FlowSplit().AddBranches(AddString(branch1Str), AddString(branch2Str));
        ExecuteFlowchart(parallel);
        Results.ShouldBe(new() { branch1Str, branch2Str });
    }

    [Fact]
    public void Should_join_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var split = new FlowSplit()
            .AddBranches(
                AddString(branch1Str),
                AddString(branch2Str)
                );
        split.MergeNode.FlowTo(AddString(stopString));

        ExecuteFlowchart(split);
        Results.ShouldBe(new() { branch1Str, branch2Str, stopString });
    }

    [Fact]
    public void Should_join_branches_with_inner_split()
    {
        var innerSplit = new FlowSplit()
            .AddBranches(
                AddString("branch1Inner"),
                AddString("branch2Inner")
                );
        innerSplit.MergeNode.FlowTo(AddString("innerMerged"));
        var outerSplit = new FlowSplit()
            .AddBranches(
                innerSplit,
                AddString("branch2Outer").Step()
                );
        outerSplit.MergeNode.FlowTo(AddString("stop"));

        ExecuteFlowchart(outerSplit);
        Results.ShouldBe(new() { "branch1Inner", "branch2Inner", "innerMerged", "branch2Outer", "stop"});
    }


    [Fact]
    public void Should_join_with_skiped_branches()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var split = new FlowSplit()
            .AddBranches(
                AddString(branch1Str),
                AddString(branch2Str)
                );
        split.Branches.First().Condition = new LambdaValue<bool>(c => false);
        split.MergeNode.FlowTo(AddString(stopString));

        ExecuteFlowchart(split);
        Results.ShouldBe(new() { branch2Str, stopString });
    }

    [Fact]
    public void Should_join_branches_when_condition_is_met()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var split = new FlowSplit()
            .AddBranches(
                AddString(branch1Str).FlowTo(AddString(branch1Str)),
                new BlockingActivity("whatever").FlowTo(AddString(branch2Str))
                );
        split.MergeNode.FlowTo(AddString(stopString));
        split.MergeNode.Completion = new LambdaValue<bool>(c => true);

        ExecuteFlowchart(split);
        Results.ShouldBe(new() { branch1Str, branch1Str, stopString });
    }

    [Fact]
    public void Should_persist_join()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";
        var blockingContStr = "blockingContinuation";
        const string blockingBookmark = "blocking";

        var root = ParallelActivities();
        var store = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        WorkflowApplication app = new(root) { InstanceStore = store };
        app.Run();
        var appId = app.Id;
        Thread.Sleep(1000);
        app.Unload();
        root = ParallelActivities();
        WorkflowApplication resumedApp = new(root) { InstanceStore = store };
        ManualResetEvent manualResetEvent = new(default);
        WorkflowApplicationCompletedEventArgs completedArgs = null;
        resumedApp.Completed = args =>
        {
            completedArgs = args;
            manualResetEvent.Set();
        };
        resumedApp.Aborted = args => args.Reason.ShouldBeNull();
        resumedApp.Load(appId);
        resumedApp.Run();
        resumedApp.ResumeBookmark(blockingBookmark, null);
        manualResetEvent.WaitOne();
        completedArgs.TerminationException.ShouldBeNull();
        Results = (List<string>)completedArgs.Outputs["Result"];
        Results.ShouldBe(new[] { branch1Str, branch2Str, blockingContStr, stopString });

        ActivityWithResult<List<string>> ParallelActivities()
        {
            var blockingContinuation = AddString(blockingContStr);
            var blockingActivity = new BlockingActivity(blockingBookmark);

            var split = new FlowSplit().AddBranches(
                AddString(branch1Str).FlowTo(new FlowStep()),
                AddString(branch2Str).FlowTo(new FlowStep()),
                blockingActivity.FlowTo(blockingContinuation).FlowTo(new FlowStep()));
            split.MergeNode.FlowTo(AddString(stopString));
            var flowchart = new Flowchart { StartNode = split };
            return new() { In = _stringsVariable, Body = flowchart };
        }
    }
}

public class ActivityWithResult<TResult> : NativeActivity<TResult>
{
    public Variable<TResult> In { get; set; } = new();
    public Activity Body { get; set; }
    protected override void Execute(NativeActivityContext context) => context.ScheduleActivity(Body, (context, _)=>
    {
        TResult value;
        using (context.InheritVariables())
        {
            value = In.Get(context);
        }
        context.SetValue(Result, value);
    });
}
public class BlockingActivity : NativeActivity
{
    public BlockingActivity()
    {
    }

    public BlockingActivity(string displayName)
    {
        this.DisplayName = displayName;
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // nothing to do
    }

    protected override void Execute(NativeActivityContext context)
    {
        context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        // No-op
    }

    protected override bool CanInduceIdle => true;
}
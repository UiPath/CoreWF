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
using WorkflowApplicationTestExtensions;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Activities.Validation;
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
        stringsLocation.Value ??= [];
        stringsLocation.Value.Add(Item);
    }
}

public class SplitAndMergeTests
{
    private List<string> Results { get; set; }
    private readonly Variable<List<string>> _stringsVariable = new("strings", c => new());

    private AddStringActivity AddString(string stringToAdd)
    {
        var act = new AddStringActivity() { Item = stringToAdd, DisplayName = stringToAdd };
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
        var app = new WorkflowApplication(root);
        return Results = (List<string>) app.RunUntilCompletion().Outputs["Result"];
    }

    [Fact]
    public void RoundTrip_xaml()
    {
        var merge = AddString("stop").MergeAll();
        var split = new FlowSplit().AddBranches(AddString("branch1").FlowTo(merge), AddString("branch2").FlowTo(merge));
        var flowchart = new Flowchart { StartNode = split };
        var roundTrip = XamlRoundTrip(flowchart);
        ExecuteFlowchart(roundTrip);
        Results.ShouldBe([ "branch1", "branch2", "stop" ]);
        
        T XamlRoundTrip<T>(T obj)
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\testFlowchart.xaml";
            File.Delete(filePath);
            using (var stream = File.OpenWrite(filePath))
            {
                XamlTestDriver.Serialize(obj, stream);
            }
            var asm = GetType().Assembly;
            var sampleStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.TestXamls.{"testFlowchart"}.xaml");
            using var sampleReader = new StreamReader(sampleStream);
            var sampleXaml = sampleReader.ReadToEnd();
            var currentXaml = File.ReadAllText(filePath);
            currentXaml.ShouldBe(sampleXaml);

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
        var merge = AddString(stopString).MergeAll();

        var split = new FlowSplit()
            .AddBranches(
                AddString(branch1Str).FlowTo(merge),
                AddString(branch2Str).FlowTo(merge)
                );

        ExecuteFlowchart(split);
        Results.ShouldBe(new() { branch1Str, branch2Str, stopString });
    }

    [Fact]
    public void Should_join_branches_with_inner_split()
    {
        ///                      |--A--|
        ///        |---A---Split<      Merge--|
        ///  Split<              |--A--|      Merge---Stop
        ///        |___A______________________|

        var outerMerge = AddString("stop").MergeAll();
        var innerMerge = AddString("innerMerged").MergeAll();
        var innerSplit = new FlowSplit()
            .AddBranches(
                AddString("branch1Inner").FlowTo(innerMerge),
                AddString("branch2Inner").FlowTo(innerMerge)
                );
        innerMerge.FlowTo(outerMerge);
        var outerSplit = new FlowSplit()
            .AddBranches(
                AddString("branch1Outer").FlowTo(innerSplit),
                AddString("branch2Outer").Step().FlowTo(outerMerge)
                );

        ExecuteFlowchart(outerSplit);
        Results.ShouldBe(["branch1Outer", "branch1Inner", "branch2Inner", "innerMerged", "branch2Outer", "stop"]);
    }

    [Fact]
    public void Shared_merge_fails()
    {
        var sharedMerge = AddString("stop").MergeAll();
        var innerSplit = new FlowSplit()
            .AddBranches(
                AddString("branch1Inner").FlowTo(sharedMerge),
                AddString("branch2Inner").FlowTo(sharedMerge)
                );
        sharedMerge.FlowTo(sharedMerge);
        var outerSplit = new FlowSplit()
            .AddBranches(
                AddString("branch1Outer").FlowTo(innerSplit),
                AddString("branch2Outer").Step().FlowTo(sharedMerge)
                );
        var errors = ActivityValidationServices.Validate(new Flowchart() { StartNode = outerSplit });
        errors.Errors.ShouldNotBeEmpty();
        errors.Errors.Where(e => e.SourceDetail == sharedMerge).ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_join_with_skiped_branches()
    {
        var merge = AddString("stop").MergeAll();
        var skippedBranch = AddString("executedPart")
            .FlowTo(new FlowDecision()
        {
            Condition = new LambdaValue<bool>(c => false),
            True = AddString("skippedPart").FlowTo(merge)
        });
        var split = new FlowSplit()
        {
            Branches = {
                new() {StartNode = skippedBranch },
                new() {StartNode = AddString("executedBranch").FlowTo(merge) }
            }
        };

        ExecuteFlowchart(split);
        Results.ShouldBe(["executedPart", "executedBranch", "stop" ]);
    }

    [Fact]
    public void MergeAny_continues_after_first_and_cancels_the_rest()
    {
        var branch1Str = "branch1";
        var branch2Str = "branch2";
        var stopString = "stop";

        var merge = new FlowMergeAny() { Next = AddString(stopString).Step() };
        var split = new FlowSplit()
            .AddBranches(
                AddString(branch1Str).FlowTo(AddString(branch1Str)).FlowTo(merge),
                new BlockingActivity("whatever").FlowTo(AddString(branch2Str)).FlowTo(merge)
                );

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
            var merge = AddString(stopString).MergeAll();
            var split = new FlowSplit().AddBranches(
                AddString(branch1Str).FlowTo(new FlowStep()).FlowTo(merge),
                AddString(branch2Str).FlowTo(new FlowStep()).FlowTo(merge),
                blockingActivity.FlowTo(blockingContinuation).FlowTo(new FlowStep()).FlowTo(merge));
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

    private Variable<DateTime> _startExecute = new(); 
    public BlockingActivity()
    {
    }

    public BlockingActivity(string displayName)
    {
        this.DisplayName = displayName;
    }

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.AddImplementationVariable(_startExecute);
        // nothing to do
    }

    protected override void Execute(NativeActivityContext context)
    {
        _startExecute.Set(context, DateTime.Now);
        context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    protected override void Cancel(NativeActivityContext context)
    {
        base.Cancel(context);
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
        if (context.CurrentInstance.IsCancellationRequested)
            return;
        var startExecutionTimestamp = _startExecute.Get(context);
        if (startExecutionTimestamp + TimeSpan.FromSeconds(1) > DateTime.Now)
            return;
        Thread.Sleep(200);
        context.CreateBookmark(this.DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    protected override bool CanInduceIdle => true;
}
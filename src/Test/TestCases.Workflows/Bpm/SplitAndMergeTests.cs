using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Xunit;
using System.Linq;
using System.Activities.Expressions;
using TestObjects.XamlTestDriver;
using System.IO;
using System;
using WorkflowApplicationTestExtensions;
using System.Activities.Validation;
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
    public void Should_not_execute_branches_without_merge()
    {
        var split = new FlowSplit().AddBranches(AddString("branch1"), AddString("branch2"));
        var errors = ActivityValidationServices.Validate(new Flowchart() { StartNode = split });
        errors.Errors.ShouldNotBeEmpty();
        errors.Errors.Where(e => e.SourceDetail == split).ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_join_branches()
    {
        var merge = AddString("stop").MergeAll();

        var split = new FlowSplit()
            .AddBranches(
                AddString("branch1").FlowTo(merge),
                AddString("branch2").FlowTo(merge)
                );

        ExecuteFlowchart(split);
        Results.ShouldBe(["branch1", "branch2", "stop"]);
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
        Results.ShouldBe(["branch1Outer", "branch2Outer", "branch1Inner", "branch2Inner", "innerMerged", "stop"]);
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
    public void MergeAny_continues_after_first_and_cancels_the_rest_even_unconnected()
    {
        var merge = new FlowMergeAny() { Next = AddString("stop").Step() };
        var split = new FlowSplit()
            .AddBranches(
                AddString("branch1").FlowTo(AddString("branch1")).FlowTo(merge),
                new Delay() { Duration = new InArgument<TimeSpan>(TimeSpan.FromSeconds(5))}.FlowTo(AddString("delayedBranch"))
                );

        ExecuteFlowchart(split);
        Results.ShouldBe(["branch1", "branch1", "stop"]);
    }
    [Fact]
    public void MergeAny_continues_after_first_and_cancels_the_rest()
    {
        var merge = new FlowMergeAny() { Next = AddString("stop").Step() };
        var split = new FlowSplit()
            .AddBranches(
                AddString("branch1").FlowTo(AddString("branch1")).FlowTo(merge),
                new Delay() { Duration = new InArgument<TimeSpan>(TimeSpan.FromSeconds(5)) }.FlowTo(AddString("delayedBranch")).FlowTo(merge)
                );

        ExecuteFlowchart(split);
        Results.ShouldBe(["branch1", "branch1", "stop"]);
    }

    [Fact]
    public void Should_persist_join()
    {
        var merge = AddString("stop").MergeAll();
        var blockingActivity = new BlockingActivity()
            .FlowTo(AddString("blockingContinuation"))
            .FlowTo(new FlowStep());
        var split = new FlowSplit().AddBranches(
            AddString("branch1").FlowTo(new FlowStep()).FlowTo(merge),
            AddString("branch2").FlowTo(new FlowStep()).FlowTo(merge),
            blockingActivity.FlowTo(merge));

        ExecuteFlowchart(split);
        Results.ShouldBe([ "branch1", "branch2", "blockingContinuation", "stop" ]);
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
        DisplayName = "blocking";
    }


    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        // nothing to do
    }

    protected override void Execute(NativeActivityContext context)
    {
        context.CreateBookmark(DisplayName, new BookmarkCallback(OnBookmarkResumed));
    }

    protected override void Cancel(NativeActivityContext context)
    {
        base.Cancel(context);
    }

    private void OnBookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
    {
    }

    protected override bool CanInduceIdle => true;
}
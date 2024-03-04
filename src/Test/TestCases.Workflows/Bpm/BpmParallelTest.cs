using Shouldly;
using System.Activities;
using System.Activities.Validation;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Threading;
using System.Activities.Bpm;
using Xunit;
using System.Linq;

namespace TestCases.Activitiess.Bpm;
public class BpmParallelTest
{
    [Fact]
    public void Should_execute_branches()
    {
        var list = new List<string>();
        Variable<ICollection<string>> strings = new("strings", c => list);
        AddToCollection<string> branch1 = new() { Collection = strings, Item = "branch1" };
        AddToCollection<string> branch2 = new() { Collection = strings, Item = "branch2" };

        var parallel = new FlowParallel().PointTo(branch1, branch2);
        var flowchart = new Flowchart { StartNode = parallel, Variables = { strings } };

        WorkflowInvoker.Invoke(flowchart);
        list.ShouldBe(new() { "branch2", "branch1" });
    }

    [Fact]
    public void Should_join_branches()
    {
        var list = new List<string>();
        Variable<ICollection<string>> strings = new("strings", c => list);
        AddToCollection<string> branch1 = new() { Collection = strings, Item = "branch1" };
        AddToCollection<string> branch2 = new() { Collection = strings, Item = "branch2" };
        AddToCollection<string> item3 = new() { Collection = strings, Item = "item3" };

        var join = new FlowJoin().PointTo(item3);
        var parallel = new FlowParallel()
            .PointTo(
                branch1.PointTo(join),
                branch2.PointTo(join)
                );

        var flowchart = new Flowchart { StartNode = parallel };
        var root = new Sequence() { Variables = { strings }, Activities = { flowchart } };
        WorkflowInvoker.Invoke(root);

        list.ShouldBe(new() { "branch2", "branch1", "item3" });
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Should_persist_join_legacy_fails(bool resumeWithLegacy)
    {
        var action = () => Should_persist_join(startWithLegacy: true, resumeWithLegacy: resumeWithLegacy);
        action.ShouldThrow<ShouldAssertException>();
    }

    [InlineData(false)]
    [InlineData(true)]
    [Theory]
    public void Should_persist_join(bool resumeWithLegacy, bool startWithLegacy = false)
    {
        var root = ParallelActivities(startWithLegacy);
        var store = new JsonFileInstanceStore.FileInstanceStore(".\\~");
        WorkflowApplication app = new(root) { InstanceStore = store };
        app.Run();
        var appId = app.Id;
        Thread.Sleep(100);
        app.Unload();
        root = ParallelActivities(resumeWithLegacy);
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
        resumedApp.ResumeBookmark("blocking", null);
        manualResetEvent.WaitOne();
        completedArgs.TerminationException.ShouldBeNull();
        ((ICollection<string>)completedArgs.Outputs["Result"]).ShouldBe(new[] { "branch2", "branch1", "blockingContinuation", "stop" });
    }

    private static ActivityWithResult<ICollection<string>> ParallelActivities(bool useLegacyFlowchart)
    {
        Variable<ICollection<string>> strings = new("strings", c => new List<string>());
        AddToCollection<string> stop = new() { Collection = strings, Item = "stop" };

        AddToCollection<string> branch1 = new() { Collection = strings, Item = "branch1" };
        AddToCollection<string> branch2 = new() { Collection = strings, Item = "branch2" };
        AddToCollection<string> blockingContinuation = new() { Collection = strings, Item = "blockingContinuation" };
        var blockingActivity = new BlockingActivity("blocking");

        FlowParallel parallel = new();
        FlowJoin join = new();
        parallel.PointTo(
            branch1.PointTo(join), 
            branch2.PointTo(join),
            blockingActivity.PointTo(blockingContinuation)
                .PointTo(join));
        join.PointTo(stop);

        var flowchart = new Flowchart { StartNode = parallel };
        flowchart.IsLegacyFlowchart = useLegacyFlowchart;

        return new ActivityWithResult<ICollection<string>>() { In = strings, Body = flowchart };
    }
}

public static class WorkflowExtensions
{
    public static FlowParallel PointTo(this FlowParallel parallel, params Activity[] nodes)
    {
        parallel.Branches.AddRange(nodes.Select(n => new FlowStep() { Action = n }).ToList());
        return parallel;
    }
    public static FlowParallel PointTo(this FlowParallel parallel, params FlowNode[] nodes)
    {
        parallel.Branches.AddRange(nodes);
        return parallel;
    }
    public static FlowStep PointTo(this Activity predeccessor, FlowNode successor)
    {
        return new FlowStep { Action = predeccessor }.PointTo(successor);
    }
    public static FlowStep PointTo(this Activity predeccessor, Activity successor)
    {
        return new FlowStep { Action = predeccessor }.PointTo(successor);
    }
    public static T PointTo<T>(this T predeccessor, Activity successor)
        where T : FlowNode
    {
        return predeccessor.PointTo(new FlowStep { Action = successor });
    }
    public static T PointTo<T>(this T predeccessor, FlowNode successor)
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
        }
        return predeccessor;
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

    protected override bool CanInduceIdle
    {
        get
        {
            return true;
        }
    }
}
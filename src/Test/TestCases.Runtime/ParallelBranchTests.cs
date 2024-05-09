using Shouldly;
using System;
using System.Activities;
using System.Activities.ParallelTracking;
using System.Linq;
using WorkflowApplicationTestExtensions;
using WorkflowApplicationTestExtensions.Persistence;
using Xunit;

namespace TestCases.Runtime;


public class ParallelBranchTestsJson : ParallelBranchTests
{
    protected override IWorkflowSerializer Serializer => new JsonWorkflowSerializer();
}

public class ParallelBranchTests
{
    public class TestCodeActivity(Action<CodeActivityContext> onExecute) : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            onExecute(context);
        }
    }

    protected virtual IWorkflowSerializer Serializer => new DataContractWorkflowSerializer();
    string _parentLevel = default;
    readonly string _noBranch = default;

    private void Run(params Action<CodeActivityContext>[] onExecute)
    {
        new WorkflowApplication(new SuspendingWrapper(onExecute.Select(c => new TestCodeActivity(c))))
        {
            InstanceStore = new MemoryInstanceStore(Serializer)
        }.RunUntilCompletion();
    }

    private void RunWithParent(params Action<CodeActivityContext>[] onExecute)
    {
        Run([new(SetParent), .. onExecute]);
        void SetParent(CodeActivityContext context)
        {
            var parent = context.CurrentInstance.Parent;
            parent.GetCurrentParallelBranchId().ShouldBe(_noBranch);
            parent.MarkNewParallelBranch();
            _parentLevel = parent.GetCurrentParallelBranchId();
        }
    }

    [Fact]
    public void Push()
    {
        var level1 = default(string).PushNewBranch();
        var level2 = level1.PushNewBranch();
        var level3 = level2.PushNewBranch();
        level2.ShouldStartWith(level1);
        level3.ShouldStartWith(level2);
        var l3Splits = level3.Split('.');
        l3Splits.Length.ShouldBe(3);
        l3Splits.First().ShouldBe(level1);
    }

    [Fact]
    public void SetToNullWhenNull() => Run(
    context =>
    {
        context.CurrentInstance.SetCurrentParallelBranchId(_noBranch);
        context.CurrentInstance.SetCurrentParallelBranchId(_noBranch);
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldBeNull();
    });

    [Fact]
    public void SetToNullWhenNotNull() => Run(
    context =>
    {   
        context.CurrentInstance.SetCurrentParallelBranchId(_noBranch);
        context.CurrentInstance.SetCurrentParallelBranchId(_noBranch.PushNewBranch());
        context.CurrentInstance.SetCurrentParallelBranchId(_noBranch);
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldBeNull();
    });


    [Fact]
    public void GetCurrentParallelBranch_InheritsFromParent() => RunWithParent(
    context =>
    {
        var branchId = context.GetCurrentParallelBranchId();
        var currentBranch = context.CurrentInstance.GetCurrentParallelBranchId();
        currentBranch.ShouldBe(branchId);
        currentBranch.ShouldBe(_parentLevel);
    });

    [Fact]
    public void PushAndSetParallelBranch() => RunWithParent(
    context =>
    {
        var pushLevelOnSchedule = _parentLevel.PushNewBranch();
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranchId(pushLevelOnSchedule);
        var getPushedLevel = scheduledInstance.GetCurrentParallelBranchId();
        getPushedLevel.ShouldBe(pushLevelOnSchedule);
        scheduledInstance.GetCurrentParallelBranchId().ShouldBe(pushLevelOnSchedule);
    });

    [Fact]
    public void UnparentedPushFails() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch());
        Should.Throw<ArgumentException>(() => instance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch().PushNewBranch()));
    });

    [Fact]
    public void DoublePush() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch().PushNewBranch());
    });

    [Fact]
    public void DoublePop() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch().PushNewBranch());
        instance.SetCurrentParallelBranchId(_parentLevel);
    });

    [Fact]
    public void UnparentedPopFails() => RunWithParent(
    context =>
    {
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch().PushNewBranch());
        Should.Throw<ArgumentException>(() => scheduledInstance.SetCurrentParallelBranchId(_parentLevel.PushNewBranch()));
    });

    [Fact]
    public void ParallelBranchDoesNotLeakToSiblings() => RunWithParent(
    context =>
    {
        context.CurrentInstance.SetCurrentParallelBranchId(context.CurrentInstance.GetCurrentParallelBranchId().PushNewBranch());
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldNotBe(_parentLevel);
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldStartWith(_parentLevel);
    },
    context =>
    {
        var readLevel = context.CurrentInstance.GetCurrentParallelBranchId();
        var branchId = context.GetCurrentParallelBranchId();
        readLevel.ShouldBe(_parentLevel);
        branchId.ShouldBe(_parentLevel);
    });
}

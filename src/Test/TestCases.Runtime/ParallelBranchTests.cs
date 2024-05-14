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
        Run([SetParent, .. onExecute]);
        void SetParent(CodeActivityContext context)
        {
            var parent = context.CurrentInstance.Parent;
            parent.GetCurrentParallelBranchId().ShouldBe(_noBranch);
            parent.MarkNewParallelBranch();
            _parentLevel = parent.GetCurrentParallelBranchId();
        }
    }

    [Fact]
    public void GenerateChildParallelBranchId()
    {
        var level1 = ParallelTrackingExtensions.GenerateChildParallelBranchId(null);
        var level2 = ParallelTrackingExtensions.GenerateChildParallelBranchId(level1);
        var level3 = ParallelTrackingExtensions.GenerateChildParallelBranchId(level2);
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
        context.CurrentInstance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_noBranch));
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
    public void Generate_Set_And_Get_BranchId() => RunWithParent(
    context =>
    {
        var pushLevelOnSchedule = ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel);
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranchId(pushLevelOnSchedule);
        var getPushedLevel = scheduledInstance.GetCurrentParallelBranchId();
        getPushedLevel.ShouldBe(pushLevelOnSchedule);
        scheduledInstance.GetCurrentParallelBranchId().ShouldBe(pushLevelOnSchedule);
    });

    [Fact]
    public void Unrelated_Set_Fails() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel));
        Should.Throw<ArgumentException>(() => instance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel))));
    });

    [Fact]
    public void Set_Descendant_level2() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
    });

    [Fact]
    public void Restore_Ancestor_level2() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
        instance.SetCurrentParallelBranchId(_parentLevel);
    });

    [Fact]
    public void Unrelated_Restore_Fails() => RunWithParent(
    context =>
    {
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
        Should.Throw<ArgumentException>(() => scheduledInstance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
    });

    [Fact]
    public void ParallelBranchDoesNotLeakToSiblings() => RunWithParent(
    context =>
    {
        context.CurrentInstance.SetCurrentParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(context.CurrentInstance.GetCurrentParallelBranchId()));
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

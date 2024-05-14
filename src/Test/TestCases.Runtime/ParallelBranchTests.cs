using Shouldly;
using System;
using System.Activities;
using System.Activities.ParallelTracking;
using System.Activities.Statements;
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
    => Run(Suspendable(onExecute));
    private SuspendingWrapper Suspendable(params Action<CodeActivityContext>[] onExecute)
    => new(onExecute.Select(c => new TestCodeActivity(c)));

    private void Run(Activity activity)
    => new WorkflowApplication(activity)
    {
        InstanceStore = new MemoryInstanceStore(Serializer)
    }.RunUntilCompletion();

    private void RunWithParent(params Action<CodeActivityContext>[] onExecute)
    {
        Run(new Parallel() { Branches = { Suspendable([SaveParent, .. onExecute]) } });
        void SaveParent(CodeActivityContext context)
        {
            _parentLevel = context.CurrentInstance.Parent.GetParallelBranchId();
            _parentLevel.ShouldNotBeNullOrEmpty();
            context.CurrentInstance.GetParallelBranchId().ShouldBe(_parentLevel);
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
        context.CurrentInstance.SetParallelBranchId(_noBranch);
        context.CurrentInstance.SetParallelBranchId(_noBranch);
        context.CurrentInstance.GetParallelBranchId().ShouldBeNull();
    });

    [Fact]
    public void SetToNullWhenNotNull() => Run(
    context =>
    {   
        context.CurrentInstance.SetParallelBranchId(_noBranch);
        context.CurrentInstance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_noBranch));
        context.CurrentInstance.SetParallelBranchId(_noBranch);
        context.CurrentInstance.GetParallelBranchId().ShouldBeNull();
    });


    [Fact]
    public void GetCurrentParallelBranch_InheritsFromParent() => RunWithParent(
    context =>
    {
        var branchId = context.GetCurrentParallelBranchId();
        var currentBranch = context.CurrentInstance.GetParallelBranchId();
        currentBranch.ShouldBe(branchId);
        currentBranch.ShouldBe(_parentLevel);
    });

    [Fact]
    public void Generate_Set_And_Get_BranchId() => RunWithParent(
    context =>
    {
        var childBranchId = ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel);
        var instance = context.CurrentInstance;
        instance.SetParallelBranchId(childBranchId);
        var getPushedLevel = instance.GetParallelBranchId();
        getPushedLevel.ShouldBe(childBranchId);
        instance.GetParallelBranchId().ShouldBe(childBranchId);
    });

    [Fact]
    public void Unrelated_Set_Fails() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel));
        Should.Throw<ArgumentException>(() => instance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel))));
    });

    [Fact]
    public void Set_Descendant() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        var secondLevelDecendentBranch = ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel));
        instance.SetParallelBranchId(secondLevelDecendentBranch);
        instance.GetParallelBranchId().ShouldBe(secondLevelDecendentBranch);
    });

    [Fact]
    public void Restore_Ancestor() => RunWithParent(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
        instance.SetParallelBranchId(_parentLevel);
        instance.GetParallelBranchId().ShouldBe(_parentLevel);
    });

    [Fact]
    public void Unrelated_Restore_Fails() => RunWithParent(
    context =>
    {
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
        Should.Throw<ArgumentException>(() => scheduledInstance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(_parentLevel)));
    });

    [Fact]
    public void ParallelBranchDoesNotLeakToSiblings() => RunWithParent(
    context =>
    {
        context.CurrentInstance.SetParallelBranchId(ParallelTrackingExtensions.GenerateChildParallelBranchId(context.CurrentInstance.GetParallelBranchId()));
        context.CurrentInstance.GetParallelBranchId().ShouldNotBe(_parentLevel);
        context.CurrentInstance.GetParallelBranchId().ShouldStartWith(_parentLevel);
    },
    context =>
    {
        var readLevel = context.CurrentInstance.GetParallelBranchId();
        var branchId = context.GetCurrentParallelBranchId();
        readLevel.ShouldBe(_parentLevel);
        branchId.ShouldBe(_parentLevel);
    });
}

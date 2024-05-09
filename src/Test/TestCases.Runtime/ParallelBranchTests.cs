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
    ParallelBranch parentLevel = default;
    ParallelBranch noBranch = default;
    private void Run(params Action<CodeActivityContext>[] onExecute)
    {
        var execs = new Action<CodeActivityContext>[] { new(SetParent) }
            .Concat(onExecute);
        new WorkflowApplication(new SuspendingWrapper(execs.Select(c => new TestCodeActivity(c)))) 
        {
            InstanceStore = new MemoryInstanceStore(Serializer)
        }
            .RunUntilCompletion();

        void SetParent(CodeActivityContext context)
        {
            
            var parent = context.CurrentInstance.Parent;
            noBranch = parent.GetCurrentParallelBranch();
            parent.MarkNewParallelBranch();
            parentLevel = parent.GetCurrentParallelBranch();
        }
    }

    [Fact]
    public void Push()
    {
        var level1 = new ParallelBranch().Push();
        var level2 = level1.Push();
        var level3 = level2.Push();
        level2.BranchesStackString.ShouldStartWith(level1.BranchesStackString);
        level3.BranchesStackString.ShouldStartWith(level2.BranchesStackString);
        var l3Splits = level3.BranchesStackString.Split('.');
        l3Splits.Length.ShouldBe(3);
        l3Splits.First().ShouldBe(level1.BranchesStackString);
    }

    [Fact]
    public void SetToNullWhenNull() => Run(
    context =>
    {
        context.CurrentInstance.SetCurrentParallelBranch(noBranch);
        context.CurrentInstance.SetCurrentParallelBranch(noBranch);
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldBeNull();
    });

    [Fact]
    public void SetToNullWhenNotNull() => Run(
    context =>
    {
        context.CurrentInstance.SetCurrentParallelBranch(noBranch);
        context.CurrentInstance.SetCurrentParallelBranch(noBranch.Push());
        context.CurrentInstance.SetCurrentParallelBranch(noBranch);
        context.CurrentInstance.GetCurrentParallelBranchId().ShouldBeNull();
    });


    [Fact]
    public void ParallelBranchPersistence() => Run(
    context =>
    {
        PersistParallelBranch();

        void PersistParallelBranch()
        {
            new ExecutionProperties(null, context.CurrentInstance.Parent, context.CurrentInstance.Parent.PropertyManager)
            .Add("localParallelBranch", parentLevel, skipValidations: true, onlyVisibleToPublicChildren: false);
        }
    },
    context =>
    {
        var persistedParent = GetPersistedParallelBranch();
        var branchId = context.GetCurrentParallelBranchId();
        persistedParent.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
        branchId.ShouldBe(persistedParent.BranchesStackString);

        ParallelBranch GetPersistedParallelBranch()
        {
            return (ParallelBranch)new ExecutionProperties(null, context.CurrentInstance.Parent, context.CurrentInstance.Parent.PropertyManager)
            .Find("localParallelBranch");
        }
    });

    [Fact]
    public void GetCurrentParallelBranch_InheritsFromParent() => Run(
    context =>
    {
        var branchId = context.GetCurrentParallelBranchId();
        var currentBranch = context.CurrentInstance.GetCurrentParallelBranch();
        currentBranch.BranchesStackString.ShouldBe(branchId);
        currentBranch.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
    });

    [Fact]
    public void PushAndSetParallelBranch() => Run(
    context =>
    {
        var pushLevelOnSchedule = parentLevel.Push();
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranch(pushLevelOnSchedule);
        var getPushedLevel = scheduledInstance.GetCurrentParallelBranch();
        getPushedLevel.BranchesStackString.ShouldBe(pushLevelOnSchedule.BranchesStackString);
        scheduledInstance.GetCurrentParallelBranchId().ShouldBe(pushLevelOnSchedule.BranchesStackString);
    });

    [Fact]
    public void UnparentedPushFails() => Run(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranch(parentLevel.Push());
        Should.Throw<ArgumentException>(() => instance.SetCurrentParallelBranch(parentLevel.Push().Push()));
    });

    [Fact]
    public void DoublePush() => Run(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranch(parentLevel.Push().Push());
    });

    [Fact]
    public void DoublePop() => Run(
    context =>
    {
        var instance = context.CurrentInstance;
        instance.SetCurrentParallelBranch(parentLevel.Push().Push());
        instance.SetCurrentParallelBranch(parentLevel);
    });

    [Fact]
    public void UnparentedPopFails() => Run(
    context =>
    {
        var scheduledInstance = context.CurrentInstance;
        scheduledInstance.SetCurrentParallelBranch(parentLevel.Push().Push());
        Should.Throw<ArgumentException>(() => scheduledInstance.SetCurrentParallelBranch(parentLevel.Push()));
    });

    [Fact]
    public void ParallelBranchDoesNotLeakToSiblings() => Run(
    context =>
    {
        var readLevel = context.CurrentInstance.GetCurrentParallelBranch();
        var branchId = context.GetCurrentParallelBranchId();
        readLevel.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
        branchId.ShouldBe(parentLevel.BranchesStackString);
    });
}

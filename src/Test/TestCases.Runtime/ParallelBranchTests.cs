using Shouldly;
using System;
using System.Activities;
using System.Activities.ParallelTracking;
using System.Linq;
using WorkflowApplicationTestExtensions;
using Xunit;

namespace TestCases.Runtime;

public class ParallelBranchTests
{
    public class TestCodeActivity(Action<CodeActivityContext> onExecute) : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            onExecute(context);
        }
    }
    private static void Run(params Action<CodeActivityContext>[] onExecute)
    => new WorkflowApplication(new SuspendingWrapper(onExecute.Select(c => new TestCodeActivity(c)).ToArray()))
        .RunUntilCompletion();

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
    public void GetAndSet()
    {

        static void SaveParent(ActivityInstance instance, ParallelBranch parentLevel)
        {
            new ExecutionProperties(null, instance, instance.PropertyManager)
            .Add("localParallelBranch", parentLevel, skipValidations: true, onlyVisibleToPublicChildren: false);
        }
        static ParallelBranch GetParent(ActivityInstance instance)
        {
            return (ParallelBranch)new ExecutionProperties(null, instance, instance.PropertyManager)
            .Find("localParallelBranch");
        }

        ParallelBranch parentLevel = default;
        Run(SetParent,
            ParallelBranchPersistence,
            GetCurrentParallelBranch_InheritsFromParent,
            PushAndSetParallelBranch,
            ParallelBranchDoesNotLeakToSiblings
        );

        void SetParent(CodeActivityContext context)
        {
            var parent = context.CurrentInstance.Parent;
            parent.MarkNewParallelBranch();
            parentLevel = parent.GetCurrentParallelBranch();
            SaveParent(parent, parentLevel);
        }
        void ParallelBranchPersistence(CodeActivityContext context)
        {
            var persistedParent = GetParent(context.CurrentInstance);
            var branchId = context.GetCurrentParallelBranchId();
            persistedParent.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
            branchId.ShouldBe(persistedParent.BranchesStackString);
            persistedParent.InstanceId.ShouldBe(parentLevel.InstanceId);
            persistedParent.InstanceId.ShouldBe(context.CurrentInstance.Parent.Id);
        }
        void GetCurrentParallelBranch_InheritsFromParent(CodeActivityContext context)
        {
            var branchId = context.GetCurrentParallelBranchId();
            var currentBranch = context.CurrentInstance.GetCurrentParallelBranch();
            currentBranch.BranchesStackString.ShouldBe(branchId);
            currentBranch.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
        }
        void PushAndSetParallelBranch(CodeActivityContext context)
        {
            var pushLevelOnSchedule = parentLevel.Push();
            var scheduledInstance = context.CurrentInstance;
            scheduledInstance.SetCurrentParallelBranch(pushLevelOnSchedule);
            var getPushedLevel = scheduledInstance.GetCurrentParallelBranch();
            getPushedLevel.BranchesStackString.ShouldBe(pushLevelOnSchedule.BranchesStackString);
            scheduledInstance.GetCurrentParallelBranchId().ShouldBe(pushLevelOnSchedule.BranchesStackString);
        }
        void ParallelBranchDoesNotLeakToSiblings(CodeActivityContext context)
        {
            var readLevel = context.CurrentInstance.GetCurrentParallelBranch();
            var branchId = context.GetCurrentParallelBranchId();
            readLevel.BranchesStackString.ShouldBe(parentLevel.BranchesStackString);
            branchId.ShouldBe(parentLevel.BranchesStackString);
        }
    }
}


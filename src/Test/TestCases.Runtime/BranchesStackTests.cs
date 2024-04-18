using Shouldly;
using System;
using System.Activities;
using System.Activities.ParallelTracking;
using System.Activities.Statements;
using System.Linq;
using WorkflowApplicationTestExtensions;
using Xunit;

namespace TestCases.Runtime;

public class BranchesStackTests
{
    private static void Run(Activity activity) =>
        new WorkflowApplication(activity).RunUntilCompletion();
    public class TestCodeActivity(Action<CodeActivityContext> onExecute) : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            onExecute(context);
        }
    }
    private static Activity Test(Action<CodeActivityContext> onExecute)
    => new SuspendingWrapper(new TestCodeActivity(onExecute));

    [Fact]
    public void Push()
    {
        var level1 = new BranchesStack().Push();
        var level2 = level1.Push();
        var level3 = level2.Push(); 
        level2.BranchesStackString.ShouldStartWith(level1.BranchesStackString);
        level3.BranchesStackString.ShouldStartWith(level2.BranchesStackString);
        var l3Splits = level3.BranchesStackString.Split('.');
        l3Splits.Length.ShouldBe(3);
        l3Splits.First().ShouldBe(level1.BranchesStackString);
    }

    [Fact]
    public void ReadAndSave()
    {
        BranchesStack level1 = default;
        BranchesStack level2 = default;
        Run(new Sequence
        {
            Activities = {
            Test(context =>
            {
                var sequenceActivityInstance = context.CurrentInstance.Parent.Parent;
                sequenceActivityInstance.MarkNewParallelBranch();

            }),
            Test(context =>
            {
                var extensionLevel1 = context.GetCurrentParallelBranchId();
                level1 = BranchesStack.ReadFrom(context.CurrentInstance);
                level1.BranchesStackString.ShouldBe(extensionLevel1);
            }),
            Test(context =>
            {
                level2 = level1.Push();
                level2.SaveTo(context.CurrentInstance);
                var readLevel2 = BranchesStack.ReadFrom(context.CurrentInstance);
                readLevel2.ShouldBe(level2);
                context.CurrentInstance.GetCurrentParallelBranchId().ShouldBe(level2.BranchesStackString);
            }),
            Test(context =>
            {
                var readLevel = BranchesStack.ReadFrom(context.CurrentInstance);
                var extensionLevel = context.GetCurrentParallelBranchId();
                readLevel.BranchesStackString.ShouldBe(level1.BranchesStackString);
                extensionLevel.ShouldBe(level1.BranchesStackString);
            }),
        }
        });
    }
}


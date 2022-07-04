using Shouldly;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Xunit;

namespace TestCases.Activities;
public class ScheduleActivity
{
    class ParentActivity : NativeActivity
    {
        public Variable<int> Integer { get; } = new();
        public Assign<int> Assign { get; }
        public ParentActivity() => Assign = new() { To = Integer, Value = 1 };
        public Dictionary<string, object> OutArgs { get; set; }
        protected override void Execute(NativeActivityContext context) =>
            context.ScheduleActivity(Assign, (_, instance) => OutArgs = instance.GetOutputs(), null, new Dictionary<string, object> { ["Value"] = 42 });
    }
    [Fact]
    public void Should_expose_arguments()
    {
        var activity = new ParentActivity();
        WorkflowInvoker.Invoke(activity);
        activity.OutArgs.ShouldBe(new() { ["To"] = 42 });
    }
}
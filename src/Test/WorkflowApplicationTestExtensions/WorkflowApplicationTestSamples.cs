using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Threading.Tasks;
using Xunit;

namespace WorkflowApplicationTestExtensions
{
    public class WorkflowApplicationTestSamples
    {
        [Fact]
        public void RunUntilCompletion_Outputs()
        {
            var app = new WorkflowApplication(new DynamicActivity
            {
                Properties = { new DynamicActivityProperty { Name = "result", Type = typeof(OutArgument<string>) } },
                Implementation = () => new Assign<string> { To = new Reference<string>("result"), Value = "value" }
            });
            app.RunUntilCompletion().Outputs["result"].ShouldBe("value");
        }

        [Fact]
        public void RunUntilCompletion_Faulted()
        {
            var app = new WorkflowApplication(new Throw { Exception = new InArgument<Exception>(_ => new ArgumentException()) });
            Should.Throw<ArgumentException>(app.RunUntilCompletion);
        }

        [Fact]
        public void RunUntilCompletion_Aborted()
        {
            var app = new WorkflowApplication(new Delay { Duration = TimeSpan.MaxValue });
            Task.Delay(10).ContinueWith(_ => app.Abort());
            Should.Throw<WorkflowApplicationAbortedException>(app.RunUntilCompletion);
        }

        [Fact]
        public void RunUntilCompletion_AutomaticPersistence()
        {
            var app = new WorkflowApplication(new SuspendingWrapper
            {
                Activities =
                {
                    new WriteLine(),
                    new NoPersistAsyncActivity(),
                    new WriteLine()
                }
            });
            var result = app.RunUntilCompletion();
            result.PersistenceCount.ShouldBe(4);
        }
    }
}

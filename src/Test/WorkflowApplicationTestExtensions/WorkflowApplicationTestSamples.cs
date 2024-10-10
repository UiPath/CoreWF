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
            Should.Throw<ArgumentException>(() => app.RunUntilCompletion());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RunUntilCompletion_AutomaticPersistence(bool useJsonSerialization)
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
            var result = app.RunUntilCompletion(useJsonSerialization: useJsonSerialization);
            result.PersistenceCount.ShouldBe(4);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShouldPersistFaultContextInCatchBlock(bool useJsonSerialization)
        {
            var app = new WorkflowApplication(
                new SuspendingWrapper
                {
                    Activities =
                    {
                        new TryCatch
                        {
                            Try = new Throw
                            {
                                Exception = new InArgument<Exception>(
                                    activityContext => new ArgumentException("CustomArgumentException")
                                )
                            },
                            Catches =
                            {
                                new Catch<ArgumentException> {
                                    Action = new ActivityAction<ArgumentException>
                                    {
                                        Argument = new DelegateInArgument<ArgumentException>
                                        {
                                            Name = "exception"
                                        },
                                        Handler = new SuspendingWrapper
                                        {
                                            Activities =
                                            {
                                                new WriteLine(),
                                                new NoPersistAsyncActivity(),
                                                new WriteLine()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

            var result = app.RunUntilCompletion(useJsonSerialization: useJsonSerialization);
            result.PersistenceCount.ShouldBe(6);
        }
    }
}

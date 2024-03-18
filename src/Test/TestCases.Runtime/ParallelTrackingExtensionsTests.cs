using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UiPath.Workflow.Runtime.ParallelTracking;
using WorkflowApplicationTestExtensions;
using Xunit;

namespace TestCases.Runtime
{
    public class ParallelTrackingExtensionsTests
    {
        private readonly Expression<Func<ActivityContext, bool>> _checkIdAfterPersist = (ctx) => Guid.Parse(ctx.GetCurrentParallelBranchId()) == Guid.Empty;
        private readonly Expression<Func<ActivityContext, bool>> _checkComposedIdAfterPersist = (ctx) => ctx.GetCurrentParallelBranchId().Split('.', StringSplitOptions.None).Count() != 2;

        [Fact]
        public void IdRestoredAfterPersist_Parallel_ShouldBeGuid() => RunPersistScenario(CreateParallelActivity(_checkIdAfterPersist));

        [Fact]
        public void IdRestoredAfterPersist_ParallelForEach_ShouldBeGuid() => RunPersistScenario(CreateParallelForEachActivity(_checkIdAfterPersist));

        [Fact]
        public void IdRestoredAfterPersist_Parallel_ShouldHaveParentComponent() => RunPersistScenario(CreateImbricatedParallelActivity());

        [Fact]
        public void IdRestoredAfterPersist_ParallelForEach_ShouldHaveParentComponent() => RunPersistScenario(CreateImbricatedParallelForEachActivity());

        private Activity CreateParallelActivity(Expression<Func<ActivityContext, bool>> expression)
        {
            var parallelActivity = new Parallel();
            parallelActivity.Branches.Add(CheckConditionAfterPersist(expression));

            return parallelActivity;
        }

        private Activity CreateParallelForEachActivity(Expression<Func<ActivityContext, bool>> expression)
        {
            var parallelActivity = new ParallelForEach<int>()
            {
                Body = new ActivityAction<int>()
                {
                    Handler = CheckConditionAfterPersist(expression)
                },
                Values = new InArgument<IEnumerable<int>>(context => new List<int>() { 1 })
            };

           return parallelActivity;
        }

        private Activity CreateImbricatedParallelActivity()
        {
            var parentParallelActivity = new Parallel();

            var parallelActivity = CreateParallelActivity(_checkComposedIdAfterPersist);

            parentParallelActivity.Branches.Add(parallelActivity);

            return parentParallelActivity;
        }

        private Activity CreateImbricatedParallelForEachActivity()
        {
            var parentParallelActivity = new Parallel();
            var parallelForEachActivity = CreateParallelForEachActivity(_checkComposedIdAfterPersist);
            parentParallelActivity.Branches.Add(parallelForEachActivity);

            return parentParallelActivity;
        }

        private Activity CheckConditionAfterPersist(Expression<Func<ActivityContext, bool>> condition) => new Sequence()
        {
            Activities =
            {
                new NoPersistAsyncActivity(),
                new If(condition)
                {
                    Then = new Throw() { Exception =  new InArgument<Exception>(context => new Exception("CurrentParallelBranchId was not restored correctly."))}
                },
            }
        };

        private void RunPersistScenario(Activity activity)
        {
            var app = new WorkflowApplication(new SuspendingWrapper
            {
                Activities =
                {
                    activity
                }
            });
            var result = app.RunUntilCompletion();
            result.PersistenceCount.ShouldBe(2);
        }
    }
}

using Shouldly;
using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using UiPath.Workflow.Runtime.ParallelTracking;
using WorkflowApplicationTestExtensions;
using Xunit;

namespace TestCases.Runtime
{
    public class ParallelTrackingExtensionsTests
    {
        [Fact]
        public void ParallelActivity()
        {
            string nesting1Id = null;
            string nesting2Branch1 = null;
            string nesting2Branch2 = null;

            Run(Sequence
            (
                new ValidateParallelId(id => id.ShouldBeNull()), // branch ""
                Parallel
                (
                    Sequence
                    (
                        // branch "{Guid}"
                        new ValidateParallelId(id => ValidateId(nesting1Id = id, expectedNesting: 1)),
                        Parallel
                        (
                            // branches "{Guid}.{Guid}"
                            new ValidateParallelId(id => ValidateId(nesting2Branch1 = id, expectedNesting: 2, shouldStartWith: nesting1Id)),
                            new ValidateParallelId(id => ValidateId(nesting2Branch2 = id, expectedNesting: 2, shouldStartWith: nesting1Id))
                        ),
                        new ValidateParallelId(id => id.ShouldBe(nesting1Id))
                    )
                ),
                new ValidateParallelId(id => id.ShouldBeNull())
            ));

            nesting2Branch1.ShouldNotBe(nesting2Branch2);
        }

        [Fact]
        public void ParallelForEachActivity()
        {
            var nesting1Ids = new HashSet<string>();
            var nesting2Ids = new HashSet<string>();
            var nesting1IdsAfter = new HashSet<string>();

            Run(ParallelForEach(2, Sequence
                (
                    new ValidateParallelId(id => nesting1Ids.Add(id).ShouldBeTrue()),
                    ParallelForEach(2, new ValidateParallelId(id => nesting2Ids.Add(id).ShouldBeTrue())),
                    new ValidateParallelId(id => nesting1IdsAfter.Add(id).ShouldBeTrue())
                )));

            nesting1IdsAfter.ShouldBeEquivalentTo(nesting1Ids);

            // Nesting 2 ids should start with nesting 1 ids (2 counts for each nesting 1 id)
            var nesting2Prefixes = nesting2Ids
                .Select(id => id.Split('.')[0])
                .GroupBy(id => id);
            nesting2Prefixes.Select(group => group.Key).ShouldBe(nesting1Ids);
            nesting2Prefixes.ShouldAllBe(group => group.Count() == 2);
        }

        [Fact]
        public void PickActivity()
        {
            string trigger1Id = null;
            string trigger2Id = null;
            Run(Pick
            (
                new PickBranch
                {
                    Trigger = new ValidateParallelId(id => ValidateId(trigger1Id = id, expectedNesting: 1)),
                    // only one Action should execute; the one that executes shouldn't use a new ParallelId
                    Action = new ValidateParallelId(id => id.ShouldBeNull())
                },
                new PickBranch
                {
                    Trigger = new ValidateParallelId(id => ValidateId(trigger2Id = id, expectedNesting: 1)),
                    Action = new ValidateParallelId(id => id.ShouldBeNull())
                }
            ));
            trigger1Id.ShouldNotBe(trigger2Id);
        }

        private static void Run(Activity activity) =>
            new WorkflowApplication(activity).RunUntilCompletion();

        private static void ValidateId(string id, int expectedNesting, string shouldStartWith = null)
        {
            if (shouldStartWith is not null)
            {
                id.ShouldStartWith(shouldStartWith);
            }
            var parts = id.Split('.');
            parts.Length.ShouldBe(expectedNesting);
            parts.All(part => Guid.TryParse(part, out _)).ShouldBeTrue();
        }

        private static Sequence Sequence(params Activity[] activities)
        {
            var sequence = new Sequence();
            activities.ToList().ForEach(sequence.Activities.Add);
            return sequence;
        }

        private static Parallel Parallel(params Activity[] branches)
        {
            var parallel = new Parallel();
            branches.ToList().ForEach(parallel.Branches.Add);
            return parallel;
        }

        private static Pick Pick(params PickBranch[] branches)
        {
            var pick = new Pick();
            branches.ToList().ForEach(pick.Branches.Add);
            return pick;
        }

        private static ParallelForEach<int> ParallelForEach(int iterations, Activity body) => new()
        {
            Values = new InArgument<IEnumerable<int>>(_ => Enumerable.Range(0, iterations).ToArray()),
            Body = new ActivityAction<int> { Handler = body }
        };
    }

    public class ValidateParallelId : Activity
    {
        public ValidateParallelId(Action<string> validator) =>
            Implementation = () => new SuspendingWrapper(new ValidateParallelIdCore(validator));
    }

    public class ValidateParallelIdCore(Action<string> validator) : CodeActivity
    {
        protected override void Execute(CodeActivityContext context) =>
            validator(context.GetCurrentParallelBranchId());
    }
}

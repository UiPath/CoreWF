// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements;
using LegacyTest.Test.Common.TestObjects.Activities.Collections;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestPick : TestActivity
    {
        protected MemberCollection<TestPickBranch> branches;

        public TestPick()
        {
            this.ProductActivity = new Pick();
            this.branches = new MemberCollection<TestPickBranch>(AddBranch)
            {
                RemoveItem = RemoveBranch,
                InsertItem = InsertBranch,
                RemoveAtItem = RemoveAtBranch,
            };
        }

        public Pick ProductPick
        {
            get
            {
                return (Pick)this.ProductActivity;
            }
        }

        public MemberCollection<TestPickBranch> Branches
        {
            get
            {
                return this.branches;
            }
        }

        // Overriding GetExpectedTrace so that we use an UnorderedTrace.
        public override ExpectedTrace GetExpectedTrace()
        {
            return base.GetExpectedTraceUnordered();
        }

        internal override System.Collections.Generic.IEnumerable<TestActivity> GetChildren()
        {
            foreach (TestPickBranch branch in Branches)
            {
                if (branch.Trigger != null)
                {
                    yield return branch.Trigger;
                }

                if (branch.Action != null)
                {
                    yield return branch.Action;
                }
            }
        }

        protected void AddBranch(TestPickBranch branch)
        {
            this.ProductPick.Branches.Add(branch.ProductPickBranch);
        }

        protected bool RemoveBranch(TestPickBranch branch)
        {
            return this.ProductPick.Branches.Remove(branch.ProductPickBranch);
        }

        protected void InsertBranch(int index, TestPickBranch item)
        {
            ProductPick.Branches.Insert(index, item.ProductPickBranch);
        }

        protected void RemoveAtBranch(int index)
        {
            ProductPick.Branches.RemoveAt(index);
        }

        protected override void GetActivitySpecificTrace(LegacyTest.Test.Common.TestObjects.Utilities.Validation.TraceGroup traceGroup)
        {
            UnorderedTraces parallelTraceGroup = null;

            Outcome outcome = Outcome.Completed;

            parallelTraceGroup = new UnorderedTraces();
            foreach (TestPickBranch branch in this.Branches)
            {
                // Each Branch is Ordered with respect to itself (like normal)
                OrderedTraces branchTraceGroup = new OrderedTraces();

                Outcome bOutcome = branch.GetTriggerTrace(branchTraceGroup);

                if (bOutcome.GetType() != typeof(Outcome))
                {
                    outcome = bOutcome;
                }


                parallelTraceGroup.Steps.Add(branchTraceGroup);
            }
            traceGroup.Steps.Add(parallelTraceGroup);

            parallelTraceGroup = new UnorderedTraces();
            foreach (TestPickBranch branch in this.Branches)
            {
                // Each Branch is Ordered with respect to itself (like normal)
                OrderedTraces branchTraceGroup = new OrderedTraces();

                Outcome bOutcome = branch.GetActionTrace(branchTraceGroup);

                if (bOutcome != null && bOutcome.GetType() != typeof(Outcome))
                {
                    outcome = bOutcome;
                }

                parallelTraceGroup.Steps.Add(branchTraceGroup);
            }
            traceGroup.Steps.Add(parallelTraceGroup);

            this.CurrentOutcome = outcome;
        }
    }
}

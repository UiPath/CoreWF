// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using LegacyTest.Test.Common.TestObjects.Activities.Collections;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestParallel : TestActivity
    {
        private MemberCollection<TestActivity> _branches;
        private IList<Directive> _compensationHint;
        private bool _completionCondition;
        private TestActivity _expressionActivity;
        private int _numberOfBranchesExecution = -1;

        public TestParallel()
        {
            this.ProductActivity = new Parallel();
            _branches = new MemberCollection<TestActivity>(AddBranch)
            {
                InsertItem = InsertBranch,
                RemoveItem = RemoveBranch,
                RemoveAtItem = RemoveAtBranch
            };

            _compensationHint = new List<Directive>();
        }

        public TestParallel(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public IList<Variable> Variables
        {
            get { return this.ProductParallel.Variables; }
        }

        public MemberCollection<TestActivity> Branches
        {
            get { return _branches; }
        }

        public bool CompletionCondition
        {
            set { this.ProductParallel.CompletionCondition = _completionCondition = value; }
        }

        public Expression<Func<ActivityContext, bool>> CompletionConditionExpression
        {
            set
            {
                this.ProductParallel.CompletionCondition = new LambdaValue<bool>(value);
            }
        }

        public TestActivity CompletionConditionValueExpression
        {
            set
            {
                this.ProductParallel.CompletionCondition = value == null ? null : (Activity<bool>)(value.ProductActivity);
                _expressionActivity = value;
            }
        }

        public Variable<bool> CompletionConditionVariable
        {
            set { this.ProductParallel.CompletionCondition = value; }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        public int HintNumberOfBranchesExecution
        {
            get { return _numberOfBranchesExecution; }
            set { _numberOfBranchesExecution = value; }
        }

        // Overriding GetExpectedTrace so that we use an UnorderedTrace.
        public override ExpectedTrace GetExpectedTrace()
        {
            return base.GetExpectedTraceUnordered();
        }

        private Parallel ProductParallel
        {
            get { return (Parallel)this.ProductActivity; }
        }

        protected void AddBranch(TestActivity item)
        {
            ProductParallel.Branches.Add(item.ProductActivity);
        }

        protected void InsertBranch(int index, TestActivity item)
        {
            ProductParallel.Branches.Insert(index, item.ProductActivity);
        }

        protected bool RemoveBranch(TestActivity item)
        {
            return ProductParallel.Branches.Remove(item.ProductActivity);
        }

        protected void RemoveAtBranch(int index)
        {
            ProductParallel.Branches.RemoveAt(index);
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            return _branches;
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // Parallel is a collection of unordered branches
            UnorderedTraces parallelTraceGroup = new UnorderedTraces();

            bool oneCompleted = false;
            int index = 0;

            foreach (TestActivity branch in _branches)
            {
                // Each Branch is Ordered with respect to itself (like normal)
                OrderedTraces branchTraceGroup = new OrderedTraces();

                if (_numberOfBranchesExecution == index)
                {
                    // so if we have gone past the hint
                    if (branch.ExpectedOutcome.DefaultPropogationState == OutcomeState.Completed)
                    {
                        TestDummyTraceActivity tdt = new TestDummyTraceActivity(branch.DisplayName)
                        {
                            ExpectedOutcome = Outcome.Canceled
                        };
                        tdt.GetTrace(branchTraceGroup);
                    }
                }
                else
                {
                    index++;

                    Outcome bOutcome = branch.GetTrace(branchTraceGroup);
                    if (bOutcome.DefaultPropogationState == OutcomeState.Completed)
                    {
                        oneCompleted = true;

                        if (this.ProductParallel.CompletionCondition != null)
                        {
                            if (_expressionActivity != null)
                            {
                                CurrentOutcome = _expressionActivity.GetTrace(branchTraceGroup);
                            }
                            else
                            {
                                TestDummyTraceActivity tdt = new TestDummyTraceActivity(this.ProductParallel.CompletionCondition, Outcome.Completed);
                                CurrentOutcome = tdt.GetTrace(branchTraceGroup);
                            }
                        }
                    }
                    else if (CurrentOutcome.IsOverrideable)
                    {
                        CurrentOutcome = bOutcome;
                    }
                }

                parallelTraceGroup.Steps.Add(branchTraceGroup);
            }

            // If there's at least one good branch and the CompletionCondition is true, we probably succeeded
            if (oneCompleted && _completionCondition)
            {
                this.CurrentOutcome = Outcome.Completed;
            }

            traceGroup.Steps.Add(parallelTraceGroup);
        }

        protected override void GetCancelTrace(TraceGroup traceGroup)
        {
            // The Parallel.Canceled trace and Compensation traces can come unordered...
            UnorderedTraces finalCancelTraces = new UnorderedTraces();

            GetCompensationTrace(finalCancelTraces);
            finalCancelTraces.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));

            traceGroup.Steps.Add(finalCancelTraces);
        }

        //
        // Parallel Compensation/Confirmation
        // Parallel needs to participate in the results building because, again, the traces
        // will be unordered
        //
        internal override void GetCompensationTrace(TraceGroup traceGroup)
        {
            ProcessCompensationHints(this.CompensationHint, Directive.Compensate, traceGroup);
        }

        internal override void GetConfirmationTrace(TraceGroup traceGroup)
        {
            ProcessCompensationHints(this.CompensationHint, Directive.Confirm, traceGroup);
        }

        //
        // Example hint list describing 3 branches and the order of processing for CA's on each Branch
        // { Branch, CA2, CA1, Branch, CA3, Branch, CA6, CA5, CA4 }
        //

        private void ProcessCompensationHints(IList<Directive> hints, string defaultAction, TraceGroup traceGroup)
        {
            // Parallel Confirmation/Compensation are collections of unordered branch traces
            UnorderedTraces unordered = new UnorderedTraces();

            OrderedTraces ordered = null;
            foreach (Directive directive in hints)
            {
                // If we encounter a Branch directive that means we need to start a new OrderedTraces group
                if (directive.Name == "Branch")
                {
                    if (ordered != null) // Already had one, so add it to our collection before we create a new one
                    {
                        if (ordered.Steps.Count > 0) // There's a chance we didn't produce any output
                        {
                            unordered.Steps.Add(ordered);
                        }
                    }

                    ordered = new OrderedTraces();
                }
                else
                {
                    TestActivity target = FindChildActivity(directive.Name);

                    TestCompensableActivity.ProcessDirective(target, directive, defaultAction, ordered);
                }
            }

            // Was there one left over? (From the last branch directive)
            if (ordered != null)
            {
                if (ordered.Steps.Count > 0) // There's a chance we didn't produce any output
                {
                    unordered.Steps.Add(ordered);
                }
            }

            if (unordered.Steps.Count > 0)
            {
                traceGroup.Steps.Add(unordered);
            }
        }
    }
}

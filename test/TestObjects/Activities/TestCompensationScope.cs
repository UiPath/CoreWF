// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.CustomActivities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestCompensationScope : TestActivity
    {
        private TestActivity _body;

        private IList<Directive> _compensationHint;

        public TestCompensationScope()
        {
            this.ProductActivity = new CustomCompensationScope();
            _compensationHint = new List<Directive>();
        }

        public TestCompensationScope(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                if (value == null)
                {
                    this.ProductCompensationScope.CSBody = null;
                }
                else
                {
                    this.ProductCompensationScope.CSBody = _body.ProductActivity;
                }
            }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        private CustomCompensationScope ProductCompensationScope
        {
            get { return (CustomCompensationScope)this.ProductActivity; }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_body != null)
            {
                yield return _body;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (this.Body != null)
            {
                Outcome bodyOutcome = this.Body.GetTrace(traceGroup);

                // Auto-confim should only happen IFF the ExpectedOutcome is Completed
                if (this.ExpectedOutcome.DefaultPropogationState == OutcomeState.Completed)
                {
                    HandleAutoConfirm(traceGroup);
                }
                else
                {
                    HandleError(traceGroup);

                    // The CompensationScope will go to Faulted but we still need to propagate the Body's outcome
                    CurrentOutcome = new Outcome(bodyOutcome.DefaultPropogationState, OutcomeState.Faulted);
                }
            }
        }

        protected override void GetCancelTrace(TraceGroup traceGroup)
        {
            HandleError(traceGroup);

            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
        }

        protected override void GetFaultTrace(TraceGroup traceGroup)
        {
            HandleError(traceGroup);
            base.GetFaultTrace(traceGroup);
        }

        private void HandleAutoConfirm(TraceGroup traceGroup)
        {
            // Use the expected hints to get the confirmation traces for each
            foreach (Directive directive in this.CompensationHint)
            {
                TestActivity target = FindChildActivity(directive.Name);

                TestCompensableActivity.ProcessDirective(target, directive, Directive.Confirm, traceGroup);
            }
        }

        private void HandleError(TraceGroup traceGroup)
        {
            // Use the expected hints to get the compensation traces for each
            foreach (Directive directive in this.CompensationHint)
            {
                TestActivity target = FindChildActivity(directive.Name);

                TestCompensableActivity.ProcessDirective(target, directive, Directive.Compensate, traceGroup);
            }
        }
    }
}

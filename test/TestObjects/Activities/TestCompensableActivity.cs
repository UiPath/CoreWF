// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestCompensableActivity : TestActivity
    {
        private TestActivity _body;
        private TestActivity _compensation;
        private TestActivity _confirmation;
        private TestActivity _cancellation;

        private IList<Directive> _compensationHint;
        private IList<Directive> _confirmationHint;

        private int _hintIterationCount;
        private int _currentIterationCount;

        private bool _bodyExecutedSuccessfully;

        public TestCompensableActivity()
        {
            this.ProductActivity = new CompensableActivity();

            _compensationHint = new List<Directive>();
            _confirmationHint = new List<Directive>();
            _hintIterationCount = 1;
        }

        public TestCompensableActivity(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public IList<Variable> Variables
        {
            get { return this.ProductCompensableActivity.Variables; }
        }

        public Variable<CompensationToken> CompensationHandleVariable
        {
            set { this.ProductCompensableActivity.Result = new OutArgument<CompensationToken>(value); }
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
                    this.ProductCompensableActivity.Body = null;
                }
                else
                {
                    this.ProductCompensableActivity.Body = _body.ProductActivity;
                }
            }
        }

        public TestActivity Compensation
        {
            get
            {
                return _compensation;
            }
            set
            {
                _compensation = value;
                if (value == null)
                {
                    this.ProductCompensableActivity.CompensationHandler = null;
                }
                else
                {
                    this.ProductCompensableActivity.CompensationHandler = _compensation.ProductActivity;
                }
            }
        }

        public TestActivity Confirmation
        {
            get
            {
                return _confirmation;
            }
            set
            {
                _confirmation = value;
                if (value == null)
                {
                    this.ProductCompensableActivity.ConfirmationHandler = null;
                }
                else
                {
                    this.ProductCompensableActivity.ConfirmationHandler = _confirmation.ProductActivity;
                }
            }
        }

        public TestActivity Cancellation
        {
            get
            {
                return _cancellation;
            }
            set
            {
                _cancellation = value;
                if (value == null)
                {
                    this.ProductCompensableActivity.CancellationHandler = null;
                }
                else
                {
                    this.ProductCompensableActivity.CancellationHandler = _cancellation.ProductActivity;
                }
            }
        }

        public IList<Directive> CompensationHint
        {
            get { return _compensationHint; }
            set { _compensationHint = value; }
        }

        public IList<Directive> ConfirmationHint
        {
            get { return _confirmationHint; }
            set { _confirmationHint = value; }
        }

        public int HintIterationCount
        {
            get { return _hintIterationCount; }
            set { _hintIterationCount = value; }
        }

        private CompensableActivity ProductCompensableActivity
        {
            get { return (CompensableActivity)this.ProductActivity; }
        }

        internal override void ResetForValidation()
        {
            _currentIterationCount = 0;
            base.ResetForValidation();
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_body != null)
            {
                yield return _body;
            }

            if (_compensation != null)
            {
                yield return _compensation;
            }

            if (_confirmation != null)
            {
                yield return _confirmation;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (this.Body != null)
            {
                Outcome bodyOutcome = this.Body.GetTrace(traceGroup);

                if (bodyOutcome.DefaultPropogationState == OutcomeState.Completed)
                {
                    _bodyExecutedSuccessfully = true;
                }
                else if (bodyOutcome.DefaultPropogationState == OutcomeState.Canceled)
                {
                    this.GetCancellationTrace(traceGroup);
                    this.CurrentOutcome = Outcome.Canceled;
                    this.CurrentOutcome.IsOverrideable = bodyOutcome.IsOverrideable;
                }
                else if (this.ExpectedOutcome == Outcome.Canceled)
                {
                    this.GetCancellationTrace(traceGroup);
                    this.CurrentOutcome = Outcome.Canceled;
                    this.CurrentOutcome.IsOverrideable = this.ExpectedOutcome.IsOverrideable;
                }
                else if (this.ExpectedOutcome is UncaughtExceptionOutcome)
                {
                    this.CurrentOutcome = bodyOutcome;
                }
                else
                {
                    this.GetCompensationTrace(traceGroup);
                    this.CurrentOutcome = Outcome.Canceled;
                }
            }
            else
            {
                // A null body is fine
                _bodyExecutedSuccessfully = true;
            }
        }

        protected override void GetCancelTrace(TraceGroup traceGroup)
        {
            this.GetCompensationTrace(traceGroup);

            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
        }

        internal override void GetConfirmationTrace(TraceGroup traceGroup)
        {
            // This handler should only be invoked once (unless the CA is in a loop).
            _currentIterationCount++;
            if (_currentIterationCount > _hintIterationCount)
            {
                return;
            }

            // Attempt to process through the provided confirmation activity to pick up the traces
            if (_confirmation != null && _bodyExecutedSuccessfully)
            {
                _confirmation.GetTrace(traceGroup);
            }

            // Additionally, if there are any hints, trace these as well...
            // Scenario1: There are just hints but no confirmation activity -- hints control all the work
            // Scenario2: There is a confirmation activity which does work but there is work remaining afterwards -- hints control the remaining work
            foreach (Directive directive in this.ConfirmationHint)
            {
                TestActivity target = FindChildActivity(directive.Name);

                TestCompensableActivity.ProcessDirective(target, directive, Directive.Confirm, traceGroup);
            }
        }

        internal override void GetCompensationTrace(TraceGroup traceGroup)
        {
            // This handler should only be invoked once (unless the CA is in a loop).
            _currentIterationCount++;
            if (_currentIterationCount > _hintIterationCount)
            {
                return;
            }

            // Attempt to process through the provided compensation activity to pick up the traces
            if (_compensation != null && _bodyExecutedSuccessfully)
            {
                _compensation.GetTrace(traceGroup);
            }

            // Additionally, if there are any hints, trace these as well...
            // Scenario1: There are just hints but no compensation activity -- hints control all the work
            // Scenario2: There is a compensation activity which does work but there is work remaining afterwards -- hints control the remaining work
            foreach (Directive directive in this.CompensationHint)
            {
                TestActivity target = FindChildActivity(directive.Name);

                TestCompensableActivity.ProcessDirective(target, directive, Directive.Compensate, traceGroup);
            }
        }

        internal void GetCancellationTrace(TraceGroup traceGroup)
        {
            // This handler should only be invoked once (unless the CA is in a loop).
            _currentIterationCount++;
            if (_currentIterationCount > _hintIterationCount)
            {
                return;
            }

            // Attempt to process through the provided compensation activity to pick up the traces
            if (_cancellation != null)
            {
                _cancellation.GetTrace(traceGroup);
            }

            // Additionally, if there are any hints, trace these as well...
            // Scenario1: There are just hints but no compensation activity -- hints control all the work
            // Scenario2: There is a compensation activity which does work but there is work remaining afterwards -- hints control the remaining work
            foreach (Directive directive in this.CompensationHint)
            {
                TestActivity target = FindChildActivity(directive.Name);

                TestCompensableActivity.ProcessDirective(target, directive, Directive.Compensate, traceGroup);
            }
        }

        internal static void ProcessDirective(TestActivity target, Directive directive, string defaultAction, TraceGroup traceGroup)
        {
            // A directive's action of "*" == "don't care" == "use default action"
            string action = (directive.Action == Directive.Wildcard) ? defaultAction : directive.Action;

            switch (action)
            {
                case Directive.Compensate:
                    target.GetCompensationTrace(traceGroup);
                    break;
                case Directive.Confirm:
                    target.GetConfirmationTrace(traceGroup);
                    break;
                default:
                    throw new Exception(string.Format("Invalid directive {0}", directive));
            }
        }
    }
}

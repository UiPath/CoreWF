// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities.Bpm
{
    public class TestFlowStep : TestFlowElement
    {
        private FlowStep _productFlowStep;

        private TestActivity _actionActivity;

        private TestFlowElement _nextElement;

        public TestFlowStep()
        {
            _productFlowStep = new FlowStep();
        }

        public TestFlowStep(TestActivity actionActivity)
            : this()
        {
            if (actionActivity == null)
            {
                return;
            }
            _actionActivity = actionActivity;
            _productFlowStep.Action = actionActivity.ProductActivity;
        }

        public TestActivity ActionActivity
        {
            get
            {
                return _actionActivity;
            }
            set
            {
                _actionActivity = value;
                if (value != null)
                {
                    _productFlowStep.Action = value.ProductActivity;
                }
                else
                {
                    _productFlowStep.Action = null;
                }
            }
        }

        internal TestFlowElement NextElement
        {
            get { return _nextElement; }
            set
            {
                _nextElement = value;
                if (value != null)
                {
                    _productFlowStep.Next = value.GetProductElement();
                }
                else
                {
                    _productFlowStep.Next = null;
                }
            }
        }

        public override FlowNode GetProductElement()
        {
            return _productFlowStep;
        }

        internal override TestFlowElement GetNextElement()
        {
            return this.NextElement;
        }

        internal override Outcome GetTrace(TraceGroup traceGroup)
        {
            if (_actionActivity != null)
            {
                Outcome outcome = _actionActivity.GetTrace(traceGroup);
                if (outcome.DefaultPropogationState != OutcomeState.Completed)
                {
                    return outcome;
                }
            }

            if (_nextElement != null && !IsFaulting && !IsCancelling)
            {
                return _nextElement.GetTrace(traceGroup);
            }

            return Outcome.Completed;
        }
    }
}

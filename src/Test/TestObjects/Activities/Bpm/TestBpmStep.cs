// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements;
using System.Collections.Generic;

namespace Test.Common.TestObjects.Activities
{
    public class TestBpmStep : TestBpmElement
    {
        private BpmStep _productFlowStep;

        private TestActivity _actionActivity;

        private TestBpmElement _nextElement;

        public TestBpmStep()
        {
            _productFlowStep = new BpmStep();
        }

        public TestBpmStep(TestActivity actionActivity)
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

        internal TestBpmElement NextElement
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

        public override BpmNode GetProductElement()
        {
            return _productFlowStep;
        }

        public override TestBpmElement GetNextElement()
        {
            return this.NextElement;
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_actionActivity != null)
            {
                yield return _actionActivity;
            }
            if (_nextElement != null && !IsFaulting && !IsCancelling)
            {
                yield return _nextElement;
            }
        }
    }
}

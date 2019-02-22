// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Activities.Expressions;
using System.Activities;
using Test.Common.TestObjects.Utilities.Validation;
using System.Activities.Statements;
using System.ComponentModel;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestAndAlso : TestActivity, ITestBoolReturningActivity
    {
        private TestActivity _leftActivity;
        private TestActivity _rightActivity;

        public TestAndAlso()
        {
            this.ProductActivity = new AndAlso();
        }

        public TestAndAlso(bool left, bool right) : this()
        {
            ((AndAlso)this.ProductActivity).Left = new Literal<bool>(left);
            ((AndAlso)this.ProductActivity).Right = new Literal<bool>(right);
        }

        public AndAlso ProductAndAlso
        {
            get { return this.ProductActivity as AndAlso; }
        }

        public Variable<bool> LeftVariable
        {
            set
            {
                ((AndAlso)this.ProductActivity).Left = new VariableValue<bool>(value);
            }
        }

        public Variable<bool> RightVariable
        {
            set
            {
                ((AndAlso)this.ProductActivity).Right = new VariableValue<bool>(value);
            }
        }

        public bool Left
        {
            set
            {
                ((AndAlso)this.ProductActivity).Left = value;
            }
        }

        public bool Right
        {
            set
            {
                ((AndAlso)this.ProductActivity).Right = value;
            }
        }

        public TestActivity LeftActivity
        {
            set
            {
                if (value != null && !(value.ProductActivity is Activity<bool>))
                {
                    throw new Exception("LeftActivity.ProductActivity should be Activity<bool>");
                }

                if (value != null)
                {
                    ((AndAlso)this.ProductActivity).Left = value.ProductActivity as Activity<bool>;
                }
                else
                {
                    ((AndAlso)this.ProductActivity).Right = null;
                }

                _leftActivity = value;
            }
            get
            {
                return _leftActivity;
            }
        }

        public TestActivity RightActivity
        {
            set
            {
                if (value != null && !(value.ProductActivity is Activity<bool>))
                {
                    throw new Exception("RightActivity.ProductActivity should be Activity<bool>");
                }

                if (value != null)
                {
                    ((AndAlso)this.ProductActivity).Right = value.ProductActivity as Activity<bool>;
                }
                else
                {
                    ((AndAlso)this.ProductActivity).Right = null;
                }

                _rightActivity = value;
            }
            get
            {
                return _rightActivity;
            }
        }

        public Variable<bool> Result
        {
            set
            {
                ((AndAlso)this.ProductActivity).Result = new OutArgument<bool>(value);
            }
        }

        [DefaultValue(false)]
        public bool ExceptionInLeft { get; set; }

        [DefaultValue(false)]
        public bool ExceptionInRight { get; set; }

        [DefaultValue(false)]
        public bool HintShortCircuit { get; set; }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_leftActivity != null)
            {
                yield return _leftActivity;
            }
            if (!HintShortCircuit && _rightActivity != null)
            {
                yield return _rightActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(new If().DisplayName, ActivityInstanceState.Executing));
            if (_leftActivity != null)
            {
                _leftActivity.GetTrace(traceGroup);
                if (ExceptionInLeft)
                {
                    ExpectedOutcome = Outcome.None;
                    return;
                }
            }
            traceGroup.Steps.Add(new ActivityTrace(new Assign<bool>().DisplayName, ActivityInstanceState.Executing));
            if (!HintShortCircuit && _rightActivity != null)
            {
                _rightActivity.GetTrace(traceGroup);
                if (ExceptionInRight)
                {
                    ExpectedOutcome = Outcome.None;
                    return;
                }
            }
            traceGroup.Steps.Add(new ActivityTrace(new Assign<bool>().DisplayName, ActivityInstanceState.Closed));
            traceGroup.Steps.Add(new ActivityTrace(new If().DisplayName, ActivityInstanceState.Closed));
        }
    }
}

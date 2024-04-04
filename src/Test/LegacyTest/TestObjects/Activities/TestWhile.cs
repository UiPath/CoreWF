// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestWhile : TestLoop
    {
        private TestActivity _conditionActivity;

        public TestWhile()
        {
            this.ProductActivity = new While();
        }

        public TestWhile(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestWhile(TestActivity condition)
        {

            if (!(condition.ProductActivity is Activity<bool> prodActivity))
            {
                throw new ArgumentNullException("ProductActivity");
            }

            this.body = condition;
            this.ProductActivity = new While(prodActivity);
        }

        public TestWhile(Expression<Func<ActivityContext, bool>> condition)
        {
            this.ProductActivity = new While(condition);
        }

        public bool Condition
        {
            set { this.ProductWhile.Condition = new Literal<bool> { Value = value }; }
        }

        public Variable<bool> ConditionVariable
        {
            set
            {
                this.ProductWhile.Condition = new VariableValue<bool>(value);
            }
        }

        public Expression<Func<ActivityContext, bool>> ConditionExpression
        {
            set { this.ProductWhile.Condition = new LambdaValue<bool>(value); }
        }

        public TestActivity ConditionActivity
        {
            set
            {
                if (value == null)
                {
                    this.ProductWhile.Condition = null;
                }
                else
                {
                    _conditionActivity = value;
                    this.ProductWhile.Condition = (Activity<bool>)(value.ProductActivity);
                }
            }
        }

        public IList<Variable> Variables
        {
            get
            {
                return this.ProductWhile.Variables;
            }
        }

        public TestActivity Body
        {
            get
            {
                return this.body;
            }

            set
            {
                this.body = value;
                if (this.body != null)
                {
                    this.ProductWhile.Body = this.body.ProductActivity;
                }
                else
                {
                    this.ProductWhile.Body = null;
                }
            }
        }

        private While ProductWhile
        {
            get
            {
                return (While)this.ProductActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            GetConditionTrace(traceGroup);

            if (this.body != null)
            {
                for (int counter = 0; counter < HintIterationCount; counter++)
                {
                    Outcome childOut = body.GetTrace(traceGroup);

                    if (childOut.DefaultPropogationState != OutcomeState.Completed)
                    {
                        CurrentOutcome = childOut;
                        break;
                    }

                    GetConditionTrace(traceGroup);
                }
            }
        }

        private void GetConditionTrace(TraceGroup traceGroup)
        {
            if (_conditionActivity != null)
            {
                CurrentOutcome = _conditionActivity.GetTrace(traceGroup);
            }
            else if (this.ProductWhile.Condition != null)
            {
                TestActivity condition;

                //For the case where DisableXamlRoundTrip is true, the trace is different
                //if (TestParameters.DisableXamlRoundTrip)
                {
                    condition = new TestSequence()
                    {
                        DisplayName = this.ProductWhile.Condition.DisplayName,
                        ExpectedOutcome = ConditionOutcome,
                    };
                }
                //else
                //{
                //    condition = new TestDummyTraceActivity(this.ProductWhile.Condition, ConditionOutcome);
                //}
                CurrentOutcome = condition.GetTrace(traceGroup);
            }
        }
    }
}

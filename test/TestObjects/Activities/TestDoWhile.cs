// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Utilities;

namespace Test.Common.TestObjects.Activities
{
    public class TestDoWhile : TestLoop
    {
        private TestActivity _conditionActivity;

        public TestDoWhile()
        {
            this.ProductActivity = new DoWhile();
        }

        public TestDoWhile(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public TestDoWhile(TestActivity condition)
        {
            Activity<bool> prodActivity = condition.ProductActivity as Activity<bool>;

            if (prodActivity == null)
            {
                throw new Exception("DoWhile can only be constructed with Activity<bool> condition");
            }

            this.body = condition;
            this.ProductActivity = new DoWhile(prodActivity);
        }

        public TestDoWhile(Expression<Func<ActivityContext, bool>> condition)
        {
            this.ProductActivity = new DoWhile(condition);
        }

        public bool Condition
        {
            set
            {
                this.ProductDoWhile.Condition = new Literal<bool>(value);
            }
        }

        public IList<Variable> Variables
        {
            get
            {
                return this.ProductDoWhile.Variables;
            }
        }

        public Activity<bool> ConditionValueExpression
        {
            set
            {
                this.ProductDoWhile.Condition = value;
            }
        }

        public Expression<Func<ActivityContext, bool>> ConditionExpression
        {
            set
            {
                this.ProductDoWhile.Condition = new LambdaValue<bool>(value);
            }
        }

        public TestActivity ConditionActivity
        {
            set
            {
                if (value == null)
                {
                    this.ProductDoWhile.Condition = null;
                }
                else
                {
                    _conditionActivity = value;
                    this.ProductDoWhile.Condition = (Activity<bool>)(value.ProductActivity);
                }
            }
        }

        public Variable<bool> ConditionVariable
        {
            set
            {
                this.ProductDoWhile.Condition = new VariableValue<bool>(value);
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
                    this.ProductDoWhile.Body = this.body.ProductActivity;
                }
                else
                {
                    this.ProductDoWhile.Body = null;
                }
            }
        }

        private DoWhile ProductDoWhile
        {
            get
            {
                return (DoWhile)this.ProductActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            for (int counter = 0; counter < HintIterationCount; counter++)
            {
                if (this.body != null)
                {
                    Outcome childOut = body.GetTrace(traceGroup);

                    if (childOut.DefaultPropogationState != OutcomeState.Completed)
                    {
                        CurrentOutcome = childOut;
                        break;
                    }
                }

                if (_conditionActivity != null)
                {
                    CurrentOutcome = _conditionActivity.GetTrace(traceGroup);
                }
                else if (this.ProductDoWhile.Condition != null)
                {
                    TestActivity condition;

                    //For the case where DisableXamlRoundTrip is true, the trace is different
                    //if (TestParameters.DisableXamlRoundTrip)
                    //{
                    condition = new TestSequence()
                    {
                        DisplayName = this.ProductDoWhile.Condition.DisplayName,
                        ExpectedOutcome = ConditionOutcome,
                    };
                    //}
                    //else
                    //{
                    //    condition = new TestDummyTraceActivity(this.ProductDoWhile.Condition, ConditionOutcome);
                    //}

                    CurrentOutcome = condition.GetTrace(traceGroup);
                }
            }
        }
    }
}

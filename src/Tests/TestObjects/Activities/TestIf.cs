// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public enum HintThenOrElse
    {
        Then,
        Else,
        Neither,
    }

    public class TestIf : TestActivity
    {
        protected TestActivity conditionActivity;
        protected TestActivity thenActivity;
        protected TestActivity elseActivity;
        private List<HintThenOrElse> _thenOrElse;

        public TestIf(params HintThenOrElse[] thenOrElseHint)
        {
            this.ProductActivity = new If();

            if (thenOrElseHint == null)
            {
                _thenOrElse = null;
            }
            else
            {
                _thenOrElse = new List<HintThenOrElse>(thenOrElseHint);
            }
        }

        public TestIf(string displayName, params HintThenOrElse[] thenOrElseHint)
            : this(thenOrElseHint)
        {
            this.DisplayName = displayName;
        }

        public TestIf(TestActivity condition, params HintThenOrElse[] thenOrElseHint)
        {

            if (!(condition.ProductActivity is Activity<bool> prodActivity))
            {
                throw new ArgumentNullException("ProductActivity");
            }
            this.ProductActivity = new If(prodActivity);

            if (thenOrElseHint == null)
            {
                _thenOrElse = null;
            }
            else
            {
                _thenOrElse = new List<HintThenOrElse>(thenOrElseHint);
            }
        }

        public TestIf(Variable<bool> condition, params HintThenOrElse[] thenOrElseHint)
        {
            this.ProductActivity = new If(new InArgument<bool>(condition));

            if (thenOrElseHint == null)
            {
                _thenOrElse = null;
            }
            else
            {
                _thenOrElse = new List<HintThenOrElse>(thenOrElseHint);
            }
        }

        public TestIf(Expression<Func<ActivityContext, bool>> condition, params HintThenOrElse[] thenOrElseHint)
        {
            this.ProductActivity = new If(condition);
            if (thenOrElseHint == null)
            {
                _thenOrElse = null;
            }
            else
            {
                _thenOrElse = new List<HintThenOrElse>(thenOrElseHint);
            }
        }

        public bool Condition
        {
            set
            {
                this.ProductIf.Condition = new Literal<bool>(value);
            }
        }

        public Variable<bool> ConditionVariable
        {
            set
            {
                if (value == null)
                {
                    this.ProductIf.Condition = null;
                }
                else
                {
                    this.ProductIf.Condition = new VariableValue<bool>(value);
                }
            }
        }

        public Expression<Func<ActivityContext, bool>> ConditionExpression
        {
            set { this.ProductIf.Condition = new LambdaValue<bool>(value); }
        }

        public Activity<bool> ConditionValueExpression
        {
            set { this.ProductIf.Condition = value; }
        }

        public TestActivity ConditionActivity
        {
            set
            {
                this.conditionActivity = value;
                if (value == null)
                {
                    this.ProductIf.Condition = null;
                }
                else
                {
                    this.ProductIf.Condition = (Activity<bool>)(value.ProductActivity);
                }
            }
        }

        public TestActivity ElseActivity
        {
            get
            {
                return this.elseActivity;
            }

            set
            {
                this.elseActivity = value;
                if (value == null)
                {
                    this.ProductIf.Else = null;
                }
                else
                {
                    this.ProductIf.Else = this.elseActivity.ProductActivity;
                }
            }
        }

        public TestActivity ThenActivity
        {
            get
            {
                return this.thenActivity;
            }

            set
            {
                this.thenActivity = value;
                if (value == null)
                {
                    this.ProductIf.Then = null;
                }
                else
                {
                    this.ProductIf.Then = this.thenActivity.ProductActivity;
                }
            }
        }

        private If ProductIf
        {
            get
            {
                return (If)this.ProductActivity;
            }
        }

        private List<HintThenOrElse> ThenOrElse
        {
            get
            {
                if (_thenOrElse == null || _thenOrElse.Count == 0)
                {
                    _thenOrElse = new List<HintThenOrElse>() { default(HintThenOrElse) };
                }
                return _thenOrElse;
            }
        }

        private HintThenOrElse CurrentThenOrElse
        {
            get
            {
                if (this.ThenOrElse.Count == 1)
                {
                    return this.ThenOrElse[0];
                }
                else
                {
                    return this.ThenOrElse[this.iterationNumber];
                }
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.ThenActivity != null)
            {
                yield return this.ThenActivity;
            }
            if (this.ElseActivity != null)
            {
                yield return this.ElseActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            if (this.conditionActivity != null)
            {
                Outcome conditionOutcome = this.conditionActivity.GetTrace(traceGroup);

                if (conditionOutcome.DefaultPropogationState != OutcomeState.Completed)
                {
                    // propogate the unknown outcome upwards
                    this.CurrentOutcome = conditionOutcome;
                    return;
                }
            }
            if (this.CurrentThenOrElse == HintThenOrElse.Then && this.ThenActivity != null)
            {
                CurrentOutcome = ThenActivity.GetTrace(traceGroup);
            }
            else if (this.CurrentThenOrElse == HintThenOrElse.Else && this.ElseActivity != null)
            {
                CurrentOutcome = ElseActivity.GetTrace(traceGroup);
            }
        }
    }
}

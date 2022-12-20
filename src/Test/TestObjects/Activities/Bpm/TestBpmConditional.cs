// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestBpmFlowConditional : TestBpmElement
    {
        public bool ResetHints = false;
        private BpmDecision _productFlowConditional;

        private TestBpmElement _trueAction;
        private TestBpmElement _falseAction;

        private List<HintTrueFalse> _trueOrFalse;
        private int _iterationNumber = 0;

        private ExpressionType _conditionType;

        private TestActivity _expressionActivity;

        public TestBpmFlowConditional(string displayName, params HintTrueFalse[] thenOrElseHint)
            : this(thenOrElseHint)
        {
            this.DisplayName = displayName;
        }

        public TestBpmFlowConditional(params HintTrueFalse[] thenOrElseHint)
        {
            _productFlowConditional = new BpmDecision();
            if (thenOrElseHint != null)
            {
                _trueOrFalse = new List<HintTrueFalse>(thenOrElseHint);
            }
        }

        public BpmDecision ProductFlowConditional
        {
            get
            {
                return _productFlowConditional;
            }
        }

        public bool Condition
        {
            set
            {
                _productFlowConditional.Condition = new Literal<bool>(value);
                _conditionType = ExpressionType.Literal;
            }
        }

        public Expression<Func<ActivityContext, bool>> ConditionExpression
        {
            set
            {
                _productFlowConditional.Condition = new LambdaValue<bool>(value);
                _conditionType = ExpressionType.VisualBasicValue;
            }
        }

        public TestActivity ConditionValueExpression
        {
            get
            {
                return _expressionActivity;
            }
            set
            {
                _conditionType = ExpressionType.Activity;
                _expressionActivity = value;
                if (value == null)
                {
                    this.ProductFlowConditional.Condition = null;
                }
                else
                {
                    this.ProductFlowConditional.Condition = (Activity<bool>)(value.ProductActivity);
                }
            }
        }

        public Variable<bool> ConditionVariable
        {
            set
            {
                _productFlowConditional.Condition = new VariableValue<bool>(value);
                _conditionType = ExpressionType.VariableValue;
            }
        }

        internal TestBpmElement TrueAction
        {
            get
            {
                return _trueAction;
            }
            set
            {
                _trueAction = value;
                if (value != null)
                {
                    _productFlowConditional.True = value.GetProductElement();
                }
                else
                {
                    _productFlowConditional.True = null;
                }
            }
        }

        internal TestBpmElement FalseAction
        {
            get
            {
                return _falseAction;
            }
            set
            {
                _falseAction = value;
                if (value != null)
                {
                    _productFlowConditional.False = value.GetProductElement();
                }
                else
                {
                    _productFlowConditional.False = null;
                }
            }
        }

        internal List<HintTrueFalse> TrueOrFalse
        {
            get
            {
                if (_trueOrFalse == null || _trueOrFalse.Count == 0)
                {
                    _trueOrFalse = new List<HintTrueFalse>() { default(HintTrueFalse) };
                }
                return _trueOrFalse;
            }
            set
            {
                _trueOrFalse = value;
            }
        }

        private HintTrueFalse CurrentTrueOrFalse
        {
            get
            {
                if (this.TrueOrFalse.Count == 1)
                {
                    return this.TrueOrFalse[0];
                }
                else
                {
                    //In case of nested loops which do not need reset we need to
                    //return the last hint for outer loops after inner one has
                    //already executed.
                    if (_iterationNumber == TrueOrFalse.Count)
                    {
                        return this.TrueOrFalse[TrueOrFalse.Count - 1];
                    }
                    HintTrueFalse trueFalse = this.TrueOrFalse[_iterationNumber++];
                    if (_iterationNumber == TrueOrFalse.Count && this.ResetHints)
                    {
                        ResetIterationNumber();
                    }
                    return trueFalse;
                }
            }
        }

        private HintTrueFalse GetCurrentOutcome()
        {
            return CurrentTrueOrFalse;
        }
        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            var outcome = Outcome.Completed;

            HintTrueFalse currentHint = GetCurrentOutcome();

            switch (_conditionType)
            {
                case ExpressionType.Activity:
                    CurrentOutcome = _expressionActivity.GetTrace(traceGroup);

                    if (CurrentOutcome.DefaultPropogationState != OutcomeState.Completed)
                    {
                        return;
                    }
                    break;
                case ExpressionType.Literal:
                    new TestDummyTraceActivity(typeof(Literal<bool>), Outcome.Completed).GetTrace(traceGroup);
                    break;
                case ExpressionType.VisualBasicValue:
                    //Just use LambdaValue as there is no round trip
                    new TestDummyTraceActivity(typeof(LambdaValue<bool>), (currentHint == HintTrueFalse.Exception) ? Outcome.Faulted : Outcome.Completed).GetTrace(traceGroup);
                    break;
                case ExpressionType.VariableValue:
                    new TestDummyTraceActivity(typeof(VariableValue<bool>), (currentHint == HintTrueFalse.Exception) ? Outcome.Faulted : Outcome.Completed).GetTrace(traceGroup);
                    break;
                default: break;
            }

            if (currentHint == HintTrueFalse.True)
            {
                outcome = GetTrueActionTrace(traceGroup);
            }
            else if (currentHint == HintTrueFalse.False)
            {
                outcome = GetFalseActionTrace(traceGroup);
            }
            else
            {
                outcome = Outcome.None;
            }
            CurrentOutcome = outcome;
        }
        private Outcome GetTrueActionTrace(TraceGroup traceGroup)
        {
            if (_trueAction != null)
            {
                return _trueAction.GetTrace(traceGroup);
            }
            return Outcome.Completed;
        }
        private Outcome GetFalseActionTrace(TraceGroup traceGroup)
        {
            if (_falseAction != null)
            {
                return _falseAction.GetTrace(traceGroup);
            }
            return Outcome.Completed;
        }
        public override TestBpmElement GetNextElement()
        {
            if (this.CurrentTrueOrFalse == HintTrueFalse.True)
            {
                return TrueAction;
            }
            else
            {
                return FalseAction;
            }
        }

        public override BpmNode GetProductElement()
        {
            return _productFlowConditional;
        }

        internal void ResetIterationNumber()
        {
            _iterationNumber = 0;
        }
    }
}

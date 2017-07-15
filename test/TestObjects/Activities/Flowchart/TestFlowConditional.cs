// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoreWf;
using CoreWf.Statements;
using CoreWf.Expressions;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public enum HintTrueFalse
    {
        True,
        False,
        Exception
    }

    public enum ExpressionType
    {
        Activity,
        VisualBasicValue,
        Literal,
        VariableValue
    }
    public class TestFlowConditional : TestFlowElement
    {
        public bool ResetHints = false;
        private FlowDecision _productFlowConditional;

        private TestFlowElement _trueAction;
        private TestFlowElement _falseAction;

        private List<HintTrueFalse> _trueOrFalse;
        private int _iterationNumber = 0;

        private ExpressionType _conditionType;

        private TestActivity _expressionActivity;

        public TestFlowConditional(string displayName, params HintTrueFalse[] thenOrElseHint)
            : this(thenOrElseHint)
        {
            this.DisplayName = displayName;
        }

        public TestFlowConditional(params HintTrueFalse[] thenOrElseHint)
        {
            _productFlowConditional = new FlowDecision();
            if (thenOrElseHint != null)
            {
                _trueOrFalse = new List<HintTrueFalse>(thenOrElseHint);
            }
        }

        public FlowDecision ProductFlowConditional
        {
            get
            {
                return _productFlowConditional;
            }
        }

        public string DisplayName
        {
            set
            {
                _productFlowConditional.DisplayName = value;
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

        internal TestFlowElement TrueAction
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

        internal TestFlowElement FalseAction
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

        internal override Outcome GetTrace(TraceGroup traceGroup)
        {
            Outcome outcome = Outcome.Completed;

            HintTrueFalse currentHint = GetCurrentOutcome();

            switch (_conditionType)
            {
                case ExpressionType.Activity:
                    outcome = _expressionActivity.GetTrace(traceGroup);

                    if (outcome.DefaultPropogationState != OutcomeState.Completed)
                    {
                        return outcome;
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
            return outcome;
        }

        internal override TestFlowElement GetNextElement()
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

        public override FlowNode GetProductElement()
        {
            return _productFlowConditional;
        }

        internal void ResetIterationNumber()
        {
            _iterationNumber = 0;
        }
    }
}

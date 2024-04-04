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
    public class TestTransition
    {
        private Transition _productTransition;
        private TestStateMachineState _to;
        private TestActivity _trigger;
        private TestActivity _conditionActivity;
        private TestActivity _action;
        private int _conditionIterationNumber;
        private int _triggerIterationNumber;

        public TestTransition()
        {
            ResetForValidation();
            _productTransition = new Transition();
        }

        public TestTransition(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public string DisplayName
        {
            get { return this.ProductTransition.DisplayName; }
            set { this.ProductTransition.DisplayName = value; }
        }

        public TestActivity Trigger
        {
            get { return _trigger; }
            set
            {
                _trigger = value;
                this.ProductTransition.Trigger = value == null ? null : value.ProductActivity;
            }
        }

        public bool Condition
        {
            set { this.ProductTransition.Condition = new Literal<bool> { Value = value }; }
        }

        public TestActivity ConditionActivity
        {
            get { return _conditionActivity; }
            set
            {
                _conditionActivity = value;
                this.ProductTransition.Condition = value == null ? null : (Activity<bool>)value.ProductActivity;
            }
        }

        public Variable<bool> ConditionVariable
        {
            set
            {
                if (value == null)
                {
                    this.ProductTransition.Condition = null;
                }
                else
                {
                    this.ProductTransition.Condition = new VariableValue<bool>(value);
                }
            }
        }

        public Expression<Func<ActivityContext, bool>> ConditionExpression
        {
            set { this.ProductTransition.Condition = new LambdaValue<bool>(value); }
        }

        public TestActivity Action
        {
            get { return _action; }
            set
            {
                _action = value;
                this.ProductTransition.Action = value == null ? null : value.ProductActivity;
            }
        }

        public TestStateMachineState To
        {
            get { return _to; }
            set
            {
                _to = value;
                this.ProductTransition.To = value == null ? null : _to.ProductState;
            }
        }

        public Transition ProductTransition
        {
            get { return _productTransition; }
            set { _productTransition = value; }
        }

        /// <summary>
        /// Hint the number of trigger traces in each iteration
        /// Iteration should be increased every time a transition is taken. 
        /// If the transition does not happen, i.e. trigger cancels, condition evaluates to false, it is not a new iteration.
        /// 
        /// Trigger iteration count is different from condition iteration count, because trigger can be shared
        /// </summary>
        public int HintTriggerIterationCount
        {
            internal get
            {
                int ret = 1;

                if (HintTriggerIterationCountList.Count == 1)
                    ret = HintTriggerIterationCountList[0];
                else if (HintTriggerIterationCountList.Count != 0)
                    ret = HintTriggerIterationCountList[_triggerIterationNumber];

                return ret;
            }
            set { HintTriggerIterationCountList.Add(value); }
        }

        private List<int> _hintTriggerIterationCountList;
        public List<int> HintTriggerIterationCountList
        {
            get
            {
                if (_hintTriggerIterationCountList == null)
                {
                    _hintTriggerIterationCountList = new List<int>();
                }
                return _hintTriggerIterationCountList;
            }
        }

        /// <summary>
        /// If the previous trigger is Faulted, then this trigger will be scheduled but not executed
        /// </summary>
        public bool HintTriggerScheduled
        {
            internal get
            {
                bool ret = false;

                if (HintTriggerScheduledList.Count == 1)
                    ret = HintTriggerScheduledList[0];
                else if (HintTriggerScheduledList.Count != 0)
                    ret = HintTriggerScheduledList[_triggerIterationNumber];

                return ret;
            }
            set { HintTriggerScheduledList.Add(value); }
        }

        private List<bool> _hintTriggerScheduledList;
        /// <summary>
        /// If the previous trigger is Faulted, then this trigger will be scheduled but not executed
        /// </summary>
        public List<bool> HintTriggerScheduledList
        {
            get
            {
                if (_hintTriggerScheduledList == null)
                {
                    _hintTriggerScheduledList = new List<bool>();
                }
                return _hintTriggerScheduledList;
            }
        }


        /// <summary>
        /// Hint the number of condition traces in each iteration
        /// Iteration should be increased every time a transition is taken. 
        /// If the transition does not happen, i.e. trigger cancels, condition evaluates to false, it is not a new iteration.
        /// 
        /// Trigger iteration count is different from condition iteration count, because trigger can be shared
        /// </summary>
        public int HintConditionIterationCount
        {
            internal get
            {
                int ret = 1;

                if (HintConditionIterationCountList.Count == 1)
                    ret = HintConditionIterationCountList[0];
                else if (HintConditionIterationCountList.Count != 0)
                    ret = HintConditionIterationCountList[_conditionIterationNumber];

                return ret;
            }
            set { HintConditionIterationCountList.Add(value); }
        }

        private List<int> _hintConditionIterationCountList;
        public List<int> HintConditionIterationCountList
        {
            get
            {
                if (_hintConditionIterationCountList == null)
                {
                    _hintConditionIterationCountList = new List<int>();
                }
                return _hintConditionIterationCountList;
            }
        }

        private Outcome _expectedOutcome = Outcome.Completed;
        public Outcome ExpectedOutcome
        {
            get { return _expectedOutcome; }
            set { _expectedOutcome = value; }
        }

        internal void ResetForValidation()
        {
            _conditionIterationNumber = 0;
            _triggerIterationNumber = 0;

            if (this.Trigger != null)
            {
                this.Trigger.ResetForValidation();
            }

            if (this.ConditionActivity != null)
            {
                this.ConditionActivity.ResetForValidation();
            }

            if (this.Action != null)
            {
                this.Action.ResetForValidation();
            }
        }

        internal TestStateMachineState Source { get; set; }

        internal Outcome GetTriggerTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;
            for (int i = 0; i < HintTriggerIterationCount; i++)
            {
                TestActivity trigger = this.Trigger;
                if (this.Trigger == null)
                {
                    trigger = this.Source.NullTrigger;
                }
                outcome = trigger.GetTrace(traceGroup);
            }

            if (HintTriggerScheduled == true)
            {
                traceGroup.Steps.Add(new ActivityTrace(this.Trigger.DisplayName, ActivityInstanceState.Executing));
            }

            _triggerIterationNumber++;
            return outcome;
        }

        internal Outcome GetConditionTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;

            for (int i = 0; i < HintConditionIterationCount; i++)
            {
                if (_conditionActivity != null)
                {
                    outcome = _conditionActivity.GetTrace(traceGroup);
                }
                else if (this.ProductTransition.Condition != null)
                {
                    //TestActivity condition;
                    //For the case where DisableXamlRoundTrip is true, the trace is different
                    //if (TestParameters.DisableXamlRoundTrip && this.ProductTransition.Condition is LambdaValue<bool>)
                    //    condition = new TestSequence { DisplayName = "LambdaValue<Boolean>" };
                    //else
                    //    condition = new TestDummyTraceActivity(this.ProductTransition.Condition, outcome);

                    //outcome = condition.GetTrace(traceGroup);
                }
            }

            _conditionIterationNumber++;
            return outcome;
        }

        internal Outcome GetActionTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;

            if (this.Action != null)
            {
                outcome = this.Action.GetTrace(traceGroup);
            }

            return outcome;
        }
    }
}

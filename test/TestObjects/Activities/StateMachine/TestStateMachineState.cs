// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestStateMachineState
    {
        private State _productState;
        private TestActivity _entryActivity;
        private TestActivity _exitActivity;
        private MemberCollection<TestTransition> _transitions;
        private int _iterationNumber;

        public static TestStateMachineState CreateFinalState(string displayName)
        {
            return new TestStateMachineState(displayName)
            {
                IsFinal = true
            };
        }

        public TestStateMachineState()
        {
            ResetForValidation();

            _productState = new State();

            _transitions = new MemberCollection<TestTransition>(AddTransition)
            {
                RemoveItem = RemoveTransition,
                RemoveAtItem = RemoveAtTransition,
                InsertItem = InsertTransition
            };

            // InternalState default display name is String.Empty.
            this.DisplayName = string.Empty;
        }

        public TestStateMachineState(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        /// <summary>
        /// Activity.cs sets DisplayName to String.Empty when try to set it as null.
        /// Therefore, setting DisplayName to null will cause inconsistence between State.DisplayName (null) and InternalState.DisplayName (String.Empty).
        /// Confirmed with Dev on this behavior. It is by design that InternalState is created on the fly.
        /// This value can't get from InternalState or be cached.
        /// </summary>
        public string DisplayName
        {
            get { return _productState.DisplayName; }
            set { _productState.DisplayName = value; }
        }

        public IList<Variable> Variables { get { return this.ProductState.Variables; } }

        public bool IsFinal
        {
            get { return _productState.IsFinal; }
            set { _productState.IsFinal = value; }
        }

        /// <summary>
        /// Required for generating the expected tracking data
        /// </summary>
        public string StateMachineName { get; set; }

        public TestActivity Entry
        {
            get { return _entryActivity; }
            set
            {
                _entryActivity = value;
                _productState.Entry = value == null ? null : _entryActivity.ProductActivity;
            }
        }

        public TestActivity Exit
        {
            get { return _exitActivity; }
            set
            {
                _exitActivity = value;
                _productState.Exit = value == null ? null : _exitActivity.ProductActivity;
            }
        }

        public MemberCollection<TestTransition> Transitions { get { return _transitions; } }

        private List<TestTransition> _hintTransitionList;
        public List<TestTransition> HintTransitionList
        {
            get
            {
                if (_hintTransitionList == null)
                {
                    _hintTransitionList = new List<TestTransition>();
                }
                return _hintTransitionList;
            }
        }

        /// <summary>
        /// Hint the transition it takes to leave the state.
        /// Set HintTransition on the State even if it takes parent state's transition.
        /// HintTransition forms a linked list when getting activity trace
        /// </summary>
        public TestTransition HintTransition
        {
            internal get
            {
                TestTransition ret = null;

                if (HintTransitionList.Count == 1)
                    ret = HintTransitionList[0];
                else if (HintTransitionList.Count != 0)
                    ret = HintTransitionList[_iterationNumber];

                return ret;
            }
            set { HintTransitionList.Add(value); }
        }

        public State ProductState
        {
            get { return _productState; }
            set { _productState = value; }
        }

        internal Outcome CurrentOutcome { get; set; }

        private Outcome _expectedOutcome = Outcome.Completed;
        public Outcome ExpectedOutcome
        {
            internal get { return _expectedOutcome; }
            set { _expectedOutcome = value; }
        }

        internal bool HintGenerateTrackingTrace { get; set; }

        private TestActivity _nullTrigger = null;
        internal TestActivity NullTrigger
        {
            get
            {
                if (_nullTrigger == null)
                {
                    _nullTrigger = new TestSequence("Null Trigger");
                }

                return _nullTrigger;
            }
        }

        internal void ResetForValidation()
        {
            _iterationNumber = 0;
            this.CurrentOutcome = this.ExpectedOutcome;

            if (this.Entry != null)
            {
                this.Entry.ResetForValidation();
            }

            if (this.Exit != null)
            {
                this.Exit.ResetForValidation();
            }
        }

        internal Outcome GetEntryTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;

            if (this.Entry != null)
            {
                outcome = this.Entry.GetTrace(traceGroup);
            }

            return outcome.Propogate();
        }

        internal Outcome GetExitTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;
            if (this.Exit != null)
            {
                outcome = this.Exit.GetTrace(traceGroup);
            }

            return outcome.Propogate();
        }

        internal Outcome GetTrace(TraceGroup stateTrace, TestTransition incomingTransition)
        {
            //Trace for InternalState
            this.CurrentOutcome = this.ExpectedOutcome;
            Outcome outcome = this.ExpectedOutcome;

            if (!this.IsFinal)
            {
                outcome = this.GetTransitInTrace(stateTrace, incomingTransition);

                if (this.CurrentOutcome.IsOverrideable)
                    this.CurrentOutcome = outcome;

                if (outcome.DefaultPropogationState == OutcomeState.Completed)
                {
                    if (this.HintTransition == null)
                    {
                        throw new InvalidOperationException(string.Format("TestStateMachineState '{0}' HintTransition is null", this.DisplayName));
                    }

                    outcome = this.GetTransitOutTrace(stateTrace);
                    if (this.CurrentOutcome.IsOverrideable)
                        this.CurrentOutcome = outcome;
                }
            }
            else
            {
                this.GetStartTrace(stateTrace);
                if (this.Entry != null)
                    outcome = this.Entry.GetTrace(stateTrace);

                if (this.CurrentOutcome.IsOverrideable)
                    this.CurrentOutcome = outcome;

                if (CurrentOutcome.DefaultPropogationState == OutcomeState.Completed)
                {
                    this.GetCloseTrace(stateTrace);
                }
                else if (CurrentOutcome.DefaultPropogationState == OutcomeState.Canceled)
                {
                    this.GetCancelTrace(stateTrace);
                }
                else if (CurrentOutcome is CaughtExceptionOutcome)
                {
                    this.GetFaultTrace(stateTrace);
                }
            }

            _iterationNumber++;

            return outcome.Propogate();
        }

        private void GetStartTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Executing));

            GetStateTrackingTrace(traceGroup);
        }

        private void GetFaultTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Faulted));

            if (this.CurrentOutcome is HandledExceptionOutcome)
            {
                traceGroup.Steps.Add(new WorkflowInstanceTrace(WorkflowInstanceState.UnhandledException));
            }
        }

        private void GetCancelTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
        }

        private void GetCloseTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Closed));
        }

        private Outcome GetTransitInTrace(TraceGroup stateTraceGroup, TestTransition incomingTransition)
        {
            HashSet<TestActivity> triggerHash = new HashSet<TestActivity>();
            UnorderedTraces triggerTrace = new UnorderedTraces();

            Outcome outcome = this.ExpectedOutcome;
            this.CurrentOutcome = this.ExpectedOutcome;

            this.GetStartTrace(stateTraceGroup);

            outcome = this.GetEntryTrace(stateTraceGroup);
            if (this.CurrentOutcome.IsOverrideable)
                this.CurrentOutcome = outcome;

            if (outcome.DefaultPropogationState == OutcomeState.Completed)
            {
                stateTraceGroup.Steps.Add(triggerTrace);
                foreach (TestTransition t in this.Transitions)
                {
                    // Shared trigger transitions
                    if (triggerHash.Add(t.Trigger))
                    {
                        outcome = t.GetTriggerTrace(triggerTrace);

                        if (outcome.DefaultPropogationState == OutcomeState.Completed)
                        {
                            outcome = t.GetConditionTrace(triggerTrace);

                            if (this.CurrentOutcome.IsOverrideable)
                                this.CurrentOutcome = outcome.Propogate();
                        }
                        else if (outcome.DefaultPropogationState == OutcomeState.Faulted || outcome is UncaughtExceptionOutcome || outcome is CaughtExceptionOutcome)
                        {
                            if (this.CurrentOutcome.IsOverrideable)
                                this.CurrentOutcome = outcome.Propogate();
                        }
                        // trigger cancel can mean two things: 
                        // 1. trigger is cancelled by another trigger. This is normal behavior.
                        // 2. trigger is cancelled externally. In such case, Transition.ExpectedOutcome should be set to canceled.
                        else if (outcome.DefaultPropogationState == OutcomeState.Canceled)
                        {
                            if (t.ExpectedOutcome.DefaultPropogationState == OutcomeState.Canceled)
                            {
                                if (this.CurrentOutcome.IsOverrideable)
                                    this.CurrentOutcome = outcome.Propogate();
                            }
                            continue;
                        }
                    }
                    else
                    {
                        outcome = t.GetConditionTrace(triggerTrace);

                        if (outcome.DefaultPropogationState != OutcomeState.Completed)
                        {
                            if (CurrentOutcome.IsOverrideable)
                                CurrentOutcome = outcome.Propogate();
                        }
                    }
                }
            }

            if (this.CurrentOutcome.DefaultPropogationState == OutcomeState.Canceled)
            {
                this.GetCancelTrace(stateTraceGroup);
            }

            return this.CurrentOutcome.Propogate();
        }

        private Outcome GetTransitOutTrace(TraceGroup traceGroup)
        {
            Outcome outcome = this.ExpectedOutcome;
            this.CurrentOutcome = this.ExpectedOutcome;

            outcome = this.GetExitTrace(traceGroup);

            if (CurrentOutcome.IsOverrideable)
                this.CurrentOutcome = outcome;

            // transition Action is always scheduled and executed after the current State's Exit.
            if (outcome.DefaultPropogationState == OutcomeState.Completed)
            {
                outcome = this.HintTransition.GetActionTrace(traceGroup);

                if (CurrentOutcome.IsOverrideable)
                    this.CurrentOutcome = outcome;
            }

            if (CurrentOutcome.DefaultPropogationState == OutcomeState.Completed)
            {
                this.GetCloseTrace(traceGroup);
            }
            else if (CurrentOutcome.DefaultPropogationState == OutcomeState.Canceled)
            {
                this.GetCancelTrace(traceGroup);
            }
            else if (CurrentOutcome is CaughtExceptionOutcome)
            {
                this.GetFaultTrace(traceGroup);
            }

            return this.CurrentOutcome.Propogate();
        }

        private void AddTransition(TestTransition transition)
        {
            if (transition != null)
            {
                _productState.Transitions.Add(transition.ProductTransition);
                transition.Source = this;
            }
        }

        private void GetStateTrackingTrace(TraceGroup traceGroup)
        {
            if (this.HintGenerateTrackingTrace)
            {
                traceGroup.Steps.Add(new UserTrace(string.Format("StateMachineTrackingRecord: '{0}' State: '{1}'",
                                                    this.StateMachineName,
                                                    this.DisplayName)));
            }
        }

        protected bool RemoveTransition(TestTransition t)
        {
            return this.ProductState.Transitions.Remove(t.ProductTransition);
        }

        protected void RemoveAtTransition(int i)
        {
            this.ProductState.Transitions.RemoveAt(i);
        }

        protected void InsertTransition(int i, TestTransition t)
        {
            this.ProductState.Transitions.Insert(i, t.ProductTransition);
            t.Source = this;
        }
    }
}

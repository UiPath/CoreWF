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
    public class TestStateMachine : TestActivity
    {
        private MemberCollection<TestStateMachineState> _states;
        private TestStateMachineState _initialState;

        public TestStateMachine()
        {
            this.ProductActivity = new StateMachine();
            _states = new MemberCollection<TestStateMachineState>(AddState)
            {
                RemoveItem = RemoveState,
                RemoveAtItem = RemoveAtState,
                InsertItem = InsertState
            };
        }

        public TestStateMachine(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public IList<Variable> Variables { get { return this.ProductStateMachine.Variables; } }

        public MemberCollection<TestStateMachineState> States { get { return _states; } }

        public TestStateMachineState InitialState
        {
            get { return _initialState; }
            set
            {
                _initialState = value;
                ProductStateMachine.InitialState = value == null ? null : _initialState.ProductState;
            }
        }

        private bool _hintGenerateTrackingTrace = true;
        public bool HintGenerateTrackingTrace
        {
            get { return _hintGenerateTrackingTrace; }
            set { _hintGenerateTrackingTrace = value; }
        }

        private StateMachine ProductStateMachine { get { return (StateMachine)this.ProductActivity; } }

        // Required by TestServiceConfiguration.AddTestEndpointsToWorkflowService to get TestReceive
        internal override IEnumerable<TestActivity> GetChildren()
        {
            List<TestActivity> children = new List<TestActivity>();
            Traverse(null, null, new Action<TestActivity>(
                                delegate (TestActivity t)
                                {
                                    children.Add(t);
                                }));

            return children;
        }

        private void Traverse(Action<TestStateMachineState> actionForState, Action<TestTransition> actionForTransition, Action<TestActivity> actionForActivity)
        {
            HashSet<TestStateMachineState> stateHash = new HashSet<TestStateMachineState>();
            HashSet<TestTransition> transitionHash = new HashSet<TestTransition>();
            HashSet<TestActivity> triggerHash = new HashSet<TestActivity>();

            Stack<TestStateMachineState> stack = new Stack<TestStateMachineState>();

            foreach (TestStateMachineState s in this.States)
            {
                stack.Push(s);
                stateHash.Add(s);
            }

            while (stack.Count > 0)
            {
                TestStateMachineState top = stack.Pop();
                if (actionForState != null)
                    actionForState(top);

                if (actionForActivity != null)
                {
                    if (top.Entry != null)
                        actionForActivity(top.Entry);

                    if (top.Exit != null)
                        actionForActivity(top.Exit);
                }

                foreach (TestTransition t in top.Transitions)
                {
                    if (transitionHash.Add(t))
                    {
                        if (actionForTransition != null)
                            actionForTransition(t);

                        if (t.Trigger != null)
                        {
                            if (triggerHash.Add(t.Trigger) && actionForActivity != null)
                                actionForActivity(t.Trigger);
                        }

                        if (t.Action != null && actionForActivity != null)
                        {
                            actionForActivity(t.Action);
                        }

                        if (t.To == null)
                        {
                            throw new InvalidOperationException(string.Format("TestTransition '{0}' To is null. TestTransition.Source is '{1}'", t.DisplayName, t.Source == null ? null : t.Source.DisplayName));
                        }

                        if (stateHash.Add(t.To))
                        {
                            stack.Push(t.To);
                        }
                    }
                }
            }
        }

        // TestStateMachineState and TestTransition is not TestActivity, so override ResetForValidation for custom handling.
        internal override void ResetForValidation()
        {
            Traverse(
                new Action<TestStateMachineState>(
                delegate (TestStateMachineState s)
                {
                    s.ResetForValidation();
                    s.HintGenerateTrackingTrace = this.HintGenerateTrackingTrace;
                    s.StateMachineName = this.DisplayName;
                }),
                new Action<TestTransition>(
                delegate (TestTransition t)
                {
                    t.ResetForValidation();
                }),
                null);

            base.ResetForValidation();
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // To support PartialTrust, StateMachineEventManagerFactory activity is used in Variable<StateMachineEventManager>.Default
            // The code below generates the expected trace for StateMachineEventManagerFactory activity
            new TestDummyTraceActivity("StateMachineEventManagerFactory").GetTrace(traceGroup);

            Stack<TestStateMachineState> stack = new Stack<TestStateMachineState>();

            // get trace for initial state
            TestTransition fakeInitialTransition = new TestTransition("fakeinitial")
            {
                To = this.InitialState
            };

            Outcome outcome = this.ExpectedOutcome;
            TraceGroup ordered = new OrderedTraces();
            traceGroup.Steps.Add(ordered);

            TestStateMachineState currentState = null;
            TestTransition t = fakeInitialTransition;

            while (t != null && t.To != null && t.To.IsFinal != true)
            {
                TestTransition nextTransition = null;
                currentState = t.To;

                OrderedTraces stateTrace = new OrderedTraces();
                ordered.Steps.Add(stateTrace);
                // keep HintTransition in nextTransition, because TestStateMachineState.GetTrace increases TestStateMachineState.iterationNumber
                nextTransition = currentState.HintTransition;
                outcome = currentState.GetTrace(stateTrace, t);

                if (CurrentOutcome.IsOverrideable)
                    CurrentOutcome = outcome.Propogate();

                if (CurrentOutcome.DefaultPropogationState != OutcomeState.Completed)
                    break;

                t = nextTransition;
            }

            if (t == null || t.To == null)
            {
                throw new InvalidOperationException("Invalid HintTransition: null");
            }
            else if (CurrentOutcome.DefaultPropogationState == OutcomeState.Completed)
            {
                if (t.To.IsFinal == true)
                {
                    outcome = t.To.GetTrace(traceGroup, null);
                    if (CurrentOutcome.IsOverrideable)
                        CurrentOutcome = outcome.Propogate();
                }
                else
                {
                    throw new InvalidOperationException(string.Format("Invalid HintTransition: {0}", t.To.DisplayName));
                }
            }
            // faulting and cancellation will be handled by TestActivity
        }

        protected void AddState(TestStateMachineState state)
        {
            if (state != null)
            {
                this.ProductStateMachine.States.Add(state.ProductState);
            }
            else
            {
                this.ProductStateMachine.States.Add(null);
            }
        }

        protected bool RemoveState(TestStateMachineState s)
        {
            return this.ProductStateMachine.States.Remove(s.ProductState);
        }

        protected void RemoveAtState(int i)
        {
            this.ProductStateMachine.States.RemoveAt(i);
        }

        protected void InsertState(int i, TestStateMachineState s)
        {
            this.ProductStateMachine.States.Insert(i, s.ProductState);
        }
    }
}

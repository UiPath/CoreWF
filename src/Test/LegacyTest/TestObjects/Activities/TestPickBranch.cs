// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using LegacyTest.Test.Common.TestObjects.Activities.Tracing;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;
using System;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestPickBranch
    {
        private readonly PickBranch _productPickBranch;

        public TestPickBranch()
        {
            _productPickBranch = new PickBranch();
        }

        public PickBranch ProductPickBranch
        {
            get
            {
                return _productPickBranch;
            }
        }

        public IList<Variable> Variables
        {
            get
            {
                return this.ProductPickBranch.Variables;
            }
        }

        public string DisplayName
        {
            get
            {
                return this.ProductPickBranch.DisplayName;
            }

            set
            {
                this.ProductPickBranch.DisplayName = value;
            }
        }

        private TestActivity _trigger;
        public TestActivity Trigger
        {
            get
            {
                return _trigger;
            }

            set
            {
                _trigger = value;
                if (value == null)
                {
                    this.ProductPickBranch.Trigger = null;
                }
                else
                {
                    this.ProductPickBranch.Trigger = value.ProductActivity;
                }
            }
        }

        private TestActivity _action;
        public TestActivity Action
        {
            get
            {
                return _action;
            }

            set
            {
                _action = value;
                if (value == null)
                {
                    this.ProductPickBranch.Action = null;
                }
                else
                {
                    this.ProductPickBranch.Action = value.ProductActivity;
                }
            }
        }

        private Outcome _expectedOutcome = Outcome.None;
        public Outcome ExpectedOutcome
        {
            get
            {
                return _expectedOutcome;
            }
            set
            {
                _expectedOutcome = value;
            }
        }

        private Outcome TriggerOutcome
        {
            get;
            set;
        }

        // if the previous branch is Faulted, then this branch will not execute the trigger
        private Boolean _hintTriggerScheduled = false;
        public Boolean HintTriggerScheduled
        {
            get
            {
                return _hintTriggerScheduled;
            }
            set
            {
                _hintTriggerScheduled = value;
            }
        }

        internal Outcome GetTriggerTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Executing));

            if (this.HintTriggerScheduled)
            {
                this.TriggerOutcome = this.ExpectedOutcome;
            }
            else
            {
                this.TriggerOutcome = this.Trigger.GetTrace(traceGroup);
            }

            // All activities should complete with one of the following
            switch (this.TriggerOutcome.DefaultPropogationState)
            {
                case OutcomeState.Completed:
                    // close now if there is no action
                    if (_action == null)
                    {
                        traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Closed));
                    }
                    break;
                case OutcomeState.Canceled:
                    traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
                    break;
                case OutcomeState.Faulted:
                    traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Faulted));
                    //A handled exception, therefore we should output a workflow instance trace
                    if (this.TriggerOutcome is HandledExceptionOutcome)
                    {
                        traceGroup.Steps.Add(new WorkflowInstanceTrace(WorkflowInstanceState.UnhandledException));
                    }
                    break;
                default:
                    break;
            }

            return this.TriggerOutcome;
        }

        internal Outcome GetActionTrace(TraceGroup traceGroup)
        {
            Outcome bOutcome = null;

            if (this.Trigger != null && this.TriggerOutcome.DefaultPropogationState == OutcomeState.Completed && this.Action != null)
            {
                //traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Executing));


                if (this.ExpectedOutcome.DefaultPropogationState == OutcomeState.None)
                {
                    bOutcome = this.Action.GetTrace(traceGroup);
                }
                else
                {
                    bOutcome = this.ExpectedOutcome;
                }

                // All activities should complete with one of the following
                switch (bOutcome.DefaultPropogationState)
                {
                    case OutcomeState.Completed:
                        traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Closed));
                        break;
                    case OutcomeState.Canceled:
                        traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Canceled));
                        break;
                    case OutcomeState.Faulted:
                        traceGroup.Steps.Add(new ActivityTrace(this.DisplayName, ActivityInstanceState.Faulted));
                        //A handled exception, therefore we should output a workflow instance trace
                        if (bOutcome is HandledExceptionOutcome)
                        {
                            traceGroup.Steps.Add(new WorkflowInstanceTrace(WorkflowInstanceState.UnhandledException));
                        }
                        break;
                    default:
                        break;
                }
            }
            this.TriggerOutcome = null;

            return bOutcome;
        }
    }
}

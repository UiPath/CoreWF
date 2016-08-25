// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestDelay : TestActivity
    {
        private TimeSpan _duration;
        public TestDelay()
        {
            this.ProductActivity = new Delay();
            //this.HintIdleAction = TestOnIdleAction.None;
            this.HintWorkflowIdentity = null;
        }

        public TestDelay(string displayName, TimeSpan duration)
            : this()
        {
            this.Duration = duration;
            this.DisplayName = displayName;
        }

        public TimeSpan Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                this.ProductDelay.Duration = new InArgument<TimeSpan>(value);
            }
        }

        public Activity<TimeSpan> DurationExpression
        {
            set
            {
                this.ProductDelay.Duration = new InArgument<TimeSpan>(value);
            }
        }

        // public TestOnIdleAction HintIdleAction { get; set; }

        //Identity of the worklfow when this activity will get executed. Used to create the expected trace
        public WorkflowIdentity HintWorkflowIdentity { get; set; }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            /*switch (this.HintIdleAction)
            {
                case TestOnIdleAction.Unload:
                    traceGroup.Steps.Add(new WorkflowInstanceTrace(this.HintWorkflowIdentity, WorkflowInstanceState.Unloaded));
                    traceGroup.Steps.Add(new WorkflowInstanceTrace(this.HintWorkflowIdentity, WorkflowInstanceState.Resumed));
                    break;
                case TestOnIdleAction.UnloadNoAutoLoad:
                    traceGroup.Steps.Add(new WorkflowInstanceTrace(this.HintWorkflowIdentity, WorkflowInstanceState.Unloaded));
                    break;
                default:
                    //no op
                    break;
            }*/
        }

        private Delay ProductDelay
        {
            get
            {
                return (Delay)this.ProductActivity;
            }
        }
    }
}

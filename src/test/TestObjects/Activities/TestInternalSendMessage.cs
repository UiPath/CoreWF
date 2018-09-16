// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestInternalSendMessage : TestActivity
    {
        public TestInternalSendMessage()
        {
            this.ProductActivity = new Sequence();
            this.DisplayName = "InternalSendMessage";
        }

        public Outcome OpenChannelFactoryOutcome = Outcome.Completed;
        public Outcome WaitOnChannelCorrelationOutcome = Outcome.Completed;
        public Outcome OpenChannelAndSendMessageOutcome = Outcome.Completed;

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // This trace is marked optional since it may not always be there.
            traceGroup.Steps.Add(GetTraces(OpenChannelFactoryOutcome, "OpenChannelFactory"));

            traceGroup.Steps.Add(new UnorderedTraces()
            {
                Optional = true,
                Steps =
                {
                    // This trace is marked optional since it may not always be there.
                    GetTraces( WaitOnChannelCorrelationOutcome, "WaitOnChannelCorrelation"),

                    // This trace is marked optional since it may not always be there.
                    GetTraces( OpenChannelAndSendMessageOutcome, "OpenChannelAndSendMessage"),
                }
            });
        }

        public static OrderedTraces GetTraces(Outcome expectedOutcome, String name)
        {
            OrderedTraces ot = new OrderedTraces() { Optional = true };
            ot.Steps.Add(new ActivityTrace(name, ActivityInstanceState.Executing) { Optional = true });

            switch (expectedOutcome.DefaultPropogationState)
            {
                case OutcomeState.Completed:
                    ot.Steps.Add(new ActivityTrace(name, ActivityInstanceState.Closed) { Optional = true });
                    break;
                case OutcomeState.Canceled:
                    ot.Steps.Add(new ActivityTrace(name, ActivityInstanceState.Canceled) { Optional = true });
                    break;
                case OutcomeState.Faulted:
                    ot.Steps.Add(new ActivityTrace(name, ActivityInstanceState.Faulted) { Optional = true });
                    break;
                default:
                    break;
            }

            return ot;
        }
    }
}

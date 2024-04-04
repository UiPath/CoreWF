// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.Utilities.Validation
{
    // [Serializable]
    public abstract class TraceFilter
    {
        // Actual Trace Filter
        public abstract bool IsAllowed(IActualTraceStep actualTrace);

        // Expected Trace Filter
        public abstract bool IsAllowed(WorkflowTraceStep actualTrace);

        public ActualTrace FilterActualTrace(ActualTrace at)
        {
            // Actual trace is much easier, since it will always be a collection of ordered traces
            ActualTrace actualTrace = new ActualTrace();

            lock (at.Steps)
            {
                foreach (IActualTraceStep step in at.Steps)
                {
                    if (IsAllowed(step))
                    {
                        actualTrace.Add(step);
                    }
                }
            }
            return actualTrace;
        }

        public ExpectedTrace FilterExpectedTrace(ExpectedTrace et)
        {
            // This can be any combination of ordered/unordered traces, and we have to maintain that architecture

            // use copy constructor to maintain Expected traces' settings
            ExpectedTrace expectedTrace = new ExpectedTrace(et)
            {
                // copy constructor will also copy of the expected traces, need to clear them
                Trace = FilterTraceGroup(et.Trace)
            };
            return expectedTrace;
        }

        private TraceGroup FilterTraceGroup(TraceGroup oldtraces)
        {
            TraceGroup newtraces = (oldtraces is OrderedTraces) ? (TraceGroup)new OrderedTraces() : (TraceGroup)new UnorderedTraces();
            newtraces.Optional = oldtraces.Optional;

            foreach (WorkflowTraceStep step in oldtraces.Steps)
            {
                // this will either be a group of traces
                if (step is TraceGroup)
                {
                    newtraces.Steps.Add(FilterTraceGroup(step as TraceGroup));
                }
                // otherwise it is an individual trace so we should check the filter
                else
                {
                    if (IsAllowed(step))
                    {
                        newtraces.Steps.Add(step);
                    }
                }
            }
            return newtraces;
        }
    }

    // [Serializable]
    public class DisplayNameTraceFilter : TraceFilter
    {
        private List<string> _displayNames;
        public List<string> DisplayNames
        {
            get
            {
                if (_displayNames == null)
                {
                    _displayNames = new List<string>();
                }
                return _displayNames;
            }
            set
            {
                _displayNames = value;
            }
        }

        public override bool IsAllowed(IActualTraceStep actualTrace)
        {
            return !(actualTrace is ActivityTrace at) || !this.DisplayNames.Contains(at.ActivityName);
        }

        public override bool IsAllowed(WorkflowTraceStep actualTrace)
        {
            return !(actualTrace is ActivityTrace at) || !this.DisplayNames.Contains(at.ActivityName);
        }
    }

    // [Serializable]
    public class InternalActivityFilter : DisplayNameTraceFilter
    {
        public InternalActivityFilter()
        {
            this.DisplayNames = new List<string>()
            {
                // Messaging activities
                "WaitOnChannelCorrelation",
                "OpenChannelAndSendMessage",
                "OpenChannelFactory",
                "InternalSendMessage",
                "InternalReceiveMessage",
                "ToRequest",
                "FromRequest",
                "ToReply",
                "FromReply",
                "WaitForReply"
            };
        }
    }
}

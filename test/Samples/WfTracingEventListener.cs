// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Samples
{
    public class WfTracingEventListener : EventListener
    {
        private List<EventWrittenEventArgs> _recordedEvents = new List<EventWrittenEventArgs>();

        public List<EventWrittenEventArgs> RecordedEvents { get { return _recordedEvents; } }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            _recordedEvents.Add(eventData);
        }
    }
}

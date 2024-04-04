// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Activities.Tracking;

namespace LegacyTest.Test.Common.TestObjects.Tracking
{
    public class TrackingParticipantWithDelay : InMemoryTrackingParticipant
    {
        private static readonly TimeSpan s_defaultDelay = TimeSpan.FromSeconds(20);

        public TrackingParticipantWithDelay()
        {
            this.Delay = TrackingParticipantWithDelay.s_defaultDelay;
        }

        public TimeSpan Delay
        {
            get;
            set;
        }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            Thread.Sleep(this.Delay);
            base.Track(record, timeout);
        }
    }
}

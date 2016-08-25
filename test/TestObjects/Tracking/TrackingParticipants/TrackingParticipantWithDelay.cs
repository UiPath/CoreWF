// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.CoreWf.Tracking;

namespace Test.Common.TestObjects.Tracking
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

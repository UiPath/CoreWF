// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Tracking;

namespace Test.Common.TestObjects.Tracking
{
    public class TrackingParticipantWithException : InMemoryTrackingParticipant
    {
        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            throw new Exception(TrackingConstants.ExceptionMessage);
        }
    }
}

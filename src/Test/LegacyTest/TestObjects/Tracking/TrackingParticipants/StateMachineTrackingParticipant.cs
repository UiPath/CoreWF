// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Statements.Tracking;
using System.Activities.Tracking;
using System.Diagnostics;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Tracking
{
    public class StateMachineTrackingParticipant : InMemoryTrackingParticipant
    {
        public StateMachineTrackingParticipant() : base()
        {
        }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {

            if (!(record is StateMachineStateRecord smRecord))
            {
                base.Track(record, timeout);
            }
            else
            {
                StateMachineTrackingParticipant.TrackStateMachineRecord(record as StateMachineStateRecord);
            }
        }

        private static void TrackStateMachineRecord(StateMachineStateRecord stateMachineRecord)
        {
            UserTrace userTrace = new UserTrace(
                stateMachineRecord.InstanceId,
                stateMachineRecord.Activity.Id + ":" + stateMachineRecord.Activity.InstanceId,
                string.Format("StateMachineTrackingRecord: '{0}' State: '{1}'",
                                stateMachineRecord.StateMachineName,
                                stateMachineRecord.StateName));

            TraceSource ts = new TraceSource("System.Activities.Tracking", SourceLevels.Information);
            //PartialTrustTrace.TraceData(ts, TraceEventType.Information, 1, userTrace);
        }
    }
}

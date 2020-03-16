// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Tracking;
using System.Collections.Generic;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Tracking
{
    public class InMemoryTrackingParticipant : TestTrackingParticipantBase
    {
        static private Dictionary<Guid, ActualTrace> s_tracesDictionary = new Dictionary<Guid, ActualTrace>();

        public InMemoryTrackingParticipant()
        {
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]New instance of InMemoryTrackingParticipant invoked...");
            this.TestTraceManager = TestTraceManager.Instance;
            //this.ProfileProvider = new CustomCodeProfileProvider();
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]InMemoryTrackingParticipant instantiated...");
        }

        public override IEnumerable<TrackingProfile> GetTrackingProfiles(Guid workflowInstanceId)
        {
            return new List<TrackingProfile>() { this.ProfileProvider.GetActiveTrackingProfile() };
        }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            try
            {
                TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]Track()::InMemory tracking participant = {0} ; Tracking record type = {1} ; Record Details = {2}", this.Name, record.GetType(), record.ToString());
                TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]TestTraceManager.IsDefaultTrackingConfiguration = {0}", TestTraceManager.IsDefaultTrackingConfiguration.ToString());
                bool IsPushDataToTraceManager = true;

                ActualTrace _trace = this.GetActualTrackingData(record.InstanceId);

                if (record is WorkflowInstanceRecord)
                {
                    TrackWorkflowInstanceRecord(record as WorkflowInstanceRecord, _trace);
                }
                else if (record is ActivityStateRecord)
                {
                    IsPushDataToTraceManager = TrackActivityStateRecord(record as ActivityStateRecord, _trace);
                }
                else if (record is ActivityScheduledRecord)
                {
                    TrackActivityScheduledRecord(record as ActivityScheduledRecord, _trace);
                }
                else if (record is BookmarkResumptionRecord)
                {
                    TrackBookmarkResumptionRecord(record as BookmarkResumptionRecord, _trace);
                }
                else if (record is CancelRequestedRecord)
                {
                    TrackCancelRequestedRecord(record as CancelRequestedRecord, _trace);
                }
                else if (record is FaultPropagationRecord)
                {
                    TrackFaultPropagationRecord(record as FaultPropagationRecord, _trace);
                }
                if (IsPushDataToTraceManager)
                {
                    PushDataToTraceManager(record);
                }
            }
            //This exception will be eaten by the product tracking code and not available for review
            //So the only chance we have to see it is if we log it.
            catch (Exception e)
            {
                TestTraceManager.OptionalLogTrace(e.ToString());
                throw;
            }
        }

        public override ActualTrace GetActualTrackingData(Guid workflowInstanceId)
        {
            lock (s_tracesDictionary)
            {
                if (!s_tracesDictionary.ContainsKey(workflowInstanceId))
                {
                    s_tracesDictionary.Add(workflowInstanceId, new ActualTrace());
                }
            }
            return s_tracesDictionary[workflowInstanceId];
        }

        private void TrackWorkflowInstanceRecord(WorkflowInstanceRecord workflowInstanceRecord, ActualTrace _trace)
        {
            WorkflowInstanceState workflowInstanceState = (WorkflowInstanceState)Enum.Parse(typeof(WorkflowInstanceState), workflowInstanceRecord.State);
            WorkflowInstanceTrace workflowInstanceTrace = new WorkflowInstanceTrace(workflowInstanceRecord.InstanceId, workflowInstanceRecord.WorkflowDefinitionIdentity, workflowInstanceState);

            _trace.Add(workflowInstanceTrace);
        }

        private bool TrackActivityStateRecord(ActivityStateRecord activityRecord, ActualTrace _trace)
        {
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]activityRecord.Name = {0}", activityRecord.Activity.Name);
            ActivityTrace activityTrace = new ActivityTrace(
                            activityRecord.Activity.Name,
                            (ActivityInstanceState)Enum.Parse(typeof(ActivityInstanceState), activityRecord.State), activityRecord);

            //to avoid the confusion b\w Executing & scheduling events, we always use scheduled
            if (activityTrace.ActivityStatus != ActivityInstanceState.Executing)
            {
                _trace.Add(activityTrace);
                return true;
            }

            //for tracking test cases, it may be the scenario that the profile does not have a scheduled record.
            //in that scenario, we need to add that explicitly to the trace.
            if (TestTraceManager.IsDefaultTrackingConfiguration == false)//is a tracking test case
            {
                if (
                    (_trace.Steps.Count == 0) ||
                    (
                        (_trace.Steps.Count != 0) &&
                        (_trace.Steps[_trace.Steps.Count - 1].Equals(activityTrace) == false)
                     )
                    )
                {
                    _trace.Add(activityTrace);
                    return true;
                }
            }

            return false;
        }

        private void TrackActivityScheduledRecord(ActivityScheduledRecord activityScheduledRecord, ActualTrace _trace)
        {
            //the scheduling record simply states that i am scheduled blah. Currently we do not have any support in TO for this. 
            //Hence, turning it off for now. we will have the tracking tests cover the validation.
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]activityScheduledRecord.TargetName = {0}", activityScheduledRecord.Child.Name);
            _trace.Add(new ActivityTrace(activityScheduledRecord.Child.Name,
                ActivityInstanceState.Executing, activityScheduledRecord));
        }

        private void TrackCancelRequestedRecord(CancelRequestedRecord cancelRecord, ActualTrace _trace)
        {
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]cancelRequestedRecord.TargetName = {0}", cancelRecord.Child.Name);

            //this gets propogated twice by the product: ActivityStates.Fault information.
            {
                // WFCore - AT LEAST the DoWhile test cases that Cancel were failing because of these "extra"
                // Activity Canceled records in the trace. It's not clear if this is the right thing to do.
                //_trace.Add(new ActivityTrace(
                //    cancelRecord.Child.Name,
                //    (ActivityInstanceState)Enum.Parse(typeof(ActivityInstanceState), ActivityStates.Canceled)));
            }
        }

        private void TrackFaultPropagationRecord(FaultPropagationRecord faultRecord, ActualTrace _trace)
        {
            string faultName = (faultRecord.FaultHandler == null) ? "<Unknown>" : faultRecord.FaultHandler.Name;
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]faultPropatationRecord.TargetName = {0}", faultName);
            //string status = "Faulted";

            // There is no Property called State in FaultPropagationRecord. Need to understand Fault propagation here
            //this gets propogated twice by the product: ActivityStates.Fault information.
            //if (faultRecord.State == ActivityStates.Schedule)
            //{
            //    _trace.Add(new ActivityTrace(
            //        faultName,
            //        (ActivityInstanceState)Enum.Parse(typeof(ActivityInstanceState), status)));
            //}
        }

        private void TrackBookmarkResumptionRecord(BookmarkResumptionRecord bookmarkResumptionRecord, ActualTrace _trace)
        {
            TestTraceManager.OptionalLogTrace("[InMemoryTrackingParticipant]BookmarkName = {0}, bookmarkResumptionRecord = {1} ", bookmarkResumptionRecord.BookmarkName, bookmarkResumptionRecord.ToString());
            _trace.Add(new BookmarkResumptionTrace(
                bookmarkResumptionRecord.BookmarkName,
                bookmarkResumptionRecord.BookmarkScope,
                bookmarkResumptionRecord.Owner.Name));

            //_trace.Add(new ActivityTrace(bookmarkResumptionRecord.ToString(),
            //    ActivityInstanceState.Executing));
        }
    }


    //Will be invoked in the remote IIS process
    public class RemoteInMemoryTrackingParticipant : InMemoryTrackingParticipant
    {
        public RemoteInMemoryTrackingParticipant()
        {
            //TestTraceManager.OptionalLogTrace("[RemoteInMemoryTrackingParticipant]try to instantiate the RemoteInMemoryTrackingParticipant...");
            //this.TestTraceManager = RemoteTestTraceListener.GetRemoteTraceManager();
            //this.ProfileProvider = new CustomCodeProfileProvider();
            //this.PushToTrackingDataManager = true;
            //TestTraceManager.OptionalLogTrace("[RemoteInMemoryTrackingParticipant]RemoteInMemoryTrackingParticipant instantiated");
        }
    }
}

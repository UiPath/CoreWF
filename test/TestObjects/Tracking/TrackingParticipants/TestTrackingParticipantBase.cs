// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Tracking;
using System.Collections.Generic;
using System.Diagnostics;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Tracking
{
    public abstract class TestTrackingParticipantBase : TrackingParticipant
    {
        private TestProfileProvider _profileProvider;
        private TrackingProfile _trackingProfile;

        public string Name
        {
            get;
            set;
        }
        public TestProfileProvider ProfileProvider
        {
            get { return _profileProvider; }
            set { _profileProvider = value; }
        }

        public override TrackingProfile TrackingProfile
        {
            get
            {
                if (_trackingProfile != null)
                {
                    return _trackingProfile;
                }
                if (ProfileProvider != null)
                {
                    _trackingProfile = ProfileProvider.GetActiveTrackingProfile();
                }
                return _trackingProfile;
            }
        }

        public string ActiveProfileName
        {
            get;
            set;
        }

        //internal bool to ensure that only one of the tracking participants pushes the tracking data to the test trace data manager
        internal bool PushToTrackingDataManager
        {
            get;
            set;
        }


        protected TestTraceManager TestTraceManager
        {
            get;
            set;
        }

        /// <summary>
        /// To make the default Sql tracking participant work for all test cases going forward, we implement it as follows:
        /// When the tracking participant needs to be added to the workflow, the OOB SqlTrackingParticipant get invoked.
        /// For verification purposes, the custom sql tracking participant is instantiated & the corresponding methods invoked.
        /// </summary>
        public static TestTrackingParticipantBase GetInstanceForVerification(TrackingParticipantType type)
        {
            return (TestTrackingParticipantBase)TestTrackingParticipantBase.GetInstance(type, ParticipantAssociation.TestVerification);
        }

        public static TrackingParticipant GetInstance(TrackingParticipantType type, ParticipantAssociation association)
        {
            switch (type)
            {
                case TrackingParticipantType.InMemoryTrackingParticipant:
                    return new InMemoryTrackingParticipant();
                //case TrackingParticipantType.SqlTrackingParticipant:
                //    if (association == ParticipantAssociation.WorkflowExtention)
                //    {
                //        TrackingParticipant trackingParticipant = new SqlTrackingParticipant();
                //        trackingParticipant.TrackingProfile = new TrackingProfile();
                //        return trackingParticipant;
                //    }
                //    else
                //    {
                //        return null;
                //            //new CustomSqlTrackingParticipant();
                //    }
                case TrackingParticipantType.CustomTrackingParticipant1WithException:
                case TrackingParticipantType.CustomTrackingParticipant2WithException:
                    return new TrackingParticipantWithException();
                case TrackingParticipantType.CustomTrackingParticipantWithDelay:
                    return new TrackingParticipantWithDelay();
                case TrackingParticipantType.StateMachineTrackingParticipant:
                    return new StateMachineTrackingParticipant();
            }
            //default.
            return new InMemoryTrackingParticipant();
        }


        public abstract ActualTrace GetActualTrackingData(Guid workflowInstanceId);

        public abstract IEnumerable<TrackingProfile> GetTrackingProfiles(Guid workflowInstanceId);


        public void PushDataToTraceManager(TrackingRecord data)
        {
            if (this.PushToTrackingDataManager != true)
            {
                return;
            }

            TraceEventType eventType = TraceEventType.Information;
            if (data is ActivityScheduledRecord)
            {
                ActivityScheduledRecord record = (ActivityScheduledRecord)data;
                string displayName = record.Child.Name;
                if (this.TestTraceManager.TraceFilter.Contains(displayName))
                {
                    return;
                }
                AddActivityTrace(eventType, record.InstanceId, displayName, ActivityInstanceState.Executing, record);
            }
            else if (data is ActivityStateRecord)
            {
                ActivityStateRecord record = (ActivityStateRecord)data;
                ActivityInstanceState state;
                if (TryConvertToEnum(record.State, out state))
                {
                    string displayName = record.Activity.Name;
                    if (this.TestTraceManager.TraceFilter.Contains(displayName))
                    {
                        return;
                    }
                    AddActivityTrace(eventType, record.InstanceId, displayName, state, record);
                }
            }
            else if (data is WorkflowInstanceAbortedRecord)
            {
                WorkflowInstanceAbortedRecord record = (WorkflowInstanceAbortedRecord)data;
                AddWorkflowInstanceAbortedTrace(eventType, record.InstanceId, record.Reason);
            }
            //else if (data is WorkflowInstanceUpdatedRecord)
            //{
            //    WorkflowInstanceUpdatedRecord record = (WorkflowInstanceUpdatedRecord)data;
            //    AddWorkflowInstanceUpdatedTrace(eventType, record.InstanceId, record.OriginalDefinitionIdentity, record.WorkflowDefinitionIdentity, record.State, record.BlockingActivities, record.IsSuccessful);
            //}

            else if (data is WorkflowInstanceRecord)
            {
                WorkflowInstanceRecord record = (WorkflowInstanceRecord)data;
                WorkflowInstanceState state = (WorkflowInstanceState)Enum.Parse(typeof(WorkflowInstanceState), record.State);
                //these are new states that got added as part of the DCR = 109342, we do not contain them in the expceted states, hence 
                //explicitly removing them. have separate test cases to test the states. 
                if ((state == WorkflowInstanceState.Suspended) ||
                    (state == WorkflowInstanceState.Unsuspended))
                {
                    return;
                }

                AddWorkflowInstanceTrace(eventType, record.InstanceId, record.WorkflowDefinitionIdentity, state);
            }
            else if (data is BookmarkResumptionRecord)
            {
                BookmarkResumptionRecord record = (BookmarkResumptionRecord)data;
                AddBookmarkResumptionTrace(eventType, record.InstanceId, record.Owner.Name, record.BookmarkName,
                    record.BookmarkScope);
            }
        }

        private void AddWorkflowInstanceTrace(TraceEventType eventType, Guid instanceId, WorkflowIdentity workflowDefintionIdentity, WorkflowInstanceState instanceStatus)
        {
            WorkflowInstanceTrace trace = new WorkflowInstanceTrace(instanceId, workflowDefintionIdentity, instanceStatus);
            this.TestTraceManager.AddTrace(instanceId, trace);
            if (workflowDefintionIdentity != null)
            {
                TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1} : {2,-11} : {3}", instanceId, workflowDefintionIdentity, eventType, instanceStatus));
            }
            else
            {
                TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1,-11} : {2}", instanceId, eventType, instanceStatus));
            }
        }

        //void AddWorkflowInstanceUpdatedTrace(TraceEventType eventType, Guid instanceId, WorkflowIdentity originalIdentity, WorkflowIdentity updatedIdentity, string state, IList<ActivityBlockingUpdate> blockingActivities, bool IsSuccessful)
        //{
        //    WorkflowInstanceUpdatedTrace trace = new WorkflowInstanceUpdatedTrace(instanceId, originalIdentity, updatedIdentity, (WorkflowInstanceState)Enum.Parse(typeof(WorkflowInstanceState), state));
        //    trace.BlockingUpdateActivities = blockingActivities;
        //    trace.IsSuccessful = IsSuccessful;
        //    this.TestTraceManager.AddTrace(instanceId, trace);
        //    string traceOriginalIdentity = originalIdentity == null ? "<NULL>" : originalIdentity.ToString();
        //    string traceUpdatedIdentity = updatedIdentity == null ? "<NULL>" : updatedIdentity.ToString();

        //    TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1} : {2} {3,-11} : {4}", instanceId, traceOriginalIdentity, traceUpdatedIdentity, eventType, trace.InstanceStatus));
        //}
        private void AddWorkflowInstanceAbortedTrace(TraceEventType eventType, Guid instanceId, string abortedReason)
        {
            WorkflowAbortedTrace trace = new WorkflowAbortedTrace(instanceId, new Exception(abortedReason));
            this.TestTraceManager.AddTrace(instanceId, trace);
            //TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1,-11} : {2,-9} : {3}", instanceId, eventType, state, displayName));
        }

        private void AddActivityTrace(TraceEventType eventType, Guid instanceId, string displayName, ActivityInstanceState state, TrackingRecord record)
        {
            ActivityTrace trace = new ActivityTrace(displayName, state, record);
            this.TestTraceManager.AddTrace(instanceId, trace);
            TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1,-11} : {2,-9} : {3}", instanceId, eventType, state, displayName));
        }

        private void AddBookmarkResumptionTrace(TraceEventType eventType, Guid instanceId, string displayName, string bookmarkName,
            Guid subinstanceId)
        {
            BookmarkResumptionTrace trace = new BookmarkResumptionTrace(bookmarkName, subinstanceId, displayName);
            this.TestTraceManager.AddTrace(instanceId, trace);
            TestTraceManager.OptionalLogTrace(string.Format("[TestTrackingParticipantBase]{0} : {1,-11} : {2} : {3} : {4}",
                instanceId, eventType, bookmarkName, subinstanceId.ToString(), displayName));
        }

        //need to put this into a common utility class. which one? 
        private bool TryConvertToEnum(string stateAsString, out ActivityInstanceState actualState)
        {
            foreach (ActivityInstanceState state in Enum.GetValues(typeof(ActivityInstanceState)))
            {
                if (state.ToString() == stateAsString)
                {
                    actualState = state;
                    return true;
                }
            }

            // We must have had a custom state that was tracked
            actualState = default(ActivityInstanceState);
            return false;
        }
    }
}

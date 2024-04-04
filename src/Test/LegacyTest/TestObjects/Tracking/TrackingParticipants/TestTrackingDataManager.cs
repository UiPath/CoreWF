// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Tracking;
using System.Collections.Generic;
using System.Linq;
using LegacyTest.Test.Common.TestObjects.Utilities;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Tracking
{
    public class TestTrackingDataManager
    {
        private static TestTrackingDataManager s_eventTrackingDataManager;
        private static Dictionary<Guid, TestTrackingDataManager> s_testTrackingDataManagers = new Dictionary<Guid, TestTrackingDataManager>();
        private Guid _workflowId;

        private Dictionary<String, TrackingParticipant> _trackingParticipants;

        private TestTrackingDataManager(Guid workflowId)
        {
            _workflowId = workflowId;
        }

        public bool HasInMemoryParticipant()
        {
            foreach (TrackingParticipant trackingParticipant in this.GetTrackingParticipants())
            {
                if (trackingParticipant.GetType() == typeof(InMemoryTrackingParticipant))
                {
                    //Log.TraceInternal("[TestTrackingDataManager]There is at least one InMemoryTrackingParticipant");
                    return true;
                }
            }
            //Log.TraceInternal("[TestTrackingDataManager]There are no InMemoryTrackingParticipants");
            return false;
        }

        //When the TestTrackingDataManager is called the first time we do not have workflow instanceId.
        //The static TestTrackingDataManager property maintains the TestTrackingDataManager until we get the WF instance Id.
        public static TestTrackingDataManager GetInstance()
        {
            lock (TestTrackingDataManager.s_testTrackingDataManagers)
            {
                TestTrackingDataManager testTrackingDataManager = new TestTrackingDataManager(Guid.Empty);
                TestTrackingDataManager.s_eventTrackingDataManager = testTrackingDataManager;
            }
            return TestTrackingDataManager.s_eventTrackingDataManager;
        }

        public static TestTrackingDataManager GetInstance(Guid workflowId)
        {
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]TestTrackingDataManager::GetInstance() " + workflowId.ToString());
            lock (TestTrackingDataManager.s_testTrackingDataManagers)
            {
                if (!TestTrackingDataManager.s_testTrackingDataManagers.ContainsKey(workflowId))
                {
                    if (TestTrackingDataManager.s_eventTrackingDataManager != null)
                    {
                        TestTrackingDataManager.s_eventTrackingDataManager._workflowId = workflowId;
                        TestTrackingDataManager.s_testTrackingDataManagers.Add(workflowId, TestTrackingDataManager.s_eventTrackingDataManager);
                        TestTrackingDataManager.s_eventTrackingDataManager = null;
                    }
                    else
                    {
                        TestTrackingDataManager.s_testTrackingDataManagers.Add(workflowId, new TestTrackingDataManager(workflowId));
                    }
                }
            }
            return TestTrackingDataManager.s_testTrackingDataManagers[workflowId];
        }

        public void InstantiateTrackingParticipants(IEnumerable<TrackingConfiguration> config)
        {
            _trackingParticipants = new Dictionary<string, TrackingParticipant>();
            bool isTrackingParticipantSelected = false;
            int numberOfTrackingParticipants = config.Count();
            foreach (TrackingConfiguration trackingConfig in config)
            {
                TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Adding tracking participant TrackingParticipantType = {0}, ProfileManagerType = {1}, TestProfileType = {2}",
                        trackingConfig.TrackingParticipantType,
                        trackingConfig.ProfileManagerType,
                        trackingConfig.TestProfileType);
                switch (trackingConfig.TrackingParticipantType)
                {
                    //case TrackingParticipantType.SqlTrackingParticipant:
                    //    //Sql works against the product & only for the default config profile provider
                    //    SqlTrackingParticipant sqlTrackingParticipant = (SqlTrackingParticipant)TestTrackingParticipantBase.GetInstance(trackingConfig.TrackingParticipantType, ParticipantAssociation.WorkflowExtention);
                    //    SqlTrackingConfiguration sqlTrackingConfig = trackingConfig as SqlTrackingConfiguration;
                    //    //sqlTrackingParticipant.TrackingProfile = TestProfileProvider.GetTrackingProfile(sqlTrackingConfig.ProfileName);
                    //    //Log.TraceInternal("[TestTrackingDataManager]sqlTrackingParticipant.TrackingProfile =" + sqlTrackingParticipant.TrackingProfile);

                    //    sqlTrackingParticipant.ParticipateInProcessTransaction = sqlTrackingConfig.IsTransactional;
                    //    sqlTrackingParticipant.ConnectionString = sqlTrackingConfig.ConnectionString;
                    //    this.trackingParticipants.Add(trackingConfig.TrackingParticipantName, sqlTrackingParticipant);
                    //    if (!isTrackingParticipantSelected)
                    //    {
                    //        //Log.TraceInternal("[TestTrackingDataManager]Test profile type = {0}, TestProfileProvider.IsAllOrNullProfile(trackingConfig.TestProfileType) = {1}", trackingConfig.TestProfileType, TestProfileProvider.IsAllOrNullProfile(trackingConfig.TestProfileType));
                    //        //if (TestProfileProvider.IsAllOrNullProfile(trackingConfig.TestProfileType) || (numberOfTrackingParticipants == 1))
                    //        //{
                    //        //    isTrackingParticipantSelected = true;
                    //        //    sqlTrackingConfig.PushToTrackingDataManager = true;
                    //        //}
                    //    }
                    //    //Log.TraceInternal("[TestTrackingDataManager]Added PRODUCT SqlTrackingParticipant with: ProfileName={0}, isTransactional={1}, ConnectionString={2}",
                    //    //    sqlTrackingParticipant.TrackingProfile.Name,
                    //    //    sqlTrackingParticipant.ParticipateInProcessTransaction.ToString(),
                    //    //    sqlTrackingParticipant.ConnectionString);
                    //    break;
                    case TrackingParticipantType.InMemoryTrackingParticipant:
                        InMemoryTrackingParticipant memoryTrackingParticipant = (InMemoryTrackingParticipant)TestTrackingParticipantBase.GetInstance(trackingConfig.TrackingParticipantType, ParticipantAssociation.WorkflowExtention);
                        memoryTrackingParticipant.PushToTrackingDataManager = true;
                        _trackingParticipants.Add(trackingConfig.TrackingParticipantName, memoryTrackingParticipant);
                        break;

                    default:
                        TestTrackingParticipantBase trackingParticipant = (TestTrackingParticipantBase)TestTrackingParticipantBase.GetInstance(trackingConfig.TrackingParticipantType, ParticipantAssociation.TestVerification);
                        //trackingParticipant.ProfileProvider = TestProfileProvider.GetInstance(trackingConfig);
                        trackingParticipant.ProfileProvider.ActiveTrackingProfile = trackingConfig.TestProfileType;
                        trackingParticipant.Name = trackingConfig.TrackingParticipantName;
                        _trackingParticipants.Add(trackingConfig.TrackingParticipantName, trackingParticipant);
                        if (!isTrackingParticipantSelected)
                        {
                            //if (TestProfileProvider.IsAllOrNullProfile(trackingConfig.TestProfileType) || (numberOfTrackingParticipants == 1))
                            //{
                            //    isTrackingParticipantSelected = true;
                            //    trackingParticipant.PushToTrackingDataManager = true;
                            //}
                        }
                        break;
                }
                TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Successfully added tracking participant");
            }
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Successfully added all tracking participants...");
        }


        public ActualTrace GetActualTrackingData(string trackingParticipantName)
        {
            if (_trackingParticipants == null || _trackingParticipants.Count() == 0)
            {
                return new ActualTrace();
            }

            TrackingParticipant trackingParticipant = _trackingParticipants[trackingParticipantName];
            if (trackingParticipant is TestTrackingParticipantBase testTrackingParticipant)
            {
                return testTrackingParticipant.GetActualTrackingData(_workflowId);
            }
            else
            {
                //for sql tracking participant.
                return null;
            }
        }


        public IEnumerable<TrackingParticipant> GetTrackingParticipants()
        {
            return _trackingParticipants.Values.AsEnumerable();
        }

        public static bool DoesAnyProfileTrackAllEvents(IEnumerable<TrackingConfiguration> config)
        {
            //any non-sql/etw tracking participant that tracks all events?
            var results = from tc in config
                          where (
                                    tc.TestProfileType == TestProfileType.AllTrackpointsProfile ||
                                    tc.TestProfileType == TestProfileType.NullProfile
                               )
                                & (tc.TrackingParticipantType != TrackingParticipantType.SqlTrackingParticipant)
                                & (tc.TrackingParticipantType != TrackingParticipantType.ETWTrackingParticipant)
                          select tc;

            if ((results == null) || (results.Count() == 0))
            {
                //Log.TraceInternal("[TestTrackingDataManager]No in-memory tracking particiapnt exits that tracks all events.");
                return false;
            }
            else
            {
                return true;
            }
        }


        public static void ValidateTracking(ExpectedTrace expectedTrace, ActualTrace actualTrace, TrackingProfile profile, TestProfileType profileType, TrackingParticipantType participantType)
        {
            //1. Filter the expected trace against the workFlow profile
            ExpectedTrace filteredExpectedTrace = TrackingFilter.ApplyTrackingProfile(expectedTrace, profile);

            ////2. Delete not supported trace steps by testObjects.
            ActualTrace modifiedActualTrace = TrackingFilter.DeleteNotSupportedTraceSteps(actualTrace);

            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]*****ValidateTracking()");
            ////3. Validate the expected & the actual trace
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Profile = {0}", profile);
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Actual Trace = {0}", actualTrace);
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Expected Trace = {0}", expectedTrace);
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Filtered Expected Trace (after applying tracking profile) = {0}", filteredExpectedTrace);
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Modified ActualTrace Trace = {0}", modifiedActualTrace);
            TestTraceManager.OptionalLogTrace("[TestTrackingDataManager]Invoking internally the trace validation...");

            ////if (!(TestProfileProvider.IsAllOrNullProfile(profileType) &&
            if (((participantType != TrackingParticipantType.SqlTrackingParticipant) &&
                (participantType != TrackingParticipantType.ETWTrackingParticipant)))
            {
                modifiedActualTrace.Validate(filteredExpectedTrace, TestTraceManager.IsDefaultTrackingConfiguration);
            }

            //Log.TraceInternal("[TestTrackingDataManager]*****Validate method Succeeded...");
        }
    }
}

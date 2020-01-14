// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Statements.Tracking;
using System.Activities.Tracking;
using System.Collections.Generic;

namespace Test.Common.TestObjects.Tracking
{
    public abstract class TestProfileProvider
    {
        public TestProfileType ActiveTrackingProfile
        {
            get;
            set;
        }


        public List<string> ActivityNames
        {
            get;
            set;
        }


        public List<string> ActivityStates
        {
            get;
            set;
        }

        public static TrackingProfile GetTrackingProfile(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return GetTrackingProfileNullEmptyName(name);
            }
            else
            {
                return GetTrackingProfile(name, null);
            }
        }

        //Tracking Profile returned will be based on the AllTrackpointsProfile
        public static TrackingProfile GetTrackingProfileNullEmptyName(string name)
        {
            return new TrackingProfile()
            {
                Name = name,
                Queries =
                            {
                                new WorkflowInstanceQuery()
                                {
                                    States = { "*" },
                                },
                                new ActivityStateQuery()
                                {
                                    States = { "*" },
                                },
                                new BookmarkResumptionQuery()
                                {
                                   Name = "*",
                                },
                                new ActivityScheduledQuery()
                                {
                                    ActivityName = "*",
                                    ChildActivityName = "*",
                                }
                            }
            };
        }

        public static TrackingProfile GetTrackingProfile(string profileName, string scopeName)
        {
            TrackingProfile trackingProfile = null;
            //TrackingSection trackingSection = (TrackingSection)PartialTrustConfigurationManager.GetSection("system.serviceModel/tracking");
            //if (trackingSection == null)
            //{
            //    return null;
            //}

            //foreach (TrackingProfile profile in trackingSection.TrackingProfiles)
            //{
            //    if (profile.Name == profileName)
            //    {
            //        trackingProfile = profile;
            //        break;
            //    }
            //}

            //No matching profile with the specified name was found
            if (trackingProfile == null)
            {
                //return an empty profile
                trackingProfile = new TrackingProfile()
                {
                    ActivityDefinitionId = scopeName
                };
            }

            return trackingProfile;
        }

        public static TestProfileProvider GetInstance(TrackingConfiguration config)
        {
            TestProfileProvider testProfileProvider = null;
            switch (config.ProfileManagerType)
            {
                case ProfileManagerType.CodeProfileManager:
                    testProfileProvider = new CustomCodeProfileProvider();
                    break;
                default:
                    testProfileProvider = new CustomCodeProfileProvider();
                    break;
            }
            testProfileProvider.ActiveTrackingProfile = config.TestProfileType;
            if (config is FilteredTrackingConfiguration filteredTrackingConfiguration)
            {
                testProfileProvider.ActivityNames = filteredTrackingConfiguration.ActivityNames;
                testProfileProvider.ActivityStates = filteredTrackingConfiguration.ActivityStates;
            }
            return testProfileProvider;
        }


        public static TrackingProfile GetTrackingProfile(TestTrackingParticipantBase trackingParticipant, TrackingConfiguration config)
        {
            trackingParticipant.ProfileProvider = TestProfileProvider.GetInstance(config);
            trackingParticipant.ProfileProvider.ActiveTrackingProfile = config.TestProfileType;
            TrackingProfile profile = trackingParticipant.ProfileProvider.GetActiveTrackingProfile();
            return profile;
        }

        public static bool IsAllOrNullProfile(TestProfileType type)
        {
            if (
                (type == TestProfileType.AllTrackpointsProfile) ||
                (type == TestProfileType.NullProfile)
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public abstract TrackingProfile GetActiveTrackingProfile();
    }

    public class CustomCodeProfileProvider : TestProfileProvider
    {
        public TestProfileType ProfileToSwitch
        {
            get;
            set;
        }


        public override TrackingProfile GetActiveTrackingProfile()
        {
            //Log.TraceInternal("[CustomCodeProfileProvider]GetActiveTrackingProfile() ProfileType = {0}", this.ActiveTrackingProfile);
            TrackingProfile profile = new TrackingProfile
            {
                // Setting the visibility scope to All will retain the expected behaviour of old tests.
                ImplementationVisibility = ImplementationVisibility.All
            };
            WorkflowInstanceQuery wfInstanceQuery = new WorkflowInstanceQuery();
            ActivityStateQuery activityStateQuery = new ActivityStateQuery();
            ActivityScheduledQuery activityScheduledQuery = new ActivityScheduledQuery();
            BookmarkResumptionQuery bookmarkResumptionQuery = new BookmarkResumptionQuery();
            CustomTrackingQuery customTrackingQuery = new CustomTrackingQuery();
            CancelRequestedQuery cancelRequestedQuery = new CancelRequestedQuery();
            FaultPropagationQuery faultPropagationQuery = new FaultPropagationQuery();
            string all = "*";

            switch (this.ActiveTrackingProfile)
            {
                case TestProfileType.NullProfile:
                    profile = null;
                    break;

                case TestProfileType.UnavailableProfile:
                    //add nothing
                    break;

                case TestProfileType.NoProfile:
                    //add nothing.
                    break;

                case TestProfileType.EmptyProfile:
                    //add nothing. simply return the empty profile object
                    break;

                case TestProfileType.AllTrackpointsProfile:
                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case TestProfileType.AllWfTrackpointsProfile:
                    wfInstanceQuery.States.Add(WorkflowElementStates.Started);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Idle);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Closed);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Resumed);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(wfInstanceQuery);
                    break;

                case TestProfileType.ProfileScopeTarget:
                    wfInstanceQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Closed);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.DefaultITMonitoringProfile:
                    wfInstanceQuery.States.Add(WorkflowElementStates.Started);
                    profile.Queries.Add(wfInstanceQuery);

                    //activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    //profile.Queries.Add(activityScheduledQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Executing);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.AllActivityTrackpointsProfile:
                case TestProfileType.ActivityTrackpointOnlyAllActivitiesAllStates:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.ActivityTrackpointOnlyAllActivities1State:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.ActivityTrackpointOnlyAllActivities2States:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Completed);
                    activityStateQuery.States.Add(WorkflowElementStates.Faulted);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.ActivityTrackpointsOnlyProfile:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);
                    break;

                case TestProfileType.BookmarkTrackpointsOnlyProfile:
                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case TestProfileType.WFInstanceTrackpointsOnlyProfile:
                case TestProfileType.WFInstanceTrackpointOnlyAllActivitiesAllState:
                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);
                    break;

                case TestProfileType.WFInstanceTrackpointOnlyAllActivities1State:
                    wfInstanceQuery.States.Add(WorkflowElementStates.Started);
                    profile.Queries.Add(wfInstanceQuery);
                    break;

                case TestProfileType.WFInstanceTrackpointOnlyAllActivities2State:
                    wfInstanceQuery.States.Add(WorkflowElementStates.Started);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(wfInstanceQuery);
                    break;

                case TestProfileType.ActivityandBookmarkOnlyProfile:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(all);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case TestProfileType.WFInstanceandBookmarkOnlyProfile:
                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case TestProfileType.CustomWFEventsActivityCompletedOnly:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Closed);
                    wfInstanceQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.CustomWFEventsActivityExecutingOnly:
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Executing);
                    activityStateQuery.States.Add(WorkflowElementStates.Started);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.RandomProfile:
                    profile.Queries.Add(wfInstanceQuery);
                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Executing);
                    activityStateQuery.States.Add(WorkflowElementStates.Closed);
                    profile.Queries.Add(activityStateQuery);
                    break;

                case TestProfileType.MissingActivityNameProfile:

                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);

                    break;
                case TestProfileType.FuzzedProfileStatus:

                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case TestProfileType.FuzzedProfileStructure:

                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);
                    break;

                case (TestProfileType.RandomFuzzedProfile欱欲欳欴欵欶欷欸欹欺欻欼欽款欿歀歁歂):

                    wfInstanceQuery.States.Add(WorkflowElementStates.Completed);
                    profile.Queries.Add(wfInstanceQuery);

                    activityStateQuery.ActivityName = all;
                    activityStateQuery.States.Add(WorkflowElementStates.Closed);
                    profile.Queries.Add(activityStateQuery);

                    activityScheduledQuery.ActivityName = activityScheduledQuery.ChildActivityName = all;
                    profile.Queries.Add(activityScheduledQuery);

                    cancelRequestedQuery.ActivityName = cancelRequestedQuery.ChildActivityName = all;
                    profile.Queries.Add(cancelRequestedQuery);

                    faultPropagationQuery.FaultHandlerActivityName = all;
                    profile.Queries.Add(faultPropagationQuery);

                    bookmarkResumptionQuery.Name = all;
                    profile.Queries.Add(bookmarkResumptionQuery);

                    customTrackingQuery.ActivityName = all;
                    customTrackingQuery.Name = all;
                    profile.Queries.Add(customTrackingQuery);

                    break;
                case TestProfileType.StateMachineTrackpointsOnly:
                    wfInstanceQuery.States.Add(WorkflowElementStates.All);
                    profile.Queries.Add(wfInstanceQuery);

                    profile.Queries.Add(new StateMachineStateQuery()
                    {
                        ActivityName = all
                    });
                    break;
                default:
                    //Log.TraceInternal("[CustomCodeProfileProvider]Returning default null profile...");
                    profile = null;
                    break;
            }

            return profile;
        }
    }
}

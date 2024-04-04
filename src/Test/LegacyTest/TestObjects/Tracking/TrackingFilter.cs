// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Tracking;
using System.Linq;
using LegacyTest.Test.Common.TestObjects.Utilities;
using LegacyTest.Test.Common.TestObjects.Utilities.Validation;
using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.Tracking
{
    public static class TrackingFilter
    {
        //for workflow tracking records not yet supported by testObjects. 
        public static ActualTrace DeleteNotSupportedTraceSteps(ActualTrace actualTrace)
        {
            ActualTrace modifiedTrace = new ActualTrace(actualTrace);

            List<IActualTraceStep> stepsToBeRemoved = new List<IActualTraceStep>();

            foreach (IActualTraceStep actualTraceStep in modifiedTrace.Steps)
            {
                if (actualTraceStep is WorkflowInstanceTrace || actualTraceStep is BookmarkResumptionTrace)
                {
                    stepsToBeRemoved.Add(actualTraceStep);
                }
            }

            foreach (IActualTraceStep traceStepToBeRemoved in stepsToBeRemoved)
            {
                modifiedTrace.Steps.Remove(traceStepToBeRemoved);
            }

            return modifiedTrace;
        }

        public static ExpectedTrace ApplyTrackingProfile(ExpectedTrace expectedTrace, TrackingProfile profile)
        {
            ExpectedTrace modifiedTrace = TrackingFilter.RemovePlaceholderTrace(expectedTrace);
            modifiedTrace = TrackingFilter.RemoveUserTrace(modifiedTrace);
            TestTraceManager.OptionalLogTrace("[TrackingFilter]After Remove UserTrace, modifiedTrace = {0}", modifiedTrace);
            modifiedTrace = TrackingFilter.NormalizeTrace(modifiedTrace);
            TestTraceManager.OptionalLogTrace("[TrackingFilter]After NormalizeTrace, modifiedTrace = {0}", modifiedTrace);
            //vc temp only till we figure out the user record story for M2.
            if (profile == null)//all events to be returned
            {
                return modifiedTrace;
            }

            int count = modifiedTrace.Trace.Steps.Count;
            for (int i = 0; i < count; i++)
            {
                WorkflowTraceStep workflowTraceStep = modifiedTrace.Trace.Steps[i];

                // Check if this is a faulted state. 
                // When we have a faulted state the preceding executing state should be deleted.

                TrackingConfiguration currentTrackingConfiguration = GetCurrentTP(profile.Name);
                bool isExecutingRecExpectedOnFaultedState = true;
                if (!isExecutingRecExpectedOnFaultedState)
                {
                    ActivityTrace activityTrace = (ActivityTrace)workflowTraceStep;
                    if ((i > 0) && (activityTrace.ActivityStatus == ActivityInstanceState.Faulted))
                    {
                        ActivityTrace precedingActivityTrace = (ActivityTrace)modifiedTrace.Trace.Steps[i - 1];
                        if (precedingActivityTrace.ActivityStatus == ActivityInstanceState.Executing)
                        {
                            bool trackScheduledQuery = false;

                            foreach (ActivityScheduledQuery activityScheduledQuery in profile.Queries.OfType<ActivityScheduledQuery>())
                            {
                                if (IsActivityScheduledTracked(activityScheduledQuery, precedingActivityTrace.ActivityName))
                                {
                                    trackScheduledQuery = true;
                                }
                            }

                            // If we don't track the scheduled records delete the preceding executing state record.
                            // The preceding executing state is from scheduled record.
                            if (!trackScheduledQuery)
                            {
                                modifiedTrace.Trace.Steps.RemoveAt(i - 1);
                                i--;
                                count = modifiedTrace.Trace.Steps.Count;
                                //Log.TraceInternal("[TrackingFilter]Preceding executing activity trace deleted because the current expected trace state is Faulted.");
                            }
                        }
                    }
                }

                if (!profile.ShouldTrackStep(workflowTraceStep))
                {
                    modifiedTrace.Trace.Steps.RemoveAt(i);
                    count = modifiedTrace.Trace.Steps.Count;
                    //continue at the same step
                    i--;
                    //Log.TraceInternal("[TrackingFilter]Removed event = {0}=", workflowTraceStep);
                }
            }

            return modifiedTrace;
        }


        // Finds and returns the current tracking configuration from the key profile name.
        private static TrackingConfiguration GetCurrentTP(string profileName)
        {
            //foreach (TrackingConfiguration tc in TestConfiguration.Current.TrackingServiceConfigurations)
            //{
            //    if (profileName == tc.TrackingParticipantName)
            //    {
            //        return tc;
            //    }
            //}

            // There's a problem. We're not expecting to reach here.
            //throw new Exception("No matching tracking configuration is found!");
            return null;
        }


        private static ExpectedTrace NormalizeTrace(ExpectedTrace expectedTrace)
        {
            ExpectedTrace tempTrace = new ExpectedTrace(expectedTrace);

            TraceValidator.NormalizeExpectedTrace(tempTrace.Trace);

            return tempTrace;
        }


        private static bool ShouldTrackStep(this TrackingProfile profile, WorkflowTraceStep workflowTraceStep)
        {
            if (workflowTraceStep is TraceGroup)
            {
                // Don't filter out a nested TraceGroup.
                return true;
            }

            if (workflowTraceStep is ActivityTrace activityTrace)
            {
                //check the activity track queries
                foreach (ActivityStateQuery activityQuery in profile.Queries.OfType<ActivityStateQuery>())
                {
                    //either all activities are tracked or only this specific one.
                    if (TrackingFilter.IsActivityLocationTracked(activityQuery, activityTrace.ActivityName, activityTrace.ActivityStatus))
                    {
                        return true;
                    }
                }

                //check the ActivityScheduledQuery
                foreach (ActivityScheduledQuery activityScheduledQuery in profile.Queries.OfType<ActivityScheduledQuery>())
                {
                    //either all activities are tracked or only this specific one.
                    if (TrackingFilter.IsActivityScheduledTracked(activityScheduledQuery, activityTrace.ActivityName))
                    {
                        return true;
                    }
                }
            }

            if (workflowTraceStep is WorkflowInstanceTrace workflowInstanceTrace)
            {
                foreach (WorkflowInstanceQuery workflowInstanceTrackingQuery in profile.Queries.OfType<WorkflowInstanceQuery>())
                {
                    if (workflowInstanceTrackingQuery.States.Contains(workflowInstanceTrace.InstanceStatus.ToString()))
                    {
                        return true;
                    }
                }
            }

            if (workflowTraceStep is UserTrace userTrace)
            {
                //presently we (trackign team) do not track any userTrace values through profile om.
                return true;
            }
            return false;
        }

        private static string GetStatus(ActivityInstanceState activityInstanceState)
        {
            //the states b\w the wf runtime dev code & teh staus enums are not in sync.
            string status = activityInstanceState.ToString();
            if (activityInstanceState.ToString() == WorkflowElementStates.Executing)
            {
                status = ActivityStates.Schedule;
            }
            else if (activityInstanceState.ToString() == WorkflowElementStates.Faulted)
            {
                status = ActivityStates.Fault;
            }

            return status;
        }

        private static bool IsActivityLocationTracked(ActivityStateQuery activityQuery, string activityName, ActivityInstanceState activityInstanceState)
        {
            //todo_vc
            //if the activity query contains the "Executing" state we need to delete that. since there is no "Executing" state defined in product.
            //The correct fix is in the product. 

            //this is fixed now for Beta2.
            //activityQuery.States.Remove(WorkflowElementStates.Executing);

            if (
                (activityQuery.ActivityName == "*" || activityQuery.ActivityName == activityName)
                &&
                (activityQuery.States.Contains("*") || activityQuery.States.Contains(activityInstanceState.ToString()))
               )
            {
                return true;
            }
            return false;
        }


        private static bool IsActivityScheduledTracked(ActivityScheduledQuery activityScheduledQuery, string activityName)
        {
            if (
                (activityScheduledQuery.ChildActivityName == "*" || activityScheduledQuery.ChildActivityName == activityName)
               )
            {
                return true;
            }
            return false;
        }


        private static ExpectedTrace RemoveUserTrace(ExpectedTrace expectedTrace)
        {
            ExpectedTrace modifiedTrace = new ExpectedTrace(expectedTrace);
            int count = modifiedTrace.Trace.Steps.Count;
            int removedCount = 0;
            for (int i = 0; i < count; i++)
            {
                WorkflowTraceStep workflowTraceStep = expectedTrace.Trace.Steps[i];
                if (workflowTraceStep is UserTrace)
                {
                    modifiedTrace.Trace.Steps.Remove(workflowTraceStep);
                    removedCount++;
                    continue;
                }
                else if (workflowTraceStep is TraceGroup)
                {
                    ExpectedTrace tempExpectedTrace = new ExpectedTrace
                    {
                        Trace = TraceGroup.GetNewTraceGroup((TraceGroup)workflowTraceStep)
                    };
                    //take into account for activities already removed.
                    modifiedTrace.Trace.Steps.RemoveAt(i - removedCount);

                    ExpectedTrace cleanedUpExpectedTrace = TrackingFilter.RemoveUserTrace(tempExpectedTrace);

                    //add only if it is non-empty
                    if ((cleanedUpExpectedTrace != null) &&
                        (cleanedUpExpectedTrace.Trace != null) &&
                        (cleanedUpExpectedTrace.Trace.Steps != null) &&
                        (cleanedUpExpectedTrace.Trace.Steps.Count != 0)
                        )
                    {
                        modifiedTrace.Trace.Steps.Insert(i - removedCount, cleanedUpExpectedTrace.Trace);
                    }
                    else
                    {
                        removedCount++;
                    }
                }
            }
            return modifiedTrace;
        }


        private static ExpectedTrace RemovePlaceholderTrace(ExpectedTrace expectedTrace)
        {
            ExpectedTrace modifiedTrace = new ExpectedTrace(expectedTrace);
            int count = modifiedTrace.Trace.Steps.Count;
            for (int i = 0; i < count; i++)
            {
                WorkflowTraceStep workflowTraceStep = expectedTrace.Trace.Steps[i];
                if (workflowTraceStep is IPlaceholderTraceProvider)
                {
                    modifiedTrace.Trace.Steps[i] = ((IPlaceholderTraceProvider)workflowTraceStep).GetPlaceholderTrace();
                    continue;
                }
                else if (workflowTraceStep is TraceGroup)
                {
                    ExpectedTrace tempExpectedTrace = new ExpectedTrace
                    {
                        Trace = TraceGroup.GetNewTraceGroup((TraceGroup)workflowTraceStep)
                    };

                    modifiedTrace.Trace.Steps.RemoveAt(i);

                    ExpectedTrace cleanedUpExpectedTrace = TrackingFilter.RemovePlaceholderTrace(tempExpectedTrace);

                    //add only if it is non-empty
                    modifiedTrace.Trace.Steps.Insert(i, cleanedUpExpectedTrace.Trace);
                }
            }
            return modifiedTrace;
        }
    }//end of class Tracking filter
}

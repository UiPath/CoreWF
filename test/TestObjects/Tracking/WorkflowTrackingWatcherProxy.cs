// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf.Tracking;
using System.Collections.Generic;
using System.Threading;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Tracking
{
    // Watcher waits for specfic traces from the runtime
    //   Two ways that we can create a watcher, from a workflow runtime and a workflow service runtime
    //   A watcher is linked to a specific instance of a workflow. This means that we can prepopulate the 
    //   Watcher with an ExpectedTrace (this is done by TestWorkflowRuntime and TestWorkflowServiceRuntime).
    //
    //  Because this is created by the RemoteWorkflow* classes, it could be in a remoted app domain, in which case
    //   its marked as Marshal by ref so we get a ref to it in the original client app domain.
    //
    //  This class cant be merged into TestTraceManager, because TestTraceManager is singleton, and tracks workflow
    //   instance specific information
    public class WorkflowTrackingWatcher //: MarshalByRefObject
    {
        private Guid _workflowInstanceId;
        private RemoteWorkflowRuntime _remoteworkflowRuntime;
        private bool _hasPersistenceProvider;

        internal WorkflowTrackingWatcher(Guid instanceId, RemoteWorkflowRuntime runtime)
        {
            _workflowInstanceId = instanceId;
            _remoteworkflowRuntime = runtime;
            _hasPersistenceProvider = runtime.InstanceStoreType != null;
        }

        // To prevent the Overloads below from exploding into 15+ variations like we used to have, the traces should be set in advance as properties.
        public ExpectedTrace ExpectedTraces
        {
            get;
            set;
        }
        public ExpectedTrace ExpectedInstanceTraces
        {
            get;
            set;
        }

        #region Generic Trace events

        public void WaitForTrace(IActualTraceStep trace)
        {
            WaitForTrace(trace, 1);
        }

        public void WaitForTrace(IActualTraceStep trace, int numOccurences)
        {
            TestTraceManager.Instance.WaitForTrace(_workflowInstanceId, trace, numOccurences);
        }

        public void WaitForActivityStatusChange(string activityDisplayName, TestActivityInstanceState targetState, int numOccurences)
        {
            WaitForTrace(new ActivityTrace(activityDisplayName, ActivityTrace.GetActivityInstanceState(targetState)), numOccurences);
        }

        public void WaitForActivityStatusChange(string activityDisplayName, TestActivityInstanceState targetState)
        {
            WaitForActivityStatusChange(activityDisplayName, targetState, 1);
        }

        public IActualTraceStep WaitForEitherOfTraces(IActualTraceStep trace, IActualTraceStep otherTrace)
        {
            IActualTraceStep succesfulTrace;
            TestTraceManager.Instance.WaitForEitherOfTraces(_workflowInstanceId, trace, otherTrace, out succesfulTrace);
            return succesfulTrace;
        }

        public void WaitForEitherOfTraces(IActualTraceStep trace, IActualTraceStep otherTrace, out IActualTraceStep successfulTrace)
        {
            successfulTrace = WaitForEitherOfTraces(trace, otherTrace);
        }

        // legacy
        public void WaitForSynchronizeTrace()
        {
            WaitForSynchronizeTrace(1);
        }
        public void WaitForSynchronizeTrace(int numOccurences)
        {
            WaitForTrace(new SynchronizeTrace(RemoteWorkflowRuntime.CompletedOrAbortedHandlerCalled), numOccurences);
        }

        #endregion

        #region Final

        // Returns true if no aborted or unhandled exception
        internal void WaitForFinalTrace(WorkflowInstanceState state, int numOccurences)
        {
            // If not using the DefaultTracking configuration, then we need to wait for a user trace. The reason is that
            //  InMemoryTrackingParticipant, unlike SQL, pushed traces to the subscription. SQL however is pull only, 
            //  so the subscription will hang forever. The workaround is in the Tracking tests wait for the synchronize trace 
            //  since it is a user trace and we will always get it.
            if (!TestTraceManager.IsDefaultTrackingConfiguration)
            {
                WaitForSynchronizeTrace(numOccurences);
            }
            else
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                FinalTraceSubscription subscription = new FinalTraceSubscription(_workflowInstanceId, mre, numOccurences, _hasPersistenceProvider, state);
                TestTraceManager.Instance.AddSubscription(_workflowInstanceId, subscription);
                if (!mre.WaitOne(TimeSpan.FromSeconds(TestTraceManager.MaximumNumberOfSecondsToWaitForATrace)))
                {
                    throw new TimeoutException(string.Format("Waited for {0} seconds in WaitForFinalTrace without getting the expeced trace of {1}",
                        TestTraceManager.MaximumNumberOfSecondsToWaitForATrace, state.ToString()));
                }
                subscription.CheckIfSuccessful();
            }
        }

        public void WaitForTerminalState(WorkflowInstanceState expected, bool validate, int numOccurences)
        {
            WaitForFinalTrace(expected, numOccurences);

            if (this.ExpectedInstanceTraces == null)
            {
                this.ExpectedTraces.AddIgnoreTypes(false, typeof(WorkflowInstanceTrace));
            }

            if (validate)
            {
                if (this.ExpectedInstanceTraces == null)
                {
                    ValidateTrackingAndTracing(this.ExpectedTraces);
                }
                else
                {
                    ValidateTrackingAndTracing(this.ExpectedTraces, this.ExpectedInstanceTraces);
                }
            }
        }

        public void WaitForWorkflowCompletion()
        {
            this.WaitForWorkflowCompletion(true, 1);
        }

        public void WaitForWorkflowCompletion(bool validate, int numOccurences)
        {
            WaitForTerminalState(WorkflowInstanceState.Completed, validate, numOccurences);
        }

        public void WaitForWorkflowCanceled()
        {
            this.WaitForWorkflowCanceled(true, 1);
        }

        public void WaitForWorkflowCanceled(bool validate, int numOccurences)
        {
            WaitForTerminalState(WorkflowInstanceState.Canceled, validate, numOccurences);
        }

        public void WaitForWorkflowAborted(out Exception exception)
        {
            this.WaitForWorkflowAborted(out exception, true, 1);
        }

        public void WaitForWorkflowAborted(out Exception exception, bool validate, int numOccurences)
        {
            WaitForTerminalState(WorkflowInstanceState.Aborted, validate, numOccurences);
            exception = GetAbortedReason();
        }

        public void WaitForWorkflowTerminated(out Exception exception)
        {
            this.WaitForWorkflowTerminated(out exception, true, 1);
        }

        public void WaitForWorkflowTerminated(out Exception exception, bool validate, int numOccurences)
        {
            WaitForTerminalState(WorkflowInstanceState.Terminated, validate, numOccurences);
            exception = GetUnhandledException();
        }

        #endregion

        #region Helpers

        public Exception GetAbortedReason()
        {
            ActualTrace actualTrace = GetActualTrace();

            // Can shortcut in the workflowruntime case
            if (!IsHosted && _remoteworkflowRuntime.LastException != null)
            {
                return _remoteworkflowRuntime.LastException;
            }

            //With unhandled exceptions, the workflow running in standalone is aborted with AbortedTrace and it having the right exception message
            //But when hosted as a service, it gets the aborted trace with a generic system.exception message. 
            //To make HostWFAsService compatible with WFRuntime, we should check for the WorkflowExceptionTrace instead of WorkflowAbortedTrace.

            foreach (IActualTraceStep traceStep in actualTrace.Steps)
            {
                if (traceStep is WorkflowAbortedTrace)
                {
                    return ((WorkflowAbortedTrace)traceStep).AbortedReason;
                }
            }

            return null;
        }

        public Exception GetUnhandledException()
        {
            ActualTrace actualTrace = GetActualTrace();

            // Can shortcut in the workflowruntime case
            if (!IsHosted && _remoteworkflowRuntime.LastException != null)
            {
                return _remoteworkflowRuntime.LastException;
            }

            foreach (IActualTraceStep traceStep in actualTrace.Steps)
            {
                if (traceStep is WorkflowExceptionTrace)
                {
                    return ((WorkflowExceptionTrace)traceStep).InstanceException;
                }
            }
            return null;
        }

        public ActualTrace GetActualTrace()
        {
            return _remoteworkflowRuntime.ActualTrace;
        }

        private bool IsHosted
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Tracking and Tracing

        internal void ValidateTrackingAndTracing(ExpectedTrace expectedTrace)
        {
            expectedTrace.AddIgnoreTypes(false, typeof(WorkflowInstanceTrace));
            //The expected trace may get modified in the validateTraces method. Hence make a copy to be userd for tracking validation.
            ExpectedTrace copyOfExpectedTrace = new ExpectedTrace(expectedTrace);
            ValidateTracking(copyOfExpectedTrace);
            //Log.TraceInternal("[TestTracingWatcher]Validate Tracing...");
            ValidateTraces(expectedTrace, GetActualTrace());
        }

        internal void ValidateTrackingAndTracing(ExpectedTrace expectedTrace, ExpectedTrace expectedWorkflowInstanceTrace)
        {
            //The expected trace may get modified in the validateTraces method. Hence make a copy to be prior to tracking validation.
            //We are merging two sets of traces 
            //1) the traces generated by activities
            //2) the traces generated by workflowinstance method calls
            UnorderedTraces mergedTrace = new UnorderedTraces();
            mergedTrace.Steps.Add(expectedTrace.Trace);
            if (expectedWorkflowInstanceTrace != null)
            {
                mergedTrace.Steps.Add(expectedWorkflowInstanceTrace.Trace);
            }

            ExpectedTrace expectedMergedTrace = new ExpectedTrace(expectedTrace);
            expectedMergedTrace.Trace = mergedTrace;
            ValidateTracking(expectedTrace);

            //Log.TraceInternal("[TestWorkflowRuntime]***Validate Tracing...");

            ValidateTraces(expectedMergedTrace, GetActualTrace());
        }

        internal void ValidateTraces(ExpectedTrace expectedTrace, ActualTrace actualTrace)
        {
            //If no tracking participant tracks all events tracign validation will fail since it doesn't accoutn for the tracking profiles.
            //This is true ONLY for some trakcing test cases & not for runtime test cases which run with all tracking turned Off.
            //Hence we will not to tracing validation in this case & only do the tracking validation.
            //if (!TestTrackingDataManager.DoesAnyProfileTrackAllEvents(TestConfiguration.Current.TrackingServiceConfigurations))
            //{
            //    Log.TraceInternal("[TestTracingWatcher]No tracking participant tracks all events. Hece skipp tracing verification");
            //    return;
            //}

            foreach (TraceFilter filter in expectedTrace.Filters)
            {
                expectedTrace = filter.FilterExpectedTrace(expectedTrace);
                actualTrace = filter.FilterActualTrace(actualTrace);
            }

            actualTrace.Validate(expectedTrace);
        }

        internal void ValidateTracking(ExpectedTrace expectedTrace)
        {
            // Sort before 
            if (expectedTrace.SortBeforeVerification)
            {
                //Log.TraceInternal("[TestTracingWatcher] Sort before verification is enabled, tracking will go into an infinite loop, so skipping.");
                return;
            }

            ActualTrace actualTrace;
            //TestTrackingDataManager.ValidateTracking(expectedTrace, actualTrace, profile, trackingConfig.TestProfileType, trackingConfig.TrackingParticipantType);

            //expected trace needs to be validated with each of the trackign services that are currently enabled.
            //Initial Design: Moved the common validation to the base method to avoid redundant code. Validation still 
            //in the tracking service so as to take into account the scneario for profile versioning when 
            //the validation is tricky due to multipel profiles & the particular service may need finer control
            //for that particular workflow id.
            //Final Design: Moved it from the TrackingParticipant since we need validation to work fine even if say 
            //for Partial trust.
            foreach (TrackingConfiguration trackingConfig in _remoteworkflowRuntime.TrackingConfigurations)
            {
                TestTraceManager.OptionalLogTrace("[TestTracingWatcher]******Tracking validation for {0}", trackingConfig.TrackingParticipantName);

                //1. get the profile for the workFlow
                //note profiles are not seralizable. Hence you need ot get it from a local instance. (in account for the web-hosted scenario)
                TestTrackingParticipantBase trackingParticipant = TestTrackingParticipantBase.GetInstanceForVerification(trackingConfig.TrackingParticipantType);
                TrackingProfile profile = TestProfileProvider.GetTrackingProfile(trackingParticipant, trackingConfig);

                // Assign tracking participant name to new profile name because this will be used to find out 
                // the current tracking configuration in TrackingFilter.
                if (profile != null)
                {
                    profile.Name = trackingConfig.TrackingParticipantName;
                }

                switch (trackingConfig.TrackingParticipantType)
                {
                    case TrackingParticipantType.SqlTrackingParticipant:
                        SqlTrackingConfiguration sqlTrackingConfiguration = trackingConfig as SqlTrackingConfiguration;
                        if (sqlTrackingConfiguration != null)
                        {
                            trackingParticipant.PushToTrackingDataManager = sqlTrackingConfiguration.PushToTrackingDataManager;
                        }
                        actualTrace = trackingParticipant.GetActualTrackingData(_workflowInstanceId);

                        for (int i = profile.Queries.Count - 1; i >= 0; i--)
                        {
                            Type queryType = profile.Queries[i].GetType();
                            if (queryType == typeof(ActivityScheduledQuery))
                            {
                                profile.Queries.RemoveAt(i);
                            }
                        }
                        break;

                    default:
                        actualTrace = _remoteworkflowRuntime.ActualTrackingData(trackingConfig.TrackingParticipantName);
                        break;
                }

                //3. validate
                TestTrackingDataManager.ValidateTracking(expectedTrace, actualTrace, profile, trackingConfig.TestProfileType, trackingConfig.TrackingParticipantType);
            }
        }

        #endregion
    }

    /// <summary>
    /// This subscription waits for either an aborted trace, or else a Completed/Deleted trace.
    /// </summary>
    internal class FinalTraceSubscription : TestTraceManager.Subscription
    {
        private string _error = null;
        private WorkflowInstanceState _expectedFinalState;

        // marked readonly, should never edit this field to prevent race conditions
        private readonly int _numOccurences;
        private bool _waitingForDeleted;

        private Guid _instanceId;

        public FinalTraceSubscription(Guid instanceId, ManualResetEvent mre, int numOccurences, bool hasPersistence, WorkflowInstanceState finalState)
        {
            this.manualResetEvent = mre;
            _numOccurences = numOccurences;

            // If there is no persistence provider wait for the completed trace
            _expectedFinalState = finalState;

            // Also wait for the deleted
            if (hasPersistence) _numOccurences++;
            _waitingForDeleted = hasPersistence;

            _instanceId = instanceId;

            //Log.TraceInternal(String.Format("[FinalTraceSubscription]Instance {0} Waiting for {1}.", instanceId, (hasPersistence)?"Deleted":finalState.ToString()));
        }

        // Tracks whether we completed, or aborted, if numOccurrances > 1 then this will describe the last trace found
        internal void CheckIfSuccessful()
        {
            if (!string.IsNullOrEmpty(_error))
            {
                // want to throw the exception on the waiting thread
                throw new Exception(_error);
            }
        }

        internal override bool NotifyTraces(ActualTrace instanceTraces)
        {
            // Dont want to modify the original numOccurances
            int countCopy = _numOccurences;

            foreach (IActualTraceStep step in instanceTraces.Steps)
            {
                if (step is WorkflowAbortedTrace)
                {
                    countCopy--;

                    // if this aborted is the last trace, we wont have a deleted
                    if (_waitingForDeleted && countCopy == 1) countCopy--;

                    if (_expectedFinalState != WorkflowInstanceState.Aborted)
                    {
                        _error = string.Format("While waiting for a {0} trace, received an aborted trace - {1}", _expectedFinalState, ActualTracesAsString());
                    }
                }
                else if (step is WorkflowInstanceTrace)
                {
                    WorkflowInstanceTrace wit = (WorkflowInstanceTrace)step;

                    if (wit.InstanceStatus == WorkflowInstanceState.Deleted)
                    {
                        countCopy--;

                        // Deleted trace will always be last, if its not then this will hang, so instead complete it and throw exception
                        if (countCopy > 0)
                        {
                            _error = string.Format("Received deleted trace, before the expected number of {0} - {1}.", _expectedFinalState, ActualTracesAsString());
                            countCopy = 0;
                        }
                    }
                    else if (wit.InstanceStatus == WorkflowInstanceState.Completed ||
                        wit.InstanceStatus == WorkflowInstanceState.Canceled ||
                        wit.InstanceStatus == WorkflowInstanceState.Terminated)
                    {
                        // reset error
                        countCopy--;

                        _error = (wit.InstanceStatus == _expectedFinalState) ? null :
                            string.Format("While waiting for a {0} trace, received another terminal trace, {1} - {2}.",
                                    _expectedFinalState.ToString(), wit.InstanceStatus.ToString(), ActualTracesAsString());
                    }
                }
            }

            bool success = countCopy <= 0;
            if (success)
            {
                this.manualResetEvent.Set();
            }
            return success;
        }

        private string ActualTracesAsString()
        {
            string concatenatedSteps = string.Format("InstanceID {0} traces: ", _instanceId.ToString());
            ActualTrace actualTrace = TestTraceManager.Instance.GetInstanceActualTrace(_instanceId);
            foreach (IActualTraceStep step in actualTrace.Steps)
            {
                concatenatedSteps = concatenatedSteps + "; " + step.ToString();
            }
            return concatenatedSteps;
        }
    }
}

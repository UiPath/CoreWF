// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Utilities
{
    public sealed class TestTraceManager
    {
        #region Optional Trace logger

        // We only want to do alot of traces with tracking information for the tracking test cases
        //  In the case that we have the default tracking configuration, we will not do these traces.
        public static String DefaultInMemoryTrackingParticipantName = "DefaultInMemoryTrackingParticipant";
        public static bool IsDefaultTrackingConfiguration = true;

        public const int MaximumNumberOfSecondsToWaitForATrace = 60;

        //static TestTraceManager()
        //{
        //    if (TestConfiguration.Current.TrackingServiceConfigurations != null)
        //    {
        //        if (TestConfiguration.Current.TrackingServiceConfigurations.Count() == 1)
        //        {
        //            if (TestConfiguration.Current.TrackingServiceConfigurations.ElementAt(0).TrackingParticipantName == DefaultInMemoryTrackingParticipantName)
        //                IsDefaultTrackingConfiguration = true;
        //        }
        //    }
        //}

        public static void OptionalLogTrace(String trace, params object[] args)
        {
            if (!IsDefaultTrackingConfiguration)
            {
                System.Diagnostics.Trace.TraceInformation(trace, args);
                //Log.TraceInternal(trace, args);
            }
        }

        #endregion

        private static TestTraceManager s_instance = new TestTraceManager();

        private Dictionary<Guid, ActualTrace> _allTraces;
        private Dictionary<Guid, List<Subscription>> _allSubscriptions;
        private HashSet<Guid> _allKnownTraces;
        private List<string> _traceFilter;

        private readonly object _thisLock;

        private TestTraceManager()
        {
            _thisLock = new object();

            _allTraces = new Dictionary<Guid, ActualTrace>();
            _allSubscriptions = new Dictionary<Guid, List<Subscription>>();
            _allKnownTraces = new HashSet<Guid>();
            _traceFilter = new List<string>();

            AddCompensationFilterTraces();
        }

        public static TestTraceManager Instance
        {
            get { return TestTraceManager.s_instance; }
        }

        // Reset will completely destroy all existing traces, subscriptions, known trace WF IDs, and filters
        // It is meant to be used as a setup task to help guarantee test case isolation (i.e. traces from a previous test
        // do not interfere with the current test)
        public static void Reset()
        {
            TestTraceManager.s_instance = new TestTraceManager();
        }

        public Dictionary<Guid, ActualTrace> AllTraces
        {
            get { return _allTraces; }
        }

        public List<string> TraceFilter
        {
            get { return _traceFilter; }
        }

        public void AddTrace(Guid instanceId, IActualTraceStep trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }

            //Log.TraceInternal("[TestTraceManager] " + instanceId + " - " + trace.ToString());

            lock (_thisLock)
            {
                ActualTrace instanceTraces = GetInstanceActualTrace(instanceId);
                instanceTraces.Add(trace);
                CheckSubscriptions(instanceId, instanceTraces);
            }
        }

        private void CheckSubscriptions(Guid instanceId, ActualTrace instanceTraces)
        {
            lock (_thisLock)
            {
                if (_allSubscriptions.TryGetValue(instanceId, out List<Subscription> workflowInstanceSubscriptions))
                {
                    List<Subscription> subscriptionsPendingRemoval = new List<Subscription>();

                    foreach (Subscription subscription in workflowInstanceSubscriptions)
                    {
                        if (subscription.NotifyTraces(instanceTraces))
                        {
                            subscriptionsPendingRemoval.Add(subscription);
                        }
                    }

                    foreach (Subscription subscription in subscriptionsPendingRemoval)
                    {
                        workflowInstanceSubscriptions.Remove(subscription);
                    }
                    if (workflowInstanceSubscriptions.Count == 0)
                    {
                        _allSubscriptions.Remove(instanceId);
                    }
                }
            }
        }

        internal void AddSubscription(Guid instanceId, Subscription subscription)
        {
            lock (_thisLock)
            {
                if (_allSubscriptions.TryGetValue(instanceId, out List<Subscription> workflowInstanceSubscriptions))
                {
                    workflowInstanceSubscriptions.Add(subscription);
                }

                else
                {
                    workflowInstanceSubscriptions = new List<Subscription>();
                    workflowInstanceSubscriptions.Add(subscription);
                    _allSubscriptions.Add(instanceId, workflowInstanceSubscriptions);
                }

                // Make sure that the condition is not already met //
                CheckSubscriptions(instanceId, GetInstanceActualTrace(instanceId));
            }
        }

        public ActualTrace GetInstanceActualTrace(Guid instanceId)
        {
            lock (_thisLock)
            {

                if (!_allTraces.TryGetValue(instanceId, out ActualTrace instanceTraces))
                {
                    instanceTraces = new ActualTrace();
                    _allTraces.Add(instanceId, instanceTraces);
                }

                return instanceTraces;
            }
        }

        public void MarkInstanceAsKnown(Guid id)
        {
            lock (_thisLock)
            {
                _allKnownTraces.Add(id);
            }
        }

        public bool IsInstanceKnown(Guid id)
        {
            return _allKnownTraces.Contains(id);
        }

        // Get the instance ID after a start time. The start time should be the earliest possible time the instance ID could have been created at.
        public Guid GetInstanceIdAfterStartTime(DateTime startedTime)
        {
            lock (_thisLock)
            {
                Guid lastTraceGuid = Guid.Empty;
                DateTime lastTimestamp = DateTime.MinValue;
                foreach (Guid key in this.AllTraces.Keys)
                {
                    if (this.AllTraces[key].Steps.Count > 0)
                    {
                        DateTime timestamp = this.AllTraces[key].Steps[0].TimeStamp;
                        if (!IsInstanceKnown(key) && timestamp > lastTimestamp && timestamp > startedTime)
                        {
                            lastTimestamp = timestamp;
                            lastTraceGuid = key;
                        }
                    }
                }

                if (lastTimestamp == DateTime.MinValue)
                {
                    throw new TestTraceManagerException("Couldn't find any more unknown instances");
                }

                return lastTraceGuid;
            }
        }

        public void AddFilterTrace(string displayNameToFilter)
        {
            lock (_thisLock)
            {
                _traceFilter.Add(displayNameToFilter);
            }
        }

        public void WaitForTrace(Guid workflowInstanceId, IActualTraceStep trace, int count)
        {
            ManualResetEvent mre = new ManualResetEvent(false);

            Subscription subscription = new Subscription(trace, count, mre);
            AddSubscription(workflowInstanceId, subscription);

            if (!mre.WaitOne(TimeSpan.FromSeconds(TestTraceManager.MaximumNumberOfSecondsToWaitForATrace)))
            {
                throw new TimeoutException(string.Format("Waited for {0} seconds in WaitForTrace without getting the expeced trace of {1}",
                    TestTraceManager.MaximumNumberOfSecondsToWaitForATrace, trace.ToString()));
            }
        }

        public void WaitForEitherOfTraces(Guid workflowInstanceId, IActualTraceStep trace, IActualTraceStep otherTrace, out IActualTraceStep succesfulTrace)
        {
            ManualResetEvent mre = new ManualResetEvent(false);

            ORSubscription subscription = new ORSubscription(trace, otherTrace, mre);
            AddSubscription(workflowInstanceId, subscription);

            if (!mre.WaitOne(TimeSpan.FromSeconds(TestTraceManager.MaximumNumberOfSecondsToWaitForATrace)))
            {
                throw new TimeoutException(string.Format("Waited for {0} seconds in WaitForEitherOfTraces without getting either trace of {1} or {2}",
                    TestTraceManager.MaximumNumberOfSecondsToWaitForATrace, trace.ToString(), otherTrace.ToString()));
            }

            succesfulTrace = subscription.SuccessfulTraceStep;
        }

        private void AddCompensationFilterTraces()
        {
            AddFilterTrace("WorkflowCompensationBehavior");
            AddFilterTrace("CompensableActivity");
            AddFilterTrace("CompensationParticipant");
            AddFilterTrace("InternalConfirm");
            AddFilterTrace("InternalCompensate");
            AddFilterTrace("Confirm");
            AddFilterTrace("Compensate");
            AddFilterTrace("DefaultConfirmation");
            AddFilterTrace("DefaultCompensation");

            // For CustomCompensationScope Internal Activities
            AddFilterTrace("CustomCS_Sequence");
            AddFilterTrace("CustomCS_TryCatch");
            AddFilterTrace("CustomCS_CA");
            AddFilterTrace("CustomCS_If");
            AddFilterTrace("CustomCS_Confirm");
        }

        internal class Subscription
        {
            protected IActualTraceStep traceStep;
            protected int count;
            protected ManualResetEvent manualResetEvent;

            public Subscription()
            {
            }

            public Subscription(IActualTraceStep traceStep, int numOccurance, ManualResetEvent mre)
            {
                this.traceStep = traceStep;
                this.count = numOccurance;
                this.manualResetEvent = mre;
            }

            internal virtual bool NotifyTraces(ActualTrace instanceTraces)
            {
                bool removeSubscription = false;

                int currentCount = this.count;
                foreach (IActualTraceStep step in instanceTraces.Steps)
                {
                    if (step.Equals(this.traceStep))
                    {
                        currentCount--;
                    }
                    if (currentCount == 0)
                    {
                        removeSubscription = true;
                        this.manualResetEvent.Set();
                        break;
                    }
                }

                return removeSubscription;
            }
        }

        internal class ORSubscription : Subscription
        {
            private readonly IActualTraceStep _otherTraceStep;
            private IActualTraceStep _successfulTraceStep;

            public ORSubscription(IActualTraceStep traceStep, IActualTraceStep otherTraceStep, ManualResetEvent mre)
                : base(traceStep, 1, mre)
            {
                _otherTraceStep = otherTraceStep;
            }

            internal IActualTraceStep SuccessfulTraceStep
            {
                get { return _successfulTraceStep; }
            }

            internal override bool NotifyTraces(ActualTrace instanceTraces)
            {
                bool foundTrace = false;
                foreach (IActualTraceStep step in instanceTraces.Steps)
                {
                    if (step.Equals(this.traceStep))
                    {
                        foundTrace = true;
                        _successfulTraceStep = this.traceStep;
                    }
                    else if (step.Equals(_otherTraceStep))
                    {
                        foundTrace = true;
                        _successfulTraceStep = _otherTraceStep;
                    }

                    if (foundTrace)
                    {
                        this.manualResetEvent.Set();
                        break;
                    }
                }

                return foundTrace;
            }
        }
    }

    [DataContract]
    public class TestTraceManagerException : Exception
    {
        public TestTraceManagerException() :
            this(null, null)
        { }

        public TestTraceManagerException(string message) :
            this(message, null)
        { }

        public TestTraceManagerException(string message, Exception innerException) :
            base(message, innerException)
        { }

        //public TestTraceManagerException(SerializationInfo info, StreamingContext context) :
        //    base(info, context)
        //{ }
    }
}

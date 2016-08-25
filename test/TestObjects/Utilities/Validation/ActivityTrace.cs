// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Tracking;
using System.Runtime.Serialization;
using System.Xml;

namespace Test.Common.TestObjects.Utilities.Validation
{
    public enum TestActivityInstanceState
    {
        Executing = 0,
        Closed = 1,
        Canceling = 2,
        Canceled = 3,
        Faulting = 4,
        Faulted = 5,
    }

    [DataContract]
    public class ActivityTrace : WorkflowTraceStep, IActualTraceStep
    {
        private string _activityName;
        private ActivityInstanceState _activityStatus;
        private DateTime _timeStamp;
        private int _validated;

        public ActivityTrace()
        {
        }

        public ActivityTrace(string activityName, ActivityInstanceState activityStatus)
        {
            _activityName = activityName;
            _activityStatus = activityStatus;
        }

        public ActivityTrace(string activityName, ActivityInstanceState activityStatus, TrackingRecord record)
        {
            _activityName = activityName;
            _activityStatus = activityStatus;
            if (record != null)
            {
                this.ActivityInstanceId = record.InstanceId.ToString();

                if (record is ActivityStateRecord)
                {
                    if ((record as ActivityStateRecord).Activity != null)
                    {
                        this.ActivityId = (record as ActivityStateRecord).Activity.Id;
                        this.ActivityInstanceId = (record as ActivityStateRecord).Activity.InstanceId;
                    }
                }

                if (record is ActivityScheduledRecord)
                {
                    this.IsScheduled = true;
                    ActivityScheduledRecord activityScheduledRecord = record as ActivityScheduledRecord;
                    if (activityScheduledRecord.Activity != null)
                    {
                        this.ActivityId = activityScheduledRecord.Activity.Id;
                        this.ActivityInstanceId = activityScheduledRecord.Activity.InstanceId;

                        ChildActivityId = activityScheduledRecord.Child.Id;
                        ChildActivityInstanceId = activityScheduledRecord.Child.InstanceId;
                    }
                }
            }
        }

        public ActivityTrace(ActivityTrace trace)
        {
            this.IsScheduled = trace.IsScheduled;
            _activityName = trace._activityName;
            _activityStatus = trace._activityStatus;
            this.ActivityId = trace.ActivityId;
            this.ActivityInstanceId = trace.ActivityInstanceId;
            this.ChildActivityId = trace.ChildActivityId;
            this.ChildActivityInstanceId = trace.ChildActivityInstanceId;
            this.Optional = trace.Optional;
        }

        public bool IsScheduled { get; set; }
        public string ActivityId { get; set; }
        public string ActivityInstanceId { get; set; }
        public string ChildActivityId { get; set; }
        public string ChildActivityInstanceId { get; set; }

        internal string ActivityName
        {
            get { return _activityName; }
        }

        internal ActivityInstanceState ActivityStatus
        {
            get { return _activityStatus; }
        }
        DateTime IActualTraceStep.TimeStamp
        {
            get { return _timeStamp; }
            set { _timeStamp = value; }
        }

        int IActualTraceStep.Validated
        {
            get { return _validated; }
            set { _validated = value; }
        }

        protected override void WriteInnerXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", _activityName);
            writer.WriteAttributeString("status", _activityStatus.ToString());

            base.WriteInnerXml(writer);
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }

        public override bool Equals(object obj)
        {
            ActivityTrace trace = obj as ActivityTrace;
            if (trace != null)
            {
                if (
                    (this.ActivityName == trace.ActivityName) &&
                    (this.ActivityStatus == trace.ActivityStatus)
                    )
                {
                    return true;
                }
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region IActualTraceStep implementation

        bool IActualTraceStep.Equals(IActualTraceStep trace)
        {
            ActivityTrace activityTrace = trace as ActivityTrace;

            if (activityTrace != null &&
                activityTrace._activityName == _activityName &&
                activityTrace._activityStatus == _activityStatus)
            {
                return true;
            }

            return false;
        }

        string IActualTraceStep.GetStringId()
        {
            string stepId = String.Format(
                "ActivityTrace: {0}, {1}",
                _activityName,
                _activityStatus.ToString());

            return stepId;
        }
        #endregion

        #region ActivityTrace helpers

        internal static ActivityInstanceState GetActivityInstanceState(TestActivityInstanceState testActivityInstanceState)
        {
            switch (testActivityInstanceState)
            {
                case TestActivityInstanceState.Executing:
                    return ActivityInstanceState.Executing;


                case TestActivityInstanceState.Closed:
                    return ActivityInstanceState.Closed;


                case TestActivityInstanceState.Canceled:
                    return ActivityInstanceState.Canceled;


                case TestActivityInstanceState.Faulted:
                    return ActivityInstanceState.Faulted;

                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
    }
}

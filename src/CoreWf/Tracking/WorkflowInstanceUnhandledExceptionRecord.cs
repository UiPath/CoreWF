// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.Serialization;

namespace CoreWf.Tracking
{
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class WorkflowInstanceUnhandledExceptionRecord : WorkflowInstanceRecord
    {
        private Exception _unhandledException;
        private ActivityInfo _faultSource;

        public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, string activityDefinitionId, ActivityInfo faultSource, Exception exception)
            : this(instanceId, 0, activityDefinitionId, faultSource, exception)
        {
        }

        public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, long recordNumber, string activityDefinitionId, ActivityInfo faultSource, Exception exception)
            : base(instanceId, recordNumber, activityDefinitionId, WorkflowInstanceStates.UnhandledException)
        {
            if (string.IsNullOrEmpty(activityDefinitionId))
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNullOrEmpty(nameof(activityDefinitionId));
            }

            this.FaultSource = faultSource ?? throw CoreWf.Internals.FxTrace.Exception.ArgumentNull(nameof(faultSource));
            this.UnhandledException = exception ?? throw CoreWf.Internals.FxTrace.Exception.ArgumentNull(nameof(exception));
            this.Level = EventLevel.Error;
        }

        public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, string activityDefinitionId, ActivityInfo faultSource, Exception exception, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, activityDefinitionId, faultSource, exception)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        public WorkflowInstanceUnhandledExceptionRecord(Guid instanceId, long recordNumber, string activityDefinitionId, ActivityInfo faultSource, Exception exception, WorkflowIdentity workflowDefinitionIdentity)
            : this(instanceId, recordNumber, activityDefinitionId, faultSource, exception)
        {
            this.WorkflowDefinitionIdentity = workflowDefinitionIdentity;
        }

        private WorkflowInstanceUnhandledExceptionRecord(WorkflowInstanceUnhandledExceptionRecord record)
            : base(record)
        {
            this.FaultSource = record.FaultSource;
            this.UnhandledException = record.UnhandledException;
        }

        public Exception UnhandledException
        {
            get
            {
                return _unhandledException;
            }
            private set
            {
                _unhandledException = value;
            }
        }

        public ActivityInfo FaultSource
        {
            get
            {
                return _faultSource;
            }
            private set
            {
                _faultSource = value;
            }
        }

        [DataMember(Name = "UnhandledException")]
        internal Exception SerializedUnhandledException
        {
            get { return this.UnhandledException; }
            set { this.UnhandledException = value; }
        }

        [DataMember(Name = "FaultSource")]
        internal ActivityInfo SerializedFaultSource
        {
            get { return this.FaultSource; }
            set { this.FaultSource = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new WorkflowInstanceUnhandledExceptionRecord(this);
        }

        public override string ToString()
        {
            // For backward compatibility, the ToString() does not return 
            // WorkflowIdentity, if it is null.
            if (this.WorkflowDefinitionIdentity == null)
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceUnhandledExceptionRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, FaultSource {{ {4} }}, UnhandledException = {5} }} ",
                    this.InstanceId,
                    this.RecordNumber,
                    this.EventTime,
                    this.ActivityDefinitionId,
                    this.FaultSource.ToString(),
                    this.UnhandledException);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "WorkflowInstanceUnhandledExceptionRecord {{ InstanceId = {0}, RecordNumber = {1}, EventTime = {2}, ActivityDefinitionId = {3}, FaultSource {{ {4} }}, UnhandledException = {5}, WorkflowDefinitionIdentity = {6} }} ",
                    this.InstanceId,
                    this.RecordNumber,
                    this.EventTime,
                    this.ActivityDefinitionId,
                    this.FaultSource.ToString(),
                    this.UnhandledException,
                    this.WorkflowDefinitionIdentity);
            }
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace LegacyTest.Test.Common.TestObjects.Utilities.Validation
{
    public enum WorkflowInstanceState
    {
        Started,
        Aborted,
        Idle,
        Completed,
        Persisted,
        Unloaded,
        UnhandledException,
        Deleted,
        Resumed,
        Canceled,
        Suspended,
        Terminated,
        Unsuspended,
        Updated,
        UpdateFailed,
    }

    [DataContract]
    public class WorkflowInstanceTrace : WorkflowTraceStep, IActualTraceStep
    {
        private Guid _instanceName;
        private WorkflowIdentity _workflowDefintionIdentity;
        private WorkflowInstanceState _instanceStatus;
        private DateTime _timeStamp;
        private int _validated;

        public WorkflowInstanceTrace(WorkflowInstanceState instanceStatus) :
            this(Guid.Empty, null, instanceStatus)
        {
        }

        public WorkflowInstanceTrace(WorkflowIdentity workflowDefintionIdentity, WorkflowInstanceState instanceStatus) :
            this(Guid.Empty, workflowDefintionIdentity, instanceStatus)
        {
        }

        public WorkflowInstanceTrace(Guid instanceName, WorkflowInstanceState instanceStatus) :
            this(instanceName, null, instanceStatus)
        {
        }

        public WorkflowInstanceTrace(Guid instanceName, WorkflowIdentity workflowDefintionIdentity, WorkflowInstanceState instanceStatus)
        {
            _instanceName = instanceName;
            _instanceStatus = instanceStatus;
            _workflowDefintionIdentity = workflowDefintionIdentity;
        }

        internal Guid InstanceName
        {
            get { return _instanceName; }
            set { _instanceName = value; }
        }

        public WorkflowIdentity WorkflowDefinitionIdentity
        {
            get { return _workflowDefintionIdentity; }
            set { _workflowDefintionIdentity = value; }
        }

        internal WorkflowInstanceState InstanceStatus
        {
            get { return _instanceStatus; }
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
            if (this.WorkflowDefinitionIdentity != null)
            {
                writer.WriteAttributeString("workflowDefintionIdentity", this.WorkflowDefinitionIdentity.ToString());
            }
            writer.WriteAttributeString("status", this.InstanceStatus.ToString());
            base.WriteInnerXml(writer);
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }

        public override bool Equals(object obj)
        {
            if (obj is WorkflowInstanceTrace trace)
            {
                if (this.InstanceStatus == trace.InstanceStatus &&
                    WorkflowInstanceTrace.CompareIdentities(trace.WorkflowDefinitionIdentity, this.WorkflowDefinitionIdentity))
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

            if (trace is WorkflowInstanceTrace instanceTrace)
            {
                if (this.InstanceStatus == instanceTrace.InstanceStatus &&
                    WorkflowInstanceTrace.CompareIdentities(instanceTrace.WorkflowDefinitionIdentity, this.WorkflowDefinitionIdentity))
                {
                    return true;
                }
            }

            return false;
        }

        string IActualTraceStep.GetStringId()
        {
            string stepId = null;
            if (this.WorkflowDefinitionIdentity == null)
            {
                stepId = String.Format("WorkflowInstanceTrace: {0}", _instanceStatus.ToString());
            }
            else
            {
                stepId = String.Format("WorkflowInstanceTrace: {0}, {1}", this.WorkflowDefinitionIdentity.ToString(), _instanceStatus.ToString());
            }


            return stepId;
        }
        #endregion

        #region WorkflowInstanceTrace helpers


        public static void Trace(Guid workflowInstanceId, WorkflowIdentity workflowDefinitionIdentity, WorkflowInstanceState state)
        {
            WorkflowInstanceTrace trace = new WorkflowInstanceTrace(workflowInstanceId, workflowDefinitionIdentity, state);

            TraceSource ts = new TraceSource("System.Activities.Tracking", SourceLevels.Information);
            ts.TraceData(TraceEventType.Information, 1, trace);
        }


        //Compare identities and returns true if they are equal otherwise false
        public static bool CompareIdentities(WorkflowIdentity baseIdentity, WorkflowIdentity identity)
        {
            if (baseIdentity == null && identity == null)
            {
                return true;
            }
            else if (baseIdentity == null || identity == null) //Either one of them is null
            {
                return false;
            }
            return (baseIdentity.Equals(identity) || object.Equals(baseIdentity, identity));
        }
        #endregion
    }
}

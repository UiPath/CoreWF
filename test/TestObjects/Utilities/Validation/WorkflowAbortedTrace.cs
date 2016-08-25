// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace Test.Common.TestObjects.Utilities.Validation
{
    [DataContract]
    public class WorkflowAbortedTrace : WorkflowTraceStep, IActualTraceStep
    {
        private Guid _instanceId;
        private Exception _abortedReason;
        private DateTime _timeStamp;
        private int _validated;

        public WorkflowAbortedTrace(Guid instanceId, Exception abortedReason)
        {
            _instanceId = instanceId;
            _abortedReason = abortedReason;
        }

        internal Guid InstanceId
        {
            get { return _instanceId; }
        }

        internal Exception AbortedReason
        {
            get { return _abortedReason; }
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
            writer.WriteAttributeString("name", _instanceId.ToString());
            writer.WriteElementString("Exception", _abortedReason?.ToString());
            base.WriteInnerXml(writer);
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }

        bool IActualTraceStep.Equals(IActualTraceStep trace)
        {
            WorkflowAbortedTrace exceptionTrace = trace as WorkflowAbortedTrace;

            return exceptionTrace != null &&
                exceptionTrace._instanceId == _instanceId &&
                ((_abortedReason == null || exceptionTrace._abortedReason == null) ||
                (exceptionTrace._abortedReason.GetType() == _abortedReason.GetType() &&
                exceptionTrace._abortedReason.Message == _abortedReason.Message));
        }

        string IActualTraceStep.GetStringId()
        {
            return String.Format("WorkflowAbortedTrace: {0}, {1}", _instanceId.ToString(), _abortedReason.ToString());
        }

        public static void Trace(Guid workflowInstanceId, Exception abortReason)
        {
            WorkflowAbortedTrace trace = new WorkflowAbortedTrace(workflowInstanceId, abortReason);
            TraceSource ts = new TraceSource("Microsoft.CoreWf.Tracking", SourceLevels.Information);
            ts.TraceData(TraceEventType.Information, 1, trace);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace Test.Common.TestObjects.Utilities.Validation
{
    [DataContract]
    public class WorkflowExceptionTrace : WorkflowTraceStep, IActualTraceStep
    {
        private Guid _instanceName;
        private Exception _instanceException;
        private DateTime _timeStamp;
        private int _validated;

        public WorkflowExceptionTrace(Guid instanceName, Exception instanceException)
        {
            _instanceName = instanceName;
            _instanceException = instanceException;
        }

        internal Guid InstanceName
        {
            get { return _instanceName; }
        }

        internal Exception InstanceException
        {
            get { return _instanceException; }
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
            writer.WriteAttributeString("name", _instanceName.ToString());
            writer.WriteElementString("Exception", _instanceException.ToString());

            base.WriteInnerXml(writer);
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }

        #region IActualTraceStep implementation

        bool IActualTraceStep.Equals(IActualTraceStep trace)
        {
            WorkflowExceptionTrace exceptionTrace = trace as WorkflowExceptionTrace;

            if (exceptionTrace != null &&
                exceptionTrace._instanceName == _instanceName &&
                exceptionTrace._instanceException.ToString() == _instanceException.ToString())
            {
                return true;
            }

            return false;
        }

        string IActualTraceStep.GetStringId()
        {
            string stepId = String.Format(
                "WorkflowExceptionTrace: {0}, {1}",
                _instanceName.ToString(),
                _instanceException.ToString());

            return stepId;
        }
        #endregion

        #region WorkflowInstanceTrace helpers

        public static void Trace(Guid workflowInstanceId, Exception instanceException)
        {
            WorkflowExceptionTrace trace = new WorkflowExceptionTrace(workflowInstanceId, instanceException);

            TraceSource ts = new TraceSource("CoreWf.Tracking", SourceLevels.Information);
            ts.TraceData(TraceEventType.Information, 1, trace);
        }

        #endregion
    }
}

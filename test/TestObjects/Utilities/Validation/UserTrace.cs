// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using Test.Common.TestObjects.Utilities;

namespace Test.Common.TestObjects.Utilities.Validation
{
    [DataContract]
    public class UserTrace : WorkflowTraceStep, IActualTraceStep
    {
        private string _message;
        private DateTime _timeStamp;
        private int _validated;

        private Guid _instanceId;
        private String _parentActivityID;

        public UserTrace(string message)
        {
            _message = message;
            _parentActivityID = "";
        }

        internal UserTrace(Guid instanceId, string message)
            : this(message)
        {
            _instanceId = instanceId;
            _parentActivityID = "";
        }

        internal UserTrace(Guid instanceId, string parentID, string message)
            : this(message)
        {
            _instanceId = instanceId;
            _parentActivityID = parentID;
        }

        internal string Message
        {
            get { return _message; }
        }

        internal Guid InstanceId
        {
            get { return _instanceId; }
        }

        internal string ActivityParent
        {
            get { return _parentActivityID; }
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
            writer.WriteAttributeString("message", _message);

            base.WriteInnerXml(writer);
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }

        public override bool Equals(object obj)
        {
            UserTrace trace = obj as UserTrace;
            if (trace != null)
            {
                if (this.ToString() == trace.ToString())
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
            UserTrace userTrace = trace as UserTrace;

            if (userTrace != null &&
                userTrace._message == _message)
            {
                return true;
            }

            return false;
        }

        string IActualTraceStep.GetStringId()
        {
            string stepId = String.Format(
                "UserTrace: {0}",
                _message);

            return stepId;
        }
        #endregion

        #region UserTrace helpers
        public static void Trace(TestTraceListenerExtension listenerExtension, Guid instanceId, string format, params object[] args)
        {
            UserTrace.Trace(listenerExtension, instanceId, String.Format(format, args));
        }

        public static void Trace(TestTraceListenerExtension listenerExtension, Guid instanceId, string message)
        {
            if (listenerExtension != null)
            {
                UserTrace userTrace = new UserTrace(instanceId, message);
                //TraceSource ts = new TraceSource("Microsoft.CoreWf.Tracking", SourceLevels.Information);
                //ts.TraceData(TraceEventType.Information, 1, userTrace);
                listenerExtension.TraceData(userTrace);
            }
        }
        #endregion
    }
}

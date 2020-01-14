// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Xml;

namespace Test.Common.TestObjects.Utilities.Validation
{
    // [Serializable]
    public class BookmarkResumptionTrace : WorkflowTraceStep, IActualTraceStep
    {
        private string _activityName;
        private string _bookmarkName;
        private Guid _subinstanceId;
        private DateTime _timeStamp;
        private int _validated;

        public BookmarkResumptionTrace(string bookmarkName, Guid subinstanceId, string activityName)
        {
            _bookmarkName = bookmarkName;
            _subinstanceId = subinstanceId;
            _activityName = activityName;
        }

        public string ActivityName
        {
            get { return _activityName; }
            set { _activityName = value; }
        }

        public string BookmarkName
        {
            get { return _bookmarkName; }
            set { _bookmarkName = value; }
        }

        public Guid SubinstanceId
        {
            get { return _subinstanceId; }
            set { _subinstanceId = value; }
        }

        protected override void WriteInnerXml(XmlWriter writer)
        {
            writer.WriteAttributeString("activityName", _activityName);
            writer.WriteAttributeString("bookmarkName", _bookmarkName);
            writer.WriteAttributeString("subinstanceId", _subinstanceId.ToString());

            base.WriteInnerXml(writer);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BookmarkResumptionTrace trace)
            {
                if (_activityName == trace._activityName &&
                    _bookmarkName == trace._bookmarkName &&
                    _subinstanceId == trace._subinstanceId)
                {
                    return true;
                }
            }
            return base.Equals(obj);
        }

        #region IActualTraceStep Members

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

        bool IActualTraceStep.Equals(IActualTraceStep trace)
        {

            if (trace is BookmarkResumptionTrace bookmarkResumptionTrace &&
                bookmarkResumptionTrace._activityName == _activityName &&
                bookmarkResumptionTrace._bookmarkName == _bookmarkName &&
                bookmarkResumptionTrace._subinstanceId == _subinstanceId)
            {
                return true;
            }

            return false;
        }

        string IActualTraceStep.GetStringId()
        {
            string stepId = String.Format(
                "BookmarkResumptionTrace: {0}, {1}",
                _bookmarkName,
                _subinstanceId.ToString());
            return stepId;
        }

        public override string ToString()
        {
            return ((IActualTraceStep)this).GetStringId();
        }
        #endregion
    }
}

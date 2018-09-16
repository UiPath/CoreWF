// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;
using System.Xml;

namespace Test.Common.TestObjects.Utilities.Validation
{
    [DataContract]
    public class DelayTrace : WorkflowTraceStep
    {
        private TimeSpan _timeSpan;

        public DelayTrace(TimeSpan timeSpan)
        {
            _timeSpan = timeSpan;
        }

        internal TimeSpan TimeSpan
        {
            get { return _timeSpan; }
        }

        protected override void WriteInnerXml(XmlWriter writer)
        {
            writer.WriteAttributeString("timeSpan", _timeSpan.ToString());

            base.WriteInnerXml(writer);
        }
    }
}

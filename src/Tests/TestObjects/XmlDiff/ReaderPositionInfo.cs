// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml;

namespace Test.Common.TestObjects.XmlDiff
{
    internal class ReaderPositionInfo : PositionInfo
    {
        private IXmlLineInfo _mlineInfo;

        public ReaderPositionInfo(IXmlLineInfo lineInfo)
        {
            _mlineInfo = lineInfo;
        }
        public override int LineNumber { get { return _mlineInfo.LineNumber; } }
        public override int LinePosition { get { return _mlineInfo.LinePosition; } }

        public override bool HasLineInfo()
        {
            return true;
        }
    }
}

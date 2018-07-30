// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Xml;

namespace Test.Common.TestObjects.XmlDiff
{
    internal class PositionInfo : IXmlLineInfo
    {
        public virtual int LineNumber { get { return 0; } }
        public virtual int LinePosition { get { return 0; } }
        public virtual bool HasLineInfo()
        {
            return false;
        }

        public static PositionInfo GetPositionInfo(Object o)
        {
            if (o is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return new ReaderPositionInfo(lineInfo);
            }
            else
            {
                return new PositionInfo();
            }
        }
    }
}

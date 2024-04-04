// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;

namespace LegacyTest.Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffCharacterData : XmlDiffNode
    {
        private string _value;
        private XmlDiffNodeType _nodetype;
        public XmlDiffCharacterData(string value, XmlDiffNodeType nt, bool NormalizeNewline)
            : base()
        {
            _value = value;
            if (NormalizeNewline)
            {
                _value = _value.Replace("\n", "");
                _value = _value.Replace("\r", "");
            }
            _nodetype = nt;
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        public override XmlDiffNodeType NodeType { get { return _nodetype; } }

        public override void WriteTo(XmlWriter w)
        {
            switch (_nodetype)
            {
                case XmlDiffNodeType.Comment:
                    w.WriteComment(Value);
                    break;
                case XmlDiffNodeType.CData:
                    w.WriteCData(Value);
                    break;
                case XmlDiffNodeType.WS:
                case XmlDiffNodeType.Text:
                    w.WriteString(Value);
                    break;
                default:
                    Debug.Assert(false, "Wrong type for text-like node : " + _nodetype.ToString());
                    break;
            }
        }

        public override void WriteContentTo(XmlWriter w)
        {
        }
    }
}

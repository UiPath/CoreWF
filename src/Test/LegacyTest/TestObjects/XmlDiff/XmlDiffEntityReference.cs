// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml;

namespace LegacyTest.Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffEntityReference : XmlDiffNode
    {
        private readonly string _name;
        public XmlDiffEntityReference(string name)
            : base()
        {
            _name = name;
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.ER; } }
        public string Name { get { return _name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteEntityRef(_name);
        }

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = this.FirstChild;
            while (child != null)
            {
                child.WriteTo(w);
                child = child.NextSibling;
            }
        }
    }
}

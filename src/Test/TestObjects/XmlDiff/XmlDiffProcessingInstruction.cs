// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Xml;

namespace Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffProcessingInstruction : XmlDiffCharacterData
    {
        private readonly string _name;
        public XmlDiffProcessingInstruction(string name, string value)
            : base(value, XmlDiffNodeType.PI, false)
        {
            _name = name;
        }
        public string Name { get { return _name; } }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteProcessingInstruction(_name, Value);
        }
        public override void WriteContentTo(XmlWriter w)
        {
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffAttribute : XmlDiffNode
    {
        internal XmlDiffElement _ownerElement;
        private string _lName;
        private string _prefix;
        private string _ns;
        private string _value;
        private XmlQualifiedName _valueAsQName;

        public XmlDiffAttribute(string localName, string prefix, string ns, string value)
            : base()
        {
            _lName = localName;
            _prefix = prefix;
            _ns = ns;
            _value = value;
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
        }
        public XmlQualifiedName ValueAsQName
        {
            get { return _valueAsQName; }
        }

        public string LocalName { get { return _lName; } }
        public string NamespaceURI { get { return _ns; } }
        public string Prefix { get { return _prefix; } }

        public string Name
        {
            get
            {
                if (_prefix.Length > 0)
                {
                    return _prefix + ":" + _lName;
                }
                else
                {
                    return _lName;
                }
            }
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Attribute; } }

        internal void SetValueAsQName(XmlReader reader, string value)
        {
            int indexOfColon = value.IndexOf(':');
            if (indexOfColon == -1)
            {
                _valueAsQName = new XmlQualifiedName(value);
            }
            else
            {
                string prefix = value.Substring(0, indexOfColon);
                string ns = reader.LookupNamespace(prefix);
                if (ns == null)
                {
                    _valueAsQName = null;
                }
                else
                {
                    try
                    {
                        string localName = XmlConvert.VerifyNCName(value.Substring(indexOfColon + 1));
                        _valueAsQName = new XmlQualifiedName(localName, ns);
                    }
                    catch (XmlException) // jasonv - approved; specific, converted to an acceptable value
                    {
                        _valueAsQName = null;
                    }
                }
            }
        }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartAttribute(Prefix, LocalName, NamespaceURI);
            WriteContentTo(w);
            w.WriteEndAttribute();
        }

        public override void WriteContentTo(XmlWriter w)
        {
            w.WriteString(Value);
        }
    }
}

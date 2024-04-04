// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Xml;

namespace LegacyTest.Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffElement : XmlDiffNode
    {
        private readonly string _lName;
        private string _prefix;
        private readonly string _ns;
        private XmlDiffAttribute _firstAttribute;
        private XmlDiffAttribute _lastAttribute;
        private int _attrC;
        private int _endLineNumber, _endLinePosition;

        public XmlDiffElement(string localName, string prefix, string ns)
            : base()
        {
            _lName = localName;
            _prefix = prefix;
            _ns = ns;
            _firstAttribute = null;
            _lastAttribute = null;
            _attrC = -1;
        }

        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Element; } }
        public string LocalName { get { return _lName; } }
        public string NamespaceURI { get { return _ns; } }
        public string Prefix { get { return _prefix; } }

        public string Name
        {
            get
            {
                if (_prefix.Length > 0)
                {
                    return Prefix + ":" + LocalName;
                }
                else
                {
                    return LocalName;
                }
            }
        }

        public XmlDiffAttribute FirstAttribute
        {
            get
            {
                return _firstAttribute;
            }
        }
        public XmlDiffAttribute LastAttribute
        {
            get
            {
                return _lastAttribute;
            }
        }

        public int AttributeCount
        {
            get
            {
                if (_attrC != -1)
                {
                    return _attrC;
                }
                XmlDiffAttribute attr = _firstAttribute;
                _attrC = 0;
                while (attr != null)
                {
                    _attrC++;
                    attr = (XmlDiffAttribute)attr.NextSibling;
                }
                return _attrC;
            }
        }
        public override bool IgnoreValue
        {
            set
            {
                base.IgnoreValue = value;
                XmlDiffAttribute current = _firstAttribute;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = (XmlDiffAttribute)current._next;
                }
            }
        }

        public int EndLineNumber
        {
            get { return _endLineNumber; }
            set { _endLineNumber = value; }
        }

        public int EndLinePosition
        {
            get { return _endLinePosition; }
            set { _endLinePosition = value; }
        }

        public string Value
        {
            get
            {
                if (this.IgnoreValue)
                {
                    return "";
                }
                if (_firstChild != null)
                {
                    StringBuilder _bldr = new StringBuilder();
                    XmlDiffNode _current = _firstChild;
                    do
                    {
                        if (_current is XmlDiffCharacterData && _current.NodeType != XmlDiffNodeType.Comment && _current.NodeType != XmlDiffNodeType.PI)
                        {
                            _bldr.Append(((XmlDiffCharacterData)_current).Value);
                        }
                        else if (_current is XmlDiffElement)
                        {
                            _bldr.Append(((XmlDiffElement)_current).Value);
                        }
                        _current = _current._next;
                    }
                    while (_current != null);
                    return _bldr.ToString();
                }
                return "";
            }
        }
        public string GetAttributeValue(string LocalName, string NamespaceUri)
        {
            if (_firstAttribute != null)
            {
                XmlDiffAttribute _current = _firstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current.Value;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != _lastAttribute);
            }
            return "";
        }

        public XmlDiffAttribute GetAttribute(string LocalName, string NamespaceUri)
        {
            if (_firstAttribute != null)
            {
                XmlDiffAttribute _current = _firstAttribute;
                do
                {
                    if (_current.LocalName == LocalName && _current.NamespaceURI == NamespaceURI)
                    {
                        return _current;
                    }
                    _current = (XmlDiffAttribute)_current._next;
                }
                while (_current != _lastAttribute);
            }
            return null;
        }

        internal void InsertAttributeAfter(XmlDiffAttribute attr, XmlDiffAttribute newAttr)
        {
            Debug.Assert(newAttr != null);
            newAttr._ownerElement = this;
            if (attr == null)
            {
                newAttr._next = _firstAttribute;
                _firstAttribute = newAttr;
            }
            else
            {
                Debug.Assert(attr._ownerElement == this);
                newAttr._next = attr._next;
                attr._next = newAttr;
            }
            if (newAttr._next == null)
            {
                _lastAttribute = newAttr;
            }
        }

        internal void DeleteAttribute(XmlDiffAttribute attr)
        {
            if (attr == this.FirstAttribute)//delete head
            {
                if (attr == this.LastAttribute) //tail being deleted
                {
                    _lastAttribute = (XmlDiffAttribute)attr.NextSibling;
                }
                _firstAttribute = (XmlDiffAttribute)this.FirstAttribute.NextSibling;
            }
            else
            {
                XmlDiffAttribute current = this.FirstAttribute;
                XmlDiffAttribute previous = null;
                while (current != attr)
                {
                    previous = current;
                    current = (XmlDiffAttribute)current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == this.LastAttribute) //tail being deleted
                {
                    _lastAttribute = (XmlDiffAttribute)current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }

        public override void WriteTo(XmlWriter w)
        {
            w.WriteStartElement(Prefix, LocalName, NamespaceURI);
            XmlDiffAttribute attr = _firstAttribute;
            while (attr != null)
            {
                attr.WriteTo(w);
                attr = (XmlDiffAttribute)(attr.NextSibling);
            }
            WriteContentTo(w);
            w.WriteFullEndElement();
        }

        public override void WriteContentTo(XmlWriter w)
        {
            XmlDiffNode child = FirstChild;
            while (child != null)
            {
                child.WriteTo(w);
                child = child.NextSibling;
            }
        }
    }
}

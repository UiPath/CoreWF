// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace LegacyTest.Test.Common.TestObjects.XmlDiff
{
    //navgator over the xmldiffdocument
    public class XmlDiffNavigator : XPathNavigator
    {
        private XmlDiffDocument _document;
        private XmlDiffNode _currentNode;

        public XmlDiffNavigator(XmlDiffDocument doc)
        {
            _document = doc;
            _currentNode = _document;
        }

        //properties

        public override XPathNodeType NodeType
        {
            get
            {
                //namespace, comment and whitespace node types are not supported
                switch (_currentNode.NodeType)
                {
                    case XmlDiffNodeType.Element:
                        return XPathNodeType.Element;
                    case XmlDiffNodeType.Attribute:
                        return XPathNodeType.Attribute;
                    case XmlDiffNodeType.ER:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.Text:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.CData:
                        return XPathNodeType.Text;
                    case XmlDiffNodeType.Comment:
                        return XPathNodeType.Comment;
                    case XmlDiffNodeType.PI:
                        return XPathNodeType.ProcessingInstruction;
                    case XmlDiffNodeType.WS:
                        return XPathNodeType.SignificantWhitespace;
                    case XmlDiffNodeType.Document:
                        return XPathNodeType.Root;
                    default:
                        return XPathNodeType.All;
                }
            }
        }

        public override string LocalName
        {
            get
            {
                if (_currentNode.NodeType == XmlDiffNodeType.Element)
                {
                    return ((XmlDiffElement)_currentNode).LocalName;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)_currentNode).LocalName;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)_currentNode).Name;
                }
                return "";
            }
        }

        public override string Name
        {
            get
            {
                if (_currentNode.NodeType == XmlDiffNodeType.Element)
                {
                    //return ((XmlDiffElement)m_currentNode).Name;
                    return _document.nameTable.Get(((XmlDiffElement)_currentNode).Name);
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.Attribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Name;
                }
                else if (_currentNode.NodeType == XmlDiffNodeType.PI)
                {
                    return ((XmlDiffProcessingInstruction)_currentNode).Name;
                }
                return "";
            }
        }

        public override string NamespaceURI
        {
            get
            {
                if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).NamespaceURI;
                }
                else if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).NamespaceURI;
                }
                return "";
            }
        }

        public override string Value
        {
            get
            {
                if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Value;
                }
                else if (_currentNode is XmlDiffCharacterData)
                {
                    return ((XmlDiffCharacterData)_currentNode).Value;
                }
                else if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).Value;
                }
                return "";
            }
        }

        public override string Prefix
        {
            get
            {
                if (_currentNode is XmlDiffElement)
                {
                    return ((XmlDiffElement)_currentNode).Prefix;
                }
                else if (_currentNode is XmlDiffAttribute)
                {
                    return ((XmlDiffAttribute)_currentNode).Prefix;
                }
                return "";
            }
        }

        public override string BaseURI
        {
            get
            {
                Debug.Assert(false, "BaseURI is NYI");
                return "";
            }
        }
        public override string XmlLang
        {
            get
            {
                Debug.Assert(false, "XmlLang not supported");
                return "";
            }
        }
        public override bool HasAttributes
        {
            get
            {
                return (_currentNode is XmlDiffElement && ((XmlDiffElement)_currentNode).FirstAttribute != null) ? true : false;
            }
        }
        public override bool HasChildren
        {
            get
            {
                return _currentNode._next != null ? true : false;
            }
        }
        public override bool IsEmptyElement
        {
            get
            {
                return _currentNode is XmlDiffEmptyElement ? true : false;
            }
        }
        public override XmlNameTable NameTable
        {
            get
            {
                return _document.nameTable;
                //return new NameTable();
            }
        }
        public XmlDiffNode CurrentNode
        {
            get
            {
                return _currentNode;
            }
        }
        public override XPathNavigator Clone()
        {
            XmlDiffNavigator _clone = new XmlDiffNavigator(_document);
            if (!_clone.MoveTo(this))
            {
                throw new Exception("Cannot clone");
            }
            return _clone;
        }
        public override XmlNodeOrder ComparePosition(XPathNavigator nav)
        {
            XmlDiffNode targetNode = ((XmlDiffNavigator)nav).CurrentNode;
            //        Debug.Assert(false, "ComparePosition is NYI");
            if (!(nav is XmlDiffNavigator))
            {
                return XmlNodeOrder.Unknown;
            }
            if (targetNode == this.CurrentNode)
            {
                return XmlNodeOrder.Same;
            }
            else
            {
                if (this.CurrentNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.After;
                }
                else if (targetNode.ParentNode == null) //this is root
                {
                    return XmlNodeOrder.Before;
                }
                else //look in the following nodes
                {
                    if (targetNode.LineNumber + targetNode.LinePosition > this.CurrentNode.LinePosition + this.CurrentNode.LineNumber)
                    {
                        return XmlNodeOrder.After;
                    }
                    return XmlNodeOrder.Before;
                }
            }
        }
        public override String GetAttribute(String localName, String namespaceURI)
        {
            if (_currentNode is XmlDiffElement)
            {
                return ((XmlDiffElement)_currentNode).GetAttributeValue(localName, namespaceURI);
            }
            return "";
        }

        public override String GetNamespace(String name)
        {
            Debug.Assert(false, "GetNamespace is NYI");
            return "";
        }


        // public override bool IsDescendant (XPathNavigator nav)  
        //         {
        //             Debug.Assert(false, "IsDescendant is NYI");
        //             return false;
        //         }//         

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                if (_currentNode == ((XmlDiffNavigator)other).CurrentNode)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool MoveTo(XPathNavigator other)
        {
            if (other is XmlDiffNavigator)
            {
                _currentNode = ((XmlDiffNavigator)other).CurrentNode;
                return true;
            }
            return false;
        }

        public override bool MoveToAttribute(String localName, String namespaceURI)
        {
            if (_currentNode is XmlDiffElement)
            {
                XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).GetAttribute(localName, namespaceURI);
                if (_attr != null)
                {
                    _currentNode = _attr;
                    return true;
                }
            }
            return false;
        }
        public override bool MoveToFirst()
        {
            if (!(_currentNode is XmlDiffAttribute))
            {
                if (_currentNode.ParentNode.FirstChild == _currentNode)
                {
                    if (_currentNode.ParentNode.FirstChild._next != null)
                    {
                        _currentNode = _currentNode.ParentNode.FirstChild._next;
                        return true;
                    }
                }
                else
                {
                    _currentNode = _currentNode.ParentNode.FirstChild;
                    return true;
                }
            }
            return false;
        }
        public override bool MoveToFirstAttribute()
        {
            if (_currentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)_currentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).FirstAttribute;
                    while (_attr != null && IsNamespaceNode(_attr))
                    {
                        _attr = (XmlDiffAttribute)_attr._next;
                    }
                    if (_attr != null)
                    {
                        _currentNode = _attr;
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool MoveToFirstChild()
        {
            if ((_currentNode is XmlDiffDocument || _currentNode is XmlDiffElement) && _currentNode.FirstChild != null)
            {
                _currentNode = _currentNode.FirstChild;
                return true;
            }
            return false;
        }

        //sunghonhack
        public new bool MoveToFirstNamespace()
        {
            if (_currentNode is XmlDiffElement)
            {
                if (((XmlDiffElement)_currentNode).FirstAttribute != null)
                {
                    XmlDiffAttribute _attr = ((XmlDiffElement)_currentNode).FirstAttribute;
                    while (_attr != null && !IsNamespaceNode(_attr))
                    {
                        _attr = (XmlDiffAttribute)_attr._next;
                    }
                    if (_attr != null)
                    {
                        _currentNode = _attr;
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return this.MoveToFirstNamespace();
        }
        public override bool MoveToId(String id)
        {
            Debug.Assert(false, "MoveToId is NYI");
            return false;
        }
        public override bool MoveToNamespace(String name)
        {
            Debug.Assert(false, "MoveToNamespace is NYI");
            return false;
        }
        public override bool MoveToNext()
        {
            if (!(_currentNode is XmlDiffAttribute) && _currentNode._next != null)
            {
                _currentNode = _currentNode._next;
                return true;
            }
            return false;
        }
        public override bool MoveToNextAttribute()
        {
            if (_currentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)_currentNode._next;
                while (_attr != null && IsNamespaceNode(_attr))
                {
                    _attr = (XmlDiffAttribute)_attr._next;
                }
                if (_attr != null)
                {
                    _currentNode = _attr;
                    return true;
                }
            }
            return false;
        }

        //sunghonhack
        public new bool MoveToNextNamespace()
        {
            if (_currentNode is XmlDiffAttribute)
            {
                XmlDiffAttribute _attr = (XmlDiffAttribute)_currentNode._next;
                while (_attr != null && !IsNamespaceNode(_attr))
                {
                    _attr = (XmlDiffAttribute)_attr._next;
                }
                if (_attr != null)
                {
                    _currentNode = _attr;
                    return true;
                }
            }
            return false;
        }
        private bool IsNamespaceNode(XmlDiffAttribute attr)
        {
            return attr.LocalName.ToLowerInvariant() == "xmlns" ||
                   attr.Prefix.ToLowerInvariant() == "xmlns";
        }
        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return this.MoveToNextNamespace();
        }
        public override bool MoveToParent()
        {
            if (!(_currentNode is XmlDiffDocument))
            {
                _currentNode = _currentNode.ParentNode;
                return true;
            }
            return false;
        }
        public override bool MoveToPrevious()
        {
            if (_currentNode != _currentNode.ParentNode.FirstChild)
            {
                XmlDiffNode _current = _currentNode.ParentNode.FirstChild;
                XmlDiffNode _prev = _currentNode.ParentNode.FirstChild;
                while (_current != _currentNode)
                {
                    _prev = _current;
                    _current = _current._next;
                }
                _currentNode = _prev;
                return true;
            }
            return false;
        }
        public override void MoveToRoot()
        {
            _currentNode = _document;
        }
        public bool IsOnRoot()
        {
            return _currentNode == null ? true : false;
        }
    }
}

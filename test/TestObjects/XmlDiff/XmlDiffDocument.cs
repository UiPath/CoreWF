// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace Test.Common.TestObjects.XmlDiff
{
    public class XmlDiffDocument : XmlDiffNode, IXPathNavigable
    {
        public XmlNameTable nameTable;
        //XmlDiffNameTable    _nt;
        private bool _bLoaded;
        private bool _bIgnoreAttributeOrder;
        private bool _bIgnoreChildOrder;
        private bool _bIgnoreComments;
        private bool _bIgnoreWhitespace;
        private bool _bIgnoreDTD;
        private bool _bIgnoreNS;
        private bool _bIgnorePrefix;
        private bool _bCDataAsText;
        private bool _bNormalizeNewline;
        private bool _bTreatWhitespaceTextAsWSNode = false;
        private bool _bIgnoreEmptyTextNodes = false;
        private bool _bWhitespaceAsText = false;

        public XmlDiffDocument()
            : base()
        {
            _bLoaded = false;
            _bIgnoreAttributeOrder = false;
            _bIgnoreChildOrder = false;
            _bIgnoreComments = false;
            _bIgnoreWhitespace = false;
            _bIgnoreDTD = false;
            _bCDataAsText = false;
            _bWhitespaceAsText = false;
        }

        public XmlDiffOption Option
        {
            set
            {
                this.IgnoreAttributeOrder = (((int)value & (int)(XmlDiffOption.IgnoreAttributeOrder)) > 0);
                this.IgnoreChildOrder = (((int)value & (int)(XmlDiffOption.IgnoreChildOrder)) > 0);
                this.IgnoreComments = (((int)value & (int)(XmlDiffOption.IgnoreComments)) > 0);
                this.IgnoreWhitespace = (((int)value & (int)(XmlDiffOption.IgnoreWhitespace)) > 0);
                this.IgnoreDTD = (((int)value & (int)(XmlDiffOption.IgnoreDTD)) > 0);
                this.IgnoreNS = (((int)value & (int)(XmlDiffOption.IgnoreNS)) > 0);
                this.IgnorePrefix = (((int)value & (int)(XmlDiffOption.IgnorePrefix)) > 0);
                this.CDataAsText = (((int)value & (int)(XmlDiffOption.CDataAsText)) > 0);
                this.NormalizeNewline = (((int)value & (int)(XmlDiffOption.NormalizeNewline)) > 0);
                this.TreatWhitespaceTextAsWSNode = (((int)value & (int)(XmlDiffOption.TreatWhitespaceTextAsWSNode)) > 0);
                this.IgnoreEmptyTextNodes = (((int)value & (int)(XmlDiffOption.IgnoreEmptyTextNodes)) > 0);
                this.WhitespaceAsText = (((int)value & (int)(XmlDiffOption.WhitespaceAsText)) > 0);
            }
        }
        public override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Document; } }

        public bool IgnoreAttributeOrder
        {
            get { return _bIgnoreAttributeOrder; }
            set { _bIgnoreAttributeOrder = value; }
        }

        public bool IgnoreChildOrder
        {
            get { return _bIgnoreChildOrder; }
            set { _bIgnoreChildOrder = value; }
        }

        public bool IgnoreComments
        {
            get { return _bIgnoreComments; }
            set { _bIgnoreComments = value; }
        }

        public bool IgnoreWhitespace
        {
            get { return _bIgnoreWhitespace; }
            set { _bIgnoreWhitespace = value; }
        }

        public bool IgnoreDTD
        {
            get { return _bIgnoreDTD; }
            set { _bIgnoreDTD = value; }
        }

        public bool IgnoreNS
        {
            get { return _bIgnoreNS; }
            set { _bIgnoreNS = value; }
        }

        public bool IgnorePrefix
        {
            get { return _bIgnorePrefix; }
            set { _bIgnorePrefix = value; }
        }

        public bool CDataAsText
        {
            get { return _bCDataAsText; }
            set { _bCDataAsText = value; }
        }

        public bool NormalizeNewline
        {
            get { return _bNormalizeNewline; }
            set { _bNormalizeNewline = value; }
        }

        public bool TreatWhitespaceTextAsWSNode
        {
            get { return _bTreatWhitespaceTextAsWSNode; }
            set { _bTreatWhitespaceTextAsWSNode = value; }
        }

        public bool IgnoreEmptyTextNodes
        {
            get { return _bIgnoreEmptyTextNodes; }
            set { _bIgnoreEmptyTextNodes = value; }
        }

        public bool WhitespaceAsText
        {
            get { return _bWhitespaceAsText; }
            set { _bWhitespaceAsText = value; }
        }

        //NodePosition.Before is returned if node2 should be before node1;
        //NodePosition.After is returned if node2 should be after node1;
        //In any case, NodePosition.Unknown should never be returned.
        internal NodePosition ComparePosition(XmlDiffNode node1, XmlDiffNode node2)
        {
            int nt1 = (int)(node1.NodeType);
            int nt2 = (int)(node2.NodeType);
            if (nt2 > nt1)
            {
                return NodePosition.After;
            }
            if (nt2 < nt1)
            {
                return NodePosition.Before;
            }
            //now nt1 == nt2
            if (nt1 == (int)XmlDiffNodeType.Element)
            {
                return CompareElements(node1 as XmlDiffElement, node2 as XmlDiffElement);
            }
            else if (nt1 == (int)XmlDiffNodeType.Attribute)
            {
                return CompareAttributes(node1 as XmlDiffAttribute, node2 as XmlDiffAttribute);
            }
            else if (nt1 == (int)XmlDiffNodeType.ER)
            {
                return CompareERs(node1 as XmlDiffEntityReference, node2 as XmlDiffEntityReference);
            }
            else if (nt1 == (int)XmlDiffNodeType.PI)
            {
                return ComparePIs(node1 as XmlDiffProcessingInstruction, node2 as XmlDiffProcessingInstruction);
            }
            else if (node1 is XmlDiffCharacterData)
            {
                return CompareTextLikeNodes(node1 as XmlDiffCharacterData, node2 as XmlDiffCharacterData);
            }
            else
            {
                //something really wrong here, what should we do???
                Debug.Assert(false, "ComparePosition meets an undecision situation.");
                return NodePosition.Unknown;
            }
        }

        private NodePosition CompareElements(XmlDiffElement elem1, XmlDiffElement elem2)
        {
            Debug.Assert(elem1 != null);
            Debug.Assert(elem2 != null);
            int nCompare = 0;
            if ((nCompare = CompareText(elem2.LocalName, elem1.LocalName)) == 0)
            {
                if (IgnoreNS || (nCompare = CompareText(elem2.NamespaceURI, elem1.NamespaceURI)) == 0)
                {
                    if (IgnorePrefix || (nCompare = CompareText(elem2.Prefix, elem1.Prefix)) == 0)
                    {
                        if ((nCompare = CompareText(elem2.Value, elem1.Value)) == 0)
                        {
                            if ((nCompare = CompareAttributes(elem2, elem1)) == 0)
                            {
                                return NodePosition.After;
                            }
                        }
                    }
                }
            }
            if (nCompare > 0)
            {
                //elem2 > elem1
                return NodePosition.After;
            }
            else
            {
                //elem2 < elem1
                return NodePosition.Before;
            }
        }

        private int CompareAttributes(XmlDiffElement elem1, XmlDiffElement elem2)
        {
            int count1 = elem1.AttributeCount;
            int count2 = elem2.AttributeCount;
            if (count1 > count2)
            {
                return 1;
            }
            else if (count1 < count2)
            {
                return -1;
            }
            else
            {
                XmlDiffAttribute current1 = elem1.FirstAttribute;
                XmlDiffAttribute current2 = elem2.FirstAttribute;
                //			NodePosition result = 0;
                int nCompare = 0;
                while (current1 != null && current2 != null && nCompare == 0)
                {
                    if ((nCompare = CompareText(current2.LocalName, current1.LocalName)) == 0)
                    {
                        if (IgnoreNS || (nCompare = CompareText(current2.NamespaceURI, current1.NamespaceURI)) == 0)
                        {
                            if (IgnorePrefix || (nCompare = CompareText(current2.Prefix, current1.Prefix)) == 0)
                            {
                                if ((nCompare = CompareText(current2.Value, current1.Value)) == 0)
                                {
                                    //do nothing!
                                }
                            }
                        }
                    }
                    current1 = (XmlDiffAttribute)current1._next;
                    current2 = (XmlDiffAttribute)current2._next;
                }
                if (nCompare > 0)
                {
                    //elem1 > attr2
                    return 1;
                }
                else
                {
                    //elem1 < elem2
                    return -1;
                }
            }
        }

        private NodePosition CompareAttributes(XmlDiffAttribute attr1, XmlDiffAttribute attr2)
        {
            Debug.Assert(attr1 != null);
            Debug.Assert(attr2 != null);

            int nCompare = 0;
            if ((nCompare = CompareText(attr2.LocalName, attr1.LocalName)) == 0)
            {
                if (IgnoreNS || (nCompare = CompareText(attr2.NamespaceURI, attr1.NamespaceURI)) == 0)
                {
                    if (IgnorePrefix || (nCompare = CompareText(attr2.Prefix, attr1.Prefix)) == 0)
                    {
                        if ((nCompare = CompareText(attr2.Value, attr1.Value)) == 0)
                        {
                            return NodePosition.After;
                        }
                    }
                }
            }
            if (nCompare > 0)
            {
                //attr2 > attr1
                return NodePosition.After;
            }
            else
            {
                //attr2 < attr1
                return NodePosition.Before;
            }
        }

        private NodePosition CompareERs(XmlDiffEntityReference er1, XmlDiffEntityReference er2)
        {
            Debug.Assert(er1 != null);
            Debug.Assert(er2 != null);

            int nCompare = CompareText(er2.Name, er1.Name);
            if (nCompare >= 0)
            {
                return NodePosition.After;
            }
            else
            {
                return NodePosition.Before;
            }
        }

        private NodePosition ComparePIs(XmlDiffProcessingInstruction pi1, XmlDiffProcessingInstruction pi2)
        {
            Debug.Assert(pi1 != null);
            Debug.Assert(pi2 != null);

            int nCompare = 0;
            if ((nCompare = CompareText(pi2.Name, pi1.Name)) == 0)
            {
                if ((nCompare = CompareText(pi2.Value, pi1.Value)) == 0)
                {
                    return NodePosition.After;
                }
            }
            if (nCompare > 0)
            {
                //pi2 > pi1
                return NodePosition.After;
            }
            else
            {
                //pi2 < pi1
                return NodePosition.Before;
            }
        }

        private NodePosition CompareTextLikeNodes(XmlDiffCharacterData t1, XmlDiffCharacterData t2)
        {
            Debug.Assert(t1 != null);
            Debug.Assert(t2 != null);

            int nCompare = CompareText(t2.Value, t1.Value);
            if (nCompare >= 0)
            {
                return NodePosition.After;
            }
            else
            {
                return NodePosition.Before;
            }
        }

        //returns 0 if the same string; 1 if s1 > s1 and -1 if s1 < s2
        private int CompareText(string s1, string s2)
        {
            return Math.Sign(String.Compare(s1, s2, StringComparison.Ordinal));
        }

        public virtual void Load(string xmlFileName)
        {
            //XmlReaderSettings readerSettings = new XmlReaderSettings();
            //if (IgnoreDTD)
            //{
            //    readerSettings.ValidationType = ValidationType.None;
            //}
            //else
            //{
            //    readerSettings.ValidationType = ValidationType.DTD;
            //}

            //using (FileStream fs = PartialTrustFileStream.CreateFileStream(xmlFileName, FileMode.Open))
            //{
            //    using (XmlReader reader = XmlReader.Create(fs))
            //    {
            //        Load(reader);
            //    }
            //}
        }

        public virtual void Load(XmlReader reader)
        {
            if (_bLoaded)
            {
                throw new InvalidOperationException("The document already contains data and should not be used again.");
            }
            if (reader.ReadState == ReadState.Initial)
            {
                if (!reader.Read())
                {
                    return;
                }
            }
            PositionInfo pInfo = PositionInfo.GetPositionInfo(reader);
            ReadChildNodes(this, reader, pInfo);
            _bLoaded = true;
            this.nameTable = reader.NameTable;
        }

        internal void ReadChildNodes(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            bool lookAhead = false;
            do
            {
                lookAhead = false;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        LoadElement(parent, reader, pInfo);
                        break;
                    case XmlNodeType.Comment:
                        if (!IgnoreComments)
                        {
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.Comment);
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        LoadPI(parent, reader, pInfo);
                        break;
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        if (!IgnoreWhitespace)
                        {
                            if (this.WhitespaceAsText)
                            {
                                LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.Text);
                            }
                            else
                            {
                                LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.WS);
                            }
                        }
                        break;
                    case XmlNodeType.CDATA:
                        if (!CDataAsText)
                        {
                            LoadTextNode(parent, reader, pInfo, XmlDiffNodeType.CData);
                        }
                        else //merge with adjacent text/CDATA nodes
                        {
                            StringBuilder text = new StringBuilder();
                            text.Append(reader.Value);
                            while ((lookAhead = reader.Read()) && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
                            {
                                text.Append(reader.Value);
                            }
                            LoadTextNode(parent, text.ToString(), pInfo, XmlDiffNodeType.Text);
                        }
                        break;
                    case XmlNodeType.Text:
                        if (!CDataAsText)
                        {
                            LoadTextNode(parent, reader, pInfo, TextNodeIsWhitespace(reader.Value) ? XmlDiffNodeType.WS : XmlDiffNodeType.Text);
                        }
                        else //merge with adjacent text/CDATA nodes
                        {
                            StringBuilder text = new StringBuilder();
                            text.Append(reader.Value);
                            while ((lookAhead = reader.Read()) && (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA))
                            {
                                text.Append(reader.Value);
                            }
                            string txt = text.ToString();
                            LoadTextNode(parent, txt, pInfo, TextNodeIsWhitespace(txt) ? XmlDiffNodeType.WS : XmlDiffNodeType.Text);
                        }
                        break;
                    case XmlNodeType.EntityReference:
                        LoadEntityReference(parent, reader, pInfo);
                        break;
                    case XmlNodeType.EndElement:
                        SetElementEndPosition(parent as XmlDiffElement, pInfo);
                        return;
                    case XmlNodeType.Attribute: //attribute at top level
                        string attrVal = reader.Name + "=\"" + reader.Value + "\"";
                        LoadTopLevelAttribute(parent, attrVal, pInfo, XmlDiffNodeType.Text);
                        break;
                    default:
                        break;
                }
            }
            while (lookAhead || reader.Read());
        }

        private bool TextNodeIsWhitespace(string p)
        {
            if (!this.TreatWhitespaceTextAsWSNode)
            {
                return false;
            }
            for (int i = 0; i < p.Length; i++)
            {
                if (!Char.IsWhiteSpace(p[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private void LoadElement(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffElement elem = null;
            bool bEmptyElement = reader.IsEmptyElement;
            if (bEmptyElement)
            {
                elem = new XmlDiffEmptyElement(reader.LocalName, reader.Prefix, reader.NamespaceURI);
            }
            else
            {
                elem = new XmlDiffElement(reader.LocalName, reader.Prefix, reader.NamespaceURI);
            }
            elem.LineNumber = pInfo.LineNumber;
            elem.LinePosition = pInfo.LinePosition;
            ReadAttributes(elem, reader, pInfo);
            if (!bEmptyElement)
            {
                //            bool rtn = reader.Read();
                //			rtn = reader.Read();
                reader.Read(); //move to child
                ReadChildNodes(elem, reader, pInfo);
            }
            InsertChild(parent, elem);
        }

        private void ReadAttributes(XmlDiffElement parent, XmlReader reader, PositionInfo pInfo)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    XmlDiffAttribute attr = new XmlDiffAttribute(reader.LocalName, reader.Prefix, reader.NamespaceURI, reader.Value);
                    attr.SetValueAsQName(reader, reader.Value);
                    attr.LineNumber = pInfo.LineNumber;
                    attr.LinePosition = pInfo.LinePosition;
                    InsertAttribute(parent, attr);
                }
                while (reader.MoveToNextAttribute());
            }
        }

        private void LoadTextNode(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            LoadTextNode(parent, reader.Value, pInfo, nt);
        }

        private void LoadTextNode(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            if (!this.IgnoreEmptyTextNodes || !String.IsNullOrEmpty(text))
            {
                XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, this.NormalizeNewline);
                textNode.LineNumber = pInfo.LineNumber;
                textNode.LinePosition = pInfo.LinePosition;
                InsertChild(parent, textNode);
            }
        }

        private void LoadTopLevelAttribute(XmlDiffNode parent, string text, PositionInfo pInfo, XmlDiffNodeType nt)
        {
            XmlDiffCharacterData textNode = new XmlDiffCharacterData(text, nt, this.NormalizeNewline);
            textNode.LineNumber = pInfo.LineNumber;
            textNode.LinePosition = pInfo.LinePosition;
            InsertTopLevelAttributeAsText(parent, textNode);
        }

        private void LoadPI(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffProcessingInstruction pi = new XmlDiffProcessingInstruction(reader.Name, reader.Value);
            pi.LineNumber = pInfo.LineNumber;
            pi.LinePosition = pInfo.LinePosition;
            InsertChild(parent, pi);
        }

        private void LoadEntityReference(XmlDiffNode parent, XmlReader reader, PositionInfo pInfo)
        {
            XmlDiffEntityReference er = new XmlDiffEntityReference(reader.Name);
            er.LineNumber = pInfo.LineNumber;
            er.LinePosition = pInfo.LinePosition;
            InsertChild(parent, er);
        }

        private void SetElementEndPosition(XmlDiffElement elem, PositionInfo pInfo)
        {
            Debug.Assert(elem != null);
            elem.EndLineNumber = pInfo.LineNumber;
            elem.EndLinePosition = pInfo.LinePosition;
        }


        private void InsertChild(XmlDiffNode parent, XmlDiffNode newChild)
        {
            if (IgnoreChildOrder)
            {
                XmlDiffNode child = parent.FirstChild;
                XmlDiffNode prevChild = null;
                while (child != null && (ComparePosition(child, newChild) == NodePosition.After))
                {
                    prevChild = child;
                    child = child.NextSibling;
                }
                parent.InsertChildAfter(prevChild, newChild);
            }
            else
            {
                parent.InsertChildAfter(parent.LastChild, newChild);
            }
        }

        private void InsertTopLevelAttributeAsText(XmlDiffNode parent, XmlDiffCharacterData newChild)
        {
            if (parent.LastChild != null && (parent.LastChild.NodeType == XmlDiffNodeType.Text || parent.LastChild.NodeType == XmlDiffNodeType.WS))
            {
                ((XmlDiffCharacterData)parent.LastChild).Value = ((XmlDiffCharacterData)parent.LastChild).Value + " " + newChild.Value;
            }
            else
            {
                parent.InsertChildAfter(parent.LastChild, newChild);
            }
        }

        private void InsertAttribute(XmlDiffElement parent, XmlDiffAttribute newAttr)
        {
            Debug.Assert(parent != null);
            Debug.Assert(newAttr != null);
            newAttr._parent = parent;
            if (IgnoreAttributeOrder)
            {
                XmlDiffAttribute attr = parent.FirstAttribute;
                XmlDiffAttribute prevAttr = null;
                while (attr != null && (CompareAttributes(attr, newAttr) == NodePosition.After))
                {
                    prevAttr = attr;
                    attr = (XmlDiffAttribute)(attr.NextSibling);
                }
                parent.InsertAttributeAfter(prevAttr, newAttr);
            }
            else
            {
                parent.InsertAttributeAfter(parent.LastAttribute, newAttr);
            }
        }

        public override void WriteTo(XmlWriter w)
        {
            WriteContentTo(w);
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

        //IXPathNavigable override
        public XPathNavigator CreateNavigator()
        {
            return new XmlDiffNavigator(this);
        }

        //public void SortChildren(string expr, XmlNamespaceManager mngr)
        //{
        //    XPathNavigator _nav = this.CreateNavigator();
        //    XPathExpression _expr = _nav.Compile(expr);
        //    if (mngr != null)
        //    {
        //        _expr.SetContext(mngr);
        //    }
        //    XPathNodeIterator _iter = _nav.Select(_expr);
        //    while (_iter.MoveNext())
        //    {
        //        if (((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffElement)
        //        {
        //            SortChildren((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode);
        //        }
        //    }
        //}

        public void SortChildren(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                if (((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffElement)
                {
                    SortChildren((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
            }
        }

        public void IgnoreNodes(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                if (((XmlDiffNavigator)_iter.Current).CurrentNode is XmlDiffAttribute)
                {
                    ((XmlDiffElement)((XmlDiffNavigator)_iter.Current).CurrentNode.ParentNode).DeleteAttribute((XmlDiffAttribute)((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
                else
                {
                    ((XmlDiffNavigator)_iter.Current).CurrentNode.ParentNode.DeleteChild(((XmlDiffNavigator)_iter.Current).CurrentNode);
                }
            }
        }

        public void IgnoreValues(XPathExpression expr)
        {
            if (expr == null)
            {
                return;
            }
            XPathNavigator _nav = this.CreateNavigator();
            XPathNodeIterator _iter = _nav.Select(expr);
            while (_iter.MoveNext())
            {
                ((XmlDiffNavigator)_iter.Current).CurrentNode.IgnoreValue = true; ;
            }
        }

        private void SortChildren(XmlDiffElement elem)
        {
            if (elem.FirstChild != null)
            {
                XmlDiffNode _first = elem.FirstChild;
                XmlDiffNode _current = elem.FirstChild;
                XmlDiffNode _last = elem.LastChild;
                elem._firstChild = null;
                elem._lastChild = null;
                //set flag to ignore child order
                bool temp = IgnoreChildOrder;
                IgnoreChildOrder = true;
                XmlDiffNode _next = null;
                do
                {
                    if (_current is XmlDiffElement)
                    {
                        _next = _current._next;
                    }
                    _current._next = null;
                    InsertChild(elem, _current);
                    if (_current == _last)
                    {
                        break;
                    }
                    _current = _next;
                }
                while (true);
                //restore flag for ignoring child order
                IgnoreChildOrder = temp;
            }
        }
    }
}


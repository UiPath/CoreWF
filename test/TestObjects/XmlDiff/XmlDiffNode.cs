// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Xml;

namespace Test.Common.TestObjects.XmlDiff
{
    public abstract class XmlDiffNode
    {
        internal XmlDiffNode _next;
        internal XmlDiffNode _firstChild;
        internal XmlDiffNode _lastChild;
        internal XmlDiffNode _parent;
        internal int _lineNumber, _linePosition;
        internal bool _bIgnoreValue;
        private PropertyCollection _extendedProperties;

        public XmlDiffNode()
        {
            this._next = null;
            this._firstChild = null;
            this._lastChild = null;
            this._parent = null;
            this._lineNumber = 0;
            this._linePosition = 0;
        }

        public XmlDiffNode FirstChild
        {
            get
            {
                return this._firstChild;
            }
        }
        public XmlDiffNode LastChild
        {
            get
            {
                return this._lastChild;
            }
        }
        public XmlDiffNode NextSibling
        {
            get
            {
                return this._next;
            }
        }
        public XmlDiffNode ParentNode
        {
            get
            {
                return this._parent;
            }
        }

        public virtual bool IgnoreValue
        {
            get
            {
                return this._bIgnoreValue;
            }
            set
            {
                this._bIgnoreValue = value;
                XmlDiffNode current = this._firstChild;
                while (current != null)
                {
                    current.IgnoreValue = value;
                    current = current._next;
                }
            }
        }


        public abstract XmlDiffNodeType NodeType { get; }

        public virtual string OuterXml
        {
            get
            {
                StringWriter sw = new StringWriter();
                using (XmlWriter xw = XmlWriter.Create(sw))
                {
                    WriteTo(xw);
                }

                return sw.ToString();
            }
        }
        public virtual string InnerXml
        {
            get
            {
                StringWriter sw = new StringWriter();
                using (XmlWriter xw = XmlWriter.Create(sw))
                {
                    WriteTo(xw);
                }

                return sw.ToString();
            }
        }

        public PropertyCollection ExtendedProperties
        {
            get
            {
                if (_extendedProperties == null)
                {
                    _extendedProperties = new PropertyCollection();
                }
                return _extendedProperties;
            }
        }

        public int LineNumber
        {
            get { return this._lineNumber; }
            set { this._lineNumber = value; }
        }

        public int LinePosition
        {
            get { return this._linePosition; }
            set { this._linePosition = value; }
        }

        public abstract void WriteTo(XmlWriter w);
        public abstract void WriteContentTo(XmlWriter w);
        public virtual void InsertChildAfter(XmlDiffNode child, XmlDiffNode newChild)
        {
            Debug.Assert(newChild != null);
            newChild._parent = this;
            if (child == null)
            {
                newChild._next = this._firstChild;
                this._firstChild = newChild;
            }
            else
            {
                Debug.Assert(child._parent == this);
                newChild._next = child._next;
                child._next = newChild;
            }
            if (newChild._next == null)
            {
                this._lastChild = newChild;
            }
        }

        public virtual void DeleteChild(XmlDiffNode child)
        {
            if (child == this.FirstChild)//delete head
            {
                this._firstChild = this.FirstChild.NextSibling;
            }
            else
            {
                XmlDiffNode current = this.FirstChild;
                XmlDiffNode previous = null;
                while (current != child)
                {
                    previous = current;
                    current = current.NextSibling;
                }
                Debug.Assert(current != null);
                if (current == this.LastChild) //tail being deleted
                {
                    this._lastChild = current.NextSibling;
                }
                previous._next = current.NextSibling;
            }
        }
    }
}

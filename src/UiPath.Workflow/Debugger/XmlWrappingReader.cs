// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace System.Activities.Debugger;

internal class XmlWrappingReader : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
{
    private XmlReader _baseReader;
    private IXmlLineInfo _baseReaderAsLineInfo;
    private IXmlNamespaceResolver _baseReaderAsNamespaceResolver;

    public override XmlReaderSettings Settings => _baseReader.Settings;

    public override XmlNodeType NodeType => _baseReader.NodeType;

    public override string Name => _baseReader.Name;

    public override string LocalName => _baseReader.LocalName;

    public override string NamespaceURI => _baseReader.NamespaceURI;

    public override string Prefix => _baseReader.Prefix;

    public override bool HasValue => _baseReader.HasValue;

    public override string Value => _baseReader.Value;

    public override int Depth => _baseReader.Depth;

    public override string BaseURI => _baseReader.BaseURI;

    public override bool IsEmptyElement => _baseReader.IsEmptyElement;

    public override bool IsDefault => _baseReader.IsDefault;

    public override char QuoteChar => _baseReader.QuoteChar;

    public override XmlSpace XmlSpace => _baseReader.XmlSpace;

    public override string XmlLang => _baseReader.XmlLang;

    public override IXmlSchemaInfo SchemaInfo => _baseReader.SchemaInfo;

    public override Type ValueType => _baseReader.ValueType;

    public override int AttributeCount => _baseReader.AttributeCount;

    public override bool CanResolveEntity => _baseReader.CanResolveEntity;

    public override bool EOF => _baseReader.EOF;

    public override ReadState ReadState => _baseReader.ReadState;

    public override bool HasAttributes => _baseReader.HasAttributes;

    public override XmlNameTable NameTable => _baseReader.NameTable;

    public virtual int LineNumber => (_baseReaderAsLineInfo == null) ? 0 : _baseReaderAsLineInfo.LineNumber;

    public virtual int LinePosition => (_baseReaderAsLineInfo == null) ? 0 : _baseReaderAsLineInfo.LinePosition;

    protected XmlReader BaseReader
    {
        set
        {
            _baseReader = value;
            _baseReaderAsLineInfo = value as IXmlLineInfo;
            _baseReaderAsNamespaceResolver = value as IXmlNamespaceResolver;
        }
    }

    protected IXmlLineInfo BaseReaderAsLineInfo => _baseReaderAsLineInfo;

    public override string this[int i] => _baseReader[i];

    public override string this[string name] => _baseReader[name];

    public override string this[string name, string namespaceURI] => _baseReader[name, namespaceURI];

    public override string GetAttribute(string name) => _baseReader.GetAttribute(name);

    public override string GetAttribute(string name, string namespaceURI) => _baseReader.GetAttribute(name, namespaceURI);

    public override string GetAttribute(int i) => _baseReader.GetAttribute(i);

    public override bool MoveToAttribute(string name) => _baseReader.MoveToAttribute(name);

    public override bool MoveToAttribute(string name, string ns) => _baseReader.MoveToAttribute(name, ns);

    public override void MoveToAttribute(int i) => _baseReader.MoveToAttribute(i);

    public override bool MoveToFirstAttribute() => _baseReader.MoveToFirstAttribute();

    public override bool MoveToNextAttribute() => _baseReader.MoveToNextAttribute();

    public override bool MoveToElement() => _baseReader.MoveToElement();

    public override bool Read() => _baseReader.Read();

    public override void Close() => _baseReader.Close();

    public override void Skip() => _baseReader.Skip();

    public override string LookupNamespace(string prefix) => _baseReader.LookupNamespace(prefix);

    public override void ResolveEntity() => _baseReader.ResolveEntity();

    public override bool ReadAttributeValue() => _baseReader.ReadAttributeValue();

    public virtual bool HasLineInfo() => _baseReaderAsLineInfo != null && _baseReaderAsLineInfo.HasLineInfo();

    string IXmlNamespaceResolver.LookupPrefix(string namespaceName) => _baseReaderAsNamespaceResolver?.LookupPrefix(namespaceName);

    IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope) => _baseReaderAsNamespaceResolver?.GetNamespacesInScope(scope);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            if (_baseReader != null)
            {
                ((IDisposable)_baseReader).Dispose();
            }

            _baseReader = null;
        }
    }
}

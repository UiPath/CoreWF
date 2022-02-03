// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace System.Activities.Debugger;

internal class XmlReaderWithSourceLocation : XmlWrappingReader
{
    private readonly Stack<DocumentLocation> _contentStartLocationStack;
    private Dictionary<DocumentLocation, DocumentRange> _attributeValueRanges;
    private CharacterSpottingTextReader _characterSpottingTextReader;
    private Dictionary<DocumentLocation, DocumentRange> _contentValueRanges;
    private Dictionary<DocumentLocation, DocumentRange> _emptyElementRanges;
    private Dictionary<DocumentLocation, DocumentLocation> _endElementLocations;
    private Dictionary<DocumentLocation, DocumentLocation> _startElementLocations;

    public XmlReaderWithSourceLocation(TextReader underlyingTextReader)
    {
        UnitTestUtility.Assert(underlyingTextReader != null,
            "CharacterSpottingTextReader cannot be null and should be ensured by caller.");
        var characterSpottingTextReader = new CharacterSpottingTextReader(underlyingTextReader);
        BaseReader = Create(characterSpottingTextReader);
        UnitTestUtility.Assert(BaseReaderAsLineInfo != null,
            "The XmlReader created by XmlReader.Create should ensure this.");
        UnitTestUtility.Assert(BaseReaderAsLineInfo!.HasLineInfo(),
            "The XmlReader created by XmlReader.Create should ensure this.");
        _characterSpottingTextReader = characterSpottingTextReader;
        _contentStartLocationStack = new Stack<DocumentLocation>();
    }

    public Dictionary<DocumentLocation, DocumentRange> AttributeValueRanges =>
        _attributeValueRanges ??= new Dictionary<DocumentLocation, DocumentRange>();

    public Dictionary<DocumentLocation, DocumentRange> ContentValueRanges =>
        _contentValueRanges ??= new Dictionary<DocumentLocation, DocumentRange>();

    public Dictionary<DocumentLocation, DocumentRange> EmptyElementRanges =>
        _emptyElementRanges ??= new Dictionary<DocumentLocation, DocumentRange>();

    public Dictionary<DocumentLocation, DocumentLocation> StartElementLocations =>
        _startElementLocations ??= new Dictionary<DocumentLocation, DocumentLocation>();

    public Dictionary<DocumentLocation, DocumentLocation> EndElementLocations =>
        _endElementLocations ??= new Dictionary<DocumentLocation, DocumentLocation>();

    private DocumentLocation CurrentLocation => new(BaseReaderAsLineInfo.LineNumber, BaseReaderAsLineInfo.LinePosition);

    public override bool Read()
    {
        var result = base.Read();
        if (NodeType == XmlNodeType.Element)
        {
            var elementLocation = CurrentLocation;
            if (IsEmptyElement)
            {
                var emptyElementRange = FindEmptyElementRange(elementLocation);
                EmptyElementRanges.Add(elementLocation, emptyElementRange);
            }
            else
            {
                var startElementBracket = FindStartElementBracket(elementLocation);
                StartElementLocations.Add(elementLocation, startElementBracket);

                // Push a null as a place holder. In XmlNodeType.Text part, we replace this
                // null with real data. Why not pushing real data only without this place holder?
                // Because in XmlNodeType.EndElement, we need to know whether there is Text. Think 
                // about situation like <a>Text1<b><c>Text2</c></b>Text3</a>
                // So, each time an Element starts, we push a place holder in the stack so that Start
                // and End don't mis-match.
                _contentStartLocationStack.Push(null);
            }

            var attributeCount = AttributeCount;
            if (attributeCount > 0)
            {
                for (var i = 0; i < attributeCount; i++)
                {
                    MoveToAttribute(i);
                    var memberLocation = CurrentLocation;
                    var attributeValueRange = FindAttributeValueLocation(memberLocation);
                    AttributeValueRanges.Add(memberLocation, attributeValueRange);
                }

                MoveToElement();
            }
        }
        else if (NodeType == XmlNodeType.EndElement)
        {
            var endElementLocation = CurrentLocation;
            var endElementBracket = FindEndElementBracket(endElementLocation);
            EndElementLocations.Add(endElementLocation, endElementBracket);
            UnitTestUtility.Assert(
                _contentStartLocationStack.Count > 0,
                "The stack should contain at least a null we pushed in StartElement.");
            var contentStartLocation = _contentStartLocationStack.Pop();
            if (contentStartLocation != null)
            {
                var contentEnd = FindContentEndBefore(endElementLocation);
                ContentValueRanges.Add(endElementLocation, new DocumentRange(contentStartLocation, contentEnd));
            }
        }
        else if (NodeType == XmlNodeType.Text)
        {
            UnitTestUtility.Assert(_contentStartLocationStack.Count > 0, "Adding Text with out StartElement?");
            if (_contentStartLocationStack.Peek() == null)
            {
                // no text was added since the last StartElement.
                // This is the start of the content of this Element.
                // <a>ABCDE</a>
                // Sometimes, xml reader gives the text by ABC and DE in 
                // two times.
                _contentStartLocationStack.Pop();
                _contentStartLocationStack.Push(CurrentLocation);
            }
        }

        return result;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            ((IDisposable) _characterSpottingTextReader)?.Dispose();

            _characterSpottingTextReader = null;
        }
    }

    private DocumentLocation FindStartElementBracket(DocumentLocation elementLocation) =>
        _characterSpottingTextReader.FindCharacterStrictlyBefore('<', elementLocation);

    private DocumentLocation FindEndElementBracket(DocumentLocation elementLocation) =>
        _characterSpottingTextReader.FindCharacterStrictlyAfter('>', elementLocation);

    private DocumentRange FindEmptyElementRange(DocumentLocation elementLocation)
    {
        var startBracket = FindStartElementBracket(elementLocation);
        var endBracket = FindEndElementBracket(elementLocation);
        UnitTestUtility.Assert(startBracket != null, "XmlReader should guarantee there must be a start angle bracket.");
        UnitTestUtility.Assert(endBracket != null, "XmlReader should guarantee there must be an end angle bracket.");
        var emptyElementRange = new DocumentRange(startBracket, endBracket);
        return emptyElementRange;
    }

    private DocumentRange FindAttributeValueLocation(DocumentLocation memberLocation)
    {
        UnitTestUtility.Assert(_characterSpottingTextReader != null, "Ensured by constructor.");
        var attributeStart = _characterSpottingTextReader.FindCharacterStrictlyAfter(QuoteChar, memberLocation);
        UnitTestUtility.Assert(attributeStart != null, "Read should ensure the two quote characters exist");
        var attributeEnd = _characterSpottingTextReader.FindCharacterStrictlyAfter(QuoteChar, attributeStart);
        UnitTestUtility.Assert(attributeEnd != null, "Read should ensure the two quote characters exist");
        return new DocumentRange(attributeStart, attributeEnd);
    }

    private DocumentLocation FindContentEndBefore(DocumentLocation location)
    {
        var contentEnd = FindStartElementBracket(location);
        var linePosition = contentEnd.LinePosition.Value - 1;

        // Line position is 1-based
        if (linePosition < 1)
        {
            return _characterSpottingTextReader.FindCharacterStrictlyBefore('\n', contentEnd);
        }

        return new DocumentLocation(contentEnd.LineNumber.Value, linePosition);
    }
}

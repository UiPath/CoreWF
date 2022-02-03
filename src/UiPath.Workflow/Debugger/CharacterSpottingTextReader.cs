// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace System.Activities.Debugger;

internal class CharacterSpottingTextReader : TextReader
{
    // These 'special characters' couple with the fact that we are working on XML.
    private const char StartAngleBracket = '<';
    private const char EndAngleBracket = '>';
    private const char SingleQuote = '\'';
    private const char DoubleQuote = '"';
    private const char EndLine = '\n';
    private const char CarriageReturn = '\r';
    private readonly List<DocumentLocation> _doubleQuotes;
    private readonly List<DocumentLocation> _endAngleBrackets;
    private readonly List<DocumentLocation> _endLines;
    private readonly List<DocumentLocation> _singleQuotes;
    private readonly List<DocumentLocation> _startAngleBrackets;

    private readonly TextReader _underlyingReader;
    private int _currentLine;
    private int _currentPosition;

    public CharacterSpottingTextReader(TextReader underlyingReader)
    {
        UnitTestUtility.Assert(underlyingReader != null,
            "underlyingReader should not be null and should be ensured by caller.");
        _underlyingReader = underlyingReader;
        _currentLine = 1;
        _currentPosition = 1;
        _startAngleBrackets = new List<DocumentLocation>();
        _endAngleBrackets = new List<DocumentLocation>();
        _singleQuotes = new List<DocumentLocation>();
        _doubleQuotes = new List<DocumentLocation>();
        _endLines = new List<DocumentLocation>();
    }

    // CurrentLocation consists of the current line number and the current position on the line.
    // 
    // The current position is like a cursor moving along the line. For example, a string "abc" ending with "\r\n":
    // 
    //    abc\r\n
    // 
    // the current position, depicted as | below, moves from char to char:
    // 
    //    |a|b|c|\r|\n
    // 
    // When we are at the beginning of the line, the current position is 1. After we read the first char, 
    // we advance the current position to 2, and so on:
    // 
    //    1 2 3 4 
    //    |a|b|c|\r|\n
    // 
    // As we reach the end-of-line character on the line, which can be \r, \r\n or \n, we move to the next line and reset the current position to 1.
    private DocumentLocation CurrentLocation => new(_currentLine, _currentPosition);

    public override void Close()
    {
        _underlyingReader.Close();
    }

    public override int Peek()
    {
        // This character is not consider read, therefore we don't need to analyze this.
        return _underlyingReader.Peek();
    }

    public override int Read()
    {
        var result = _underlyingReader.Read();
        if (result != -1)
        {
            result = AnalyzeReadData((char) result);
        }

        return result;
    }

    internal DocumentLocation FindCharacterStrictlyAfter(char c, DocumentLocation afterLocation)
    {
        var locationList = GetLocationList(c);
        UnitTestUtility.Assert(locationList != null, "We should always find character for special characters only");

        // Note that this 'nextLocation' may not represent a real document location (we could hit an end line character here so that there is no next line
        // position. This is merely used for the search algorithm below:
        var nextLocation = new DocumentLocation(afterLocation.LineNumber,
            new OneBasedCounter(afterLocation.LinePosition.Value + 1));
        var result = locationList.MyBinarySearch(nextLocation);
        if (result.IsFound)
            // It is possible that the next location is a quote itself, or
        {
            return nextLocation;
        }

        if (result.IsNextIndexAvailable)
            // Some other later position is the quote, or
        {
            return locationList[result.NextIndex];
        }

        return null;
    }

    internal DocumentLocation FindCharacterStrictlyBefore(char c, DocumentLocation documentLocation)
    {
        var locationList = GetLocationList(c);
        UnitTestUtility.Assert(locationList != null, "We should always find character for special characters only");

        var result = locationList.MyBinarySearch(documentLocation);
        if (result.IsFound)
        {
            if (result.FoundIndex > 0)
            {
                return locationList[result.FoundIndex - 1];
            }

            return null;
        }

        if (result.IsNextIndexAvailable)
        {
            if (result.NextIndex > 0)
            {
                return locationList[result.NextIndex - 1];
            }

            return null;
        }

        if (locationList.Count > 0)
        {
            return locationList[locationList.Count - 1];
        }

        return null;
    }

    private List<DocumentLocation> GetLocationList(char c)
    {
        return c switch
        {
            StartAngleBracket => _startAngleBrackets,
            EndAngleBracket   => _endAngleBrackets,
            SingleQuote       => _singleQuotes,
            DoubleQuote       => _doubleQuotes,
            EndLine           => _endLines,
            _                 => null
        };
    }

    /// <summary>
    ///     Process last character read, and canonicalize end line.
    /// </summary>
    /// <param name="lastCharacterRead">The last character read by the underlying reader</param>
    /// <returns>The last character processed</returns>
    private char AnalyzeReadData(char lastCharacterRead)
    {
        // XML specification requires end-of-line == '\n' or '\r' or "\r\n"
        // See http://www.w3.org/TR/2008/REC-xml-20081126/#sec-line-ends for details.
        if (lastCharacterRead == CarriageReturn)
        {
            // if reading \r and peek next char is \n, then process \n as well
            var nextChar = _underlyingReader.Peek();
            if (nextChar == EndLine)
            {
                lastCharacterRead = (char) _underlyingReader.Read();
            }
        }

        if (lastCharacterRead == EndLine || lastCharacterRead == CarriageReturn)
        {
            _endLines.Add(CurrentLocation);
            _currentLine++;
            _currentPosition = 1;

            // according to XML spec, both \r\n and \r should be translated to \n
            return EndLine;
        }

        var locations = GetLocationList(lastCharacterRead);
        locations?.Add(CurrentLocation);

        _currentPosition++;
        return lastCharacterRead;
    }
}

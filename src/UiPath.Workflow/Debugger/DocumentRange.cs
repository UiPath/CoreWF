// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Activities.Debugger;

// Immutable, start and end always non-null.
[DebuggerDisplay(
    "({Start.LineNumber.Value}:{Start.LinePosition.Value}) - ({End.LineNumber.Value}:{End.LinePosition.Value})")]
internal class DocumentRange : IEquatable<DocumentRange>
{
    internal DocumentRange(DocumentLocation start, DocumentLocation end)
    {
        UnitTestUtility.Assert(start != null, "DocumentRange.Start cannot be null");
        UnitTestUtility.Assert(end != null, "DocumentRange.End cannot be null");
        UnitTestUtility.Assert(
            start.LineNumber.Value < end.LineNumber.Value || start.LineNumber.Value == end.LineNumber.Value &&
            start.LinePosition.Value <= end.LinePosition.Value, "Start cannot before go after End.");
        Start = start;
        End = end;
    }

    internal DocumentRange(int startLineNumber, int startLinePosition, int endLineNumber, int endLinePosition)
        : this(new DocumentLocation(startLineNumber, startLinePosition),
            new DocumentLocation(endLineNumber, endLinePosition)) { }

    internal DocumentLocation Start { get; }

    internal DocumentLocation End { get; }

    public bool Equals(DocumentRange other)
    {
        if (other == null)
        {
            return false;
        }

        return Start.Equals(other.Start) && End.Equals(other.End);
    }

    public override int GetHashCode()
    {
        return Start.GetHashCode() ^ End.GetHashCode();
    }
}

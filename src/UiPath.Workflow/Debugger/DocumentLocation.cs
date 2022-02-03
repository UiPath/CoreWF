// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Activities.Debugger;

// Immutable, lineNumber and linePosition always non-null.
[DebuggerDisplay("({LineNumber.Value}:{LinePosition.Value})")]
internal class DocumentLocation : IEquatable<DocumentLocation>, IComparable<DocumentLocation>
{
    internal DocumentLocation(OneBasedCounter lineNumber, OneBasedCounter linePosition)
    {
        UnitTestUtility.Assert(lineNumber != null, "lineNumber should not be null.");
        UnitTestUtility.Assert(linePosition != null, "linePosition should not be null.");
        LineNumber = lineNumber;
        LinePosition = linePosition;
    }

    internal DocumentLocation(int lineNumber, int linePosition)
        : this(new OneBasedCounter(lineNumber), new OneBasedCounter(linePosition)) { }

    internal OneBasedCounter LineNumber { get; }

    internal OneBasedCounter LinePosition { get; }

    public int CompareTo(DocumentLocation that)
    {
        if (that == null)
        {
            // Following the convention we have in System.Int32 that anything is considered bigger than null.
            return 1;
        }

        if (LineNumber.Value == that.LineNumber.Value)
            // The subtraction of two numbers >= 1 must not underflow integer.
        {
            return LinePosition.Value - that.LinePosition.Value;
        }

        return LineNumber.Value - that.LineNumber.Value;
    }

    public bool Equals(DocumentLocation that)
    {
        if (that == null)
        {
            return false;
        }

        return LineNumber.Value == that.LineNumber.Value && LinePosition.Value == that.LinePosition.Value;
    }

    public override int GetHashCode()
    {
        return LineNumber.Value.GetHashCode() ^ LinePosition.Value.GetHashCode();
    }
}

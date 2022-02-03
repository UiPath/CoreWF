// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Debugger.Symbol;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Diagnostics;
using System.Linq;

namespace System.Activities.Debugger;

// Identifies a specific location in the target source code.
//
// This source information is used in creating PDBs, which will be passed to the debugger,
// which will resolve the source file based off its own source paths.
// Source ranges can:
// * refer to just an entire single line.
// * can be a subset within a single line (when StartLine == EndLine)
// * can also span multiple lines.
// When column info is provided, the debugger will highlight the characters starting at the start line and start column,
// and going up to but not including the character specified by the end line and end column.
[Fx.Tag.SecurityNoteAttribute(Miscellaneous =
    "RequiresReview - Our partial trust mechanisms require that this class remain Immutable. Do not add code that allows an instance of this class to change after creation without strict review.")]
[DebuggerNonUserCode]
[Serializable]
[Fx.Tag.XamlVisibleAttribute(false)]
public class SourceLocation
{
    // Define a source location from a filename and line-number (1-based).
    // This is a convenience constructor to specify the entire line.
    // This does not load the source file to determine column ranges.
    public SourceLocation(string fileName, int line)
        : this(fileName, line, 1, line, int.MaxValue) { }

    public SourceLocation(
        string fileName,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
        : this(fileName, null, startLine, startColumn, endLine, endColumn) { }

    // Define a source location in a file.
    // Line/Column are 1-based.
    internal SourceLocation(
        string fileName,
        byte[] checksum,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        if (startLine <= 0)
        {
            throw FxTrace.Exception.Argument("startLine", SR.InvalidSourceLocationLineNumber("startLine", startLine));
        }

        if (startColumn <= 0)
        {
            throw FxTrace.Exception.Argument("startColumn", SR.InvalidSourceLocationColumn("startColumn", startColumn));
        }

        if (endLine <= 0)
        {
            throw FxTrace.Exception.Argument("endLine", SR.InvalidSourceLocationLineNumber("endLine", endLine));
        }

        if (endColumn <= 0)
        {
            throw FxTrace.Exception.Argument("endColumn", SR.InvalidSourceLocationColumn("endColumn", endColumn));
        }

        if (startLine > endLine)
        {
            throw FxTrace.Exception.ArgumentOutOfRange("endLine", endLine,
                SR.OutOfRangeSourceLocationEndLine(startLine));
        }

        if (startLine == endLine && startColumn > endColumn)
        {
            throw FxTrace.Exception.ArgumentOutOfRange("endColumn", endColumn,
                SR.OutOfRangeSourceLocationEndColumn(startColumn));
        }

        FileName = fileName?.ToUpperInvariant();
        StartLine = startLine;
        EndLine = endLine;
        StartColumn = startColumn;
        EndColumn = endColumn;
        Checksum = checksum;
    }

    public string FileName { get; }

    // Get the 1-based start line.
    public int StartLine { get; }

    // Get the 1-based starting column.
    public int StartColumn { get; }

    // Get the 1-based end line. This should be greater or equal to StartLine.
    public int EndLine { get; }

    // Get the 1-based ending column.
    public int EndColumn { get; }

    // get the checksum of the source file
    internal byte[] Checksum { get; }

    public bool IsSingleWholeLine => EndColumn == int.MaxValue && StartLine == EndLine && StartColumn == 1;

    // Equality comparison function. This checks for strict equality and
    // not for superset or subset relationships.
    public override bool Equals(object obj)
    {
        if (obj is not SourceLocation rsl)
        {
            return false;
        }

        if (FileName != rsl.FileName)
        {
            return false;
        }

        if (StartLine != rsl.StartLine ||
            StartColumn != rsl.StartColumn ||
            EndLine != rsl.EndLine ||
            EndColumn != rsl.EndColumn)
        {
            return false;
        }

        if ((Checksum == null) ^ (rsl.Checksum == null))
        {
            return false;
        }

        if (Checksum != null && rsl.Checksum != null && !Checksum.SequenceEqual(rsl.Checksum))
        {
            return false;
        }

        // everything matches
        return true;
    }

    // Get a hash code.
    public override int GetHashCode()
    {
        return (string.IsNullOrEmpty(FileName) ? 0 : FileName.GetHashCode()) ^
            StartLine.GetHashCode() ^
            StartColumn.GetHashCode() ^
            (Checksum == null ? 0 : SymbolHelper.GetHexStringFromChecksum(Checksum).GetHashCode());
    }

    internal static bool IsValidRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        return
            startLine > 0 && startColumn > 0 && endLine > 0 && endColumn > 0 &&
            (startLine < endLine || startLine == endLine && startColumn < endColumn);
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Debugger.Symbol;
using System.Activities.Internals;

namespace System.Activities.Debugger;

/// <remarks>
/// Identifies a specific location in the target source code.
/// This source information is used in creating PDBs, which will be passed to the debugger,
/// which will resolve the source file based off its own source paths.
/// Source ranges can:
/// <list type="bullet">
/// <item>refer to just an entire single line</item>
/// <item>be a subset within a single line (when StartLine == EndLine)</item>
/// <item>span multiple lines</item>
/// </list>
/// <para>
/// When column info is provided, the debugger will highlight the characters starting at the start line and start column,
/// and going up to but not including the character specified by the end line and end column.
/// </para>
/// </remarks>
public record SourceLocation
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
            throw FxTrace.Exception.Argument(nameof(startLine), SR.InvalidSourceLocationLineNumber(nameof(startLine), startLine));
        }

        if (startColumn <= 0)
        {
            throw FxTrace.Exception.Argument(nameof(startColumn), SR.InvalidSourceLocationColumn(nameof(startColumn), startColumn));
        }

        if (endLine <= 0)
        {
            throw FxTrace.Exception.Argument(nameof(endLine), SR.InvalidSourceLocationLineNumber(nameof(endLine), endLine));
        }

        if (endColumn <= 0)
        {
            throw FxTrace.Exception.Argument(nameof(endColumn), SR.InvalidSourceLocationColumn(nameof(endColumn), endColumn));
        }

        if (startLine > endLine)
        {
            throw FxTrace.Exception.ArgumentOutOfRange(nameof(endLine), endLine,
                SR.OutOfRangeSourceLocationEndLine(startLine));
        }

        if (startLine == endLine && startColumn > endColumn)
        {
            throw FxTrace.Exception.ArgumentOutOfRange(nameof(endColumn), endColumn,
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

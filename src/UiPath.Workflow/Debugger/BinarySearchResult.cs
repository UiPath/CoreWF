// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Activities.Debugger;

[DebuggerDisplay("{this.ToString()}")]
internal class BinarySearchResult
{
    private readonly int _count;
    private readonly int _result;

    internal BinarySearchResult(int resultFromBinarySearch, int count)
    {
        _result = resultFromBinarySearch;
        _count = count;
    }

    internal bool IsFound => _result >= 0;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal int FoundIndex
    {
        get
        {
            UnitTestUtility.Assert(IsFound, "We should not call FoundIndex if we cannot find the element.");
            return _result;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal int NextIndex
    {
        get
        {
            UnitTestUtility.Assert(!IsFound, "We should not call NextIndex if we found the element.");
            UnitTestUtility.Assert(IsNextIndexAvailable,
                "We should not call NextIndex if next index is not available.");
            return NextIndexValue;
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal bool IsNextIndexAvailable
    {
        get
        {
            UnitTestUtility.Assert(!IsFound, "We should not call IsNextIndexAvailable if we found the element.");
            return NextIndexValue != _count;
        }
    }

    private int NextIndexValue => ~_result;

    [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider",
        MessageId = "System.String.Format(System.String,System.Object)",
        Justification = "Message used in debugger only.")]
    public override string ToString()
    {
        if (IsFound)
        {
            return $"Data is found at index {FoundIndex}.";
        }

        if (IsNextIndexAvailable)
        {
            return $"Data is not found, the next index is {NextIndex}.";
        }

        return "Data is not found and there is no next index.";
    }
}

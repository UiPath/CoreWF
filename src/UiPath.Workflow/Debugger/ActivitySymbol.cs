// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;

namespace System.Activities.Debugger.Symbol;

// Represent the debug symbol for an Activity.
// It defines the start/end of Activity in the Xaml file.
public class ActivitySymbol
{
    private string _id;

    // Binary deserializer.
    internal ActivitySymbol(BinaryReader reader)
    {
        StartLine = SymbolHelper.ReadEncodedInt32(reader);
        StartColumn = SymbolHelper.ReadEncodedInt32(reader);
        EndLine = SymbolHelper.ReadEncodedInt32(reader);
        EndColumn = SymbolHelper.ReadEncodedInt32(reader);
        var qidLength = SymbolHelper.ReadEncodedInt32(reader);
        if (qidLength > 0)
        {
            QualifiedId = reader.ReadBytes(qidLength);
        }
    }

    internal ActivitySymbol() { }

    public int StartLine { get; internal init; }
    public int StartColumn { get; internal init; }
    public int EndLine { get; internal init; }

    public int EndColumn { get; internal init; }

    // Internal representation of QualifiedId.
    internal byte[] QualifiedId { get; init; }

    // Publicly available Id.
    public string Id
    {
        get
        {
            if (_id == null)
            {
                _id = QualifiedId != null ? new QualifiedId(QualifiedId).ToString() : string.Empty;
            }

            return _id;
        }
    }

    // Binary serializer.
    internal void Write(BinaryWriter writer)
    {
        SymbolHelper.WriteEncodedInt32(writer, StartLine);
        SymbolHelper.WriteEncodedInt32(writer, StartColumn);
        SymbolHelper.WriteEncodedInt32(writer, EndLine);
        SymbolHelper.WriteEncodedInt32(writer, EndColumn);
        if (QualifiedId != null)
        {
            SymbolHelper.WriteEncodedInt32(writer, QualifiedId.Length);
            writer.Write(QualifiedId, 0, QualifiedId.Length);
        }
        else
        {
            SymbolHelper.WriteEncodedInt32(writer, 0);
        }
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}", Id, StartLine, StartColumn, EndLine,
            EndColumn);
    }
}

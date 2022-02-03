// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace System.Activities.Debugger.Symbol;

// Represent debug symbol of a workflow tree (similar to pdb file).
// It contains the absolute path of the xaml file and the location of each activity in the workflow tree.
// This is used to instrument the workflow without having access to the original xaml file.
public class WorkflowSymbol
{
    internal const EncodingFormat DefaultEncodingFormat = EncodingFormat.Binary;

    private byte[] _checksum;

    public WorkflowSymbol() { }

    // These constructors are private and used by Decode() method.

    // Binary deserializer.
    private WorkflowSymbol(BinaryReader reader, byte[] checksum)
    {
        FileName = reader.ReadString();
        var numSymbols = SymbolHelper.ReadEncodedInt32(reader);
        Symbols = new List<ActivitySymbol>(numSymbols);
        for (var i = 0; i < numSymbols; ++i)
        {
            Symbols.Add(new ActivitySymbol(reader));
        }

        _checksum = checksum;
    }

    public string FileName { get; set; }
    public ICollection<ActivitySymbol> Symbols { get; set; }

    public byte[] GetChecksum()
    {
        return (byte[]) _checksum?.Clone();
    }

    // Decode from Base64 string.
    public static WorkflowSymbol Decode(string symbolString)
    {
        var data = Convert.FromBase64String(symbolString);
        using var reader = new BinaryReader(new MemoryStream(data));
        byte[] checksum = null;
        var format = (EncodingFormat) reader.ReadByte();
        var payloadBytesCount = data.Length - sizeof(EncodingFormat);
        if (0 != (format & EncodingFormat.Checksum))
        {
            var bytesCount = SymbolHelper.ReadEncodedInt32(reader);
            checksum = reader.ReadBytes(bytesCount);
            payloadBytesCount -= SymbolHelper.GetEncodedSize(bytesCount);
            format &= ~EncodingFormat.Checksum;
        }

        return format switch
        {
            EncodingFormat.Binary => ParseBinary(reader.ReadBytes(payloadBytesCount), checksum),
            EncodingFormat.String => ParseStringRepresentation(reader.ReadString(), checksum),
            _                     => throw FxTrace.Exception.AsError(new SerializationException())
        };
    }

    // Serialization

    // Encode to Base64 string
    public string Encode() => Encode(DefaultEncodingFormat); // default format

    internal string Encode(EncodingFormat encodingFormat)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        if (_checksum != null)
        {
            writer.Write((byte) (encodingFormat | EncodingFormat.Checksum));
            SymbolHelper.WriteEncodedInt32(writer, _checksum.Length);
            writer.Write(_checksum);
        }
        else
        {
            writer.Write((byte) encodingFormat);
        }

        switch (encodingFormat)
        {
            case EncodingFormat.Binary:
                Write(writer);
                break;
            case EncodingFormat.String:
                writer.Write(ToString());
                break;
            default:
                throw FxTrace.Exception.AsError(new SerializationException());
        }

        // Need to copy to a buffer to trim excess capacity.
        var buffer = new byte[ms.Length];
        Array.Copy(ms.GetBuffer(), buffer, ms.Length);
        return Convert.ToBase64String(buffer);
    }

    // Binary deserializer
    private static WorkflowSymbol ParseBinary(byte[] bytes, byte[] checksum)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes));
        return new WorkflowSymbol(reader, checksum);
    }

    // Binary serializer
    private void Write(BinaryWriter writer)
    {
        writer.Write(FileName ?? string.Empty);
        if (Symbols != null)
        {
            SymbolHelper.WriteEncodedInt32(writer, Symbols.Count);
            foreach (var actSym in Symbols)
            {
                actSym.Write(writer);
            }
        }
        else
        {
            SymbolHelper.WriteEncodedInt32(writer, 0);
        }
    }

    // String encoding serialization.

    // This is used for String encoding format.
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"{FileName ?? string.Empty}");
        if (Symbols == null)
        {
            return builder.ToString();
        }

        foreach (var symbol in Symbols)
        {
            builder.Append($";{symbol}");
        }

        return builder.ToString();
    }

    // Deserialization of string encoding format.
    private static WorkflowSymbol ParseStringRepresentation(string symbolString, byte[] checksum)
    {
        var s = symbolString.Split(';');
        var numSymbols = s.Length - 1;
        var symbols = new ActivitySymbol[numSymbols];
        for (var i = 0; i < numSymbols; ++i)
        {
            var symbolSegments = s[i + 1].Split(',');
            Fx.Assert(symbolSegments.Length == 5, "Invalid activity symbol");
            symbols[i] = new ActivitySymbol
            {
                QualifiedId = QualifiedId.Parse(symbolSegments[0]).AsByteArray(),
                StartLine = int.Parse(symbolSegments[1], CultureInfo.InvariantCulture),
                StartColumn = int.Parse(symbolSegments[2], CultureInfo.InvariantCulture),
                EndLine = int.Parse(symbolSegments[3], CultureInfo.InvariantCulture),
                EndColumn = int.Parse(symbolSegments[4], CultureInfo.InvariantCulture)
            };
        }

        return new WorkflowSymbol
        {
            FileName = s[0],
            Symbols = symbols,
            _checksum = checksum
        };
    }

    public bool CalculateChecksum()
    {
        _checksum = null;
        if (!string.IsNullOrEmpty(FileName))
        {
            _checksum = SymbolHelper.CalculateChecksum(FileName);
        }

        return _checksum != null;
    }

    [Flags]
    internal enum EncodingFormat : byte
    {
        String = 0x76, // Format as well as cookie. String format is hidden from public.
        Binary = 0x77,
        Checksum = 0x80
    }
}

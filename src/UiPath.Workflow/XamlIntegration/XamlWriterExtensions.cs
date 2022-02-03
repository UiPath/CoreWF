// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Xaml;

namespace System.Activities.XamlIntegration;

internal static class XamlWriterExtensions
{
    public static void PropagateLineInfo(XamlWriter targetWriter, IXamlLineInfo lineInfo)
    {
        if (lineInfo != null)
        {
            var consumer = targetWriter as IXamlLineInfoConsumer;
            Fx.Assert(consumer is {ShouldProvideLineInfo: true},
                "Should only call this function to write into a XamlNodeQueue.Writer, which is always IXamlLineInfoConsumer");
            consumer.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
        }
    }

    public static void PropagateLineInfo(XamlWriter targetWriter, int lineNumber, int linePosition)
    {
        var consumer = targetWriter as IXamlLineInfoConsumer;
        Fx.Assert(consumer is {ShouldProvideLineInfo: true},
            "Should only call this function to write into a XamlNodeQueue.Writer, which is always IXamlLineInfoConsumer");
        consumer.SetLineInfo(lineNumber, linePosition);
    }

    // This method is a workaround for TFS bug #788190, since XamlReader.ReadSubtree() should (but doesn't) preserve IXamlLineInfo on the subreader
    public static void Transform(XamlReader reader, XamlWriter writer, IXamlLineInfo readerLineInfo, bool closeWriter)
    {
        var consumer = writer as IXamlLineInfoConsumer;
        Fx.Assert(consumer is {ShouldProvideLineInfo: true},
            "Should only call this function to write into a XamlNodeQueue.Writer, which is always IXamlLineInfoConsumer");
        var shouldPassLineNumberInfo = readerLineInfo != null;

        while (reader.Read())
        {
            if (shouldPassLineNumberInfo)
            {
                consumer.SetLineInfo(readerLineInfo.LineNumber, readerLineInfo.LinePosition);
            }

            writer.WriteNode(reader);
        }

        if (closeWriter)
        {
            writer.Close();
        }
    }

    public static void WriteNode(this XamlWriter writer, XamlReader reader, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteNode(reader);
    }

    public static void WriteEndMember(this XamlWriter writer, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteEndMember();
    }

    public static void WriteEndObject(this XamlWriter writer, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteEndObject();
    }

    public static void WriteGetObject(this XamlWriter writer, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteGetObject();
    }

    public static void WriteNamespace(this XamlWriter writer, NamespaceDeclaration namespaceDeclaration,
        IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteNamespace(namespaceDeclaration);
    }

    public static void WriteStartMember(this XamlWriter writer, XamlMember xamlMember, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteStartMember(xamlMember);
    }

    public static void WriteStartMember(this XamlWriter writer, XamlMember xamlMember, int lineNumber, int linePosition)
    {
        PropagateLineInfo(writer, lineNumber, linePosition);
        writer.WriteStartMember(xamlMember);
    }

    public static void WriteStartObject(this XamlWriter writer, XamlType type, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteStartObject(type);
    }

    public static void WriteValue(this XamlWriter writer, object value, IXamlLineInfo lineInfo)
    {
        PropagateLineInfo(writer, lineInfo);
        writer.WriteValue(value);
    }
}

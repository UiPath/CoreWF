// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace System.Activities.XamlIntegration;

// Exposes a DC-serializable type as IXmlSerializable so it can be serialized to XAML using x:XData
internal class NetDataContractXmlSerializable<T> : IXmlSerializable where T : class
{
    public NetDataContractXmlSerializable(T value = null)
    {
        Value = value;
    }

    public T Value { get; private set; }

    public XmlSchema GetSchema()
    {
        throw FxTrace.Exception.AsError(
            new NotSupportedException(SR.CannotGenerateSchemaForXmlSerializable(typeof(T).Name)));
    }

    public void ReadXml(XmlReader reader)
    {
        var serializer = CreateSerializer();
        Value = (T) serializer.ReadObject(reader);
    }

    public void WriteXml(XmlWriter writer)
    {
        if (Value != null)
        {
            var serializer = CreateSerializer();
            serializer.WriteObject(writer, Value);
        }
    }

    private static DataContractSerializer CreateSerializer()
    {
        var result = new DataContractSerializer(typeof(T));
        return result;
    }
}

using System.IO;
using System.Activities.Runtime.DurableInstancing;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.Collections;
using System.Xml;
using System;
using System.Activities;
using System.Reflection;

namespace WorkflowApplicationTestExtensions.Persistence;


using XInstanceDictionary = IDictionary<XName, InstanceValue>;
using InstanceDictionary = Dictionary<string, InstanceValue>;

public interface IFaultHandler
{
    void OnFault(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom);
    void OnComplete(NativeActivityContext context, ActivityInstance completedInstance);
}

public class DataContractWorkflowSerializer : IWorkflowSerializer
{
    public XInstanceDictionary LoadWorkflowInstance(Stream sourceStream) =>
            WorkflowSerializerHelpers.ToNameDictionary(GetDataContractSerializer().ReadObject(sourceStream));

    public void SaveWorkflowInstance(XInstanceDictionary workflowInstanceState, Stream destinationStream) =>
        GetDataContractSerializer().WriteObject(destinationStream, workflowInstanceState.ToSave());

    protected virtual DataContractSerializer GetDataContractSerializer()
    {
        DataContractSerializerSettings settings = new()
        {
            PreserveObjectReferences = true,
            DataContractResolver = new WorkflowDataContractResolver()
        };
        var dataContractSerializer = new DataContractSerializer(typeof(InstanceDictionary), settings);
        return dataContractSerializer;
    }

    private sealed class WorkflowDataContractResolver : DataContractResolver
    {
        private readonly Dictionary<string, Type> _cachedTypes = new();

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver) =>
            _cachedTypes.TryGetValue(typeName, out var cachedType) ? cachedType : FindType(typeName);

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            typeName = new XmlDictionaryString(XmlDictionary.Empty, type.AssemblyQualifiedName, 0);
            typeNamespace = new XmlDictionaryString(XmlDictionary.Empty, type.Namespace, 0);

            return true;
        }

        private Type FindType(string qualifiedTypeName)
        {
            var type = Type.GetType(qualifiedTypeName, throwOnError: true);
            _cachedTypes[qualifiedTypeName] = type;

            return type;
        }
    }
}

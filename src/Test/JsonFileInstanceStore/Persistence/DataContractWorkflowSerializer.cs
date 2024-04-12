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

namespace JsonFileInstanceStore.Persistence;


using XInstanceDictionary = IDictionary<XName, InstanceValue>;
using InstanceDictionary = Dictionary<string, InstanceValue>;

public interface IFaultHandler
{
    void OnFault(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom);
    void OnComplete(NativeActivityContext context, ActivityInstance completedInstance);
}

public sealed class DataContractWorkflowSerializer : IWorkflowSerializer
{
    public XInstanceDictionary LoadWorkflowInstance(Stream sourceStream) =>
            WorkflowSerializerHelpers.ToNameDictionary(GetDataContractSerializer().ReadObject(sourceStream));

    public void SaveWorkflowInstance(XInstanceDictionary workflowInstanceState, Stream destinationStream) =>
        GetDataContractSerializer().WriteObject(destinationStream, workflowInstanceState.ToSave());

    private static DataContractSerializer GetDataContractSerializer()
    {
        DataContractSerializerSettings settings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = true,
            DataContractResolver = new WorkflowDataContractResolver()
        };
        var dataContractSerializer = new DataContractSerializer(typeof(InstanceDictionary), settings);
        dataContractSerializer.SetSerializationSurrogateProvider(new SerializationSurrogateProvider());
        return dataContractSerializer;
    }

    /// <summary>
    /// WF knows how to serialize Delegates and callbacks, but *only* if they are an Activity method
    /// See https://referencesource.microsoft.com/#System.Activities/System/Activities/Runtime/CallbackWrapper.cs,273
    /// Because of how global exception handling and debug tracking is implemented, we are breaking that assumption
    /// We force a serialization surrogate that knows how to handle our <see cref="ExceptionHandlerImpl.FaultHandler"/> delegates
    /// We're replacing the serialization of ActivityCompletionCallbackWrapper and FaultCallbackWrapper
    /// see https://github.com/Microsoft/referencesource/blob/master/System.Activities/System/Activities/Runtime/CallbackWrapper.cs
    /// </summary>
    [DataContract]
    internal sealed class SurrogateWrapper
    {
        [DataMember]
        public bool IsFaultCallback { get; set; }

        [DataMember]
        public IFaultHandler Handler { get; set; }

        [DataMember]
        public ActivityInstance ActivityInstance { get; set; }
    }

    internal sealed class SerializationSurrogateProvider : ISerializationSurrogateProvider
    {
        private const string FaultWrapperTypeName = "System.Activities.Runtime.FaultCallbackWrapper";
        private const string CompletionWrapperTypeName = "System.Activities.Runtime.ActivityCompletionCallbackWrapper";
        private const string NewtonsoftUnserializableNamespace = "Newtonsoft.Json.Linq";

        private static bool IsWrapperType(Type type) => type.FullName == FaultWrapperTypeName || type.FullName == CompletionWrapperTypeName;
        public Type GetSurrogateType(Type type) => IsWrapperType(type) ? typeof(SurrogateWrapper) : null;

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            var typeToSerialize = obj.GetType();
            System.Diagnostics.Trace.TraceInformation($"TypeToSerialize = {typeToSerialize.FullName}");
            //to be removed after .NET8 upgrade ROBO-2615
            if (typeToSerialize.FullName.Contains(NewtonsoftUnserializableNamespace))
            {
                throw new InvalidDataContractException(string.Format(Resources.NewtonsoftTypesSerializationError, NewtonsoftUnserializableNamespace, typeToSerialize.FullName));
            }

            if (!IsWrapperType(typeToSerialize))
            {
                return obj;
            }

            Delegate callback = GetPrivateField<Delegate>(obj, "_callback");
            if (callback?.Target is not IFaultHandler handler)
            {
                return obj;
            }
            return new SurrogateWrapper
            {
                IsFaultCallback = callback is FaultCallback,
                Handler = handler,
                ActivityInstance = GetPrivateField<ActivityInstance>(obj, "_activityInstance"),
            };
        }

        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = GetFieldInfo(obj.GetType(), fieldName);
            var value = field?.GetValue(obj);
            return value is T t ? t : default;
        }

        private static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj is not SurrogateWrapper surrogate)
            {
                return obj;
            }
            var originalType = typeof(Activity).Assembly.GetType(surrogate.IsFaultCallback
                ? FaultWrapperTypeName
                : CompletionWrapperTypeName);

            return Activator.CreateInstance(originalType, surrogate.IsFaultCallback
                ? (FaultCallback)surrogate.Handler.OnFault
                : (CompletionCallback)surrogate.Handler.OnComplete,
                surrogate.ActivityInstance);
        }
    }

    private sealed class WorkflowDataContractResolver : DataContractResolver
    {
        private readonly Dictionary<string, Type> _cachedTypes = new();

        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver) =>
            _cachedTypes.TryGetValue(typeName, out var cachedType) ? cachedType : FindType(typeName);

        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            if (typeof(IEnumerable) == declaredType) //ROBO-2904
            {
                typeName = null;
                typeNamespace = null;
                return true;
            }

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

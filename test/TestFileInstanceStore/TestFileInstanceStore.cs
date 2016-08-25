// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.DurableInstancing;
using Microsoft.CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace TestFileInstanceStore
{
    public class FileInstanceStore : InstanceStore
    {
        private string _storeDirectoryPath;

        private List<Type> _knownTypes;
        //Type[] knownTypes;

        public FileInstanceStore(string storeDirectoryPath) :
            this(storeDirectoryPath, null)
        {
        }

        public FileInstanceStore(string storeDirectoryPath, IEnumerable<Type> knownTypesForDataContractSerializer)
        {
            _storeDirectoryPath = storeDirectoryPath;
            Directory.CreateDirectory(storeDirectoryPath);

            InitializeKnownTypes(knownTypesForDataContractSerializer);
        }

        public bool KeepInstanceDataAfterCompletion
        {
            get;
            set;
        }

        private void InitializeKnownTypes(IEnumerable<Type> knownTypesForDataContractSerializer)
        {
            _knownTypes = new List<Type>();

            Assembly sysActivitiesAssembly = typeof(Activity).GetTypeInfo().Assembly;
            Type[] typesArray = sysActivitiesAssembly.GetTypes();

            // Remove types that are not decorated with a DataContract attribute
            foreach (Type t in typesArray)
            {
                TypeInfo typeInfo = t.GetTypeInfo();
                if (typeInfo.GetCustomAttribute<DataContractAttribute>() != null)
                {
                    _knownTypes.Add(t);
                }
            }

            if (knownTypesForDataContractSerializer != null)
            {
                foreach (Type knownType in knownTypesForDataContractSerializer)
                {
                    _knownTypes.Add(knownType);
                }
            }
        }

        private void DeleteFiles(Guid instanceId)
        {
            try
            {
                File.Delete(_storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceData");
                File.Delete(_storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceMetadata");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Caught exception trying to delete files for {0}: {1} - {2}", instanceId.ToString(), ex.GetType().ToString(), ex.Message);
            }
        }

        private void GetDataContractSerializers(out DataContractSerializer instanceDataSerializer, out DataContractSerializer instanceMetadataSerializer)
        {
            instanceDataSerializer = null;
            instanceMetadataSerializer = null;

            DataContractSerializerSettings settings = new DataContractSerializerSettings
            {
                PreserveObjectReferences = true,
                KnownTypes = _knownTypes
            };
            instanceDataSerializer = new DataContractSerializer(typeof(Dictionary<string, InstanceValue>), settings);
            instanceMetadataSerializer = new DataContractSerializer(typeof(Dictionary<string, InstanceValue>), settings);
        }

        private void GetFileStreams(Guid instanceId, out FileStream instanceDataStream, out FileStream instanceMetadataStream, FileMode fileMode)
        {
            string instanceDataPath = _storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceData";
            string instanceMetadataPath = _storeDirectoryPath + "\\" + instanceId.ToString() + "-InstanceMetadata";

            instanceDataStream = new FileStream(instanceDataPath, fileMode);
            instanceMetadataStream = new FileStream(instanceMetadataPath, fileMode);
        }
        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                if (command is SaveWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(SaveWorkflow(context, (SaveWorkflowCommand)command), callback, state);
                }
                else if (command is LoadWorkflowCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(LoadWorkflow(context, (LoadWorkflowCommand)command), callback, state);
                }
                else if (command is CreateWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(CreateWorkflowOwner(context, (CreateWorkflowOwnerCommand)command), callback, state);
                }
                else if (command is DeleteWorkflowOwnerCommand)
                {
                    return new TypedCompletedAsyncResult<bool>(DeleteWorkflowOwner(context, (DeleteWorkflowOwnerCommand)command), callback, state);
                }
                return new TypedCompletedAsyncResult<bool>(false, callback, state);
            }
            catch (Exception e)
            {
                return new TypedCompletedAsyncResult<Exception>(e, callback, state);
            }
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            TypedCompletedAsyncResult<Exception> exceptionResult = result as TypedCompletedAsyncResult<Exception>;
            if (exceptionResult != null)
            {
                throw exceptionResult.Data;
            }
            return TypedCompletedAsyncResult<bool>.End(result);
        }

        private bool SaveWorkflow(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            if (command.CompleteInstance)
            {
                context.CompletedInstance();
                if (!KeepInstanceDataAfterCompletion)
                {
                    DeleteFiles(context.InstanceView.InstanceId);
                }
            }
            else
            {
                Dictionary<string, InstanceValue> instanceData = SerializeablePropertyBagConvertXNameInstanceValue(command.InstanceData);
                Dictionary<string, InstanceValue> instanceMetadata = SerializeInstanceMetadataConvertXNameInstanceValue(context, command);

                FileStream instanceDataStream;
                FileStream instanceMetadataStream;
                GetFileStreams(context.InstanceView.InstanceId, out instanceDataStream, out instanceMetadataStream, FileMode.OpenOrCreate);

                DataContractSerializer instanceDataSerializer;
                DataContractSerializer instanceMetadataSerializer;
                GetDataContractSerializers(out instanceDataSerializer, out instanceMetadataSerializer);

                try
                {
                    instanceDataSerializer.WriteObject(instanceDataStream, instanceData);
                    instanceMetadataSerializer.WriteObject(instanceMetadataStream, instanceMetadata);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    instanceDataStream.Flush();
                    instanceDataStream.Dispose();
                    instanceMetadataStream.Flush();
                    instanceMetadataStream.Dispose();
                }

                foreach (KeyValuePair<XName, InstanceValue> property in command.InstanceMetadataChanges)
                {
                    context.WroteInstanceMetadataValue(property.Key, property.Value);
                }

                context.PersistedInstance(command.InstanceData);
                if (command.CompleteInstance)
                {
                    context.CompletedInstance();
                }

                if (command.UnlockInstance || command.CompleteInstance)
                {
                    context.InstanceHandle.Free();
                }
            }

            return true;
        }

        private bool LoadWorkflow(InstancePersistenceContext context, LoadWorkflowCommand command)
        {
            if (command.AcceptUninitializedInstance)
            {
                return false;
            }

            if (context.InstanceVersion == -1)
            {
                context.BindAcquiredLock(0);
            }

            IDictionary<XName, InstanceValue> instanceData = null;
            IDictionary<XName, InstanceValue> instanceMetadata = null;

            FileStream instanceDataStream;
            FileStream instanceMetadataStream;
            GetFileStreams(context.InstanceView.InstanceId, out instanceDataStream, out instanceMetadataStream, FileMode.Open);

            DataContractSerializer instanceDataSerializer;
            DataContractSerializer instanceMetadataSerializer;
            GetDataContractSerializers(out instanceDataSerializer, out instanceMetadataSerializer);

            Dictionary<string, InstanceValue> serializableInstanceData;
            Dictionary<string, InstanceValue> serializableInstanceMetadata;

            try
            {
                serializableInstanceData = (Dictionary<string, InstanceValue>)instanceDataSerializer.ReadObject(instanceDataStream);
                serializableInstanceMetadata = (Dictionary<string, InstanceValue>)instanceMetadataSerializer.ReadObject(instanceMetadataStream);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                instanceDataStream.Dispose();
                instanceMetadataStream.Dispose();
            }

            instanceData = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceData);
            instanceMetadata = this.DeserializePropertyBagConvertXNameInstanceValue(serializableInstanceMetadata);

            context.LoadedInstance(InstanceState.Initialized, instanceData, instanceMetadata, null, null);

            return true;
        }

        private bool CreateWorkflowOwner(InstancePersistenceContext context, CreateWorkflowOwnerCommand command)
        {
            Guid instanceOwnerId = Guid.NewGuid();
            context.BindInstanceOwner(instanceOwnerId, instanceOwnerId);
            context.BindEvent(HasRunnableWorkflowEvent.Value);
            return true;
        }

        private bool DeleteWorkflowOwner(InstancePersistenceContext context, DeleteWorkflowOwnerCommand command)
        {
            return true;
        }

        private Dictionary<string, InstanceValue> SerializeablePropertyBagConvertXNameInstanceValue(IDictionary<XName, InstanceValue> source)
        {
            Dictionary<string, InstanceValue> scratch = new Dictionary<string, InstanceValue>();
            foreach (KeyValuePair<XName, InstanceValue> property in source)
            {
                bool writeOnly = (property.Value.Options & InstanceValueOptions.WriteOnly) != 0;

                if (!writeOnly && !property.Value.IsDeletedValue)
                {
                    scratch.Add(property.Key.ToString(), property.Value);
                }
            }

            return scratch;
        }

        private Dictionary<string, InstanceValue> SerializeInstanceMetadataConvertXNameInstanceValue(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            Dictionary<string, InstanceValue> metadata = null;

            foreach (var property in command.InstanceMetadataChanges)
            {
                if (!property.Value.Options.HasFlag(InstanceValueOptions.WriteOnly))
                {
                    if (metadata == null)
                    {
                        metadata = new Dictionary<string, InstanceValue>();
                        // copy current metadata. note that we must get rid of InstanceValue as it is not properly serializeable
                        foreach (var m in context.InstanceView.InstanceMetadata)
                        {
                            metadata.Add(m.Key.ToString(), m.Value);
                        }
                    }

                    if (metadata.ContainsKey(property.Key.ToString()))
                    {
                        if (property.Value.IsDeletedValue) metadata.Remove(property.Key.ToString());
                        else metadata[property.Key.ToString()] = property.Value;
                    }
                    else
                    {
                        if (!property.Value.IsDeletedValue) metadata.Add(property.Key.ToString(), property.Value);
                    }
                }
            }

            if (metadata == null)
                metadata = new Dictionary<string, InstanceValue>();

            return metadata;
        }

        private IDictionary<XName, InstanceValue> DeserializePropertyBagConvertXNameInstanceValue(Dictionary<string, InstanceValue> source)
        {
            Dictionary<XName, InstanceValue> destination = new Dictionary<XName, InstanceValue>();

            foreach (KeyValuePair<string, InstanceValue> property in source)
            {
                destination.Add(property.Key, property.Value);
            }

            return destination;
        }
    }
}

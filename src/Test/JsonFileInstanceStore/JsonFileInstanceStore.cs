// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.DurableInstancing;
using System.Activities.Runtime.DurableInstancing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace JsonFileInstanceStore
{
    public class FileInstanceStore : InstanceStore
    {
        private readonly string _storeDirectoryPath;

        public static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

        public FileInstanceStore(string storeDirectoryPath)
        {
            _storeDirectoryPath = storeDirectoryPath;
            Directory.CreateDirectory(storeDirectoryPath);
        }

        public bool KeepInstanceDataAfterCompletion
        {
            get;
            set;
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

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
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

        protected override bool EndTryCommand(IAsyncResult result) => TypedCompletedAsyncResult<bool>.End(result);

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

                var serializedInstanceData = JsonConvert.SerializeObject(instanceData, Formatting.Indented, JsonSerializerSettings);
                File.WriteAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceData", serializedInstanceData);

                var serializedInstanceMetadata = JsonConvert.SerializeObject(instanceMetadata, Formatting.Indented, JsonSerializerSettings);
                File.WriteAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceMetadata", serializedInstanceMetadata);

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

            Dictionary<string, InstanceValue> serializableInstanceData;
            Dictionary<string, InstanceValue> serializableInstanceMetadata;

            try
            {
                var serializedInstanceData = File.ReadAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceData");
                serializableInstanceData = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceData, JsonSerializerSettings);

                var serializedInstanceMetadata = File.ReadAllText(_storeDirectoryPath + "\\" + context.InstanceView.InstanceId + "-InstanceMetadata");
                serializableInstanceMetadata = JsonConvert.DeserializeObject<Dictionary<string, InstanceValue>>(serializedInstanceMetadata, JsonSerializerSettings);
            }
            catch (Exception)
            {
                throw;
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

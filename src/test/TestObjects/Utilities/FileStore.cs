// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.DurableInstancing;
using CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Test.Common.TestObjects.Utilities
{
    public class FileStore : InstanceStore
    {
        private readonly static object s_thisLock = new object();

        public static string FileStorePath
        {
            get { return PersistenceItemManager.StorePath; }
            set { PersistenceItemManager.StorePath = value; }
        }

        public FileStore()
        {
        }

        public FileStore(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            PersistenceItemManager.StorePath = path;
        }

        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            SaveWorkflowCommand save = command as SaveWorkflowCommand;
            if (save != null)
            {
                lock (FileStore.s_thisLock)
                {
                    ProcessSaveCommand(context, save);
                }
            }

            LoadWorkflowCommand load = command as LoadWorkflowCommand;
            if (load != null)
            {
                lock (FileStore.s_thisLock)
                {
                    ProcessLoadCommand(context, load);
                }
            }

            LoadWorkflowByInstanceKeyCommand loadByKey = command as LoadWorkflowByInstanceKeyCommand;
            if (loadByKey != null)
            {
                lock (FileStore.s_thisLock)
                {
                    ProcessLoadByKeyCommand(context, loadByKey);
                }
            }

            if (save != null || load != null || loadByKey != null)
            {
                return new CompletedAsyncResult(callback, state);
            }

            if (command is CreateWorkflowOwnerCommand createOwner)
            {
                Guid ownerId = Guid.NewGuid();
                Owner owner = new Owner();
                lock (s_thisLock)
                {
                    owner.Id = ownerId;
                    owner.LockToken = Guid.NewGuid();
                    owner.Metadata = new PropertyBag(createOwner.InstanceOwnerMetadata);
                    PersistenceItemManager.SaveToFile<Owner>(owner);
                }

                context.BindInstanceOwner(ownerId, owner.LockToken);
                context.BindEvent(HasRunnableWorkflowEvent.Value);
                return new CompletedAsyncResult(callback, state);
            }

            if (command is DeleteWorkflowOwnerCommand deleteOwner)
            {
                Guid ownerId = context.InstanceView.InstanceOwner.InstanceOwnerId;

                lock (FileStore.s_thisLock)
                {
                    Owner owner = PersistenceItemManager.Load<Owner>(ownerId);
                    if (owner != null && owner.LockToken == context.LockToken)
                    {
                        PersistenceItemManager.Remove<Owner>(ownerId);
                    }
                }

                context.InstanceHandle.Free();
                return new CompletedAsyncResult(callback, state);
            }

            return base.BeginTryCommand(context, command, timeout, callback, state);
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            if (result is CompletedAsyncResult)
            {
                CompletedAsyncResult.End(result);
                return true;
            }
            else
            {
                return base.EndTryCommand(result);
            }
        }

        private long ProcessLoadByKeyCommand(InstancePersistenceContext context, LoadWorkflowByInstanceKeyCommand command)
        {
            Owner owner = CheckOwner(context, command.Name);

            Key key = PersistenceItemManager.Load<Key>(command.LookupInstanceKey);
            Instance instance;
            if (key == null)
            {
                if (context.InstanceView.IsBoundToLock && context.InstanceView.InstanceId != command.AssociateInstanceKeyToInstanceId)
                {
                    // This happens in the bind reclaimed lock case.
                    context.InstanceHandle.Free();
                    throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                }
                if (command.AssociateInstanceKeyToInstanceId == Guid.Empty)
                {
                    throw new InstanceKeyNotReadyException(command.Name, new InstanceKey(command.LookupInstanceKey));
                }

                key = new Key()
                {
                    Id = command.LookupInstanceKey,
                    TargetInstanceId = command.AssociateInstanceKeyToInstanceId,
                    Metadata = new PropertyBag()
                };

                instance = PersistenceItemManager.Load<Instance>(command.AssociateInstanceKeyToInstanceId);
                if (instance == null)
                {
                    // Checking instance.Owner is like an InstanceLockQueryResult.
                    context.QueriedInstanceStore(new InstanceLockQueryResult(command.AssociateInstanceKeyToInstanceId, Guid.Empty));

                    if (context.InstanceView.IsBoundToLock)
                    {
                        // This happens in the bind reclaimed lock case.
                        context.InstanceHandle.Free();
                        throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                    }

                    context.BindInstance(command.AssociateInstanceKeyToInstanceId);

                    if (command.AcceptUninitializedInstance)
                    {
                        instance = new Instance()
                        {
                            Id = command.AssociateInstanceKeyToInstanceId,
                            Version = 1,
                            Metadata = new PropertyBag(),
                            Owner = context.InstanceView.InstanceOwner.InstanceOwnerId
                        };
                        PersistenceItemManager.SaveToFile<Instance>(instance);
                    }
                    else
                    {
                        throw new Exception("Could not create new Instance");
                    }

                    context.BindAcquiredLock(1);
                }
                else
                {
                    // Checking instance.Owner is like an InstanceLockQueryResult.
                    context.QueriedInstanceStore(new InstanceLockQueryResult(command.AssociateInstanceKeyToInstanceId, instance.Owner));

                    if (context.InstanceView.IsBoundToLock)
                    {
                        if (instance.Version != context.InstanceVersion || instance.Owner != context.InstanceView.InstanceOwner.InstanceOwnerId)
                        {
                            if (context.InstanceVersion > instance.Version)
                            {
                                throw new InvalidProgramException("This is a bug, the context should never be bound higher than the lock.");
                            }
                            context.InstanceHandle.Free();
                            throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                        }
                    }

                    if (instance.Data != null)
                    {
                        // LoadByInstanceKeyCommand only allows auto-association to an uninitialized instance.
                        throw new InstanceCollisionException(command.Name, command.AssociateInstanceKeyToInstanceId);
                    }

                    if (!context.InstanceView.IsBoundToLock)
                    {
                        if (instance.Owner == Guid.Empty)
                        {
                            context.BindInstance(command.AssociateInstanceKeyToInstanceId);
                            instance.Version++;
                            instance.Owner = context.InstanceView.InstanceOwner.InstanceOwnerId;
                            PersistenceItemManager.SaveToFile<Instance>(instance);
                            context.BindAcquiredLock(instance.Version);
                        }
                        else if (instance.Owner == context.InstanceView.InstanceOwner.InstanceOwnerId)
                        {
                            // This is a pretty weird case - maybe it's a retry?
                            context.BindInstance(command.AssociateInstanceKeyToInstanceId);
                            return instance.Version;
                        }
                        else
                        {
                            throw new InstanceLockedException(command.Name, instance.Owner);
                        }
                    }
                }

                if (command.InstanceKeysToAssociate.TryGetValue(command.LookupInstanceKey, out IDictionary<XName, InstanceValue> lookupKeyMetadata))
                {
                    key.Metadata = new PropertyBag(lookupKeyMetadata);
                }
                else
                {
                    key.Metadata = new PropertyBag();
                }
                key.Id = command.LookupInstanceKey;
                key.TargetInstanceId = command.AssociateInstanceKeyToInstanceId;
                PersistenceItemManager.SaveToFile<Key>(key);
                context.AssociatedInstanceKey(command.LookupInstanceKey);
                if (lookupKeyMetadata != null)
                {
                    foreach (KeyValuePair<XName, InstanceValue> property in lookupKeyMetadata)
                    {
                        context.WroteInstanceKeyMetadataValue(command.LookupInstanceKey, property.Key, property.Value);
                    }
                }
            }
            else
            {
                if (context.InstanceView.IsBoundToLock && (key.State == InstanceKeyState.Completed || key.TargetInstanceId != context.InstanceView.InstanceId))
                {
                    // This happens in the bind reclaimed lock case.
                    context.InstanceHandle.Free();
                    throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                }
                if (key.State == InstanceKeyState.Completed)
                {
                    throw new InstanceKeyCompleteException(command.Name, new InstanceKey(command.LookupInstanceKey));
                }

                instance = PersistenceItemManager.Load<Instance>(key.TargetInstanceId);

                // Checking instance.Owner is like an InstanceLockQueryResult.
                context.QueriedInstanceStore(new InstanceLockQueryResult(key.TargetInstanceId, instance.Owner));

                if (context.InstanceView.IsBoundToLock)
                {
                    if (instance.Version != context.InstanceVersion || instance.Owner != context.InstanceView.InstanceOwner.InstanceOwnerId)
                    {
                        if (context.InstanceVersion > instance.Version)
                        {
                            throw new InvalidProgramException("This is a bug, the context should never be bound higher than the lock.");
                        }
                        context.InstanceHandle.Free();
                        throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                    }
                }

                if (instance.Data == null && !command.AcceptUninitializedInstance)
                {
                    throw new InstanceNotReadyException(command.Name, key.TargetInstanceId);
                }

                if (!context.InstanceView.IsBoundToLock)
                {
                    if (instance.Owner == Guid.Empty)
                    {
                        context.BindInstance(key.TargetInstanceId);
                        instance.Version++;
                        instance.Owner = context.InstanceView.InstanceOwner.InstanceOwnerId;
                        PersistenceItemManager.SaveToFile<Instance>(instance);
                        context.BindAcquiredLock(instance.Version);
                    }
                    else if (instance.Owner == context.InstanceView.InstanceOwner.InstanceOwnerId)
                    {
                        // This is the very interesting parallel-convoy conflicting handle race resolution case.  Two handles
                        // can get bound to the same lock, which is necessary to allow parallel convoy to succeed without preventing
                        // zombied locked instances from being reclaimed.
                        context.BindInstance(key.TargetInstanceId);
                        return instance.Version;
                    }
                    else
                    {
                        throw new InstanceLockedException(command.Name, instance.Owner);
                    }
                }
            }

            Key newKey;
            Exception exception = null;
            foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> keyEntry in command.InstanceKeysToAssociate)
            {
                newKey = PersistenceItemManager.Load<Key>(keyEntry.Key);
                if (newKey == null)
                {
                    newKey = new Key()
                    {
                        Id = keyEntry.Key,
                        TargetInstanceId = key.TargetInstanceId,
                        Metadata = new PropertyBag(keyEntry.Value)
                    };
                    PersistenceItemManager.AddKeyToInstance(key.TargetInstanceId, newKey);

                    context.AssociatedInstanceKey(keyEntry.Key);
                    if (keyEntry.Value != null)
                    {
                        foreach (KeyValuePair<XName, InstanceValue> property in keyEntry.Value)
                        {
                            context.WroteInstanceKeyMetadataValue(keyEntry.Key, property.Key, property.Value);
                        }
                    }
                }
                else
                {
                    if (newKey.TargetInstanceId != key.TargetInstanceId && exception == null)
                    {
                        exception = new InstanceKeyCollisionException(command.Name, key.TargetInstanceId, new InstanceKey(keyEntry.Key), newKey.TargetInstanceId);
                    }
                }
            }
            if (exception != null)
            {
                throw exception;
            }

            Dictionary<Guid, IDictionary<XName, InstanceValue>> associatedKeys = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
            Dictionary<Guid, IDictionary<XName, InstanceValue>> completedKeys = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();

            foreach (Guid keyId in instance.RelatedKeys)
            {
                key = PersistenceItemManager.Load<Key>(keyId);

                if (key.TargetInstanceId == context.InstanceView.InstanceId)
                {
                    if (key.State == InstanceKeyState.Completed)
                    {
                        completedKeys.Add(key.Id, ExcludeWriteOnlyPropertyBagItems(key.Metadata));
                    }
                    else
                    {
                        associatedKeys.Add(key.Id, ExcludeWriteOnlyPropertyBagItems(key.Metadata));
                    }
                }
            }

            instance.State = instance.Data == null ? InstanceState.Uninitialized : InstanceState.Initialized;
            PersistenceItemManager.SaveToFile<Instance>(instance);
            context.LoadedInstance(instance.State, ExcludeWriteOnlyPropertyBagItems(instance.Data),
                ExcludeWriteOnlyPropertyBagItems(instance.Metadata), associatedKeys, completedKeys);

            return 0;
        }

        private long ProcessLoadCommand(InstancePersistenceContext context, LoadWorkflowCommand command)
        {
            Owner owner = CheckOwner(context, command.Name);

            Instance instance = PersistenceItemManager.Load<Instance>(context.InstanceView.InstanceId);
            if (instance == null)
            {
                // Checking instance.Owner is like an InstanceLockQueryResult.
                context.QueriedInstanceStore(new InstanceLockQueryResult(context.InstanceView.InstanceId, Guid.Empty));

                if (context.InstanceView.IsBoundToLock)
                {
                    context.InstanceHandle.Free();
                    throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                }
                if (!command.AcceptUninitializedInstance)
                {
                    throw new InstanceNotReadyException(command.Name, context.InstanceView.InstanceId);
                }
                instance = new Instance()
                {
                    Version = 1,
                    Id = context.InstanceView.InstanceId,
                    Owner = context.InstanceView.InstanceOwner.InstanceOwnerId,
                    Metadata = new PropertyBag()
                };
                PersistenceItemManager.SaveToFile<Instance>(instance);
                context.BindAcquiredLock(1);
            }
            else
            {
                // Checking instance.Owner is like an InstanceLockQueryResult.
                context.QueriedInstanceStore(new InstanceLockQueryResult(context.InstanceView.InstanceId, instance.Owner));

                if (context.InstanceView.IsBoundToLock)
                {
                    if (instance.Version != context.InstanceVersion || instance.Owner != context.InstanceView.InstanceOwner.InstanceOwnerId)
                    {
                        if (context.InstanceVersion > instance.Version)
                        {
                            throw new InvalidProgramException("This is a bug, the context should never be bound higher than the lock.");
                        }
                        context.InstanceHandle.Free();
                        throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                    }
                }

                if (instance.State == InstanceState.Completed)
                {
                    throw new InstanceCompleteException(command.Name, context.InstanceView.InstanceId);
                }
                if ((instance.Data == null || instance.Data.Count < 1) && !command.AcceptUninitializedInstance)
                {
                    throw new InstanceNotReadyException(command.Name, context.InstanceView.InstanceId);
                }

                if (!context.InstanceView.IsBoundToLock)
                {
                    if (instance.Owner == Guid.Empty)
                    {
                        instance.Version++;
                        instance.Owner = context.InstanceView.InstanceOwner.InstanceOwnerId;
                        PersistenceItemManager.SaveToFile<Instance>(instance);
                        context.BindAcquiredLock(instance.Version);
                    }
                    else if (instance.Owner == context.InstanceView.InstanceOwner.InstanceOwnerId)
                    {
                        // This is the very interesting parallel-convoy conflicting handle race resolution case.  Two handles
                        // can get bound to the same lock, which is necessary to allow parallel convoy to succeed without preventing
                        // zombied locked instances from being reclaimed.
                        return instance.Version;
                    }
                    else
                    {
                        throw new InstanceLockedException(command.Name, instance.Owner);
                    }
                }
            }

            Dictionary<Guid, IDictionary<XName, InstanceValue>> associatedKeys = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();
            Dictionary<Guid, IDictionary<XName, InstanceValue>> completedKeys = new Dictionary<Guid, IDictionary<XName, InstanceValue>>();

            foreach (Guid keyId in instance.RelatedKeys)
            {
                Key key = PersistenceItemManager.Load<Key>(keyId);

                if (key.TargetInstanceId == context.InstanceView.InstanceId)
                {
                    if (key.State == InstanceKeyState.Completed)
                    {
                        completedKeys.Add(key.Id, ExcludeWriteOnlyPropertyBagItems(key.Metadata));
                    }
                    else
                    {
                        associatedKeys.Add(key.Id, ExcludeWriteOnlyPropertyBagItems(key.Metadata));
                    }
                }
            }

            instance.State = instance.Data == null ? InstanceState.Uninitialized : InstanceState.Initialized;

            PersistenceItemManager.SaveToFile<Instance>(instance);
            context.LoadedInstance(instance.State, ExcludeWriteOnlyPropertyBagItems(instance.Data),
                ExcludeWriteOnlyPropertyBagItems(instance.Metadata), associatedKeys, completedKeys);

            return 0;
        }

        private long ProcessSaveCommand(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            Owner owner = CheckOwner(context, command.Name);

            Instance instance = PersistenceItemManager.Load<Instance>(context.InstanceView.InstanceId);
            if (instance == null)
            {
                // Checking instance.Owner is like an InstanceLockQueryResult.
                context.QueriedInstanceStore(new InstanceLockQueryResult(context.InstanceView.InstanceId, Guid.Empty));

                if (context.InstanceView.IsBoundToLock)
                {
                    context.InstanceHandle.Free();
                    throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                }

                instance = new Instance()
                {
                    Version = 1,
                    Id = context.InstanceView.InstanceId,
                    Owner = context.InstanceView.InstanceOwner.InstanceOwnerId,
                    Metadata = new PropertyBag()
                };

                PersistenceItemManager.SaveToFile<Instance>(instance);
                context.BindAcquiredLock(1);
            }
            else
            {
                // Checking instance.Owner is like an InstanceLockQueryResult.
                context.QueriedInstanceStore(new InstanceLockQueryResult(context.InstanceView.InstanceId, instance.Owner));

                if (instance.State == InstanceState.Completed)
                {
                    throw new InstanceCompleteException(command.Name, context.InstanceView.InstanceId);
                }

                if (instance.Owner == Guid.Empty)
                {
                    if (context.InstanceView.IsBoundToLock)
                    {
                        context.InstanceHandle.Free();
                        throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                    }

                    instance.Version++;
                    instance.Owner = context.InstanceView.InstanceOwner.InstanceOwnerId;
                    PersistenceItemManager.SaveToFile<Instance>(instance);
                    context.BindAcquiredLock(instance.Version);
                }
                else
                {
                    if (instance.Owner != context.InstanceView.InstanceOwner.InstanceOwnerId)
                    {
                        if (context.InstanceView.IsBoundToLock)
                        {
                            context.InstanceHandle.Free();
                            throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                        }

                        throw new InstanceLockedException(command.Name, instance.Owner);
                    }
                    if (context.InstanceView.IsBoundToLock)
                    {
                        if (context.InstanceVersion != instance.Version)
                        {
                            if (context.InstanceVersion > instance.Version)
                            {
                                throw new InvalidProgramException("This is a bug, the context should never be bound higher than the lock.");
                            }
                            context.InstanceHandle.Free();
                            throw new InstanceLockLostException(command.Name, context.InstanceView.InstanceId);
                        }
                    }
                    else
                    {
                        // This is the very interesting parallel-convoy conflicting handle race resolution case.  Two handles
                        // can get bound to the same lock, which is necessary to allow parallel convoy to succeed without preventing
                        // zombied locked instances from being reclaimed.
                        return instance.Version;
                    }
                }
            }

            foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> keyEntry in command.InstanceKeysToAssociate)
            {
                Key key = PersistenceItemManager.Load<Key>(keyEntry.Key);
                if (key != null)
                {
                    if (key.TargetInstanceId != Guid.Empty && key.TargetInstanceId != context.InstanceView.InstanceId)
                    {
                        throw new InstanceKeyCollisionException(command.Name, context.InstanceView.InstanceId, new InstanceKey(keyEntry.Key), key.TargetInstanceId);
                    }
                    // The SaveWorkflowCommand treats this as a no-op, whether completed or not.
                }
                else
                {
                    key = new Key()
                    {
                        Id = keyEntry.Key,
                        State = InstanceKeyState.Associated,
                        TargetInstanceId = context.InstanceView.InstanceId,
                        Metadata = new PropertyBag(keyEntry.Value)
                    };
                    PersistenceItemManager.SaveToFile<Key>(key);
                    context.AssociatedInstanceKey(keyEntry.Key);
                    if (keyEntry.Value != null)
                    {
                        foreach (KeyValuePair<XName, InstanceValue> property in keyEntry.Value)
                        {
                            context.WroteInstanceKeyMetadataValue(keyEntry.Key, property.Key, property.Value);
                        }
                    }
                }
            }

            foreach (Guid keyGuid in command.InstanceKeysToComplete)
            {
                Key key = PersistenceItemManager.Load<Key>(keyGuid);
                if (key != null && key.TargetInstanceId == context.InstanceView.InstanceId)
                {
                    if (key.State == InstanceKeyState.Associated) //if (key.State != InstanceKeyState.Completed)
                    {
                        key.State = InstanceKeyState.Completed;
                        PersistenceItemManager.SaveToFile<Key>(key);
                        context.CompletedInstanceKey(keyGuid);
                    }
                }
                else
                {
                    // The SaveWorkflowCommand does not allow this.  (Should it validate against it?)
                    throw new InvalidOperationException("Attempting to complete a key which is not associated.");
                }
            }

            foreach (Guid keyGuid in command.InstanceKeysToFree)
            {
                Key key = PersistenceItemManager.Load<Key>(keyGuid);
                if (key != null && key.TargetInstanceId == context.InstanceView.InstanceId)
                {
                    if (key.State != InstanceKeyState.Completed)
                    {
                        context.CompletedInstanceKey(keyGuid);
                    }
                    key.State = InstanceKeyState.Unknown;
                    key.TargetInstanceId = Guid.Empty;
                    key.Metadata = null;
                    PersistenceItemManager.SaveToFile<Key>(key);
                    context.UnassociatedInstanceKey(keyGuid);
                }
                else
                {
                    // The SaveWorkflowCommand does not allow this.  (Should it validate against it?)
                    throw new InvalidOperationException("Attempting to complete a key which is not associated.");
                }
            }

            foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> keyEntry in command.InstanceKeyMetadataChanges)
            {
                Key key = PersistenceItemManager.Load<Key>(keyEntry.Key);
                if (key != null && key.TargetInstanceId == context.InstanceView.InstanceId && key.State == InstanceKeyState.Associated)
                {
                    if (keyEntry.Value != null)
                    {
                        foreach (KeyValuePair<XName, InstanceValue> property in keyEntry.Value)
                        {
                            if (property.Value.IsDeletedValue)
                            {
                                key.Metadata.Remove(property.Key);
                            }
                            else
                            {
                                key.Metadata[property.Key] = new InstanceValue(property.Value);
                            }
                            context.WroteInstanceKeyMetadataValue(keyEntry.Key, property.Key, property.Value);
                        }
                        PersistenceItemManager.SaveToFile<Key>(key);
                    }
                }
                else
                {
                    // The SaveWorkflowCommand does not allow this.  (Should it validate against it?)
                    throw new InvalidOperationException("Attempting to complete a key which is not associated.");
                }
            }

            foreach (KeyValuePair<XName, InstanceValue> property in command.InstanceMetadataChanges)
            {
                if (property.Value.IsDeletedValue)
                {
                    instance.Metadata.Remove(property.Key);
                }
                else
                {
                    instance.Metadata[property.Key] = new InstanceValue(property.Value);
                }

                context.WroteInstanceMetadataValue(property.Key, property.Value);
            }

            if (command.InstanceData.Count > 0)
            {
                instance.Data = new PropertyBag(command.InstanceData);
                context.PersistedInstance(command.InstanceData);
            }

            PersistenceItemManager.SaveToFile<Instance>(instance);

            // The command does the implicit advancement of everything into safe completed states.
            if (command.CompleteInstance)
            {
                if (instance.Data == null)
                {
                    instance.Data = new PropertyBag();
                    PersistenceItemManager.SaveToFile<Instance>(instance);
                    context.PersistedInstance(new Dictionary<XName, InstanceValue>());
                }

                Queue<Guid> keysToComplete = new Queue<Guid>();

                foreach (KeyValuePair<Guid, InstanceKeyView> keyEntry in context.InstanceView.InstanceKeys)
                {
                    if (keyEntry.Value.InstanceKeyState == InstanceKeyState.Associated)
                    {
                        keysToComplete.Enqueue(keyEntry.Key);
                    }
                }

                foreach (Guid keyToComplete in keysToComplete)
                {
                    Key key = PersistenceItemManager.Load<Key>(keyToComplete);
                    key.State = InstanceKeyState.Completed;
                    PersistenceItemManager.SaveToFile<Key>(key);
                    context.CompletedInstanceKey(keyToComplete);
                }

                instance.State = InstanceState.Completed;
                instance.Owner = Guid.Empty;

                PersistenceItemManager.SaveToFile<Instance>(instance);
                context.CompletedInstance();
                context.InstanceHandle.Free();
            }
            if (command.UnlockInstance)
            {
                instance.Owner = Guid.Empty;
                PersistenceItemManager.SaveToFile<Instance>(instance);
                context.InstanceHandle.Free();
            }

            return 0;
        }

        private static Owner CheckOwner(InstancePersistenceContext context, XName name)
        {
            Owner owner = PersistenceItemManager.Load<Owner>(context.InstanceView.InstanceOwner.InstanceOwnerId);

            if (owner == null || owner.LockToken != context.LockToken)
            {
                context.InstanceHandle.Free();
                throw new InstanceOwnerException(name, context.InstanceView.InstanceOwner.InstanceOwnerId);
            }

            return owner;
        }

        private static Dictionary<XName, InstanceValue> ExcludeWriteOnlyPropertyBagItems(PropertyBag bag)
        {
            Dictionary<XName, InstanceValue> dict = new Dictionary<XName, InstanceValue>();
            if (bag != null)
            {
                foreach (KeyValuePair<XName, InstanceValue> p in bag)
                {
                    if ((p.Value.Options & InstanceValueOptions.WriteOnly) != InstanceValueOptions.WriteOnly)
                    {
                        dict.Add(p.Key, p.Value);
                    }
                }
            }
            return dict;
        }

        #region PropertyBag
        public class PropertyBag : Dictionary<XName, InstanceValue>, IXmlSerializable
        {
            public PropertyBag()
                : base()
            {
            }

            public PropertyBag(IDictionary<XName, InstanceValue> value)
                : this()
            {
                foreach (KeyValuePair<XName, InstanceValue> pair in value)
                {
                    this.Add(pair.Key, pair.Value);
                }
            }

            public Dictionary<XName, InstanceValue> ToDictionary()
            {
                return new Dictionary<XName, InstanceValue>(this);
            }

            public System.Xml.Schema.XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(XmlReader reader)
            {
                if (reader.IsEmptyElement)
                {
                    return;
                }
                //string xmlString = reader.ReadString();
                //NetDataContractSerializer serializer = new NetDataContractSerializer();
                //StringReader stringReader = new StringReader(xmlString);
                //XmlReader xmlReader = XmlReader.Create(stringReader);
                //Dictionary<XName, InstanceValueWrapper> wrapperObject = (Dictionary<XName, InstanceValueWrapper>)serializer.ReadObject(xmlReader);
                //foreach (KeyValuePair<XName, InstanceValueWrapper> entry in wrapperObject)
                //{
                //    InstanceValue value = entry.Value.CreateInstanceValue();
                //    this.Add(entry.Key, value);
                //}
                //reader.ReadEndElement();
                //xmlReader.Close();
            }

            public void WriteXml(XmlWriter writer)
            {
                if (this.Count > 0)
                {
                    Dictionary<XName, InstanceValueWrapper> wrapperDic = new Dictionary<XName, InstanceValueWrapper>();
                    foreach (KeyValuePair<XName, InstanceValue> property in this)
                    {
                        wrapperDic.Add(property.Key, new InstanceValueWrapper(property.Value));
                    }

                    string xmlData = String.Empty;
                    //NetDataContractSerializer serializer = new NetDataContractSerializer();
                    //using (MemoryStream stream = new MemoryStream())
                    //{
                    //    serializer.WriteObject(stream, wrapperDic);
                    //    stream.Flush();
                    //    xmlData = UnicodeEncoding.UTF8.GetString(stream.ToArray());
                    //}
                    //writer.WriteString(xmlData);
                }
            }
            [DataContract]

            private class InstanceValueWrapper
            {
                public InstanceValueWrapper(InstanceValue value)
                {
                    this.IsDeletedValue = value.IsDeletedValue;
                    this.Options = value.Options;
                    this.Value = value.Value;
                }

                public InstanceValueWrapper()
                {
                }

                [DataMember]
                public bool IsDeletedValue { get; set; }

                [DataMember]
                public InstanceValueOptions Options { get; set; }

                [DataMember]
                public object Value { get; set; }

                public InstanceValue CreateInstanceValue()
                {
                    if (this.IsDeletedValue)
                    {
                        return InstanceValue.DeletedValue;
                    }
                    else
                    {
                        return new InstanceValue(this.Value, this.Options);
                    }
                }
            }
        }
        #endregion

        #region PersistenceItems
        // [Serializable]
        public abstract class PersistenceItem
        {
            public Guid Id { get; set; }
            public PropertyBag Metadata { get; set; }
        }

        // [Serializable]
        public class Instance : PersistenceItem
        {
            public InstanceState State { get; set; }
            public Guid Owner { get; set; }
            public long Version { get; set; }
            public PropertyBag Data { get; set; }
            public List<Guid> RelatedKeys { get; set; }

            public Instance()
            {
                this.RelatedKeys = new List<Guid>();
            }
        }

        // [Serializable]
        public class Key : PersistenceItem
        {
            public InstanceKeyState State { get; set; }
            public Guid TargetInstanceId { get; set; }
        }

        // [Serializable]
        public class Owner : PersistenceItem
        {
            public Guid LockToken { get; set; }
        }
        #endregion

        #region PersistenceItemManager
        public static class PersistenceItemManager
        {
            private static readonly object s_thisLock = new object();

            public static string StorePath;

            public static List<Guid> GetKeys(Guid instanceId)
            {
                lock (s_thisLock)
                {
                    Instance instance = Load<Instance>(instanceId);
                    if (instance == null)
                    {
                        return new List<Guid>();
                    }
                    else
                    {
                        return instance.RelatedKeys;
                    }
                }
            }

            public static void AddKeyToInstance(Guid instanceId, Key key)
            {
                lock (s_thisLock)
                {
                    key.TargetInstanceId = instanceId;
                    Instance instance = LoadInstance(instanceId);
                    instance.RelatedKeys.Add(key.Id);
                    SaveToFile<Key>(key);
                    SaveToFile<Instance>(instance);
                }
            }

            public static void RemoveKeyFromInstance(Guid keyId, Guid instanceId)
            {
                lock (s_thisLock)
                {
                    Instance instance = LoadInstance(instanceId);
                    instance.RelatedKeys.Remove(keyId);
                    SaveToFile<Instance>(instance);
                    Remove<Key>(keyId);
                }
            }

            public static void Remove<T>(Guid id) where T : PersistenceItem
            {
                lock (s_thisLock)
                {
                    string fn = FileName<T>(id);
                    File.Delete(fn);
                }
            }

            public static Instance LoadInstance(Guid id)
            {
                Instance instance = Load<Instance>(id);
                if (instance == null)
                {
                    throw new InvalidOperationException("Instance does not exist");
                }
                return instance;
            }

            public static Instance GetInstanceFromKey(Guid keyId)
            {
                lock (s_thisLock)
                {
                    Key key = Load<Key>(keyId);
                    if (key == null)
                    {
                        throw new InvalidOperationException("Invalid key");
                    }

                    Instance instance = Load<Instance>(key.TargetInstanceId);
                    if (instance == null)
                    {
                        throw new InvalidOperationException("Instances and Keys are out of sync");
                    }
                    return instance;
                }
            }

            public static string FileName<T>(Guid id) where T : PersistenceItem
            {
                string filePath = string.Format("{0}_{1}.xml", typeof(T).Name, id);

                if (!string.IsNullOrEmpty(StorePath))
                {
                    filePath = Path.Combine(StorePath, filePath);
                }

                return filePath;
            }

            public static T Load<T>(Guid id) where T : PersistenceItem
            {
                lock (s_thisLock)
                {
                    try
                    {
                        string fn = FileName<T>(id);
                        //using (FileStream myFileStream = PartialTrustFileStream.CreateFileStream(fn, FileMode.Open))
                        //{
                        //    XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                        //    return (T)mySerializer.Deserialize(myFileStream);
                        //}
                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            public static void SaveToFile<T>(T item) where T : PersistenceItem
            {
                lock (s_thisLock)
                {
                    string fn = FileName<T>(item.Id);
                    //using (FileStream myFileStream = PartialTrustFileStream.CreateFileStream(fn, FileMode.Create))
                    //{
                    //    XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                    //    mySerializer.Serialize(myFileStream, item);
                    //    myFileStream.Close();
                    //}
                }
            }
        }
        #endregion
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Threading;
using System.Transactions;
using System.Xml.Linq;

namespace System.Activities;
using DurableInstancing;
using Hosting;
using Internals;
using Runtime;
using Runtime.DurableInstancing;

public partial class WorkflowApplication
{

    // This is a thin shell of PersistenceManager functionality so that WorkflowApplicationInstance
    // can hold onto a PM without exposing the entire persistence functionality
    internal abstract class PersistenceManagerBase
    {
        public abstract InstanceStore InstanceStore { get; }
        public abstract Guid InstanceId { get; }
    }

    private class PersistenceManager : PersistenceManagerBase
    {
        private InstanceHandle _handle;
        private InstanceHandle _temporaryHandle;
        private InstanceOwner _owner;
        private bool _ownerWasCreated;
        private bool _isLocked;
        private bool _aborted;
        private readonly bool _isTryLoad;
        private Guid _instanceId;
        private readonly InstanceStore _store;

        // Initializing metadata, used when instance is created
        private IDictionary<XName, InstanceValue> _instanceMetadata;

        // Updateable metadata, used when instance is saved
        private IDictionary<XName, InstanceValue> _mutableMetadata;

        public PersistenceManager(InstanceStore store, IDictionary<XName, InstanceValue> instanceMetadata, Guid instanceId)
        {
            Fx.Assert(store != null, "We should never gets here without a store.");

            _instanceId = instanceId;
            _instanceMetadata = instanceMetadata;

            InitializeInstanceMetadata();

            _owner = store.DefaultInstanceOwner;
            if (_owner != null)
            {
                _handle = store.CreateInstanceHandle(_owner, instanceId);
            }

            _store = store;
        }

        public PersistenceManager(InstanceStore store, IDictionary<XName, InstanceValue> instanceMetadata)
        {
            Fx.Assert(store != null, "We should never get here without a store.");

            _isTryLoad = true;
            _instanceMetadata = instanceMetadata;

            InitializeInstanceMetadata();

            _owner = store.DefaultInstanceOwner;
            if (_owner != null)
            {
                _handle = store.CreateInstanceHandle(_owner);
            }

            _store = store;
        }

        public sealed override Guid InstanceId => _instanceId;

        public sealed override InstanceStore InstanceStore => _store;

        public bool IsInitialized => (_handle != null);

        public bool IsLocked => _isLocked;

        public bool OwnerWasCreated => _ownerWasCreated;

        private void InitializeInstanceMetadata()
        {
            _instanceMetadata ??= new Dictionary<XName, InstanceValue>(1);

            // We always set this key explicitly so that users can't override
            // this metadata value
            _instanceMetadata[PersistenceMetadataNamespace.InstanceType] = new InstanceValue(WorkflowNamespace.WorkflowHostType, InstanceValueOptions.WriteOnly);
        }

        public void SetInstanceMetadata(IDictionary<XName, InstanceValue> metadata)
        {
            Fx.Assert(_instanceMetadata.Count == 1, "We should only have the default metadata from InitializeInstanceMetadata");
            if (metadata != null)
            {
                _instanceMetadata = metadata;
                InitializeInstanceMetadata();
            }
        }

        public void SetMutablemetadata(IDictionary<XName, InstanceValue> metadata) => _mutableMetadata = metadata;

        public void Initialize(WorkflowIdentity definitionIdentity, TimeSpan timeout)
        {
            Fx.Assert(_handle == null, "We are already initialized by now");

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                try
                {
                    CreateTemporaryHandle(null);
                    _owner = _store.Execute(_temporaryHandle, GetCreateOwnerCommand(definitionIdentity), timeout).InstanceOwner;
                    _ownerWasCreated = true;
                }
                finally
                {
                    FreeTemporaryHandle();
                }

                _handle = _isTryLoad ? _store.CreateInstanceHandle(_owner) : _store.CreateInstanceHandle(_owner, InstanceId);

                Thread.MemoryBarrier();
                if (_aborted)
                {
                    _handle.Free();
                }
            }
        }

        private void CreateTemporaryHandle(InstanceOwner owner)
        {
            _temporaryHandle = _store.CreateInstanceHandle(owner);

            Thread.MemoryBarrier();

            if (_aborted)
            {
                FreeTemporaryHandle();
            }
        }

        private void FreeTemporaryHandle()
        {
            InstanceHandle handle = _temporaryHandle;
            handle?.Free();
        }

        public IAsyncResult BeginInitialize(WorkflowIdentity definitionIdentity, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.Assert(_handle == null, "We are already initialized by now");

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                IAsyncResult result = null;

                try
                {
                    CreateTemporaryHandle(null);
                    result = _store.BeginExecute(_temporaryHandle, GetCreateOwnerCommand(definitionIdentity), timeout, callback, state);
                }
                finally
                {
                    // We've encountered an exception
                    if (result == null)
                    {
                        FreeTemporaryHandle();
                    }
                }
                return result;
            }
        }

        public void EndInitialize(IAsyncResult result)
        {
            try
            {
                _owner = _store.EndExecute(result).InstanceOwner;
                _ownerWasCreated = true;
            }
            finally
            {
                FreeTemporaryHandle();
            }

            _handle = _isTryLoad ? _store.CreateInstanceHandle(_owner) : _store.CreateInstanceHandle(_owner, InstanceId);
            Thread.MemoryBarrier();
            if (_aborted)
            {
                _handle.Free();
            }
        }

        public void DeleteOwner(TimeSpan timeout)
        {
            try
            {
                CreateTemporaryHandle(_owner);
                _store.Execute(_temporaryHandle, new DeleteWorkflowOwnerCommand(), timeout);
            }
            // Ignore some exceptions because DeleteWorkflowOwner is best effort.
            catch (InstancePersistenceCommandException) { }
            catch (InstanceOwnerException) { }
            catch (OperationCanceledException) { }
            finally
            {
                FreeTemporaryHandle();
            }
        }

        public IAsyncResult BeginDeleteOwner(TimeSpan timeout, AsyncCallback callback, object state)
        {
            IAsyncResult result = null;
            try
            {
                CreateTemporaryHandle(_owner);
                result = _store.BeginExecute(_temporaryHandle, new DeleteWorkflowOwnerCommand(), timeout, callback, state);
            }
            // Ignore some exceptions because DeleteWorkflowOwner is best effort.
            catch (InstancePersistenceCommandException) { }
            catch (InstanceOwnerException) { }
            catch (OperationCanceledException) { }
            finally
            {
                if (result == null)
                {
                    FreeTemporaryHandle();
                }
            }
            return result;
        }

        public void EndDeleteOwner(IAsyncResult result)
        {
            try
            {
                _store.EndExecute(result);
            }
            // Ignore some exceptions because DeleteWorkflowOwner is best effort.
            catch (InstancePersistenceCommandException) { }
            catch (InstanceOwnerException) { }
            catch (OperationCanceledException) { }
            finally
            {
                FreeTemporaryHandle();
            }
        }

        public void EnsureReadyness(TimeSpan timeout)
        {
            Fx.Assert(_handle != null, "We should already be initialized by now");
            Fx.Assert(!IsLocked, "We are already ready for persistence; why are we being called?");
            Fx.Assert(!_isTryLoad, "Should not be on an initial save path if we tried load.");

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                _store.Execute(_handle, CreateSaveCommand(null, _instanceMetadata, PersistenceOperation.Save), timeout);
                _isLocked = true;
            }
        }

        public IAsyncResult BeginEnsureReadyness(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.Assert(_handle != null, "We should already be initialized by now");
            Fx.Assert(!IsLocked, "We are already ready for persistence; why are we being called?");
            Fx.Assert(!_isTryLoad, "Should not be on an initial save path if we tried load.");

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                return _store.BeginExecute(_handle, CreateSaveCommand(null, _instanceMetadata, PersistenceOperation.Save), timeout, callback, state);
            }
        }

        public void EndEnsureReadyness(IAsyncResult result)
        {
            _store.EndExecute(result);
            _isLocked = true;
        }

        public static Dictionary<XName, InstanceValue> GenerateInitialData(WorkflowApplication instance)
        {
            Dictionary<XName, InstanceValue> data = new(10);
            data[WorkflowNamespace.Bookmarks] = new InstanceValue(instance.Controller.GetBookmarks(), InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
            data[WorkflowNamespace.LastUpdate] = new InstanceValue(DateTime.UtcNow, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);

            foreach (KeyValuePair<string, LocationInfo> mappedVariable in instance.Controller.GetMappedVariables())
            {
                data[WorkflowNamespace.VariablesPath.GetName(mappedVariable.Key)] = new InstanceValue(mappedVariable.Value, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
            }

            Fx.AssertAndThrow(instance.Controller.State != WorkflowInstanceState.Aborted, "Cannot generate data for an aborted instance.");
            if (instance.Controller.State != WorkflowInstanceState.Complete)
            {
                // TODO Research.
                data[WorkflowNamespace.Workflow] = new InstanceValue(instance.Controller.PrepareForSerialization());
                data[WorkflowNamespace.Status] = new InstanceValue(instance.Controller.State == WorkflowInstanceState.Idle ? "Idle" : "Executing", InstanceValueOptions.WriteOnly);
            }
            else
            {
                data[WorkflowNamespace.Workflow] = new InstanceValue(instance.Controller.PrepareForSerialization(), InstanceValueOptions.Optional);
                ActivityInstanceState completionState = instance.Controller.GetCompletionState(out IDictionary<string, object> outputs, out Exception completionException);

                if (completionState == ActivityInstanceState.Faulted)
                {
                    data[WorkflowNamespace.Status] = new InstanceValue("Faulted", InstanceValueOptions.WriteOnly);
                    data[WorkflowNamespace.Exception] = new InstanceValue(completionException, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                }
                else if (completionState == ActivityInstanceState.Closed)
                {
                    data[WorkflowNamespace.Status] = new InstanceValue("Closed", InstanceValueOptions.WriteOnly);
                    if (outputs != null)
                    {
                        foreach (KeyValuePair<string, object> output in outputs)
                        {
                            data[WorkflowNamespace.OutputPath.GetName(output.Key)] = new InstanceValue(output.Value, InstanceValueOptions.WriteOnly | InstanceValueOptions.Optional);
                        }
                    }
                }
                else
                {
                    Fx.AssertAndThrow(completionState == ActivityInstanceState.Canceled, "Cannot be executing when WorkflowState was completed.");
                    data[WorkflowNamespace.Status] = new InstanceValue("Canceled", InstanceValueOptions.WriteOnly);
                }
            }
            return data;
        }

        private static InstancePersistenceCommand GetCreateOwnerCommand(WorkflowIdentity definitionIdentity)
        {
            // Technically, we only need to pass the owner identity when doing LoadRunnable.
            // However, if we create an instance with identity on a store that doesn't recognize it,
            // the identity metadata might be stored in a way which makes it unqueryable if the store
            // is later upgraded to support identity (e.g. SWIS 4.0 -> 4.5 upgrade). So to be on the
            // safe side, if we're using identity, we require the store to explicitly support it.
            if (definitionIdentity != null)
            {
                CreateWorkflowOwnerWithIdentityCommand result = new();
                if (!ReferenceEquals(definitionIdentity, unknownIdentity))
                {
                    result.InstanceOwnerMetadata.Add(Workflow45Namespace.DefinitionIdentities,
                        new InstanceValue(new Collection<WorkflowIdentity> { definitionIdentity }));
                }
                return result;
            }
            else
            {
                return new CreateWorkflowOwnerCommand();
            }
        }

        private static SaveWorkflowCommand CreateSaveCommand(IDictionary<XName, InstanceValue> instance, IDictionary<XName, InstanceValue> instanceMetadata, PersistenceOperation operation)
        {
            SaveWorkflowCommand saveCommand = new()
            {
                CompleteInstance = operation == PersistenceOperation.Complete,
                UnlockInstance = operation != PersistenceOperation.Save,
            };

            if (instance != null)
            {
                foreach (KeyValuePair<XName, InstanceValue> value in instance)
                {
                    saveCommand.InstanceData.Add(value);
                }
            }

            if (instanceMetadata != null)
            {
                foreach (KeyValuePair<XName, InstanceValue> value in instanceMetadata)
                {
                    saveCommand.InstanceMetadataChanges.Add(value);
                }
            }

            return saveCommand;
        }

        private bool TryLoadHelper(InstanceView view, out IDictionary<XName, InstanceValue> data)
        {
            if (!view.IsBoundToLock)
            {
                data = null;
                return false;
            }
            _instanceId = view.InstanceId;
            _isLocked = true;

            if (!_handle.IsValid)
            {
                throw FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
            }

            data = view.InstanceData;
            return true;
        }

        public void Save(IDictionary<XName, InstanceValue> instance, PersistenceOperation operation, TimeSpan timeout)
        {
            _store.Execute(_handle, CreateSaveCommand(instance, (_isLocked ? _mutableMetadata : _instanceMetadata), operation), timeout);
            _isLocked = true;
        }

        public IDictionary<XName, InstanceValue> Load(TimeSpan timeout)
        {
            InstanceView view = _store.Execute(_handle, new LoadWorkflowCommand(), timeout);
            _isLocked = true;

            if (!_handle.IsValid)
            {
                throw FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
            }

            return view.InstanceData;
        }

        public bool TryLoad(TimeSpan timeout, out IDictionary<XName, InstanceValue> data)
        {
            InstanceView view = _store.Execute(_handle, new TryLoadRunnableWorkflowCommand(), timeout);
            return TryLoadHelper(view, out data);
        }

        public IAsyncResult BeginSave(IDictionary<XName, InstanceValue> instance, PersistenceOperation operation, TimeSpan timeout, AsyncCallback callback, object state)
            => _store.BeginExecute(_handle, CreateSaveCommand(instance, (_isLocked ? _mutableMetadata : _instanceMetadata), operation), timeout, callback, state);

        public void EndSave(IAsyncResult result)
        {
            _store.EndExecute(result);
            _isLocked = true;
        }

        public IAsyncResult BeginLoad(TimeSpan timeout, AsyncCallback callback, object state)
            => _store.BeginExecute(_handle, new LoadWorkflowCommand(), timeout, callback, state);

        public IDictionary<XName, InstanceValue> EndLoad(IAsyncResult result)
        {
            InstanceView view = _store.EndExecute(result);
            _isLocked = true;

            if (!_handle.IsValid)
            {
                throw FxTrace.Exception.AsError(new OperationCanceledException(SR.WorkflowInstanceAborted(InstanceId)));
            }

            return view.InstanceData;
        }

        public IAsyncResult BeginTryLoad(TimeSpan timeout, AsyncCallback callback, object state)
            => _store.BeginExecute(_handle, new TryLoadRunnableWorkflowCommand(), timeout, callback, state);

        public bool EndTryLoad(IAsyncResult result, out IDictionary<XName, InstanceValue> data)
        {
            InstanceView view = _store.EndExecute(result);
            return TryLoadHelper(view, out data);
        }

        public void Abort()
        {
            _aborted = true;

            // Make sure the setter of handle sees aborted, or v.v., or both.
            Thread.MemoryBarrier();

            InstanceHandle handle = _handle;
            handle?.Free();
            FreeTemporaryHandle();
        }

        public void Unlock(TimeSpan timeout)
        {
            SaveWorkflowCommand saveCmd = new()
            {
                UnlockInstance = true,
            };

            _store.Execute(_handle, saveCmd, timeout);
        }

        public IAsyncResult BeginUnlock(TimeSpan timeout, AsyncCallback callback, object state)
        {
            SaveWorkflowCommand saveCmd = new()
            {
                UnlockInstance = true,
            };

            return _store.BeginExecute(_handle, saveCmd, timeout, callback, state);
        }

        public void EndUnlock(IAsyncResult result)
        {
            _store.EndExecute(result);
        }
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstancePersistenceContext
    {
        private readonly TimeSpan _timeout;

        private int _cancellationHandlerCalled;
        private readonly EventTraceActivity _eventTraceActivity;

        internal InstancePersistenceContext(InstanceHandle handle, TimeSpan timeout)
            : this(handle)
        {
            _timeout = timeout;
        }

        private InstancePersistenceContext(InstanceHandle handle)
        {
            Fx.Assert(handle != null, "Null handle passed to InstancePersistenceContext.");

            InstanceHandle = handle;

            // Fork a copy of the current view to be the new working view. It starts with no query results.
            InstanceView newView = handle.View.Clone();
            newView.InstanceStoreQueryResults = null;
            InstanceView = newView;

            _cancellationHandlerCalled = 0;
            _eventTraceActivity = handle.EventTraceActivity;
        }

        public InstanceHandle InstanceHandle { get; private set; }
        public InstanceView InstanceView { get; private set; }

        public long InstanceVersion
        {
            get
            {
                return InstanceHandle.Version;
            }
        }

        internal EventTraceActivity EventTraceActivity
        {
            get
            {
                return _eventTraceActivity;
            }
        }

        public Guid LockToken
        {
            get
            {
                Fx.Assert(InstanceHandle.Owner == null || InstanceHandle.Owner.OwnerToken == InstanceView.InstanceOwner.OwnerToken, "Mismatched lock tokens.");

                // If the handle doesn't own the lock yet, return the owner LockToken, which is needed to check whether this owner already owns locks.
                return InstanceHandle.Owner == null ? Guid.Empty : InstanceHandle.Owner.OwnerToken;
            }
        }

        public object UserContext
        {
            get
            {
                return InstanceHandle.ProviderObject;
            }
        }

        private bool CancelRequested { get; set; }

        private ExecuteAsyncResult RootAsyncResult { get; set; }
        private ExecuteAsyncResult LastAsyncResult { get; set; }

        private bool Active
        {
            get
            {
                return RootAsyncResult != null;
            }
        }

        public void SetCancellationHandler(Action<InstancePersistenceContext> cancellationHandler)
        {
            ThrowIfNotActive("SetCancellationHandler");
            LastAsyncResult.CancellationHandler = cancellationHandler;
            if (CancelRequested && (cancellationHandler != null))
            {
                try
                {
                    if (Interlocked.CompareExchange(ref _cancellationHandlerCalled, 0, 1) == 0)
                    {
                        cancellationHandler(this);
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    throw Fx.Exception.AsError(new CallbackException(SR.OnCancelRequestedThrew, exception));
                }
            }
        }

        public void BindInstanceOwner(Guid instanceOwnerId, Guid lockToken)
        {
            if (instanceOwnerId == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(instanceOwnerId), SR.GuidCannotBeEmpty);
            }
            if (lockToken == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(lockToken), SR.GuidCannotBeEmpty);
            }
            ThrowIfNotActive("BindInstanceOwner");

            InstanceOwner owner = InstanceHandle.Store.GetOrCreateOwner(instanceOwnerId, lockToken);

            InstanceView.BindOwner(owner);
            IsHandleDoomedByRollback = true;

            InstanceHandle.BindOwner(owner);
        }

        public void BindInstance(Guid instanceId)
        {
            if (instanceId == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(instanceId), SR.GuidCannotBeEmpty);
            }
            ThrowIfNotActive("BindInstance");

            InstanceView.BindInstance(instanceId);
            IsHandleDoomedByRollback = true;

            InstanceHandle.BindInstance(instanceId);
        }

        public void BindEvent(InstancePersistenceEvent persistenceEvent)
        {
            if (persistenceEvent == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(persistenceEvent));
            }
            ThrowIfNotActive("BindEvent");

            if (!InstanceView.IsBoundToInstanceOwner)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.ContextMustBeBoundToOwner));
            }
            IsHandleDoomedByRollback = true;

            InstanceHandle.BindOwnerEvent(persistenceEvent);
        }

        public void BindAcquiredLock(long instanceVersion)
        {
            if (instanceVersion < 0)
            {
                throw Fx.Exception.ArgumentOutOfRange(nameof(instanceVersion), instanceVersion, SR.InvalidLockToken);
            }
            ThrowIfNotActive("BindAcquiredLock");

            // This call has a synchronization, so we are guaranteed it is only successful once.
            InstanceView.BindLock(instanceVersion);
            IsHandleDoomedByRollback = true;

            InstanceHandle.Bind(instanceVersion);
        }

        public void BindReclaimedLock(long instanceVersion, TimeSpan timeout)
        {
            AsyncWaitHandle wait = InitiateBindReclaimedLockHelper("BindReclaimedLock", instanceVersion, timeout);
            if (!wait.Wait(timeout))
            {
                InstanceHandle.CancelReclaim(new TimeoutException(SR.TimedOutWaitingForLockResolution));
            }
            ConcludeBindReclaimedLockHelper();
        }

        public IAsyncResult BeginBindReclaimedLock(long instanceVersion, TimeSpan timeout, AsyncCallback callback, object state)
        {
            AsyncWaitHandle wait = InitiateBindReclaimedLockHelper("BeginBindReclaimedLock", instanceVersion, timeout);
            return new BindReclaimedLockAsyncResult(this, wait, timeout, callback, state);
        }

        public void EndBindReclaimedLock(IAsyncResult result)
        {
            BindReclaimedLockAsyncResult.End(result);
        }

        public Exception CreateBindReclaimedLockException(long instanceVersion)
        {
            AsyncWaitHandle wait = InitiateBindReclaimedLockHelper("CreateBindReclaimedLockException", instanceVersion, TimeSpan.MaxValue);
            return new BindReclaimedLockException(wait);
        }

        private AsyncWaitHandle InitiateBindReclaimedLockHelper(string methodName, long instanceVersion, TimeSpan timeout)
        {
            if (instanceVersion < 0)
            {
                throw Fx.Exception.ArgumentOutOfRange(nameof(instanceVersion), instanceVersion, SR.InvalidLockToken);
            }
            TimeoutHelper.ThrowIfNegativeArgument(timeout);
            ThrowIfNotActive(methodName);

            // This call has a synchronization, so we are guaranteed it is only successful once.
            InstanceView.StartBindLock(instanceVersion);
            IsHandleDoomedByRollback = true;

            AsyncWaitHandle wait = InstanceHandle.StartReclaim(instanceVersion);
            if (wait == null)
            {
                InstanceHandle.Free();
                throw Fx.Exception.AsError(new InstanceHandleConflictException(LastAsyncResult.CurrentCommand.Name, InstanceView.InstanceId));
            }
            return wait;
        }

        private void ConcludeBindReclaimedLockHelper()
        {
            // If FinishReclaim doesn't throw an exception, we are done - the reclaim was successful.
            // The Try / Finally makes up for the reverse order of setting the handle, then the view.
            long instanceVersion = -1;
            try
            {
                if (!InstanceHandle.FinishReclaim(ref instanceVersion))
                {
                    InstanceHandle.Free();
                    throw Fx.Exception.AsError(new InstanceHandleConflictException(LastAsyncResult.CurrentCommand.Name, InstanceView.InstanceId));
                }
                Fx.Assert(instanceVersion >= 0, "Where did the instance version go?");
            }
            finally
            {
                if (instanceVersion >= 0)
                {
                    InstanceView.FinishBindLock(instanceVersion);
                }
            }
        }

        public void PersistedInstance(IDictionary<XName, InstanceValue> data)
        {
            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("PersistedInstance");

            InstanceView.InstanceData = data.ReadOnlyCopy(true);
            InstanceView.InstanceDataConsistency = InstanceValueConsistency.None;
            InstanceView.InstanceState = InstanceState.Initialized;
        }

        public void LoadedInstance(InstanceState state, IDictionary<XName, InstanceValue> instanceData, IDictionary<XName, InstanceValue> instanceMetadata, IDictionary<Guid, IDictionary<XName, InstanceValue>> associatedInstanceKeyMetadata, IDictionary<Guid, IDictionary<XName, InstanceValue>> completedInstanceKeyMetadata)
        {
            if (state == InstanceState.Uninitialized)
            {
                if (instanceData != null && instanceData.Count > 0)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.UninitializedCannotHaveData));
                }
            }
            else if (state == InstanceState.Completed)
            {
                if (associatedInstanceKeyMetadata != null && associatedInstanceKeyMetadata.Count > 0)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.CompletedMustNotHaveAssociatedKeys));
                }
            }
            else if (state != InstanceState.Initialized)
            {
                throw Fx.Exception.Argument(nameof(state), SR.InvalidInstanceState);
            }
            ThrowIfNoInstance();
            ThrowIfNotActive("PersistedInstance");

            InstanceValueConsistency consistency = InstanceView.IsBoundToLock || state == InstanceState.Completed ? InstanceValueConsistency.None : InstanceValueConsistency.InDoubt;

            ReadOnlyDictionary<XName, InstanceValue> instanceDataCopy = instanceData.ReadOnlyCopy(false);
            ReadOnlyDictionary<XName, InstanceValue> instanceMetadataCopy = instanceMetadata.ReadOnlyCopy(false);

            Dictionary<Guid, InstanceKeyView> keysCopy = null;
            int totalKeys = (associatedInstanceKeyMetadata != null ? associatedInstanceKeyMetadata.Count : 0) + (completedInstanceKeyMetadata != null ? completedInstanceKeyMetadata.Count : 0);
            if (totalKeys > 0)
            {
                keysCopy = new Dictionary<Guid, InstanceKeyView>(totalKeys);
            }
            if (associatedInstanceKeyMetadata != null && associatedInstanceKeyMetadata.Count > 0)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> keyMetadata in associatedInstanceKeyMetadata)
                {
                    InstanceKeyView view = new InstanceKeyView(keyMetadata.Key)
                    {
                        InstanceKeyState = InstanceKeyState.Associated,
                        InstanceKeyMetadata = keyMetadata.Value.ReadOnlyCopy(false),
                        InstanceKeyMetadataConsistency = InstanceView.IsBoundToLock ? InstanceValueConsistency.None : InstanceValueConsistency.InDoubt
                    };
                    keysCopy.Add(view.InstanceKey, view);
                }
            }

            if (completedInstanceKeyMetadata != null && completedInstanceKeyMetadata.Count > 0)
            {
                foreach (KeyValuePair<Guid, IDictionary<XName, InstanceValue>> keyMetadata in completedInstanceKeyMetadata)
                {
                    InstanceKeyView view = new InstanceKeyView(keyMetadata.Key)
                    {
                        InstanceKeyState = InstanceKeyState.Completed,
                        InstanceKeyMetadata = keyMetadata.Value.ReadOnlyCopy(false),
                        InstanceKeyMetadataConsistency = consistency
                    };
                    keysCopy.Add(view.InstanceKey, view);
                }
            }

            InstanceView.InstanceState = state;

            InstanceView.InstanceData = instanceDataCopy;
            InstanceView.InstanceDataConsistency = consistency;

            InstanceView.InstanceMetadata = instanceMetadataCopy;
            InstanceView.InstanceMetadataConsistency = consistency;

            InstanceView.InstanceKeys = keysCopy == null ? null : new ReadOnlyDictionary<Guid, InstanceKeyView>(keysCopy);
            InstanceView.InstanceKeysConsistency = consistency;
        }

        public void CompletedInstance()
        {
            ThrowIfNotLocked();
            ThrowIfUninitialized();
            ThrowIfCompleted();
            if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.InDoubt) == 0)
            {
                foreach (KeyValuePair<Guid, InstanceKeyView> key in InstanceView.InstanceKeys)
                {
                    if (key.Value.InstanceKeyState == InstanceKeyState.Associated)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotCompleteWithKeys));
                    }
                }
            }
            ThrowIfNotTransactional("CompletedInstance");

            InstanceView.InstanceState = InstanceState.Completed;
        }

        public void ReadInstanceMetadata(IDictionary<XName, InstanceValue> metadata, bool complete)
        {
            ThrowIfNoInstance();
            ThrowIfNotActive("ReadInstanceMetadata");

            if (InstanceView.InstanceMetadataConsistency == InstanceValueConsistency.None)
            {
                return;
            }

            if (complete)
            {
                InstanceView.InstanceMetadata = metadata.ReadOnlyCopy(false);
                InstanceView.InstanceMetadataConsistency = InstanceView.IsBoundToLock || InstanceView.InstanceState == InstanceState.Completed ? InstanceValueConsistency.None : InstanceValueConsistency.InDoubt;
            }
            else
            {
                if ((InstanceView.IsBoundToLock || InstanceView.InstanceState == InstanceState.Completed) && (InstanceView.InstanceMetadataConsistency & InstanceValueConsistency.InDoubt) != 0)
                {
                    // In this case, prefer throwing out old data and keeping only authoritative data.
                    InstanceView.InstanceMetadata = metadata.ReadOnlyMergeInto(null, false);
                    InstanceView.InstanceMetadataConsistency = InstanceValueConsistency.Partial;
                }
                else
                {
                    InstanceView.InstanceMetadata = metadata.ReadOnlyMergeInto(InstanceView.InstanceMetadata, false);
                    InstanceView.InstanceMetadataConsistency |= InstanceValueConsistency.Partial;
                }
            }
        }

        public void WroteInstanceMetadataValue(XName name, InstanceValue value)
        {
            if (name == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(name));
            }

            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("WroteInstanceMetadataValue");

            InstanceView.AccumulatedMetadataWrites[name] = value ?? throw Fx.Exception.ArgumentNull(nameof(value));
        }

        public void AssociatedInstanceKey(Guid key)
        {
            if (key == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(key), SR.InvalidKeyArgument);
            }
            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("AssociatedInstanceKey");

            Dictionary<Guid, InstanceKeyView> copy = new Dictionary<Guid, InstanceKeyView>(InstanceView.InstanceKeys);
            if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.InDoubt) == 0 && copy.ContainsKey(key))
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyAlreadyAssociated));
            }
            InstanceKeyView keyView = new InstanceKeyView(key)
            {
                InstanceKeyState = InstanceKeyState.Associated,
                InstanceKeyMetadataConsistency = InstanceValueConsistency.None
            };
            copy[keyView.InstanceKey] = keyView;
            InstanceView.InstanceKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(copy);
        }

        public void CompletedInstanceKey(Guid key)
        {
            if (key == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(key), SR.InvalidKeyArgument);
            }
            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("CompletedInstanceKey");

            InstanceView.InstanceKeys.TryGetValue(key, out InstanceKeyView existingKeyView);
            if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.InDoubt) == 0)
            {
                if (existingKeyView != null)
                {
                    if (existingKeyView.InstanceKeyState == InstanceKeyState.Completed)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyAlreadyCompleted));
                    }
                }
                else if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.Partial) == 0)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyNotAssociated));
                }
            }

            if (existingKeyView != null)
            {
                existingKeyView.InstanceKeyState = InstanceKeyState.Completed;
            }
            else
            {
                Dictionary<Guid, InstanceKeyView> copy = new Dictionary<Guid, InstanceKeyView>(InstanceView.InstanceKeys);
                InstanceKeyView keyView = new InstanceKeyView(key)
                {
                    InstanceKeyState = InstanceKeyState.Completed,
                    InstanceKeyMetadataConsistency = InstanceValueConsistency.Partial
                };
                copy[keyView.InstanceKey] = keyView;
                InstanceView.InstanceKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(copy);
            }
        }

        public void UnassociatedInstanceKey(Guid key)
        {
            if (key == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(key), SR.InvalidKeyArgument);
            }
            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("UnassociatedInstanceKey");

            InstanceView.InstanceKeys.TryGetValue(key, out InstanceKeyView existingKeyView);
            if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.InDoubt) == 0)
            {
                if (existingKeyView != null)
                {
                    if (existingKeyView.InstanceKeyState == InstanceKeyState.Associated)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyNotCompleted));
                    }
                }
                else if ((InstanceView.InstanceKeysConsistency & InstanceValueConsistency.Partial) == 0)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyAlreadyUnassociated));
                }
            }

            if (existingKeyView != null)
            {
                Dictionary<Guid, InstanceKeyView> copy = new Dictionary<Guid, InstanceKeyView>(InstanceView.InstanceKeys);
                copy.Remove(key);
                InstanceView.InstanceKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(copy);
            }
        }

        public void ReadInstanceKeyMetadata(Guid key, IDictionary<XName, InstanceValue> metadata, bool complete)
        {
            if (key == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(key), SR.InvalidKeyArgument);
            }
            ThrowIfNoInstance();
            ThrowIfNotActive("ReadInstanceKeyMetadata");

            if (!InstanceView.InstanceKeys.TryGetValue(key, out InstanceKeyView keyView))
            {
                if (InstanceView.InstanceKeysConsistency == InstanceValueConsistency.None)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyNotAssociated));
                }

                Dictionary<Guid, InstanceKeyView> copy = new Dictionary<Guid, InstanceKeyView>(InstanceView.InstanceKeys);
                keyView = new InstanceKeyView(key);
                if (complete)
                {
                    keyView.InstanceKeyMetadata = metadata.ReadOnlyCopy(false);
                    keyView.InstanceKeyMetadataConsistency = InstanceValueConsistency.None;
                }
                else
                {
                    keyView.InstanceKeyMetadata = metadata.ReadOnlyMergeInto(null, false);
                    keyView.InstanceKeyMetadataConsistency = InstanceValueConsistency.Partial;
                }
                if (!InstanceView.IsBoundToLock && InstanceView.InstanceState != InstanceState.Completed)
                {
                    keyView.InstanceKeyMetadataConsistency |= InstanceValueConsistency.InDoubt;
                }
                copy[keyView.InstanceKey] = keyView;
                InstanceView.InstanceKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(copy);
            }
            else
            {
                if (keyView.InstanceKeyMetadataConsistency == InstanceValueConsistency.None)
                {
                    return;
                }

                if (complete)
                {
                    keyView.InstanceKeyMetadata = metadata.ReadOnlyCopy(false);
                    keyView.InstanceKeyMetadataConsistency = InstanceView.IsBoundToLock || InstanceView.InstanceState == InstanceState.Completed ? InstanceValueConsistency.None : InstanceValueConsistency.InDoubt;
                }
                else
                {
                    if ((InstanceView.IsBoundToLock || InstanceView.InstanceState == InstanceState.Completed) && (keyView.InstanceKeyMetadataConsistency & InstanceValueConsistency.InDoubt) != 0)
                    {
                        // In this case, prefer throwing out old data and keeping only authoritative data.
                        keyView.InstanceKeyMetadata = metadata.ReadOnlyMergeInto(null, false);
                        keyView.InstanceKeyMetadataConsistency = InstanceValueConsistency.Partial;
                    }
                    else
                    {
                        keyView.InstanceKeyMetadata = metadata.ReadOnlyMergeInto(keyView.InstanceKeyMetadata, false);
                        keyView.InstanceKeyMetadataConsistency |= InstanceValueConsistency.Partial;
                    }
                }
            }
        }

        public void WroteInstanceKeyMetadataValue(Guid key, XName name, InstanceValue value)
        {
            if (key == Guid.Empty)
            {
                throw Fx.Exception.Argument(nameof(key), SR.InvalidKeyArgument);
            }
            if (name == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(name));
            }
            if (value == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(value));
            }
            ThrowIfNotLocked();
            ThrowIfCompleted();
            ThrowIfNotTransactional("WroteInstanceKeyMetadataValue");

            if (!InstanceView.InstanceKeys.TryGetValue(key, out InstanceKeyView keyView))
            {
                if (InstanceView.InstanceKeysConsistency == InstanceValueConsistency.None)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.KeyNotAssociated));
                }

                if (!value.IsWriteOnly() && !value.IsDeletedValue)
                {
                    Dictionary<Guid, InstanceKeyView> copy = new Dictionary<Guid, InstanceKeyView>(InstanceView.InstanceKeys);
                    keyView = new InstanceKeyView(key);
                    keyView.AccumulatedMetadataWrites.Add(name, value);
                    keyView.InstanceKeyMetadataConsistency = InstanceValueConsistency.Partial;
                    copy[keyView.InstanceKey] = keyView;
                    InstanceView.InstanceKeys = new ReadOnlyDictionary<Guid, InstanceKeyView>(copy);
                    InstanceView.InstanceKeysConsistency |= InstanceValueConsistency.Partial;
                }
            }
            else
            {
                keyView.AccumulatedMetadataWrites.Add(name, value);
            }
        }

        public void ReadInstanceOwnerMetadata(IDictionary<XName, InstanceValue> metadata, bool complete)
        {
            ThrowIfNoOwner();
            ThrowIfNotActive("ReadInstanceOwnerMetadata");

            if (InstanceView.InstanceOwnerMetadataConsistency == InstanceValueConsistency.None)
            {
                return;
            }

            if (complete)
            {
                InstanceView.InstanceOwnerMetadata = metadata.ReadOnlyCopy(false);
                InstanceView.InstanceOwnerMetadataConsistency = InstanceValueConsistency.InDoubt;
            }
            else
            {
                InstanceView.InstanceOwnerMetadata = metadata.ReadOnlyMergeInto(InstanceView.InstanceOwnerMetadata, false);
                InstanceView.InstanceOwnerMetadataConsistency |= InstanceValueConsistency.Partial;
            }
        }

        public void WroteInstanceOwnerMetadataValue(XName name, InstanceValue value)
        {
            if (name == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(name));
            }
            if (value == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(value));
            }
            ThrowIfNoOwner();
            ThrowIfNotTransactional("WroteInstanceOwnerMetadataValue");

            InstanceView.AccumulatedOwnerMetadataWrites.Add(name, value);
        }

        public void QueriedInstanceStore(InstanceStoreQueryResult queryResult)
        {
            if (queryResult == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(queryResult));
            }
            ThrowIfNotActive("QueriedInstanceStore");

            InstanceView.QueryResultsBacking.Add(queryResult);
        }

        [Fx.Tag.Throws.Timeout("The operation timed out.")]
        [Fx.Tag.Throws(typeof(OperationCanceledException), "The operation was canceled because the InstanceHandle has been freed.")]
        [Fx.Tag.Throws(typeof(InstancePersistenceException), "A command failed.")]
        [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree")]
        public void Execute(InstancePersistenceCommand command, TimeSpan timeout)
        {
            if (command == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(command));
            }
            ThrowIfNotActive("Execute");

            try
            {
                ExecuteAsyncResult.End(new ExecuteAsyncResult(this, command, timeout));
            }
            catch (TimeoutException)
            {
                InstanceHandle.Free();
                throw;
            }
            catch (OperationCanceledException)
            {
                InstanceHandle.Free();
                throw;
            }
        }

        // For each level of hierarchy of command execution, only one BeginExecute may be pending at a time.
        [Fx.Tag.InheritThrows(From = "Execute")]
        public IAsyncResult BeginExecute(InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (command == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(command));
            }
            ThrowIfNotActive("BeginExecute");

            try
            {
                return new ExecuteAsyncResult(this, command, timeout, callback, state);
            }
            catch (TimeoutException)
            {
                InstanceHandle.Free();
                throw;
            }
            catch (OperationCanceledException)
            {
                InstanceHandle.Free();
                throw;
            }
        }

        [Fx.Tag.InheritThrows(From = "Execute")]
        [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", Conditional = "!result.IsCompleted")]
        public void EndExecute(IAsyncResult result)
        {
            ExecuteAsyncResult.End(result);
        }

        internal bool IsHandleDoomedByRollback { get; private set; }

        internal void PrepareForReuse()
        {
            Fx.AssertAndThrow(!Active, "Prior use not yet complete!");
        }

        internal void NotifyHandleFree()
        {
            CancelRequested = true;
            ExecuteAsyncResult lastAsyncResult = LastAsyncResult;
            Action<InstancePersistenceContext> onCancel = lastAsyncResult == null ? null : lastAsyncResult.CancellationHandler;
            if (onCancel != null)
            {
                try
                {
                    if (Interlocked.CompareExchange(ref _cancellationHandlerCalled, 0, 1) == 0)
                    {
                        onCancel(this);
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    throw Fx.Exception.AsError(new CallbackException(SR.OnCancelRequestedThrew, exception));
                }
            }
        }

        [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree")]
        internal static InstanceView OuterExecute(InstanceHandle initialInstanceHandle, InstancePersistenceCommand command, TimeSpan timeout)
        {
            try
            {
                return ExecuteAsyncResult.End(new ExecuteAsyncResult(initialInstanceHandle, command, timeout));
            }
            catch (TimeoutException)
            {
                initialInstanceHandle.Free();
                throw;
            }
            catch (OperationCanceledException)
            {
                initialInstanceHandle.Free();
                throw;
            }
        }

        internal static IAsyncResult BeginOuterExecute(InstanceHandle initialInstanceHandle, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                return new ExecuteAsyncResult(initialInstanceHandle, command, timeout, callback, state);
            }
            catch (TimeoutException)
            {
                initialInstanceHandle.Free();
                throw;
            }
            catch (OperationCanceledException)
            {
                initialInstanceHandle.Free();
                throw;
            }
        }

        [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", Conditional = "!result.IsCompleted")]
        internal static InstanceView EndOuterExecute(IAsyncResult result)
        {
            InstanceView finalState = ExecuteAsyncResult.End(result);
            if (finalState == null)
            {
                throw Fx.Exception.Argument(nameof(result), SR.InvalidAsyncResult);
            }
            return finalState;
        }

        private void ThrowIfNotLocked()
        {
            if (!InstanceView.IsBoundToLock)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceOperationRequiresLock));
            }
        }

        private void ThrowIfNoInstance()
        {
            if (!InstanceView.IsBoundToInstance)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceOperationRequiresInstance));
            }
        }

        private void ThrowIfNoOwner()
        {
            if (!InstanceView.IsBoundToInstanceOwner)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceOperationRequiresOwner));
            }
        }

        private void ThrowIfCompleted()
        {
            if (InstanceView.IsBoundToLock && InstanceView.InstanceState == InstanceState.Completed)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceOperationRequiresNotCompleted));
            }
        }

        private void ThrowIfUninitialized()
        {
            if (InstanceView.IsBoundToLock && InstanceView.InstanceState == InstanceState.Uninitialized)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceOperationRequiresNotUninitialized));
            }
        }

        private void ThrowIfNotActive(string methodName)
        {
            if (!Active)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.OutsideInstanceExecutionScope(methodName)));
            }
        }

        private void ThrowIfNotTransactional(string methodName)
        {
            ThrowIfNotActive(methodName);
            //if (RootAsyncResult.CurrentCommand.IsTransactionEnlistmentOptional)
            //{
            //    throw Fx.Exception.AsError(new InvalidOperationException(SR.OutsideTransactionalCommand(methodName)));
            //}
        }

        private class ExecuteAsyncResult : AsyncResult
        {
            private static AsyncCompletion s_onAcquireContext = new AsyncCompletion(OnAcquireContext);
            private static AsyncCompletion s_onTryCommand = new AsyncCompletion(OnTryCommand);
            private static Action<object, TimeoutException> s_onBindReclaimed = new Action<object, TimeoutException>(OnBindReclaimed);

            private readonly InstanceHandle _initialInstanceHandle;
            private readonly Stack<IEnumerator<InstancePersistenceCommand>> _executionStack;
            private readonly TimeoutHelper _timeoutHelper;
            private readonly ExecuteAsyncResult _priorAsyncResult;

            private InstancePersistenceContext _context;
            private IEnumerator<InstancePersistenceCommand> _currentExecution;
            private Action<InstancePersistenceContext> _cancellationHandler;
            private bool _executeCalledByCurrentCommand;

            private InstanceView _finalState;

            public ExecuteAsyncResult(InstanceHandle initialInstanceHandle, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
                : this(command, timeout, callback, state)
            {
                _initialInstanceHandle = initialInstanceHandle;

                OnCompleting = new Action<AsyncResult, Exception>(SimpleCleanup);

                IAsyncResult result = _initialInstanceHandle.BeginAcquireExecutionContext(_timeoutHelper.RemainingTime(), PrepareAsyncCompletion(s_onAcquireContext), this);
                if (result.CompletedSynchronously)
                {
                    // After this stage, must complete explicitly in order to get Cleanup to run correctly.
                    bool completeSelf = false;
                    Exception completionException = null;
                    try
                    {
                        completeSelf = OnAcquireContext(result);
                    }
                    catch (Exception exception)
                    {
                        if (Fx.IsFatal(exception))
                        {
                            throw;
                        }
                        completeSelf = true;
                        completionException = exception;
                    }
                    if (completeSelf)
                    {
                        Complete(true, completionException);
                    }
                }
            }

            public ExecuteAsyncResult(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
                : this(command, timeout, callback, state)
            {
                _context = context;

                _priorAsyncResult = _context.LastAsyncResult;
                Fx.Assert(_priorAsyncResult != null, "The LastAsyncResult should already have been checked.");
                _priorAsyncResult._executeCalledByCurrentCommand = true;

                OnCompleting = new Action<AsyncResult, Exception>(SimpleCleanup);

                bool completeSelf = false;
                bool success = false;
                try
                {
                    _context.LastAsyncResult = this;
                    if (RunLoop())
                    {
                        completeSelf = true;
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        _context.LastAsyncResult = _priorAsyncResult;
                    }
                }
                if (completeSelf)
                {
                    Complete(true);
                }
            }

            [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", CancelDeclaringType = typeof(InstancePersistenceContext))]
            public ExecuteAsyncResult(InstanceHandle initialInstanceHandle, InstancePersistenceCommand command, TimeSpan timeout)
                : this(command, timeout, null, null)
            {
                _initialInstanceHandle = initialInstanceHandle;
                _context = _initialInstanceHandle.AcquireExecutionContext(_timeoutHelper.RemainingTime());

                Exception completionException = null;
                try
                {
                    // After this stage, must complete explicitly in order to get Cleanup to run correctly.
                    _context.RootAsyncResult = this;
                    _context.LastAsyncResult = this;
                    OnCompleting = new Action<AsyncResult, Exception>(Cleanup);

                    RunLoopCore(true);

                    DoWaitForTransaction();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    completionException = exception;
                }
                Complete(true, completionException);
            }

            [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", CancelDeclaringType = typeof(InstancePersistenceContext))]
            public ExecuteAsyncResult(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout)
                : this(command, timeout, null, null)
            {
                _context = context;

                _priorAsyncResult = _context.LastAsyncResult;
                Fx.Assert(_priorAsyncResult != null, "The LastAsyncResult should already have been checked.");
                _priorAsyncResult._executeCalledByCurrentCommand = true;

                bool success = false;
                try
                {
                    _context.LastAsyncResult = this;
                    RunLoopCore(true);
                    success = true;
                }
                finally
                {
                    _context.LastAsyncResult = _priorAsyncResult;
                    if (!success && _context.IsHandleDoomedByRollback)
                    {
                        _context.InstanceHandle.Free();
                    }
                }
                Complete(true);
            }

            private ExecuteAsyncResult(InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _executionStack = new Stack<IEnumerator<InstancePersistenceCommand>>(2);
                _timeoutHelper = new TimeoutHelper(timeout);

                _currentExecution = (new List<InstancePersistenceCommand> { command }).GetEnumerator();
            }

            internal InstancePersistenceCommand CurrentCommand { get; private set; }

            internal Action<InstancePersistenceContext> CancellationHandler
            {
                get
                {
                    Action<InstancePersistenceContext> handler = _cancellationHandler;
                    ExecuteAsyncResult current = this;
                    while (handler == null)
                    {
                        current = current._priorAsyncResult;
                        if (current == null)
                        {
                            break;
                        }
                        handler = current._cancellationHandler;
                    }
                    return handler;
                }

                set
                {
                    _cancellationHandler = value;
                }
            }

            [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", CancelDeclaringType = typeof(InstancePersistenceContext), Conditional = "!result.IsCOmpleted")]
            public static InstanceView End(IAsyncResult result)
            {
                ExecuteAsyncResult thisPtr = End<ExecuteAsyncResult>(result);
                Fx.Assert((thisPtr._finalState == null) == (thisPtr._initialInstanceHandle == null), "Should have thrown an exception if this is null on the outer result.");
                return thisPtr._finalState;
            }

            private static bool OnAcquireContext(IAsyncResult result)
            {
                ExecuteAsyncResult thisPtr = (ExecuteAsyncResult)result.AsyncState;
                thisPtr._context = thisPtr._initialInstanceHandle.EndAcquireExecutionContext(result);
                thisPtr._context.RootAsyncResult = thisPtr;
                thisPtr._context.LastAsyncResult = thisPtr;
                thisPtr.OnCompleting = new Action<AsyncResult, Exception>(thisPtr.Cleanup);
                return thisPtr.RunLoop();
            }
            [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", CancelDeclaringType = typeof(InstancePersistenceContext), Conditional = "synchronous")]

            private bool RunLoopCore(bool synchronous)
            {
                while (_currentExecution != null)
                {
                    if (_currentExecution.MoveNext())
                    {
                        bool isFirstCommand = CurrentCommand == null;
                        _executeCalledByCurrentCommand = false;
                        CurrentCommand = _currentExecution.Current;

                        Fx.Assert(isFirstCommand || _executionStack.Count > 0, "The first command should always remain at the top of the stack.");

                        //if (isFirstCommand)
                        //{
                        //    if (this.priorAsyncResult != null)
                        //    {
                        //        if (this.priorAsyncResult.CurrentCommand.IsTransactionEnlistmentOptional && !CurrentCommand.IsTransactionEnlistmentOptional)
                        //        {
                        //            throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotInvokeTransactionalFromNonTransactional));
                        //        }
                        //    }
                        //}
                        //else if (this.executionStack.Peek().Current.IsTransactionEnlistmentOptional)
                        //{
                        //    if (!CurrentCommand.IsTransactionEnlistmentOptional)
                        //    {
                        //        throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotInvokeTransactionalFromNonTransactional));
                        //    }
                        //}

                        // Intentionally calling MayBindLockToInstanceHandle prior to Validate.  This is a publically visible order.
                        bool mayBindLockToInstanceHandle = CurrentCommand.AutomaticallyAcquiringLock;
                        CurrentCommand.Validate(_context.InstanceView);

                        if (mayBindLockToInstanceHandle)
                        {
                            if (isFirstCommand)
                            {
                                if (_priorAsyncResult != null)
                                {
                                    if (!_priorAsyncResult.CurrentCommand.AutomaticallyAcquiringLock)
                                    {
                                        throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotInvokeBindingFromNonBinding));
                                    }
                                }
                                else if (!_context.InstanceView.IsBoundToInstanceOwner)
                                {
                                    throw Fx.Exception.AsError(new InvalidOperationException(SR.MayBindLockCommandShouldValidateOwner));
                                }
                                else if (!_context.InstanceView.IsBoundToLock)
                                {
                                    // This is the first command in the set and it may lock, so we must start the bind.
                                    _context.InstanceHandle.StartPotentialBind();
                                }
                            }
                            else if (!_executionStack.Peek().Current.AutomaticallyAcquiringLock)
                            {
                                throw Fx.Exception.AsError(new InvalidOperationException(SR.CannotInvokeBindingFromNonBinding));
                            }
                        }

                        if (_context.CancelRequested)
                        {
                            throw Fx.Exception.AsError(new OperationCanceledException(SR.HandleFreed));
                        }

                        BindReclaimedLockException bindReclaimedLockException = null;
                        if (synchronous)
                        {
                            bool commandProcessed;
                            try
                            {
                                commandProcessed = _context.InstanceHandle.Store.TryCommand(_context, CurrentCommand, _timeoutHelper.RemainingTime());
                            }
                            catch (BindReclaimedLockException exception)
                            {
                                bindReclaimedLockException = exception;
                                commandProcessed = true;
                            }

                            AfterCommand(commandProcessed);
                            if (bindReclaimedLockException != null)
                            {
                                BindReclaimed(!bindReclaimedLockException.MarkerWaitHandle.Wait(_timeoutHelper.RemainingTime()));
                            }
                        }
                        else
                        {
                            IAsyncResult result;
                            try
                            {
                                result = _context.InstanceHandle.Store.BeginTryCommand(_context, CurrentCommand, _timeoutHelper.RemainingTime(), PrepareAsyncCompletion(s_onTryCommand), this);
                            }
                            catch (BindReclaimedLockException exception)
                            {
                                bindReclaimedLockException = exception;
                                result = null;
                            }

                            if (result == null)
                            {
                                AfterCommand(true);
                                if (!bindReclaimedLockException.MarkerWaitHandle.WaitAsync(s_onBindReclaimed, this, _timeoutHelper.RemainingTime()))
                                {
                                    return false;
                                }
                                BindReclaimed(false);
                            }
                            else
                            {
                                if (!CheckSyncContinue(result) || !DoEndCommand(result))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    else if (_executionStack.Count > 0)
                    {
                        _currentExecution = _executionStack.Pop();
                    }
                    else
                    {
                        _currentExecution = null;
                    }
                }

                CurrentCommand = null;
                return true;
            }

            private bool RunLoop()
            {
                if (!RunLoopCore(false))
                {
                    return false;
                }

                // If this is an inner command, return true right away to continue this execution episode in a different async result.
                if (_initialInstanceHandle == null)
                {
                    return true;
                }

                return DoWaitForTransaction();
            }

            private static bool OnTryCommand(IAsyncResult result)
            {
                ExecuteAsyncResult thisPtr = (ExecuteAsyncResult)result.AsyncState;
                return thisPtr.DoEndCommand(result) && thisPtr.RunLoop();
            }
            [Fx.Tag.GuaranteeNonBlocking]

            private bool DoEndCommand(IAsyncResult result)
            {
                bool commandProcessed;
                BindReclaimedLockException bindReclaimedLockException = null;
                try
                {
                    commandProcessed = _context.InstanceHandle.Store.EndTryCommand(result);
                }
                catch (BindReclaimedLockException exception)
                {
                    bindReclaimedLockException = exception;
                    commandProcessed = true;
                }
                AfterCommand(commandProcessed);
                if (bindReclaimedLockException != null)
                {
                    if (!bindReclaimedLockException.MarkerWaitHandle.WaitAsync(s_onBindReclaimed, this, _timeoutHelper.RemainingTime()))
                    {
                        return false;
                    }
                    BindReclaimed(false);
                }
                return true;
            }

            private void AfterCommand(bool commandProcessed)
            {
                if (!ReferenceEquals(_context.LastAsyncResult, this))
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.ExecuteMustBeNested));
                }
                if (!commandProcessed)
                {
                    if (_executeCalledByCurrentCommand)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.TryCommandCannotExecuteSubCommandsAndReduce));
                    }
                    IEnumerable<InstancePersistenceCommand> reduction = CurrentCommand.Reduce(_context.InstanceView);
                    if (reduction == null)
                    {
                        throw Fx.Exception.AsError(new NotSupportedException(SR.ProviderDoesNotSupportCommand(CurrentCommand.Name)));
                    }
                    _executionStack.Push(_currentExecution);
                    _currentExecution = reduction.GetEnumerator();
                }
            }

            private static void OnBindReclaimed(object state, TimeoutException timeoutException)
            {
                ExecuteAsyncResult thisPtr = (ExecuteAsyncResult)state;

                bool completeSelf;
                Exception completionException = null;
                try
                {
                    thisPtr.BindReclaimed(timeoutException != null);
                    completeSelf = thisPtr.RunLoop();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    completionException = exception;
                    completeSelf = true;
                }
                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            private void BindReclaimed(bool timedOut)
            {
                if (timedOut)
                {
                    _context.InstanceHandle.CancelReclaim(new TimeoutException(SR.TimedOutWaitingForLockResolution));
                }
                _context.ConcludeBindReclaimedLockHelper();

                // If we get here, the reclaim attempt succeeded and we own the lock - but we are in the
                // CreateBindReclaimedLockException path, which auto-cancels on success.
                _context.InstanceHandle.Free();
                throw Fx.Exception.AsError(new OperationCanceledException(SR.BindReclaimSucceeded));
            }
            [Fx.Tag.Blocking(CancelMethod = "NotifyHandleFree", CancelDeclaringType = typeof(InstancePersistenceContext), Conditional = "synchronous")]

            private bool DoWaitForTransaction()
            {
                // If we get here, there's no transaction at all.  Need to "commit" the intermediate state.
                CommitHelper();
                if (_finalState == null)
                {
                    _context.InstanceHandle.Free();
                    throw Fx.Exception.AsError(new InstanceHandleConflictException(null, _context.InstanceView.InstanceId));
                }
                return true;
            }

            private void CommitHelper()
            {
                _finalState = _context.InstanceHandle.Commit(_context.InstanceView);
            }

            private void SimpleCleanup(AsyncResult result, Exception exception)
            {
                if (_initialInstanceHandle == null)
                {
                    Fx.Assert(_priorAsyncResult != null, "In the non-outer case, we should always have a priorAsyncResult here, since we set it before assigning OnComplete.");
                    _context.LastAsyncResult = _priorAsyncResult;
                }
                if (exception != null)
                {
                    if (_context != null && _context.IsHandleDoomedByRollback)
                    {
                        _context.InstanceHandle.Free();
                    }
                    else if (exception is TimeoutException || exception is OperationCanceledException)
                    {
                        if (_context == null)
                        {
                            _initialInstanceHandle.Free();
                        }
                        else
                        {
                            _context.InstanceHandle.Free();
                        }
                    }
                }
            }

            private void Cleanup(AsyncResult result, Exception exception)
            {
                try
                {
                    SimpleCleanup(result, exception);
                }
                finally
                {
                    Fx.AssertAndThrowFatal(_context.Active, "Out-of-sync between InstanceExecutionContext and ExecutionAsyncResult.");

                    _context.LastAsyncResult = null;
                    _context.RootAsyncResult = null;
                    _context.InstanceHandle.ReleaseExecutionContext();
                }
            }
        }

        private class BindReclaimedLockAsyncResult : AsyncResult
        {
            private static Action<object, TimeoutException> s_waitComplete = new Action<object, TimeoutException>(OnWaitComplete);

            private readonly InstancePersistenceContext _context;

            public BindReclaimedLockAsyncResult(InstancePersistenceContext context, AsyncWaitHandle wait, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _context = context;

                if (wait.WaitAsync(s_waitComplete, this, timeout))
                {
                    _context.ConcludeBindReclaimedLockHelper();
                    Complete(true);
                }
            }

            private static void OnWaitComplete(object state, TimeoutException timeoutException)
            {
                BindReclaimedLockAsyncResult thisPtr = (BindReclaimedLockAsyncResult)state;

                Exception completionException = null;
                try
                {
                    if (timeoutException != null)
                    {
                        thisPtr._context.InstanceHandle.CancelReclaim(new TimeoutException(SR.TimedOutWaitingForLockResolution));
                    }
                    thisPtr._context.ConcludeBindReclaimedLockHelper();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    completionException = exception;
                }
                thisPtr.Complete(false, completionException);
            }

            public static void End(IAsyncResult result)
            {
                End<BindReclaimedLockAsyncResult>(result);
            }
        }

        //[Serializable]
        private class BindReclaimedLockException : Exception
        {
            public BindReclaimedLockException()
            {
            }

            internal BindReclaimedLockException(AsyncWaitHandle markerWaitHandle)
                : base(SR.BindReclaimedLockException)
            {
                MarkerWaitHandle = markerWaitHandle;
            }

            internal AsyncWaitHandle MarkerWaitHandle { get; private set; }

            //[SecurityCritical]
            //protected BindReclaimedLockException(SerializationInfo info, StreamingContext context)
            //    : base(info, context)
            //{
            //}
        }
    }
}

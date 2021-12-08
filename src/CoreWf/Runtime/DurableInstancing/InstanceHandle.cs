// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceHandle
    {
        [Fx.Tag.SynchronizationObject(Blocking = false)]
        private readonly object _thisLock = new object();

        private object _providerObject;
        private bool _providerObjectSet;
        private bool _needFreedNotification;
        private InstanceHandleReference _inProgressBind;
        private WaitForEventsAsyncResult _waitResult;
        private HashSet<XName> _boundOwnerEvents;
        private HashSet<InstancePersistenceEvent> _pendingOwnerEvents;
        private EventTraceActivity _eventTraceActivity;

        // Fields used to implement an atomic Guid Id get/set property.
        private Guid _id;
        private volatile bool _idIsSet;

        internal InstanceHandle(InstanceStore store, InstanceOwner owner)
        {
            Fx.Assert(store != null, "Shouldn't be possible.");

            Version = -1;
            Store = store;
            Owner = owner;
            View = new InstanceView(owner);
            IsValid = true;
        }

        internal InstanceHandle(InstanceStore store, InstanceOwner owner, Guid instanceId)
        {
            Fx.Assert(store != null, "Shouldn't be possible here either.");
            Fx.Assert(instanceId != Guid.Empty, "Should be validating this.");

            Version = -1;
            Store = store;
            Owner = owner;
            Id = instanceId;
            View = new InstanceView(owner, instanceId);
            IsValid = true;
            if (Fx.IsEtwProviderEnabled)
            {
                _eventTraceActivity = new EventTraceActivity(instanceId);
            }
        }


        public bool IsValid { get; private set; }


        internal InstanceView View { get; private set; }
        internal InstanceStore Store { get; private set; }

        internal InstanceOwner Owner { get; private set; }

        // Since writing to a Guid field is not atomic, we need synchronization between reading and writing. The idIsSet boolean field can only
        // appear true once the id field is completely written due to the memory barriers implied by the reads and writes to a volatile field.
        // Writes to bool fields are atomic, and this property is only written to once. By checking the bool prior to reading the Guid, we can
        // be sure that the Guid is fully materialized when read.
        internal Guid Id
        {
            get
            {
                // this.idIsSet is volatile.
                if (!_idIsSet)
                {
                    return Guid.Empty;
                }
                return _id;
            }

            private set
            {
                Fx.Assert(value != Guid.Empty, "Cannot set an empty Id.");
                Fx.Assert(_id == Guid.Empty, "Cannot set Id more than once.");
                Fx.Assert(!_idIsSet, "idIsSet out of sync with id.");

                _id = value;

                if (Fx.IsEtwProviderEnabled)
                {
                    _eventTraceActivity = new EventTraceActivity(value);
                }

                // this.isIdSet is volatile.
                _idIsSet = true;
            }
        }

        internal long Version { get; private set; }

        internal InstanceHandle ConflictingHandle { get; set; }

        internal object ProviderObject
        {
            get
            {
                return _providerObject;
            }
            set
            {
                _providerObject = value;
                _providerObjectSet = true;
            }
        }

        internal EventTraceActivity EventTraceActivity
        {
            get
            {
                return _eventTraceActivity;
            }
        }

        private bool OperationPending { get; set; }
        private bool TooLateToEnlist { get; set; }
        private AcquireContextAsyncResult AcquirePending { get; set; }
        private InstancePersistenceContext CurrentExecutionContext { get; set; }

        private object ThisLock
        {
            get
            {
                return _thisLock;
            }
        }


        public void Free()
        {
            if (!_providerObjectSet)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(SR.HandleFreedBeforeInitialized));
            }

            if (!IsValid)
            {
                return;
            }

            List<InstanceHandleReference> handlesPendingResolution = null;
            WaitForEventsAsyncResult resultToCancel = null;

            try
            {
                bool needNotification = false;
                InstancePersistenceContext currentContext = null;

                lock (ThisLock)
                {
                    if (!IsValid)
                    {
                        return;
                    }
                    IsValid = false;

                    IEnumerable<XName> eventsToUnbind = null;
                    if (_pendingOwnerEvents != null && _pendingOwnerEvents.Count > 0)
                    {
                        eventsToUnbind = _pendingOwnerEvents.Select(persistenceEvent => persistenceEvent.Name);
                    }
                    if (_boundOwnerEvents != null && _boundOwnerEvents.Count > 0)
                    {
                        eventsToUnbind = eventsToUnbind == null ? _boundOwnerEvents : eventsToUnbind.Concat(_boundOwnerEvents);
                    }
                    if (eventsToUnbind != null)
                    {
                        Fx.Assert(Owner != null, "How do we have owner events without an owner.");
                        Store.RemoveHandleFromEvents(this, eventsToUnbind, Owner);
                    }
                    if (_waitResult != null)
                    {
                        resultToCancel = _waitResult;
                        _waitResult = null;
                    }

                    if (OperationPending)
                    {
                        if (AcquirePending != null)
                        {
                            _needFreedNotification = true;
                        }
                        else
                        {
                            // Here, just notify the currently executing command.
                            Fx.Assert(CurrentExecutionContext != null, "Must have either this or AcquirePending set.");
                            currentContext = CurrentExecutionContext;
                        }
                    }
                    else
                    {
                        needNotification = true;

                        if (_inProgressBind != null)
                        {
                            Owner.CancelBind(ref _inProgressBind, ref handlesPendingResolution);
                        }
                        else if (Version != -1)
                        {
                            // This means the handle was successfully bound in the past.  Need to remove it from the table of handles.
                            Owner.Unbind(this);
                        }
                    }
                }

                if (currentContext != null)
                {
                    // Need to do this not in a lock.
                    currentContext.NotifyHandleFree();

                    lock (ThisLock)
                    {
                        if (OperationPending)
                        {
                            _needFreedNotification = true;

                            // Cancel any pending lock reclaim here.
                            if (_inProgressBind != null)
                            {
                                Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock in CancelReclaim.");

                                // Null reason defaults to OperationCanceledException.  (Defer creating it since this might not be a
                                // reclaim attempt, but we don't know until we take the HandlesLock.)
                                Owner.FaultBind(ref _inProgressBind, ref handlesPendingResolution, null);
                            }
                        }
                        else
                        {
                            needNotification = true;
                        }
                    }
                }

                if (needNotification)
                {
                    Store.FreeInstanceHandle(this, ProviderObject);
                }
            }
            finally
            {
                if (resultToCancel != null)
                {
                    resultToCancel.Canceled();
                }

                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        internal void BindOwnerEvent(InstancePersistenceEvent persistenceEvent)
        {
            lock (ThisLock)
            {
                Fx.Assert(OperationPending, "Should only be called during an operation.");
                Fx.Assert(AcquirePending == null, "Should only be called after acquiring the transaction.");
                Fx.Assert(Owner != null, "Must be bound to owner to have an owner-scoped event.");

                if (IsValid && (_boundOwnerEvents == null || !_boundOwnerEvents.Contains(persistenceEvent.Name)))
                {
                    if (_pendingOwnerEvents == null)
                    {
                        _pendingOwnerEvents = new HashSet<InstancePersistenceEvent>();
                    }
                    else if (_pendingOwnerEvents.Contains(persistenceEvent))
                    {
                        return;
                    }
                    _pendingOwnerEvents.Add(persistenceEvent);
                    Store.PendHandleToEvent(this, persistenceEvent, Owner);
                }
            }
        }

        internal void StartPotentialBind()
        {
            lock (ThisLock)
            {
                Fx.AssertAndThrow(Version == -1, "Handle already bound to a lock.");

                Fx.Assert(OperationPending, "Should only be called during an operation.");
                Fx.Assert(AcquirePending == null, "Should only be called after acquiring the transaction.");
                Fx.Assert(_inProgressBind == null, "StartPotentialBind should only be called once per command.");
                Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock.");

                Owner.StartBind(this, ref _inProgressBind);
            }
        }

        internal void BindOwner(InstanceOwner owner)
        {
            Fx.Assert(owner != null, "Null owner passed to BindOwner.");

            lock (ThisLock)
            {
                Fx.Assert(_inProgressBind == null, "How did we get a bind in progress without an owner?");

                Fx.Assert(Owner == null, "BindOwner called when we already have an owner.");
                Owner = owner;
            }
        }

        internal void BindInstance(Guid instanceId)
        {
            Fx.Assert(instanceId != Guid.Empty, "BindInstance called with empty Guid.");

            List<InstanceHandleReference> handlesPendingResolution = null;
            try
            {
                lock (ThisLock)
                {
                    Fx.Assert(Id == Guid.Empty, "Instance already boud in BindInstance.");
                    Id = instanceId;

                    Fx.Assert(OperationPending, "BindInstance should only be called during an operation.");
                    Fx.Assert(AcquirePending == null, "BindInstance should only be called after acquiring the transaction.");
                    if (_inProgressBind != null)
                    {
                        Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock.");
                        Owner.InstanceBound(ref _inProgressBind, ref handlesPendingResolution);
                    }
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        internal void Bind(long instanceVersion)
        {
            Fx.AssertAndThrow(instanceVersion >= 0, "Negative instanceVersion passed to Bind.");
            Fx.Assert(Owner != null, "Bind called before owner bound.");
            Fx.Assert(Id != Guid.Empty, "Bind called before instance bound.");

            lock (ThisLock)
            {
                Fx.AssertAndThrow(Version == -1, "This should only be reachable once per handle.");
                Version = instanceVersion;

                Fx.Assert(OperationPending, "Bind should only be called during an operation.");
                Fx.Assert(AcquirePending == null, "Bind should only be called after acquiring the transaction.");
                if (_inProgressBind == null)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.BindLockRequiresCommandFlag));
                }
            }
        }

        // Returns null if an InstanceHandleConflictException should be thrown.
        internal AsyncWaitHandle StartReclaim(long instanceVersion)
        {
            List<InstanceHandleReference> handlesPendingResolution = null;
            try
            {
                lock (ThisLock)
                {
                    Fx.AssertAndThrow(Version == -1, "StartReclaim should only be reachable if the lock hasn't been bound.");

                    Fx.Assert(OperationPending, "StartReclaim should only be called during an operation.");
                    Fx.Assert(AcquirePending == null, "StartReclaim should only be called after acquiring the transaction.");
                    if (_inProgressBind == null)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.BindLockRequiresCommandFlag));
                    }

                    Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock in StartReclaim.");
                    return Owner.InitiateLockResolution(instanceVersion, ref _inProgressBind, ref handlesPendingResolution);
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        // After calling this method, the caller doesn't need to wait for the wait handle to become set (but they can).
        internal void CancelReclaim(Exception reason)
        {
            List<InstanceHandleReference> handlesPendingResolution = null;
            try
            {
                lock (ThisLock)
                {
                    if (_inProgressBind == null)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.DoNotCompleteTryCommandWithPendingReclaim));
                    }

                    Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock in CancelReclaim.");
                    Owner.FaultBind(ref _inProgressBind, ref handlesPendingResolution, reason);
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        // Returns the false if an InstanceHandleConflictException should be thrown.
        internal bool FinishReclaim(ref long instanceVersion)
        {
            List<InstanceHandleReference> handlesPendingResolution = null;
            try
            {
                lock (ThisLock)
                {
                    if (_inProgressBind == null)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.DoNotCompleteTryCommandWithPendingReclaim));
                    }

                    Fx.Assert(Owner != null, "Must be bound to owner to have an inProgressBind for the lock in CancelReclaim.");
                    if (!Owner.FinishBind(ref _inProgressBind, ref instanceVersion, ref handlesPendingResolution))
                    {
                        return false;
                    }

                    Fx.AssertAndThrow(Version == -1, "Should only be able to set the version once per handle.");
                    Fx.AssertAndThrow(instanceVersion >= 0, "Incorrect version resulting from conflict resolution.");
                    Version = instanceVersion;
                    return true;
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        [Fx.Tag.Blocking(CancelMethod = "Free")]
        internal InstancePersistenceContext AcquireExecutionContext(TimeSpan timeout)
        {
            bool setOperationPending = false;
            InstancePersistenceContext result = null;
            try
            {
                result = AcquireContextAsyncResult.End(new AcquireContextAsyncResult(this, timeout, out setOperationPending));
                Fx.AssertAndThrow(result != null, "Null result returned from AcquireContextAsyncResult (synchronous).");
                return result;
            }
            finally
            {
                if (result == null && setOperationPending)
                {
                    FinishOperation();
                }
            }
        }

        internal IAsyncResult BeginAcquireExecutionContext(TimeSpan timeout, AsyncCallback callback, object state)
        {
            bool setOperationPending = false;
            IAsyncResult result = null;
            try
            {
                result = new AcquireContextAsyncResult(this, timeout, out setOperationPending, callback, state);
                return result;
            }
            finally
            {
                if (result == null && setOperationPending)
                {
                    FinishOperation();
                }
            }
        }

        [Fx.Tag.Blocking(CancelMethod = "Free", Conditional = "!result.IsCompleted")]
        internal InstancePersistenceContext EndAcquireExecutionContext(IAsyncResult result)
        {
            return AcquireContextAsyncResult.End(result);
        }

        internal void ReleaseExecutionContext()
        {
            Fx.Assert(OperationPending, "ReleaseExecutionContext called with no operation pending.");
            FinishOperation();
        }

        // Returns null if an InstanceHandleConflictException should be thrown.
        internal InstanceView Commit(InstanceView newState)
        {
            Fx.Assert(newState != null, "Null view passed to Commit.");
            newState.MakeReadOnly();
            View = newState;

            List<InstanceHandleReference> handlesPendingResolution = null;
            InstanceHandle handleToFree = null;
            List<InstancePersistenceEvent> normals = null;
            WaitForEventsAsyncResult resultToComplete = null;
            try
            {
                lock (ThisLock)
                {
                    if (_inProgressBind != null)
                    {
                        // If there's a Version, it should be committed.
                        if (Version != -1)
                        {
                            if (!Owner.TryCompleteBind(ref _inProgressBind, ref handlesPendingResolution, out handleToFree))
                            {
                                return null;
                            }
                        }
                        else
                        {
                            Fx.Assert(OperationPending, "Should have cancelled this bind in FinishOperation.");
                            Fx.Assert(AcquirePending == null, "Should not be in Commit during AcquirePending.");
                            Owner.CancelBind(ref _inProgressBind, ref handlesPendingResolution);
                        }
                    }

                    if (_pendingOwnerEvents != null && IsValid)
                    {
                        if (_boundOwnerEvents == null)
                        {
                            _boundOwnerEvents = new HashSet<XName>();
                        }

                        foreach (InstancePersistenceEvent persistenceEvent in _pendingOwnerEvents)
                        {
                            if (!_boundOwnerEvents.Add(persistenceEvent.Name))
                            {
                                Fx.Assert("Should not have conflicts between pending and bound events.");
                                continue;
                            }

                            InstancePersistenceEvent normal = Store.AddHandleToEvent(this, persistenceEvent, Owner);
                            if (normal != null)
                            {
                                if (normals == null)
                                {
                                    normals = new List<InstancePersistenceEvent>(_pendingOwnerEvents.Count);
                                }
                                normals.Add(normal);
                            }
                        }

                        _pendingOwnerEvents = null;

                        if (normals != null && _waitResult != null)
                        {
                            resultToComplete = _waitResult;
                            _waitResult = null;
                        }
                    }

                    return View;
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);

                // This is a convenience, it is not required for correctness.
                if (handleToFree != null)
                {
                    Fx.Assert(!ReferenceEquals(handleToFree, this), "Shouldn't have been told to free ourselves.");
                    handleToFree.Free();
                }

                if (resultToComplete != null)
                {
                    resultToComplete.Signaled(normals);
                }
            }
        }

        private void FinishOperation()
        {
            List<InstanceHandleReference> handlesPendingResolution = null;
            try
            {
                bool needNotification;
                lock (ThisLock)
                {
                    OperationPending = false;
                    AcquirePending = null;
                    CurrentExecutionContext = null;

                    // This means we could have bound the handle, but didn't - clear the state here.
                    if (_inProgressBind != null && (Version == -1 || !IsValid))
                    {
                        Owner.CancelBind(ref _inProgressBind, ref handlesPendingResolution);
                    }
                    else if (Version != -1 && !IsValid)
                    {
                        // This means the handle was successfully bound in the past.  Need to remove it from the table of handles.
                        Owner.Unbind(this);
                    }

                    needNotification = _needFreedNotification;
                    _needFreedNotification = false;
                }
                try
                {
                    if (needNotification)
                    {
                        Store.FreeInstanceHandle(this, ProviderObject);
                    }
                }
                finally
                {
                }
            }
            finally
            {
                InstanceOwner.ResolveHandles(handlesPendingResolution);
            }
        }

        //List<InstancePersistenceEvent> StartWaiting(WaitForEventsAsyncResult result, IOThreadTimer timeoutTimer, TimeSpan timeout)
        private List<InstancePersistenceEvent> StartWaiting(WaitForEventsAsyncResult result, DelayTimer timeoutTimer, TimeSpan timeout)
        {
            lock (ThisLock)
            {
                if (_waitResult != null)
                {
                    throw Fx.Exception.AsError(new InvalidOperationException(SR.WaitAlreadyInProgress));
                }
                if (!IsValid)
                {
                    throw Fx.Exception.AsError(new OperationCanceledException(SR.HandleFreed));
                }

                if (_boundOwnerEvents != null && _boundOwnerEvents.Count > 0)
                {
                    Fx.Assert(Owner != null, "How do we have owner events without an owner.");
                    List<InstancePersistenceEvent> readyEvents = Store.SelectSignaledEvents(_boundOwnerEvents, Owner);
                    if (readyEvents != null)
                    {
                        Fx.Assert(readyEvents.Count != 0, "Should not return a zero-length list.");
                        return readyEvents;
                    }
                }

                _waitResult = result;

                // This is done here to be under the lock.  That way it doesn't get canceled before it is set.
                if (timeoutTimer != null)
                {
                    timeoutTimer.Set(timeout);
                }

                return null;
            }
        }

        private bool CancelWaiting(WaitForEventsAsyncResult result)
        {
            lock (ThisLock)
            {
                Fx.Assert(result != null, "Null result passed to CancelWaiting.");
                if (!ReferenceEquals(_waitResult, result))
                {
                    return false;
                }
                _waitResult = null;
                return true;
            }
        }

        internal void EventReady(InstancePersistenceEvent persistenceEvent)
        {
            WaitForEventsAsyncResult resultToComplete = null;
            lock (ThisLock)
            {
                if (_waitResult != null)
                {
                    resultToComplete = _waitResult;
                    _waitResult = null;
                }
            }

            if (resultToComplete != null)
            {
                resultToComplete.Signaled(persistenceEvent);
            }
        }

        internal static IAsyncResult BeginWaitForEvents(InstanceHandle handle, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new WaitForEventsAsyncResult(handle, timeout, callback, state);
        }

        internal static List<InstancePersistenceEvent> EndWaitForEvents(IAsyncResult result)
        {
            return WaitForEventsAsyncResult.End(result);
        }

        private class AcquireContextAsyncResult : AsyncResult
        {
            private readonly InstanceHandle _handle;
            private readonly TimeoutHelper _timeoutHelper;

            private InstancePersistenceContext _executionContext;

            public AcquireContextAsyncResult(InstanceHandle handle, TimeSpan timeout, out bool setOperationPending, AsyncCallback callback, object state)
                : this(handle, timeout, out setOperationPending, false, callback, state)
            {
            }

            [Fx.Tag.Blocking(CancelMethod = "Free", CancelDeclaringType = typeof(InstanceHandle))]
            public AcquireContextAsyncResult(InstanceHandle handle, TimeSpan timeout, out bool setOperationPending)
                : this(handle, timeout, out setOperationPending, true, null, null)
            {
            }
            [Fx.Tag.Blocking(CancelMethod = "Free", CancelDeclaringType = typeof(InstanceHandle), Conditional = "synchronous")]

            private AcquireContextAsyncResult(InstanceHandle handle, TimeSpan timeout, out bool setOperationPending, bool synchronous, AsyncCallback callback, object state)
                            : base(callback, state)
            {
                // Need to report back to the caller whether or not we set OperationPending.
                setOperationPending = false;

                _handle = handle;
                _timeoutHelper = new TimeoutHelper(timeout);

                lock (_handle.ThisLock)
                {
                    if (!_handle.IsValid)
                    {
                        throw Fx.Exception.AsError(new OperationCanceledException(SR.HandleFreed));
                    }

                    if (_handle.OperationPending)
                    {
                        throw Fx.Exception.AsError(new InvalidOperationException(SR.CommandExecutionCannotOverlap));
                    }
                    setOperationPending = true;
                    _handle.OperationPending = true;
                }

                if (DoAfterTransaction())
                {
                    Complete(true);
                }
            }

            public AsyncWaitHandle WaitForHostTransaction { get; private set; }

            public static InstancePersistenceContext End(IAsyncResult result)
            {
                AcquireContextAsyncResult pThis = End<AcquireContextAsyncResult>(result);
                Fx.Assert(pThis._executionContext != null, "Somehow the execution context didn't get set.");
                return pThis._executionContext;
            }

            private bool DoAfterTransaction()
            {
                try
                {
                    lock (_handle.ThisLock)
                    {
                        if (!_handle.IsValid)
                        {
                            throw Fx.Exception.AsError(new OperationCanceledException(SR.HandleFreed));
                        }

                        _executionContext = new InstancePersistenceContext(_handle, _timeoutHelper.RemainingTime());

                        _handle.AcquirePending = null;
                        _handle.CurrentExecutionContext = _executionContext;
                        _handle.TooLateToEnlist = false;
                    }
                }
                finally
                {
                }

                return true;
            }

            private InstancePersistenceContext ReuseContext()
            {
                Fx.Assert(_executionContext != null, "ReuseContext called but there is no context.");

                _executionContext.PrepareForReuse();
                return _executionContext;
            }
        }

        private class WaitForEventsAsyncResult : AsyncResult
        {
            private static readonly Action<object> s_timeoutCallback = new Action<object>(OnTimeout);

            private readonly InstanceHandle _handle;
            private readonly TimeSpan _timeout;

            //IOThreadTimer timer;
            private DelayTimer _timer;

            private List<InstancePersistenceEvent> _readyEvents;

            internal WaitForEventsAsyncResult(InstanceHandle handle, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _handle = handle;
                _timeout = timeout;

                if (_timeout != TimeSpan.Zero && _timeout != TimeSpan.MaxValue)
                {
                    //this.timer = new IOThreadTimer(WaitForEventsAsyncResult.timeoutCallback, this, false);
                    _timer = new DelayTimer(s_timeoutCallback, this);
                }

                List<InstancePersistenceEvent> existingReadyEvents = _handle.StartWaiting(this, _timer, _timeout);
                if (existingReadyEvents == null)
                {
                    if (_timeout == TimeSpan.Zero)
                    {
                        _handle.CancelWaiting(this);
                        throw Fx.Exception.AsError(new TimeoutException(SR.WaitForEventsTimedOut(TimeSpan.Zero)));
                    }
                }
                else
                {
                    _readyEvents = existingReadyEvents;
                    Complete(true);
                }
            }

            internal void Signaled(InstancePersistenceEvent persistenceEvent)
            {
                Signaled(new List<InstancePersistenceEvent>(1) { persistenceEvent });
            }

            internal void Signaled(List<InstancePersistenceEvent> persistenceEvents)
            {
                if (_timer != null)
                {
                    _timer.Cancel();
                }
                _readyEvents = persistenceEvents;
                Complete(false);
            }

            internal void Canceled()
            {
                if (_timer != null)
                {
                    _timer.Cancel();
                }
                Complete(false, new OperationCanceledException(SR.HandleFreed));
            }

            private static void OnTimeout(object state)
            {
                WaitForEventsAsyncResult thisPtr = (WaitForEventsAsyncResult)state;
                if (thisPtr._handle.CancelWaiting(thisPtr))
                {
                    thisPtr.Complete(false, new TimeoutException(SR.WaitForEventsTimedOut(thisPtr._timeout)));
                }
            }

            internal static List<InstancePersistenceEvent> End(IAsyncResult result)
            {
                return End<WaitForEventsAsyncResult>(result)._readyEvents;
            }
        }
    }
}

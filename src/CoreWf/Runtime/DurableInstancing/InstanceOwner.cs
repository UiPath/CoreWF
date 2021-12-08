// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Xml.Linq;

namespace System.Activities.Runtime.DurableInstancing
{
    [Fx.Tag.XamlVisible(false)]
    public sealed class InstanceOwner
    {
        // These collections are synchronized by the HandlesLock.
        private readonly Dictionary<Guid, InstanceHandle> _boundHandles = new Dictionary<Guid, InstanceHandle>();
        private readonly Queue<InstanceHandleReference> _inProgressHandles = new Queue<InstanceHandleReference>();
        private readonly Dictionary<Guid, Queue<InstanceHandleReference>> _inProgressHandlesPerInstance = new Dictionary<Guid, Queue<InstanceHandleReference>>();

        // This is synchronized by the InstanceStore.
        private readonly Dictionary<XName, InstanceNormalEvent> _events = new Dictionary<XName, InstanceNormalEvent>(1);

        internal InstanceOwner(Guid ownerId, Guid lockToken)
        {
            InstanceOwnerId = ownerId;
            OwnerToken = lockToken;
        }

        public Guid InstanceOwnerId { get; private set; }

        internal Guid OwnerToken { get; private set; }

        internal Dictionary<XName, InstanceNormalEvent> Events
        {
            get
            {
                return _events;
            }
        }

        private object HandlesLock
        {
            get
            {
                return _boundHandles;
            }
        }

        private Dictionary<Guid, InstanceHandle> BoundHandles
        {
            get
            {
                return _boundHandles;
            }
        }

        private Queue<InstanceHandleReference> InProgressHandles
        {
            get
            {
                return _inProgressHandles;
            }
        }

        private Dictionary<Guid, Queue<InstanceHandleReference>> InProgressHandlesPerInstance
        {
            get
            {
                return _inProgressHandlesPerInstance;
            }
        }

        // This can be called to remove a handle from the BoundHandles table.  It should be called only after no more commands are in progress or could be made on the handle.
        internal void Unbind(InstanceHandle handle)
        {
            Fx.Assert(ReferenceEquals(this, handle.Owner), "Unbind called on the wrong owner for a handle.");
            Fx.Assert(handle.Id != Guid.Empty, "Unbind called on a handle not even bound to an instance.");

            lock (HandlesLock)
            {
                // The handle may have already been bumped - only remove it if it's still it.
                if (BoundHandles.TryGetValue(handle.Id, out InstanceHandle existingHandle) && ReferenceEquals(handle, existingHandle))
                {
                    BoundHandles.Remove(handle.Id);
                }
            }
        }

        // This doesn't check the bound handles, since one of the scenarios is to re-bind to an instance and kick out the stale handle.
        internal void StartBind(InstanceHandle handle, ref InstanceHandleReference reference)
        {
            Fx.Assert(ReferenceEquals(this, handle.Owner), "StartBind called on the wrong owner for a handle.");

            lock (HandlesLock)
            {
                Fx.Assert(reference == null, "Already have a bind in progress.");

                reference = new InstanceHandleReference(handle);
                EnqueueReference(reference);
            }
        }

        // This happens only when the transaction under which the handle was bound is committed.
        internal bool TryCompleteBind(ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution, out InstanceHandle handleToFree)
        {
            Fx.Assert(reference != null, "Bind wasn't registered - RegisterStartBind must be called.");
            Fx.Assert(reference.InstanceHandle != null, "Cannot cancel and complete a bind.");
            Fx.Assert(reference.InstanceHandle.Version != -1, "Handle state must be set first.");
            Fx.Assert(ReferenceEquals(this, reference.InstanceHandle.Owner), "TryCompleteBind called on the wrong owner for a handle.");
            Fx.Assert(!(reference is LockResolutionMarker) || ((LockResolutionMarker)reference).NonConflicting, "How did a Version get set if we're still resolving.");

            handleToFree = null;
            lock (HandlesLock)
            {
                try
                {
                    if (BoundHandles.TryGetValue(reference.InstanceHandle.Id, out InstanceHandle existingHandle))
                    {
                        Fx.AssertAndFailFast(!ReferenceEquals(existingHandle, reference.InstanceHandle), "InstanceStore lock state is not correct.");
                        if (existingHandle.Version <= 0 || reference.InstanceHandle.Version <= 0)
                        {
                            if (existingHandle.Version != 0 || reference.InstanceHandle.Version != 0)
                            {
                                throw Fx.Exception.AsError(new InvalidOperationException(SR.InvalidLockToken));
                            }

                            reference.InstanceHandle.ConflictingHandle = existingHandle;
                            return false;
                        }

                        if (existingHandle.Version > reference.InstanceHandle.Version)
                        {
                            reference.InstanceHandle.ConflictingHandle = existingHandle;
                            return false;
                        }

                        if (existingHandle.Version < reference.InstanceHandle.Version)
                        {
                            existingHandle.ConflictingHandle = reference.InstanceHandle;
                            handleToFree = existingHandle;
                            BoundHandles[reference.InstanceHandle.Id] = reference.InstanceHandle;
                            return true;
                        }

                        if (existingHandle.Version == reference.InstanceHandle.Version)
                        {
                            // This could be a case of amnesia (backup / restore).
                            throw Fx.Exception.AsError(new InvalidOperationException(SR.InstanceStoreBoundSameVersionTwice));
                        }

                        throw Fx.AssertAndThrow("All cases covered above.");
                    }
                    else
                    {
                        BoundHandles.Add(reference.InstanceHandle.Id, reference.InstanceHandle);
                        return true;
                    }
                }
                finally
                {
                    CancelReference(ref reference, ref handlesPendingResolution);
                }
            }
        }

        // This is called if we found an existing lock.  This handle doesn't own the lock, but it could claim it, if it can prove
        // that no other live handle owns it.  If this returns non-null, the outcome will be available later on the
        // InstanceHandleReference once the AsyncWaitHandle completes.  (Null indicates a conflict with another handle.)
        //
        // The instanceVersion reported here was read under the transaction, but not changed.  Either it was already committed, or it was written under
        // this transaction in a prior command on a different handle.  Due to the latter case, we treat it as dirty - we do not publish it or take
        // any visible action (such as dooming handles) based on its value.
        internal AsyncWaitHandle InitiateLockResolution(long instanceVersion, ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            Fx.Assert(reference != null, "Bind wasn't registered - RegisterStartBind must be called.");
            Fx.Assert(reference.InstanceHandle != null, "Cannot cancel and complete a bind.");
            Fx.Assert(reference.InstanceHandle.Id != Guid.Empty, "Must be bound to an instance already.");

            Fx.AssertAndThrow(!(reference is LockResolutionMarker), "InitiateLockResolution already called.");

            lock (HandlesLock)
            {
                InstanceHandleReference cancelReference = reference;
                LockResolutionMarker markerReference = null;
                try
                {
                    if (BoundHandles.TryGetValue(reference.InstanceHandle.Id, out InstanceHandle existingHandle))
                    {
                        Fx.AssertAndFailFast(!ReferenceEquals(existingHandle, reference.InstanceHandle), "InstanceStore lock state is not correct in InitiateLockResolution.");
                        if (existingHandle.Version <= 0 || instanceVersion <= 0)
                        {
                            if (existingHandle.Version != 0 || instanceVersion != 0)
                            {
                                throw Fx.Exception.AsError(new InvalidOperationException(SR.InvalidLockToken));
                            }

                            reference.InstanceHandle.ConflictingHandle = existingHandle;
                            return null;
                        }

                        if (existingHandle.Version >= instanceVersion)
                        {
                            reference.InstanceHandle.ConflictingHandle = existingHandle;
                            return null;
                        }
                    }

                    // Put a marker in the InProgressHandles.  If it makes it through, and there's still no conflicting handle,
                    // then the lock can be claimed at this version.  Only currently in-progress bindings have a chance of
                    // staking a stronger claim to the lock version (if the store actually acquired the lock for the handle).
                    markerReference = new LockResolutionMarker(reference.InstanceHandle, instanceVersion);
                    EnqueueReference(markerReference);
                    reference = markerReference;
                    Fx.Assert(markerReference.MarkerWaitHandle != null, "Null MarkerWaitHandle?");
                    return markerReference.MarkerWaitHandle;
                }
                finally
                {
                    if (!ReferenceEquals(markerReference, reference))
                    {
                        CancelReference(ref reference, ref handlesPendingResolution);
                        if (markerReference != null)
                        {
                            cancelReference = markerReference;
                            CancelReference(ref cancelReference, ref handlesPendingResolution);
                        }
                    }
                    else
                    {
                        CancelReference(ref cancelReference, ref handlesPendingResolution);
                    }
                }
            }
        }

        // Called when a handle is bound to an instance while the handle is in-progress for a lock.  This can progress the queue-states since
        // this once can move from the general queue to the per-instance queue.
        internal void InstanceBound(ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            Fx.Assert(reference != null, "InstanceBound called when no operation is in progress.");
            Fx.Assert(reference.InstanceHandle != null, "InstanceBound called after cancelling.");
            Fx.Assert(reference.InstanceHandle.Id != Guid.Empty, "InstanceBound called, but the handle isn't bound.");
            Fx.AssertAndThrow(!(reference is LockResolutionMarker), "InstanceBound called after trying to bind the lock version, which alredy required an instance.");

            lock (HandlesLock)
            {
                ProcessInProgressHandles(ref handlesPendingResolution);
            }
        }

        internal void CancelBind(ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            Fx.Assert(reference != null, "Bind not in progress.");
            Fx.Assert(reference.InstanceHandle != null, "Reference already canceled in CancelBind.");
            Fx.Assert(ReferenceEquals(this, reference.InstanceHandle.Owner), "CancelBind called on the wrong owner for a handle.");

            lock (HandlesLock)
            {
                CancelReference(ref reference, ref handlesPendingResolution);
            }
        }

        internal void FaultBind(ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution, Exception reason)
        {
            Fx.Assert(reference != null, "Bind not in progress in FaultBind.");
            Fx.Assert(reference.InstanceHandle != null, "Reference already canceled in FaultBind.");
            Fx.Assert(ReferenceEquals(this, reference.InstanceHandle.Owner), "FaultBind called on the wrong owner for a handle.");

            lock (HandlesLock)
            {
                if (reference is LockResolutionMarker marker && !marker.IsComplete)
                {
                    try
                    {
                        // Nothing to do here - following the patterns of dealing with handlesPendingResolution and setting NotifyMarkerComplete in a finally.
                    }
                    finally
                    {
                        marker.Reason = reason ?? new OperationCanceledException(SR.HandleFreed);
                        marker.NotifyMarkerComplete(false);

                        if (handlesPendingResolution == null)
                        {
                            handlesPendingResolution = new List<InstanceHandleReference>(1);
                        }
                        handlesPendingResolution.Add(marker);
                    }
                }
            }
        }

        internal bool FinishBind(ref InstanceHandleReference reference, ref long instanceVersion, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            Fx.Assert(reference != null, "Bind not in progress in FinishBind.");
            Fx.Assert(reference.InstanceHandle != null, "Reference already canceled in FinishBind.");
            Fx.Assert(ReferenceEquals(this, reference.InstanceHandle.Owner), "FinishBind called on the wrong owner for a handle.");
            Fx.Assert(reference is LockResolutionMarker, "Must have started reclaim in order to finish it.");

            lock (HandlesLock)
            {
                LockResolutionMarker marker = (LockResolutionMarker)reference;
                Fx.AssertAndThrow(marker.IsComplete, "Called FinishBind prematurely.");
                if (marker.NonConflicting)
                {
                    instanceVersion = marker.InstanceVersion;
                    return true;
                }

                try
                {
                    if (marker.Reason != null)
                    {
                        throw Fx.Exception.AsError(marker.Reason);
                    }
                    Fx.Assert(marker.ConflictingHandle != null, "Should either have a conflicting handle or a reason in the conflicting case.");
                    marker.InstanceHandle.ConflictingHandle = marker.ConflictingHandle;
                    return false;
                }
                finally
                {
                    CancelReference(ref reference, ref handlesPendingResolution);
                }
            }
        }

        // Must be called with HandlesLock held.
        private void CancelReference(ref InstanceHandleReference reference, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            Guid wasBoundToInstanceId = reference.InstanceHandle.Id;

            try
            {
                if (reference is LockResolutionMarker marker && !marker.IsComplete)
                {
                    if (handlesPendingResolution == null)
                    {
                        handlesPendingResolution = new List<InstanceHandleReference>(1);
                    }
                    handlesPendingResolution.Add(marker);
                }
            }
            finally
            {
                reference.Cancel();
                reference = null;
            }

            ProcessInProgressHandles(ref handlesPendingResolution);

            if (wasBoundToInstanceId != Guid.Empty)
            {
                if (InProgressHandlesPerInstance.TryGetValue(wasBoundToInstanceId, out Queue<InstanceHandleReference> instanceQueue))
                {
                    while (instanceQueue.Count > 0)
                    {
                        InstanceHandleReference handleRef = instanceQueue.Peek();
                        if (handleRef.InstanceHandle != null)
                        {
                            if (CheckOldestReference(handleRef, ref handlesPendingResolution))
                            {
                                break;
                            }
                        }
                        instanceQueue.Dequeue();
                    }

                    if (instanceQueue.Count == 0)
                    {
                        InProgressHandlesPerInstance.Remove(wasBoundToInstanceId);
                    }
                }
            }
        }

        // Must be called with HandlesLock held.
        // This process the top-level InProgressHandles queue, demuxing entries into the per-instance queues and completing markers.
        private void ProcessInProgressHandles(ref List<InstanceHandleReference> handlesPendingResolution)
        {
            while (InProgressHandles.Count > 0)
            {
                InstanceHandleReference handleRef = InProgressHandles.Peek();
                if (handleRef.InstanceHandle != null)
                {
                    if (handleRef.InstanceHandle.Id == Guid.Empty)
                    {
                        break;
                    }

                    if (!InProgressHandlesPerInstance.TryGetValue(handleRef.InstanceHandle.Id, out Queue<InstanceHandleReference> acceptingQueue))
                    {
                        if (CheckOldestReference(handleRef, ref handlesPendingResolution))
                        {
                            acceptingQueue = new Queue<InstanceHandleReference>(2);
                            acceptingQueue.Enqueue(handleRef);
                            InProgressHandlesPerInstance.Add(handleRef.InstanceHandle.Id, acceptingQueue);
                        }
                    }
                    else
                    {
                        // It's ok to enqueue first, then dequeue, to err on the side of duplicates.  Duplicates do not cause a problem.
                        acceptingQueue.Enqueue(handleRef);
                    }
                }
                InProgressHandles.Dequeue();
            }
        }

        // Must be called with HandlesLock held.
        private void EnqueueReference(InstanceHandleReference handleRef)
        {
            if (InProgressHandles.Count > 0)
            {
                InProgressHandles.Enqueue(handleRef);
            }
            else if (handleRef.InstanceHandle.Id != Guid.Empty)
            {
                if (!InProgressHandlesPerInstance.TryGetValue(handleRef.InstanceHandle.Id, out Queue<InstanceHandleReference> queue))
                {
                    queue = new Queue<InstanceHandleReference>(2);
                    InProgressHandlesPerInstance.Add(handleRef.InstanceHandle.Id, queue);
                }
                queue.Enqueue(handleRef);
            }
            else
            {
                InProgressHandles.Enqueue(handleRef);
            }
        }

        // Must be called with HandlesLock held.
        // This is called when a reference becomes the oldest in-progress reference for an instance.  This triggers the end of resolution for markers.
        // Returns false if the resolution failed, meaning that the marker can be removed.
        private bool CheckOldestReference(InstanceHandleReference handleRef, ref List<InstanceHandleReference> handlesPendingResolution)
        {
            if (!(handleRef is LockResolutionMarker marker) || marker.IsComplete)
            {
                return true;
            }

            bool returnValue = true;
            try
            {
                if (BoundHandles.TryGetValue(marker.InstanceHandle.Id, out InstanceHandle existingHandle))
                {
                    Fx.AssertAndFailFast(!ReferenceEquals(existingHandle, marker.InstanceHandle), "InstanceStore lock state is not correct in CheckOldestReference.");
                    if (existingHandle.Version <= 0 || marker.InstanceVersion <= 0)
                    {
                        if (existingHandle.Version != 0 || marker.InstanceVersion != 0)
                        {
                            marker.Reason = new InvalidOperationException(SR.InvalidLockToken);
                            returnValue = false;
                        }
                        else
                        {
                            marker.ConflictingHandle = existingHandle;
                            returnValue = false;
                        }
                    }
                    else if (existingHandle.Version >= marker.InstanceVersion)
                    {
                        marker.ConflictingHandle = existingHandle;
                        returnValue = false;
                    }
                }

                // No other handles have committed a bind to this or a higher version!  We are ok to do so, but it is still not committed, so we stay in queue.
                return returnValue;
            }
            finally
            {
                marker.NotifyMarkerComplete(returnValue);

                if (handlesPendingResolution == null)
                {
                    handlesPendingResolution = new List<InstanceHandleReference>(1);
                }
                handlesPendingResolution.Add(marker);
            }
        }

        // Must be called ouside InstanceHandle.ThisLock and HandlesLock.
        internal static void ResolveHandles(List<InstanceHandleReference> handlesPendingResolution)
        {
            if (handlesPendingResolution != null)
            {
                foreach (InstanceHandleReference handleRef in handlesPendingResolution)
                {
                    LockResolutionMarker marker = handleRef as LockResolutionMarker;
                    Fx.Assert(marker != null, "How did a non-marker get in here.");
                    marker.MarkerWaitHandle.Set();
                }
            }
        }

        private class LockResolutionMarker : InstanceHandleReference
        {
            private readonly AsyncWaitHandle _waitHandle = new AsyncWaitHandle(EventResetMode.ManualReset);

            internal LockResolutionMarker(InstanceHandle instanceHandle, long instanceVersion)
                : base(instanceHandle)
            {
                InstanceVersion = instanceVersion;
            }

            // This is signalled when the marker reaches the end of the queue.
            internal AsyncWaitHandle MarkerWaitHandle
            {
                get
                {
                    return _waitHandle;
                }
            }

            // The initial state of the attempt.
            internal long InstanceVersion { get; private set; }

            // State regarding a failed attempt which can be used to construct an exception.
            internal InstanceHandle ConflictingHandle { get; set; }
            internal Exception Reason { get; set; }

            // State about the success / failure of the attempt.
            internal bool IsComplete { get; private set; }
            internal bool NonConflicting { get; private set; }

            internal void NotifyMarkerComplete(bool success)
            {
                Fx.Assert(InstanceHandle != null, "NotifyNonConflicting called on a cancelled LockResolutionMarker.");
                NonConflicting = success;
                IsComplete = true;
            }
        }
    }
}

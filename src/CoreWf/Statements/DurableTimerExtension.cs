// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Hosting;
using System.Activities.Persistence;
using System.Activities.Runtime;
using System.Xml.Linq;

namespace System.Activities.Statements;

[Fx.Tag.XamlVisible(false)]
public class DurableTimerExtension : TimerExtension, IWorkflowInstanceExtension, IDisposable, ICancelable
{
    private WorkflowInstanceProxy _instance;
    private TimerTable _registeredTimers;
    private readonly Action<object> _onTimerFiredCallback;
    private readonly TimerPersistenceParticipant _timerPersistenceParticipant;
    private static readonly AsyncCallback onResumeBookmarkComplete = Fx.ThunkCallback(new AsyncCallback(OnResumeBookmarkComplete));
    private static readonly XName timerTableName = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("RegisteredTimers");
    private static readonly XName timerExpirationTimeName = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("TimerExpirationTime");
    private bool _isDisposed; 

    [Fx.Tag.SynchronizationObject()]
    private readonly object _thisLock;

    public DurableTimerExtension()
        : base()
    {
        _onTimerFiredCallback = new Action<object>(OnTimerFired);
        _thisLock = new object();
        _timerPersistenceParticipant = new TimerPersistenceParticipant(this);
        _isDisposed = false; 
    }

    private object ThisLock => _thisLock;

    internal Action<object> OnTimerFiredCallback => _onTimerFiredCallback;

    internal TimerTable RegisteredTimers
    {
        get
        {
            _registeredTimers ??= new TimerTable(this);
            return _registeredTimers;
        }
    }

    public virtual IEnumerable<object> GetAdditionalExtensions()
    {
        yield return _timerPersistenceParticipant;
    }

    public virtual void SetInstance(WorkflowInstanceProxy instance)
    {
        if (_instance != null && instance != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.TimerExtensionAlreadyAttached));
        }

        _instance = instance;
    }

    protected override void OnRegisterTimer(TimeSpan timeout, Bookmark bookmark)
    {
        // This lock is to synchronize with the Timer callback
        if (timeout < TimeSpan.MaxValue)
        {
            lock (ThisLock)
            {
                Fx.Assert(!_isDisposed, "DurableTimerExtension is already disposed, it cannot be used to register a new timer.");
                RegisteredTimers.AddTimer(timeout, bookmark);
            }
        }
    }

    protected override void OnCancelTimer(Bookmark bookmark)
    {
        // This lock is to synchronize with the Timer callback
        lock (ThisLock)
        {
            RegisteredTimers.RemoveTimer(bookmark);
        }
    }

    internal void OnSave(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
    {
        readWriteValues = null;
        writeOnlyValues = null;
            
        // Using a lock here to prevent the timer firing back without us being ready
        lock (ThisLock)
        {
            RegisteredTimers.MarkAsImmutable();
            if (_registeredTimers != null && _registeredTimers.Count > 0)
            {
                readWriteValues = new Dictionary<XName, object>(1);
                writeOnlyValues = new Dictionary<XName, object>(1);
                readWriteValues.Add(timerTableName, _registeredTimers);
                writeOnlyValues.Add(timerExpirationTimeName, _registeredTimers.GetNextDueTime());
            }
        }
    }

    internal void PersistenceDone()
    {
        lock (ThisLock)
        {
            RegisteredTimers.MarkAsMutable();
        }
    }

    internal void OnLoad(IDictionary<XName, object> readWriteValues)
    {
        lock (ThisLock)
        {
            if (readWriteValues != null && readWriteValues.TryGetValue(timerTableName, out object timerTable))
            {
                _registeredTimers = timerTable as TimerTable;
                Fx.Assert(RegisteredTimers != null, "Timer Table cannot be null");
                RegisteredTimers.OnLoad(this);
            }
        }
    }

    private void OnTimerFired(object state)
    {
        Bookmark timerBookmark = state as Bookmark;

        WorkflowInstanceProxy targetInstance = _instance;
        // it's possible that we've been unloaded while the timer was in the process of firing, in
        // which case targetInstance will be null
        if (targetInstance != null)
        {
            BookmarkResumptionResult resumptionResult;
            IAsyncResult result = null;
            bool completed = false;

            result = targetInstance.BeginResumeBookmark(timerBookmark, null, TimeSpan.MaxValue,
                onResumeBookmarkComplete, new BookmarkResumptionState(timerBookmark, this, targetInstance));
            completed = result.CompletedSynchronously; 

            if (completed && result != null)
            {
                try
                {
                    resumptionResult = targetInstance.EndResumeBookmark(result);
                    ProcessBookmarkResumptionResult(timerBookmark, resumptionResult);
                }
                catch (TimeoutException)
                {
                    ProcessBookmarkResumptionResult(timerBookmark, BookmarkResumptionResult.NotReady);
                }
            }
        }
    }

    private static void OnResumeBookmarkComplete(IAsyncResult result)
    {
        if (result.CompletedSynchronously)
        {
            return;
        }

        BookmarkResumptionState state = (BookmarkResumptionState)result.AsyncState;

        BookmarkResumptionResult resumptionResult = state.Instance.EndResumeBookmark(result);
        state.TimerExtension.ProcessBookmarkResumptionResult(state.TimerBookmark, resumptionResult);
    }

    private void ProcessBookmarkResumptionResult(Bookmark timerBookmark, BookmarkResumptionResult result)
    {
        switch (result)
        {
            case BookmarkResumptionResult.NotFound:
            case BookmarkResumptionResult.Success:
                // The bookmark is removed maybe due to WF cancel, abort or the bookmark succeeds
                // no need to keep the timer around
                lock (ThisLock)
                {
                    if (!_isDisposed)
                    {
                        RegisteredTimers.RemoveTimer(timerBookmark);
                    }
                }
                break;
            case BookmarkResumptionResult.NotReady:
                // The workflow maybe in one of these states: Completed, Aborted, Abandoned, unloading, Suspended
                // In the first 3 cases, we will let TimerExtension.CancelTimer take care of the cleanup.
                // In the 4th case, we want the timer to retry when it is loaded back, in all 4 cases we don't need to delete the timer 
                // In the 5th case, we want the timer to retry until it succeeds. 
                // Retry:
                lock (ThisLock)
                {
                    RegisteredTimers.RetryTimer(timerBookmark);
                }
                break;
        }
    }

    public void Dispose()
    {
        if (_registeredTimers != null)
        {
            lock (ThisLock)
            {
                _isDisposed = true; 
                _registeredTimers?.Dispose();
            }
        }
        GC.SuppressFinalize(this);
    }

    void ICancelable.Cancel() => Dispose();

    private class BookmarkResumptionState
    {
        public BookmarkResumptionState(Bookmark timerBookmark, DurableTimerExtension timerExtension, WorkflowInstanceProxy instance)
        {
            TimerBookmark = timerBookmark;
            TimerExtension = timerExtension;
            Instance = instance;
        }

        public Bookmark TimerBookmark { get; private set; }

        public DurableTimerExtension TimerExtension { get; private set; }

        public WorkflowInstanceProxy Instance { get; private set; }
    }

    private class TimerPersistenceParticipant : PersistenceIOParticipant
    {
        private readonly DurableTimerExtension _defaultTimerExtension;

        public TimerPersistenceParticipant(DurableTimerExtension timerExtension)
            : base(false, false)
        {
            _defaultTimerExtension = timerExtension;
        }

        protected override void CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
            => _defaultTimerExtension.OnSave(out readWriteValues, out writeOnlyValues);

        protected override void PublishValues(IDictionary<XName, object> readWriteValues)
            => _defaultTimerExtension.OnLoad(readWriteValues);

        protected override IAsyncResult BeginOnSave(IDictionary<XName, object> readWriteValues, IDictionary<XName, object> writeOnlyValues, TimeSpan timeout, AsyncCallback callback, object state)
        {
            _defaultTimerExtension.PersistenceDone();
            return base.BeginOnSave(readWriteValues, writeOnlyValues, timeout, callback, state);
        }

        protected override void Abort() => _defaultTimerExtension.PersistenceDone();
    }
}

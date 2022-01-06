// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections;

namespace System.Activities.Tracking;

internal class TrackingProvider
{
    private List<TrackingParticipant> _trackingParticipants;
    private Dictionary<TrackingParticipant, RuntimeTrackingProfile> _profileSubscriptions;
    private IList<TrackingRecord> _pendingTrackingRecords;
    private readonly Activity _definition;
    private bool _filterValuesSetExplicitly;
    private Hashtable _activitySubscriptions;
    private long _nextTrackingRecordNumber;

    public TrackingProvider(Activity definition)
    {
        _definition = definition;
        ShouldTrack = true;
        ShouldTrackActivityStateRecords = true;
        ShouldTrackActivityStateRecordsExecutingState = true;
        ShouldTrackActivityStateRecordsClosedState = true;
        ShouldTrackBookmarkResumptionRecords = true;
        ShouldTrackActivityScheduledRecords = true;
        ShouldTrackCancelRequestedRecords = true;
        ShouldTrackFaultPropagationRecords = true;
        ShouldTrackWorkflowInstanceRecords = true;
    }

    public bool HasPendingRecords => (_pendingTrackingRecords != null && _pendingTrackingRecords.Count > 0)
                || !_filterValuesSetExplicitly;

    public long NextTrackingRecordNumber => _nextTrackingRecordNumber;

    public bool ShouldTrack { get; private set; }

    public bool ShouldTrackWorkflowInstanceRecords { get; private set; }

    public bool ShouldTrackBookmarkResumptionRecords { get; private set; }

    public bool ShouldTrackActivityScheduledRecords { get; private set; }

    public bool ShouldTrackActivityStateRecords { get; private set; }

    public bool ShouldTrackActivityStateRecordsExecutingState { get; private set; }

    public bool ShouldTrackActivityStateRecordsClosedState { get; private set; }

    public bool ShouldTrackCancelRequestedRecords { get; private set; }

    public bool ShouldTrackFaultPropagationRecords { get; private set; }

    /// <remarks>
    /// We blindly do this.  On the off chance that a workflow causes it to loop back
    /// around it shouldn't cause the workflow to fail and the tracking information
    /// will still be salvagable.
    /// </remarks>
    private long GetNextRecordNumber() => _nextTrackingRecordNumber++;

    public void OnDeserialized(long nextTrackingRecordNumber) => _nextTrackingRecordNumber = nextTrackingRecordNumber;

    public void AddRecord(TrackingRecord record)
    {
        _pendingTrackingRecords ??= new List<TrackingRecord>();
        record.RecordNumber = GetNextRecordNumber();
        _pendingTrackingRecords.Add(record);
    }

    public void AddParticipant(TrackingParticipant participant)
    {
        if (_trackingParticipants == null)
        {
            _trackingParticipants = new List<TrackingParticipant>();
            _profileSubscriptions = new Dictionary<TrackingParticipant, RuntimeTrackingProfile>();
        }
        _trackingParticipants.Add(participant);
    }

    public void ClearParticipants()
    {
        _trackingParticipants = null;
        _profileSubscriptions = null;
    }

    public void FlushPendingRecords(TimeSpan timeout)
    {
        try
        {
            if (!HasPendingRecords)
            {
                return;
            }

            TimeoutHelper helper = new TimeoutHelper(timeout);
            for (int i = 0; i < _trackingParticipants.Count; i++)
            {
                TrackingParticipant participant = _trackingParticipants[i];
                RuntimeTrackingProfile runtimeProfile = GetRuntimeTrackingProfile(participant);

                // HasPendingRecords can be true for the sole purpose of populating our initial profiles, so check again here
                if (_pendingTrackingRecords == null)
                {
                    continue;
                }

                for (int j = 0; j < _pendingTrackingRecords.Count; j++)
                {
                    TrackingRecord currentRecord = _pendingTrackingRecords[j];
                    Fx.Assert(currentRecord != null, "We should never come across a null context.");
                    bool shouldClone = _trackingParticipants.Count > 1;

                    TrackingRecord preparedRecord;
                    if (runtimeProfile == null)
                    {
                        preparedRecord = shouldClone ? currentRecord.Clone() : currentRecord;
                    }
                    else
                    {
                        preparedRecord = runtimeProfile.Match(currentRecord, shouldClone);
                    }

                    if (preparedRecord != null)
                    {
                        participant.Track(preparedRecord, helper.RemainingTime());
                        if (TD.TrackingRecordRaisedIsEnabled())
                        {
                            TD.TrackingRecordRaised(preparedRecord.ToString(), participant.GetType().ToString());
                        }
                    }
                }
            }
        }
        finally
        {
            // Note that if we fail to track yet the workflow manages to recover
            // we will attempt to track those records again.
            ClearPendingRecords();
        }
    }

    public IAsyncResult BeginFlushPendingRecords(TimeSpan timeout, AsyncCallback callback, object state)
        => new FlushPendingRecordsAsyncResult(this, timeout, callback, state);

    public static void EndFlushPendingRecords(IAsyncResult result) => FlushPendingRecordsAsyncResult.End(result);

    public bool ShouldTrackActivity(string name)
        => _activitySubscriptions == null || _activitySubscriptions.ContainsKey(name) || _activitySubscriptions.ContainsKey("*");

    private void ClearPendingRecords()
    {
        if (_pendingTrackingRecords != null)
        {
            //since the number of records is small, it is faster to remove from end than to call List.Clear
            for (int i = _pendingTrackingRecords.Count - 1; i >= 0; i--)
            {
                _pendingTrackingRecords.RemoveAt(i);
            }
        }
    }

    private RuntimeTrackingProfile GetRuntimeTrackingProfile(TrackingParticipant participant)
    {
        TrackingProfile profile;

        if (!_profileSubscriptions.TryGetValue(participant, out RuntimeTrackingProfile runtimeProfile))
        {
            profile = participant.TrackingProfile;

            if (profile != null)
            {
                runtimeProfile = RuntimeTrackingProfile.GetRuntimeTrackingProfile(profile, _definition);
                Merge(runtimeProfile.Filter);

                //Add the names to the list of activities that have subscriptions.  This provides a quick lookup
                //for the runtime to check if a TrackingRecord has to be created. 
                IEnumerable<string> activityNames = runtimeProfile.GetSubscribedActivityNames();
                if (activityNames != null)
                {
                    _activitySubscriptions ??= new Hashtable();
                    foreach (string name in activityNames)
                    {
                        if (_activitySubscriptions[name] == null)
                        {
                            _activitySubscriptions[name] = name;
                        }
                    }
                }
            }
            else
            {
                //for null profiles, set all the filter flags. 
                Merge(new TrackingRecordPreFilter(true));
            }

            _profileSubscriptions.Add(participant, runtimeProfile);
        }
        return runtimeProfile;
    }

    private void Merge(TrackingRecordPreFilter filter)
    {
        if (!_filterValuesSetExplicitly)
        {
            // This it the first filter we are merging
            _filterValuesSetExplicitly = true;

            ShouldTrackActivityStateRecordsExecutingState = filter.TrackActivityStateRecordsExecutingState;
            ShouldTrackActivityScheduledRecords = filter.TrackActivityScheduledRecords;
            ShouldTrackActivityStateRecords = filter.TrackActivityStateRecords;
            ShouldTrackActivityStateRecordsClosedState = filter.TrackActivityStateRecordsClosedState;
            ShouldTrackBookmarkResumptionRecords = filter.TrackBookmarkResumptionRecords;
            ShouldTrackCancelRequestedRecords = filter.TrackCancelRequestedRecords;
            ShouldTrackFaultPropagationRecords = filter.TrackFaultPropagationRecords;
            ShouldTrackWorkflowInstanceRecords = filter.TrackWorkflowInstanceRecords;
        }
        else
        {
            ShouldTrackActivityStateRecordsExecutingState |= filter.TrackActivityStateRecordsExecutingState;
            ShouldTrackActivityScheduledRecords |= filter.TrackActivityScheduledRecords;
            ShouldTrackActivityStateRecords |= filter.TrackActivityStateRecords;
            ShouldTrackActivityStateRecordsClosedState |= filter.TrackActivityStateRecordsClosedState;
            ShouldTrackBookmarkResumptionRecords |= filter.TrackBookmarkResumptionRecords;
            ShouldTrackCancelRequestedRecords |= filter.TrackCancelRequestedRecords;
            ShouldTrackFaultPropagationRecords |= filter.TrackFaultPropagationRecords;
            ShouldTrackWorkflowInstanceRecords |= filter.TrackWorkflowInstanceRecords;
        }
    }

    private class FlushPendingRecordsAsyncResult : AsyncResult
    {
        private static readonly AsyncCompletion trackingCompleteCallback = new(OnTrackingComplete);
        private int _currentRecord;
        private int _currentParticipant;
        private TrackingProvider _provider;
        private TimeoutHelper _timeoutHelper;

        public FlushPendingRecordsAsyncResult(TrackingProvider provider, TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            _provider = provider;
            _timeoutHelper = new TimeoutHelper(timeout);

            if (RunLoop())
            {
                Complete(true);
            }
        }

        private bool RunLoop()
        {
            if (_provider.HasPendingRecords)
            {
                while (_currentParticipant < _provider._trackingParticipants.Count)
                {
                    TrackingParticipant participant = _provider._trackingParticipants[_currentParticipant];
                    RuntimeTrackingProfile runtimeProfile = _provider.GetRuntimeTrackingProfile(participant);

                    if (_provider._pendingTrackingRecords != null)
                    {
                        while (_currentRecord < _provider._pendingTrackingRecords.Count)
                        {
                            bool completedSynchronously = PostTrackingRecord(participant, runtimeProfile);
                            if (!completedSynchronously)
                            {
                                return false;
                            }
                        }
                    }

                    _currentRecord = 0;
                    _currentParticipant++;
                }
            }

            // We've now tracked all of the records.
            _provider.ClearPendingRecords();
            return true;
        }

        private static bool OnTrackingComplete(IAsyncResult result)
        {
            Fx.Assert(!result.CompletedSynchronously, "TrackingAsyncResult.OnTrackingComplete should not get called with a result that is CompletedSynchronously");

            FlushPendingRecordsAsyncResult thisPtr = (FlushPendingRecordsAsyncResult)result.AsyncState;
            TrackingParticipant participant = thisPtr._provider._trackingParticipants[thisPtr._currentParticipant];
            bool isSuccessful = false;
            try
            {
                participant.EndTrack(result);
                isSuccessful = true;
            }
            finally
            {
                if (!isSuccessful)
                {
                    thisPtr._provider.ClearPendingRecords();
                }
            }
            return thisPtr.RunLoop();
        }

        private bool PostTrackingRecord(TrackingParticipant participant, RuntimeTrackingProfile runtimeProfile)
        {
            TrackingRecord originalRecord = _provider._pendingTrackingRecords[_currentRecord];
            _currentRecord++;
            bool isSuccessful = false;

            try
            {
                TrackingRecord preparedRecord = null;
                bool shouldClone = _provider._trackingParticipants.Count > 1;
                if (runtimeProfile == null)
                {
                    preparedRecord = shouldClone ? originalRecord.Clone() : originalRecord;
                }
                else
                {
                    preparedRecord = runtimeProfile.Match(originalRecord, shouldClone);
                }

                if (preparedRecord != null)
                {
                    IAsyncResult result = participant.BeginTrack(preparedRecord, _timeoutHelper.RemainingTime(), PrepareAsyncCompletion(trackingCompleteCallback), this);
                    if (TD.TrackingRecordRaisedIsEnabled())
                    {
                        TD.TrackingRecordRaised(preparedRecord.ToString(), participant.GetType().ToString());
                    }
                    if (result.CompletedSynchronously)
                    {
                        participant.EndTrack(result);
                    }
                    else
                    {
                        isSuccessful = true;
                        return false;
                    }
                }
                isSuccessful = true;
            }
            finally
            {
                if (!isSuccessful)
                {
                    _provider.ClearPendingRecords();
                }
            }
            return true;
        }

        public static void End(IAsyncResult result) => End<FlushPendingRecordsAsyncResult>(result);
    }
}

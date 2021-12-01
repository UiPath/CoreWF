// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Activities.Tracking;

internal class RuntimeTrackingProfile
{
    private static RuntimeTrackingProfileCache profileCache;
    private List<ActivityScheduledQuery> _activityScheduledSubscriptions;
    private List<FaultPropagationQuery> _faultPropagationSubscriptions;
    private List<CancelRequestedQuery> _cancelRequestedSubscriptions;
    private Dictionary<string, HybridCollection<ActivityStateQuery>> _activitySubscriptions;
    private List<CustomTrackingQuery> _customTrackingQuerySubscriptions;
    private Dictionary<string, BookmarkResumptionQuery> _bookmarkSubscriptions;
    private Dictionary<string, WorkflowInstanceQuery> _workflowEventSubscriptions;
    private readonly TrackingProfile _associatedProfile;
    private readonly TrackingRecordPreFilter _trackingRecordPreFilter;
    private List<string> _activityNames;
    private readonly bool _isRootNativeActivity;

    internal RuntimeTrackingProfile(TrackingProfile profile, Activity rootElement)
    {
        _associatedProfile = profile;
        _isRootNativeActivity = rootElement is NativeActivity;
        _trackingRecordPreFilter = new TrackingRecordPreFilter();

        foreach (TrackingQuery query in _associatedProfile.Queries)
        {
            switch (query)
            {
                case ActivityStateQuery:
                    AddActivitySubscription((ActivityStateQuery)query);
                    break;
                case WorkflowInstanceQuery:
                    AddWorkflowSubscription((WorkflowInstanceQuery)query);
                    break;
                case BookmarkResumptionQuery:
                    AddBookmarkSubscription((BookmarkResumptionQuery)query);
                    break;
                case CustomTrackingQuery:
                    AddCustomTrackingSubscription((CustomTrackingQuery)query);
                    break;
                case ActivityScheduledQuery:
                    AddActivityScheduledSubscription((ActivityScheduledQuery)query);
                    break;
                case CancelRequestedQuery:
                    AddCancelRequestedSubscription((CancelRequestedQuery)query);
                    break;
                case FaultPropagationQuery:
                    AddFaultPropagationSubscription((FaultPropagationQuery)query);
                    break;
            }
        }
    }

    private static RuntimeTrackingProfileCache Cache
    {
        get
        {
            // We do not take a lock here because a true singleton is not required.
            profileCache ??= new RuntimeTrackingProfileCache();
            return profileCache;
        }
    }

    internal TrackingRecordPreFilter Filter => _trackingRecordPreFilter;

    internal IEnumerable<string> GetSubscribedActivityNames() => _activityNames;

    private bool ShouldTrackActivity(ActivityInfo activityInfo, string queryName)
    {
        if (activityInfo != null && queryName == "*")
        {
            if (_isRootNativeActivity)
            {
                if (activityInfo.Activity.MemberOf.ParentId != 0)
                {
                    return false;
                }
            }
            else
            {
                if ((activityInfo.Activity.MemberOf.ParentId != 0)
                    && (activityInfo.Activity.MemberOf.Parent.ParentId != 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AddActivityName(string name)
    {
        _activityNames ??= new List<string>();
        _activityNames.Add(name);
    }

    internal static RuntimeTrackingProfile GetRuntimeTrackingProfile(TrackingProfile profile, Activity rootElement)
        => Cache.GetRuntimeTrackingProfile(profile, rootElement);

    private void AddActivitySubscription(ActivityStateQuery query)
    {
        _trackingRecordPreFilter.TrackActivityStateRecords = true;

        foreach (string state in query.States)
        {
            if (string.CompareOrdinal(state, "*") == 0)
            {
                _trackingRecordPreFilter.TrackActivityStateRecordsClosedState = true;
                _trackingRecordPreFilter.TrackActivityStateRecordsExecutingState = true;
                break;
            }
            if (string.CompareOrdinal(state, ActivityStates.Closed) == 0)
            {
                _trackingRecordPreFilter.TrackActivityStateRecordsClosedState = true;
            }
            else if (string.CompareOrdinal(state, ActivityStates.Executing) == 0)
            {
                _trackingRecordPreFilter.TrackActivityStateRecordsExecutingState = true;
            }
        }

        _activitySubscriptions ??= new Dictionary<string, HybridCollection<ActivityStateQuery>>();
        if (!_activitySubscriptions.TryGetValue(query.ActivityName, out HybridCollection<ActivityStateQuery> subscription))
        {
            subscription = new HybridCollection<ActivityStateQuery>();
            _activitySubscriptions[query.ActivityName] = subscription;
        }
        subscription.Add(query);
        AddActivityName(query.ActivityName);
    }

    private void AddActivityScheduledSubscription(ActivityScheduledQuery activityScheduledQuery)
    {
        _trackingRecordPreFilter.TrackActivityScheduledRecords = true;
        _activityScheduledSubscriptions ??= new List<ActivityScheduledQuery>();
        _activityScheduledSubscriptions.Add(activityScheduledQuery);
    }

    private void AddCancelRequestedSubscription(CancelRequestedQuery cancelQuery)
    {
        _trackingRecordPreFilter.TrackCancelRequestedRecords = true;
        _cancelRequestedSubscriptions ??= new List<CancelRequestedQuery>();
        _cancelRequestedSubscriptions.Add(cancelQuery);
    }

    private void AddFaultPropagationSubscription(FaultPropagationQuery faultQuery)
    {
        _trackingRecordPreFilter.TrackFaultPropagationRecords = true;
        _faultPropagationSubscriptions ??= new List<FaultPropagationQuery>();
        _faultPropagationSubscriptions.Add(faultQuery);
    }

    private void AddBookmarkSubscription(BookmarkResumptionQuery bookmarkTrackingQuery)
    {
        _trackingRecordPreFilter.TrackBookmarkResumptionRecords = true;
        _bookmarkSubscriptions ??= new Dictionary<string, BookmarkResumptionQuery>();
        //if duplicates are found, use only the first subscription for a given bookmark name.
        _bookmarkSubscriptions.TryAdd(bookmarkTrackingQuery.Name, bookmarkTrackingQuery);
    }

    private void AddCustomTrackingSubscription(CustomTrackingQuery customQuery)
    {
        _customTrackingQuerySubscriptions ??= new List<CustomTrackingQuery>();
        _customTrackingQuerySubscriptions.Add(customQuery);
    }

    private void AddWorkflowSubscription(WorkflowInstanceQuery workflowTrackingQuery)
    {
        _trackingRecordPreFilter.TrackWorkflowInstanceRecords = true;

        _workflowEventSubscriptions ??= new Dictionary<string, WorkflowInstanceQuery>();
        if (workflowTrackingQuery.HasStates)
        {
            foreach (string state in workflowTrackingQuery.States)
            {
                //if duplicates are found, use only the first subscription for a given state.
                _workflowEventSubscriptions.TryAdd(state, workflowTrackingQuery);
            }
        }
    }

    internal TrackingRecord Match(TrackingRecord record, bool shouldClone)
    {
        TrackingQuery resultQuery = null;
        switch (record)
        {
            case WorkflowInstanceRecord:
                resultQuery = Match((WorkflowInstanceRecord)record);
                break;
            case ActivityStateRecord:
                resultQuery = Match((ActivityStateRecord)record);
                break;
            case BookmarkResumptionRecord:
                resultQuery = Match((BookmarkResumptionRecord)record);
                break;
            case CustomTrackingRecord:
                resultQuery = Match((CustomTrackingRecord)record);
                break;
            case ActivityScheduledRecord:
                resultQuery = Match((ActivityScheduledRecord)record);
                break;
            case CancelRequestedRecord:
                resultQuery = Match((CancelRequestedRecord)record);
                break;
            case FaultPropagationRecord:
                resultQuery = Match((FaultPropagationRecord)record);
                break;
        }

        return resultQuery == null ? null : PrepareRecord(record, resultQuery, shouldClone);
    }

    private ActivityStateQuery Match(ActivityStateRecord activityStateRecord)
    {
        ActivityStateQuery query = null;
        if (_activitySubscriptions != null)
        {
            //first look for a specific match, if not found, look for a generic match.
            if (_activitySubscriptions.TryGetValue(activityStateRecord.Activity.Name, out HybridCollection<ActivityStateQuery> eventSubscriptions))
            {
                query = MatchActivityState(activityStateRecord, eventSubscriptions.AsReadOnly());
            }

            if (query == null && _activitySubscriptions.TryGetValue("*", out eventSubscriptions))
            {
                query = MatchActivityState(activityStateRecord, eventSubscriptions.AsReadOnly());

                if ((query != null) && (_associatedProfile.ImplementationVisibility == ImplementationVisibility.RootScope))
                {
                    if (!ShouldTrackActivity(activityStateRecord.Activity, "*"))
                    {
                        return null;
                    }
                }
            }
        }

        return query;
    }

    private static ActivityStateQuery MatchActivityState(ActivityStateRecord activityRecord, ReadOnlyCollection<ActivityStateQuery> subscriptions)
    {
        ActivityStateQuery genericMatch = null;
        for (int i = 0; i < subscriptions.Count; i++)
        {
            if (subscriptions[i].States.Contains(activityRecord.State))
            {
                return subscriptions[i];
            }
            else if (subscriptions[i].States.Contains("*"))
            {
                genericMatch ??= subscriptions[i];
            }
        }
        return genericMatch;
    }

    private WorkflowInstanceQuery Match(WorkflowInstanceRecord workflowRecord)
    {
        WorkflowInstanceQuery trackingQuery = null;
        if (_workflowEventSubscriptions != null)
        {
            if (!_workflowEventSubscriptions.TryGetValue(workflowRecord.State, out trackingQuery))
            {
                _workflowEventSubscriptions.TryGetValue("*", out trackingQuery);
            }
        }
        return trackingQuery;
    }

    private BookmarkResumptionQuery Match(BookmarkResumptionRecord bookmarkRecord)
    {
        BookmarkResumptionQuery trackingQuery = null;
        if (_bookmarkSubscriptions != null)
        {
            if (bookmarkRecord.BookmarkName != null)
            {
                _bookmarkSubscriptions.TryGetValue(bookmarkRecord.BookmarkName, out trackingQuery);
            }
            if (trackingQuery == null)
            {
                _bookmarkSubscriptions.TryGetValue("*", out trackingQuery);
            }
        }
        return trackingQuery;
    }

    private ActivityScheduledQuery Match(ActivityScheduledRecord activityScheduledRecord)
    {
        ActivityScheduledQuery query = null;
        if (_activityScheduledSubscriptions != null)
        {
            for (int i = 0; i < _activityScheduledSubscriptions.Count; i++)
            {
                //check specific and then generic
                string activityName = activityScheduledRecord.Activity?.Name;
                if (string.CompareOrdinal(_activityScheduledSubscriptions[i].ActivityName, activityName) == 0)
                {
                    if (CheckSubscription(_activityScheduledSubscriptions[i].ChildActivityName, activityScheduledRecord.Child.Name))
                    {
                        query = _activityScheduledSubscriptions[i];
                        break;
                    }

                }
                else if (string.CompareOrdinal(_activityScheduledSubscriptions[i].ActivityName, "*") == 0)
                {
                    if (CheckSubscription(_activityScheduledSubscriptions[i].ChildActivityName, activityScheduledRecord.Child.Name))
                    {
                        query = _activityScheduledSubscriptions[i];
                        break;
                    }
                }
            }
        }

        if ((query != null) && (_associatedProfile.ImplementationVisibility == ImplementationVisibility.RootScope))
        {
            if ((!ShouldTrackActivity(activityScheduledRecord.Activity, query.ActivityName)) ||
                    (!ShouldTrackActivity(activityScheduledRecord.Child, query.ChildActivityName)))
            {
                return null;
            }
        }

        return query;
    }

    private FaultPropagationQuery Match(FaultPropagationRecord faultRecord)
    {
        FaultPropagationQuery query = null;
        if (_faultPropagationSubscriptions != null)
        {
            for (int i = 0; i < _faultPropagationSubscriptions.Count; i++)
            {
                //check specific and then generic
                string faultHandlerName = faultRecord.FaultHandler?.Name;
                if (string.CompareOrdinal(_faultPropagationSubscriptions[i].FaultSourceActivityName, faultRecord.FaultSource.Name) == 0)
                {
                    if (CheckSubscription(_faultPropagationSubscriptions[i].FaultHandlerActivityName, faultHandlerName))
                    {
                        query = _faultPropagationSubscriptions[i];
                        break;
                    }
                }
                else if (string.CompareOrdinal(_faultPropagationSubscriptions[i].FaultSourceActivityName, "*") == 0)
                {
                    if (CheckSubscription(_faultPropagationSubscriptions[i].FaultHandlerActivityName, faultHandlerName))
                    {
                        query = _faultPropagationSubscriptions[i];
                        break;
                    }
                }
            }
        }

        if ((query != null) && (_associatedProfile.ImplementationVisibility == ImplementationVisibility.RootScope))
        {
            if ((!ShouldTrackActivity(faultRecord.FaultHandler, query.FaultHandlerActivityName)) ||
                (!ShouldTrackActivity(faultRecord.FaultSource, query.FaultSourceActivityName)))
            {
                return null;
            }
        }

        return query;
    }

    private CancelRequestedQuery Match(CancelRequestedRecord cancelRecord)
    {
        CancelRequestedQuery query = null;

        if (_cancelRequestedSubscriptions != null)
        {
            for (int i = 0; i < _cancelRequestedSubscriptions.Count; i++)
            {
                //check specific and then generic
                string activityName = cancelRecord.Activity?.Name;
                if (string.CompareOrdinal(_cancelRequestedSubscriptions[i].ActivityName, activityName) == 0)
                {
                    if (CheckSubscription(_cancelRequestedSubscriptions[i].ChildActivityName, cancelRecord.Child.Name))
                    {
                        query = _cancelRequestedSubscriptions[i];
                        break;
                    }
                }
                else if (string.CompareOrdinal(_cancelRequestedSubscriptions[i].ActivityName, "*") == 0)
                {
                    if (CheckSubscription(_cancelRequestedSubscriptions[i].ChildActivityName, cancelRecord.Child.Name))
                    {
                        query = _cancelRequestedSubscriptions[i];
                        break;
                    }
                }
            }
        }

        if ((query != null) && (_associatedProfile.ImplementationVisibility == ImplementationVisibility.RootScope))
        {
            if ((!ShouldTrackActivity(cancelRecord.Activity, query.ActivityName)) ||
                (!ShouldTrackActivity(cancelRecord.Child, query.ChildActivityName)))
            {
                return null;
            }
        }

        return query;
    }

    private CustomTrackingQuery Match(CustomTrackingRecord customRecord)
    {
        CustomTrackingQuery query = null;

        if (_customTrackingQuerySubscriptions != null)
        {
            for (int i = 0; i < _customTrackingQuerySubscriptions.Count; i++)
            {
                //check specific and then generic
                if (string.CompareOrdinal(_customTrackingQuerySubscriptions[i].Name, customRecord.Name) == 0)
                {
                    if (CheckSubscription(_customTrackingQuerySubscriptions[i].ActivityName, customRecord.Activity.Name))
                    {
                        query = _customTrackingQuerySubscriptions[i];
                        break;
                    }
                }
                else if (string.CompareOrdinal(_customTrackingQuerySubscriptions[i].Name, "*") == 0)
                {
                    if (CheckSubscription(_customTrackingQuerySubscriptions[i].ActivityName, customRecord.Activity.Name))
                    {
                        query = _customTrackingQuerySubscriptions[i];
                        break;
                    }
                }
            }
        }
        return query;
    }

    private static bool CheckSubscription(string name, string value) =>
        //check specific and then generic
        (string.CompareOrdinal(name, value) == 0 ||
            string.CompareOrdinal(name, "*") == 0);

    private static void ExtractVariables(ActivityStateRecord activityStateRecord, ActivityStateQuery activityStateQuery)
    {
        if (activityStateQuery.HasVariables)
        {
            activityStateRecord.Variables = activityStateRecord.GetVariables(activityStateQuery.Variables);
        }
        else
        {
            activityStateRecord.Variables = ActivityUtilities.EmptyParameters;
        }
    }

    private static void ExtractArguments(ActivityStateRecord activityStateRecord, ActivityStateQuery activityStateQuery)
    {
        if (activityStateQuery.HasArguments)
        {
            activityStateRecord.Arguments = activityStateRecord.GetArguments(activityStateQuery.Arguments);
        }
        else
        {
            activityStateRecord.Arguments = ActivityUtilities.EmptyParameters;
        }
    }

    private static TrackingRecord PrepareRecord(TrackingRecord record, TrackingQuery query, bool shouldClone)
    {
        TrackingRecord preparedRecord = shouldClone ? record.Clone() : record;

        if (query.HasAnnotations)
        {
            preparedRecord.Annotations = new ReadOnlyDictionary<string, string>(query.QueryAnnotations);
        }

        if (query is ActivityStateQuery activityStateQuery)
        {
            ExtractArguments((ActivityStateRecord)preparedRecord, activityStateQuery);
            ExtractVariables((ActivityStateRecord)preparedRecord, activityStateQuery);                
        }
        return preparedRecord;
    }

    private class RuntimeTrackingProfileCache
    {
        [Fx.Tag.Cache(typeof(RuntimeTrackingProfile), Fx.Tag.CacheAttrition.PartialPurgeOnEachAccess)]
        private readonly ConditionalWeakTable<Activity, HybridCollection<RuntimeTrackingProfile>> _cache;

        public RuntimeTrackingProfileCache()
        {
            _cache = new ConditionalWeakTable<Activity, HybridCollection<RuntimeTrackingProfile>>();
        }

        public RuntimeTrackingProfile GetRuntimeTrackingProfile(TrackingProfile profile, Activity rootElement)
        {
            Fx.Assert(rootElement != null, "Root element must be valid");

            RuntimeTrackingProfile foundRuntimeProfile = null;
            HybridCollection<RuntimeTrackingProfile> runtimeProfileList = null;

            lock (_cache)
            {
                if (!_cache.TryGetValue(rootElement, out runtimeProfileList))
                {
                    foundRuntimeProfile = new RuntimeTrackingProfile(profile, rootElement);
                    runtimeProfileList = new HybridCollection<RuntimeTrackingProfile>();
                    runtimeProfileList.Add(foundRuntimeProfile);

                    _cache.Add(rootElement, runtimeProfileList);
                }
                else
                {
                    ReadOnlyCollection<RuntimeTrackingProfile> runtimeProfileCollection = runtimeProfileList.AsReadOnly();
                    foreach (RuntimeTrackingProfile runtimeProfile in runtimeProfileCollection)
                    {
                        if (string.CompareOrdinal(profile.Name, runtimeProfile._associatedProfile.Name) == 0 &&
                            string.CompareOrdinal(profile.ActivityDefinitionId, runtimeProfile._associatedProfile.ActivityDefinitionId) == 0)
                        {
                            foundRuntimeProfile = runtimeProfile;
                            break;
                        }
                    }

                    if (foundRuntimeProfile == null)
                    {
                        foundRuntimeProfile = new RuntimeTrackingProfile(profile, rootElement);
                        runtimeProfileList.Add(foundRuntimeProfile);
                    }
                }
            }
            return foundRuntimeProfile;
        }
    }
}

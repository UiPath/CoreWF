// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoreWf.Tracking
{
    internal class RuntimeTrackingProfile
    {
        private static RuntimeTrackingProfileCache s_profileCache;

        private List<ActivityScheduledQuery> _activityScheduledSubscriptions;
        private List<FaultPropagationQuery> _faultPropagationSubscriptions;
        private List<CancelRequestedQuery> _cancelRequestedSubscriptions;
        private Dictionary<string, HybridCollection<ActivityStateQuery>> _activitySubscriptions;
        private List<CustomTrackingQuery> _customTrackingQuerySubscriptions;
        private Dictionary<string, BookmarkResumptionQuery> _bookmarkSubscriptions;
        private Dictionary<string, WorkflowInstanceQuery> _workflowEventSubscriptions;

        private TrackingProfile _associatedProfile;
        private TrackingRecordPreFilter _trackingRecordPreFilter;
        private List<string> _activityNames;

        private bool _isRootNativeActivity;

        internal RuntimeTrackingProfile(TrackingProfile profile, Activity rootElement)
        {
            _associatedProfile = profile;
            _isRootNativeActivity = rootElement is NativeActivity;
            _trackingRecordPreFilter = new TrackingRecordPreFilter();

            foreach (TrackingQuery query in _associatedProfile.Queries)
            {
                if (query is ActivityStateQuery)
                {
                    AddActivitySubscription((ActivityStateQuery)query);
                }
                else if (query is WorkflowInstanceQuery)
                {
                    AddWorkflowSubscription((WorkflowInstanceQuery)query);
                }
                else if (query is BookmarkResumptionQuery)
                {
                    AddBookmarkSubscription((BookmarkResumptionQuery)query);
                }
                else if (query is CustomTrackingQuery)
                {
                    AddCustomTrackingSubscription((CustomTrackingQuery)query);
                }
                else if (query is ActivityScheduledQuery)
                {
                    AddActivityScheduledSubscription((ActivityScheduledQuery)query);
                }
                else if (query is CancelRequestedQuery)
                {
                    AddCancelRequestedSubscription((CancelRequestedQuery)query);
                }
                else if (query is FaultPropagationQuery)
                {
                    AddFaultPropagationSubscription((FaultPropagationQuery)query);
                }
            }
        }

        private static RuntimeTrackingProfileCache Cache
        {
            get
            {
                // We do not take a lock here because a true singleton is not required.
                if (s_profileCache == null)
                {
                    s_profileCache = new RuntimeTrackingProfileCache();
                }
                return s_profileCache;
            }
        }

        internal TrackingRecordPreFilter Filter
        {
            get
            {
                return _trackingRecordPreFilter;
            }
        }

        internal IEnumerable<string> GetSubscribedActivityNames()
        {
            return _activityNames;
        }

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
            if (_activityNames == null)
            {
                _activityNames = new List<string>();
            }
            _activityNames.Add(name);
        }

        internal static RuntimeTrackingProfile GetRuntimeTrackingProfile(TrackingProfile profile, Activity rootElement)
        {
            return RuntimeTrackingProfile.Cache.GetRuntimeTrackingProfile(profile, rootElement);
        }

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

            if (_activitySubscriptions == null)
            {
                _activitySubscriptions = new Dictionary<string, HybridCollection<ActivityStateQuery>>();
            }

            HybridCollection<ActivityStateQuery> subscription;
            if (!_activitySubscriptions.TryGetValue(query.ActivityName, out subscription))
            {
                subscription = new HybridCollection<ActivityStateQuery>();
                _activitySubscriptions[query.ActivityName] = subscription;
            }
            subscription.Add((ActivityStateQuery)query);
            AddActivityName(query.ActivityName);
        }

        private void AddActivityScheduledSubscription(ActivityScheduledQuery activityScheduledQuery)
        {
            _trackingRecordPreFilter.TrackActivityScheduledRecords = true;
            if (_activityScheduledSubscriptions == null)
            {
                _activityScheduledSubscriptions = new List<ActivityScheduledQuery>();
            }
            _activityScheduledSubscriptions.Add(activityScheduledQuery);
        }

        private void AddCancelRequestedSubscription(CancelRequestedQuery cancelQuery)
        {
            _trackingRecordPreFilter.TrackCancelRequestedRecords = true;
            if (_cancelRequestedSubscriptions == null)
            {
                _cancelRequestedSubscriptions = new List<CancelRequestedQuery>();
            }
            _cancelRequestedSubscriptions.Add(cancelQuery);
        }

        private void AddFaultPropagationSubscription(FaultPropagationQuery faultQuery)
        {
            _trackingRecordPreFilter.TrackFaultPropagationRecords = true;
            if (_faultPropagationSubscriptions == null)
            {
                _faultPropagationSubscriptions = new List<FaultPropagationQuery>();
            }
            _faultPropagationSubscriptions.Add(faultQuery);
        }

        private void AddBookmarkSubscription(BookmarkResumptionQuery bookmarkTrackingQuery)
        {
            _trackingRecordPreFilter.TrackBookmarkResumptionRecords = true;
            if (_bookmarkSubscriptions == null)
            {
                _bookmarkSubscriptions = new Dictionary<string, BookmarkResumptionQuery>();
            }
            //if duplicates are found, use only the first subscription for a given bookmark name.
            if (!_bookmarkSubscriptions.ContainsKey(bookmarkTrackingQuery.Name))
            {
                _bookmarkSubscriptions.Add(bookmarkTrackingQuery.Name, bookmarkTrackingQuery);
            }
        }

        private void AddCustomTrackingSubscription(CustomTrackingQuery customQuery)
        {
            if (_customTrackingQuerySubscriptions == null)
            {
                _customTrackingQuerySubscriptions = new List<CustomTrackingQuery>();
            }
            _customTrackingQuerySubscriptions.Add(customQuery);
        }

        private void AddWorkflowSubscription(WorkflowInstanceQuery workflowTrackingQuery)
        {
            _trackingRecordPreFilter.TrackWorkflowInstanceRecords = true;

            if (_workflowEventSubscriptions == null)
            {
                _workflowEventSubscriptions = new Dictionary<string, WorkflowInstanceQuery>();
            }
            if (workflowTrackingQuery.HasStates)
            {
                foreach (string state in workflowTrackingQuery.States)
                {
                    //if duplicates are found, use only the first subscription for a given state.
                    if (!_workflowEventSubscriptions.ContainsKey(state))
                    {
                        _workflowEventSubscriptions.Add(state, workflowTrackingQuery);
                    }
                }
            }
        }

        internal TrackingRecord Match(TrackingRecord record, bool shouldClone)
        {
            TrackingQuery resultQuery = null;
            if (record is WorkflowInstanceRecord)
            {
                resultQuery = Match((WorkflowInstanceRecord)record);
            }
            else if (record is ActivityStateRecord)
            {
                resultQuery = Match((ActivityStateRecord)record);
            }
            else if (record is BookmarkResumptionRecord)
            {
                resultQuery = Match((BookmarkResumptionRecord)record);
            }
            else if (record is CustomTrackingRecord)
            {
                resultQuery = Match((CustomTrackingRecord)record);
            }
            else if (record is ActivityScheduledRecord)
            {
                resultQuery = Match((ActivityScheduledRecord)record);
            }
            else if (record is CancelRequestedRecord)
            {
                resultQuery = Match((CancelRequestedRecord)record);
            }
            else if (record is FaultPropagationRecord)
            {
                resultQuery = Match((FaultPropagationRecord)record);
            }

            return resultQuery == null ? null : PrepareRecord(record, resultQuery, shouldClone);
        }

        private ActivityStateQuery Match(ActivityStateRecord activityStateRecord)
        {
            ActivityStateQuery query = null;
            if (_activitySubscriptions != null)
            {
                HybridCollection<ActivityStateQuery> eventSubscriptions;
                //first look for a specific match, if not found, look for a generic match.
                if (_activitySubscriptions.TryGetValue(activityStateRecord.Activity.Name, out eventSubscriptions))
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
                    if (genericMatch == null)
                    {
                        genericMatch = subscriptions[i];
                    }
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
                    string activityName = activityScheduledRecord.Activity == null ? null : activityScheduledRecord.Activity.Name;
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
                    string faultHandlerName = faultRecord.FaultHandler == null ? null : faultRecord.FaultHandler.Name;
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
                    string activityName = cancelRecord.Activity == null ? null : cancelRecord.Activity.Name;
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

        private static bool CheckSubscription(string name, string value)
        {
            //check specific and then generic
            return (string.CompareOrdinal(name, value) == 0 ||
                string.CompareOrdinal(name, "*") == 0);
        }

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

            if (query is ActivityStateQuery)
            {
                ExtractArguments((ActivityStateRecord)preparedRecord, (ActivityStateQuery)query);
                ExtractVariables((ActivityStateRecord)preparedRecord, (ActivityStateQuery)query);
            }
            return preparedRecord;
        }


        private class RuntimeTrackingProfileCache
        {
            //[Fx.Tag.Cache(typeof(RuntimeTrackingProfile), Fx.Tag.CacheAttrition.PartialPurgeOnEachAccess)]
            //ConditionalWeakTable<Activity, HybridCollection<RuntimeTrackingProfile>> cache;
            private Dictionary<Activity, HybridCollection<RuntimeTrackingProfile>> _cache;

            public RuntimeTrackingProfileCache()
            {
                //this.cache = new ConditionalWeakTable<Activity, HybridCollection<RuntimeTrackingProfile>>();
                _cache = new Dictionary<Activity, HybridCollection<RuntimeTrackingProfile>>();
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
}

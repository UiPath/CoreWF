// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

internal class TrackingRecordPreFilter
{
    public TrackingRecordPreFilter() { }

    public TrackingRecordPreFilter(bool trackingProviderInitialized)
    {
        if (trackingProviderInitialized)
        {
            TrackingProviderInitialized = true;
            TrackActivityScheduledRecords = true;
            TrackActivityStateRecords = true;
            TrackActivityStateRecordsClosedState = true;
            TrackActivityStateRecordsExecutingState = true;
            TrackBookmarkResumptionRecords = true;
            TrackCancelRequestedRecords = true;
            TrackFaultPropagationRecords = true;
            TrackWorkflowInstanceRecords = true;
        }
    }

    internal bool TrackingProviderInitialized { get; private set; }

    internal bool TrackWorkflowInstanceRecords { get; set; }

    internal bool TrackBookmarkResumptionRecords { get; set; }

    internal bool TrackActivityScheduledRecords { get; set; }

    internal bool TrackActivityStateRecordsClosedState { get; set; }

    internal bool TrackActivityStateRecordsExecutingState { get; set; }

    internal bool TrackActivityStateRecords { get; set; }

    internal bool TrackCancelRequestedRecords { get; set; }

    internal bool TrackFaultPropagationRecords { get; set; }

    internal void Merge(TrackingRecordPreFilter filter)
    {
        if (TrackingProviderInitialized)
        {
            TrackingProviderInitialized = false;
            TrackActivityStateRecordsExecutingState = filter.TrackActivityStateRecordsExecutingState;
            TrackActivityScheduledRecords = filter.TrackActivityScheduledRecords;
            TrackActivityStateRecords = filter.TrackActivityStateRecords;
            TrackActivityStateRecordsClosedState = filter.TrackActivityStateRecordsClosedState;
            TrackBookmarkResumptionRecords = filter.TrackBookmarkResumptionRecords;
            TrackCancelRequestedRecords = filter.TrackCancelRequestedRecords;
            TrackFaultPropagationRecords = filter.TrackFaultPropagationRecords;
            TrackWorkflowInstanceRecords = filter.TrackWorkflowInstanceRecords;
        }
        else
        {
            TrackActivityStateRecordsExecutingState |= filter.TrackActivityStateRecordsExecutingState;
            TrackActivityScheduledRecords |= filter.TrackActivityScheduledRecords;
            TrackActivityStateRecords |= filter.TrackActivityStateRecords;
            TrackActivityStateRecordsClosedState |= filter.TrackActivityStateRecordsClosedState;
            TrackBookmarkResumptionRecords |= filter.TrackBookmarkResumptionRecords;
            TrackCancelRequestedRecords |= filter.TrackCancelRequestedRecords;
            TrackFaultPropagationRecords |= filter.TrackFaultPropagationRecords;
            TrackWorkflowInstanceRecords |= filter.TrackWorkflowInstanceRecords;
        }
    }
}

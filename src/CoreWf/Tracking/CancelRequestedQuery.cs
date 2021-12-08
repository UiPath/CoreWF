// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

public sealed class CancelRequestedQuery : TrackingQuery
{
    public CancelRequestedQuery()
    {
        ActivityName = "*";
        ChildActivityName = "*";
    }

    public string ActivityName { get; set; }
    public string ChildActivityName { get; set; }

}

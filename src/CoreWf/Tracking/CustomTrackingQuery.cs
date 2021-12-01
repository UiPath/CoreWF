// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

public class CustomTrackingQuery : TrackingQuery
{
    public CustomTrackingQuery() { }

    public string Name { get; set; }

    public string ActivityName { get; set; }
}

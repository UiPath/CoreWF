// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking;

public sealed class FaultPropagationQuery : TrackingQuery
{
    public FaultPropagationQuery()
    {
        FaultSourceActivityName = "*";
        FaultHandlerActivityName = "*";
    }

    public string FaultHandlerActivityName { get; set; }

    public string FaultSourceActivityName { get; set; }
}

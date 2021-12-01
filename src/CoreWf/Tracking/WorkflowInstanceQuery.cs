// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Tracking;

public class WorkflowInstanceQuery : TrackingQuery
{
    private Collection<string> _states;

    public WorkflowInstanceQuery() { }

    public Collection<string> States
    {
        get
        {
            _states ??= new Collection<string>();
            return _states;
        }
    }

    internal bool HasStates => _states != null && _states.Count > 0;
}

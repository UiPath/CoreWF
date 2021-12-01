// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Tracking;

public class ActivityStateQuery : TrackingQuery
{
    private Collection<string> _arguments;
    private Collection<string> _states;
    private Collection<string> _variables;        
        
    public ActivityStateQuery()
    {
        ActivityName = "*";
    }

    public string ActivityName { get; set; }

    public Collection<string> Arguments
    {
        get
        {
            _arguments ??= new Collection<string>();
            return _arguments;
        }
    }

    public Collection<string> Variables
    {
        get
        {
            _variables ??= new Collection<string>();
            return _variables;
        }
    }        

    public Collection<string> States
    {
        get
        {
            _states ??= new Collection<string>();
            return _states;
        }
    }

    internal bool HasStates => _states != null && _states.Count > 0;

    internal bool HasArguments => _arguments != null && _arguments.Count > 0;

    internal bool HasVariables => _variables != null && _variables.Count > 0;
}

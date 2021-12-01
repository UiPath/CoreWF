// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;
using System.Globalization;

namespace System.Activities.Tracking;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class ActivityStateRecord : TrackingRecord
{
    private IDictionary<string, object> _variables;
    private IDictionary<string, object> _arguments;
    private ActivityInfo _activity;
    private string _state;
    private static readonly ReadOnlyCollection<string> wildcardCollection = new(new List<string>(1) { "*" });

    internal ActivityStateRecord(Guid instanceId, ActivityInstance instance, ActivityInstanceState state)
        : this(instanceId, new ActivityInfo(instance), state) { }

    internal ActivityStateRecord(Guid instanceId, ActivityInfo activity, ActivityInstanceState state)
        : base(instanceId)
    {
        Activity = activity;

        State = state switch
        {
            ActivityInstanceState.Executing => ActivityStates.Executing,
            ActivityInstanceState.Closed => ActivityStates.Closed,
            ActivityInstanceState.Canceled => ActivityStates.Canceled,
            ActivityInstanceState.Faulted => ActivityStates.Faulted,
            _ => throw Fx.AssertAndThrow("Invalid state value"),
        };
    }

    public ActivityStateRecord(
        Guid instanceId,
        long recordNumber,
        ActivityInfo activity,
        string state)
        : base(instanceId, recordNumber)
    {
        if (string.IsNullOrEmpty(state))
        {
            throw FxTrace.Exception.ArgumentNullOrEmpty(nameof(state));
        }

        Activity = activity ?? throw FxTrace.Exception.ArgumentNull(nameof(activity));
        State = state;
    }

    private ActivityStateRecord(ActivityStateRecord record)
        : base(record)
    {
        Activity = record.Activity;
        State = record.State;
        if (record._variables != null)
        {
            if (record._variables == ActivityUtilities.EmptyParameters)
            {
                _variables = ActivityUtilities.EmptyParameters;
            }
            else
            {
                _variables = new Dictionary<string, object>(record._variables);
            }
        }

        if (record._arguments != null)
        {
            if (record._arguments == ActivityUtilities.EmptyParameters)
            {
                _arguments = ActivityUtilities.EmptyParameters;
            }
            else
            {
                _arguments = new Dictionary<string, object>(record._arguments);
            }
        }
    }


    public ActivityInfo Activity
    {
        get => _activity;
        private set => _activity = value;
    }

    public string State
    {
        get => _state;
        private set => _state = value;
    }

    public IDictionary<string, object> Variables
    {
        get
        {
            if (_variables == null)
            {
                _variables = GetVariables(wildcardCollection);
                Fx.Assert(_variables.IsReadOnly, "only readonly dictionary can be set for variables");
            }
            return _variables;
        }
        internal set
        {
            Fx.Assert(value.IsReadOnly, "only readonly dictionary can be set for variables");
            _variables = value;
        }
    }

    public IDictionary<string, object> Arguments
    {
        get
        {
            if (_arguments == null)
            {
                _arguments = GetArguments(wildcardCollection);
                Fx.Assert(_arguments.IsReadOnly, "only readonly dictionary can be set for arguments");
            }
            return _arguments;
        }
        internal set
        {
            Fx.Assert(value.IsReadOnly, "only readonly dictionary can be set for arguments");
            _arguments = value;
        }
    }

    [DataMember(EmitDefaultValue = false, Name = "variables")]
    internal IDictionary<string, object> SerializedVariables
    {
        get => _variables;
        set => _variables = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "arguments")]
    internal IDictionary<string, object> SerializedArguments
    {
        get => _arguments;
        set => _arguments = value;
    }

    [DataMember(Name = "Activity")]
    internal ActivityInfo SerializedActivity
    {
        get => Activity;
        set => Activity = value;
    }

    [DataMember(Name = "State")]
    internal string SerializedState
    {
        get => State;
        set => State = value;
    }

    protected internal override TrackingRecord Clone() => new ActivityStateRecord(this);

    public override string ToString()
        => string.Format(CultureInfo.CurrentCulture,
            "ActivityStateRecord {{ {0}, Activity {{ {1} }}, State = {2} }}",
            base.ToString(),
            Activity.ToString(),
            State);

    internal IDictionary<string, object> GetVariables(ICollection<string> variables)
    {
        Dictionary<string, object> trackedVariables = null; // delay allocated through TrackData
        ActivityInstance currentInstance = Activity.Instance;

        if (currentInstance != null)
        {
            Activity currentElement = currentInstance.Activity;
            Activity startActivity = currentInstance.Activity;
            bool containsWildcard = variables.Contains("*");
            //count defines how many items we can get in this lookup. It represents the maximum number of items that can be extracted, 
            //if * is specified, any other names specified are expected to be variables defined in scope, not in the activity itself. 
            //if a variable name in the activity is specified, the lookup continues through the variables in scope. 
            int count = containsWildcard ? currentElement.RuntimeVariables.Count + variables.Count - 1 : variables.Count;

            IdSpace activityIdSpace = currentElement.MemberOf;

            while (currentInstance != null)
            {
                //* only extracts variables of the current Activity and not variables in scope. 
                bool useWildCard = containsWildcard && startActivity == currentElement;

                // we only track public Variables, not ImplementationVariables
                for (int i = 0; i < currentElement.RuntimeVariables.Count; i++)
                {
                    Variable variable = currentElement.RuntimeVariables[i];
                    if (TrackData(variable.Name, variable.Id, currentInstance, variables, useWildCard, ref trackedVariables)
                        && trackedVariables.Count == count)
                    {
                        return new ReadOnlyDictionary<string, object>(trackedVariables);
                    }
                }

                bool foundNext = false;

                while (!foundNext)
                {
                    currentInstance = currentInstance.Parent;

                    if (currentInstance != null)
                    {
                        currentElement = currentInstance.Activity;
                        foundNext = currentElement.MemberOf.Equals(activityIdSpace);
                    }
                    else
                    {
                        // We set foundNext to true to get out of our loop.
                        foundNext = true;
                    }
                }
            }
        }

        if (trackedVariables == null)
        {
            return ActivityUtilities.EmptyParameters;
        }
        else
        {
            Fx.Assert(trackedVariables.Count > 0, "we should only allocate the dictionary if we're putting data in it");
            return new ReadOnlyDictionary<string, object>(trackedVariables);
        }
    }

    internal IDictionary<string, object> GetArguments(ICollection<string> arguments)
    {
        Dictionary<string, object> trackedArguments = null; // delay allocated through TrackData
        ActivityInstance currentInstance = Activity.Instance;

        if (currentInstance != null)
        {
            Activity currentElement = currentInstance.Activity;
            bool containsWildcard = arguments.Contains("*");
            int count = containsWildcard ? currentElement.RuntimeArguments.Count : arguments.Count;
            bool isActivityStateExecuting = ActivityStates.Executing.Equals(State, StringComparison.Ordinal);

            //look at arguments for this element. 
            for (int i = 0; i < currentElement.RuntimeArguments.Count; i++)
            {
                RuntimeArgument argument = currentElement.RuntimeArguments[i];

                // OutArguments will always start with default(T), so there is no need to track them when state == Executing
                if (isActivityStateExecuting && argument.Direction == ArgumentDirection.Out)
                {
                    continue;
                }

                if (TrackData(argument.Name, argument.Id, currentInstance, arguments, containsWildcard, ref trackedArguments)
                    && trackedArguments.Count == count)
                {
                    break;
                }
            }
        }

        if (trackedArguments == null)
        {
            return ActivityUtilities.EmptyParameters;
        }
        else
        {
            Fx.Assert(trackedArguments.Count > 0, "we should only allocate the dictionary if we're putting data in it");
            return new ReadOnlyDictionary<string, object>(trackedArguments);
        }
    }

    private bool TrackData(string name, int id, ActivityInstance currentInstance, ICollection<string> data, bool wildcard, ref Dictionary<string, object> trackedData)
    {
        if (wildcard || data.Contains(name))
        {
            Location location = currentInstance.Environment.GetSpecificLocation(id);
            if (location != null)
            {
                trackedData ??= new Dictionary<string, object>(10);
                string dataName = name ?? NameGenerator.Next();
                trackedData[dataName] = location.Value;
                if (TD.TrackingDataExtractedIsEnabled())
                {
                    TD.TrackingDataExtracted(dataName, Activity.Name);
                }

                return true;
            }
        }
        return false;
    }
}

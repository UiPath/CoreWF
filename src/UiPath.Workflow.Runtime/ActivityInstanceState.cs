// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[DataContract]
public enum ActivityInstanceState
{
    [EnumMember]
    Executing,

    [EnumMember]
    Closed,

    [EnumMember]
    Canceled,

    [EnumMember]
    Faulted,

    // If any more states are added, ensure they are also added to ActivityInstanceStateExtension.GetStateName
}

internal static class ActivityInstanceStateExtension
{
    internal static string GetStateName(this ActivityInstanceState state)
    {
        switch (state)
        {
            case ActivityInstanceState.Executing:
                return "Executing";
            case ActivityInstanceState.Closed:
                return "Closed";
            case ActivityInstanceState.Canceled:
                return "Canceled";
            case ActivityInstanceState.Faulted:
                return "Faulted";
            default:
                Fx.Assert("Don't understand ActivityInstanceState named " + state.ToString());
                return state.ToString();
        }
    }
}

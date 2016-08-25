// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
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
}

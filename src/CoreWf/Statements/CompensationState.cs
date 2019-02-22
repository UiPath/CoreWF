// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
namespace System.Activities.Statements
{
    using System.Runtime.Serialization;

    [DataContract]
    internal enum CompensationState
    {
        [EnumMember]
        Creating,

        [EnumMember]
        Active,

        [EnumMember]
        Completed,

        [EnumMember]
        Confirming,

        [EnumMember]
        Confirmed,

        [EnumMember]
        Compensating,

        [EnumMember]
        Compensated,

        [EnumMember]
        Canceling,

        [EnumMember]
        Canceled,
    }
}

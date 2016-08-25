// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Statements
{
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

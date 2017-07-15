// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace CoreWf.Runtime.DurableInstancing
{
    [Flags]
    [DataContract]
    public enum InstanceValueOptions
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Optional = 1,

        [EnumMember]
        WriteOnly = 2,
    }
}

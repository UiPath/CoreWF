// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    [Flags]
    public enum VariableModifiers
    {
        None = 0X00,
        ReadOnly = 0X01,
        Mapped = 0X02
    }
}

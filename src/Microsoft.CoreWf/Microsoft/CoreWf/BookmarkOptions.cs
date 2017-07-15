// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf
{
    [Flags]
    public enum BookmarkOptions
    {
        None = 0x00,
        MultipleResume = 0x01,
        NonBlocking = 0x02,
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CoreWf
{
    public delegate void FaultCallback(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom);
}

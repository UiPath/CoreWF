// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace CoreWf
{
    public delegate void DelegateCompletionCallback(NativeActivityContext context, ActivityInstance completedInstance, IDictionary<string, object> outArguments);
}

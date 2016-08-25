// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CoreWf.Hosting
{
    public enum WorkflowInstanceState
    {
        Idle,
        Runnable,
        Complete,
        Aborted // only Abort is valid
    }
}

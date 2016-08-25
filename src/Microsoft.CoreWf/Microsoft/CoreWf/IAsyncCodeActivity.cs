// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CoreWf
{
    // shared interface by AsyncCodeActivity and AsyncCodeActivity<TResult> to facilitate internal code sharing
    internal interface IAsyncCodeActivity
    {
        void FinishExecution(AsyncCodeActivityContext context, IAsyncResult result);
    }
}

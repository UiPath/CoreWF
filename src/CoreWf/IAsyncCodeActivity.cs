// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

// shared interface by AsyncCodeActivity and AsyncCodeActivity<TResult> to facilitate internal code sharing
internal interface IAsyncCodeActivity
{
    void FinishExecution(AsyncCodeActivityContext context, IAsyncResult result);
}

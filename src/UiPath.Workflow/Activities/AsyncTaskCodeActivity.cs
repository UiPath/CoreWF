// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Interop;

namespace System.Activities;


/// <summary>
/// An asynchronous task-based <see cref="AsyncCodeActivity"/>
/// </summary>
public abstract class AsyncTaskCodeActivity : AsyncCodeActivity
{
    protected override IAsyncResult BeginExecute
    (
        AsyncCodeActivityContext context,
        AsyncCallback callback,
        object state
    )
    {
        var cts = new CancellationTokenSource();
        context.UserState = cts;

        var task = ExecuteAsync(context, cts.Token);

        return ApmAsyncFactory.ToBegin(task, callback, state);
    }

    protected override void EndExecute
    (
        AsyncCodeActivityContext context,
        IAsyncResult result
    )
    {
        using ((CancellationTokenSource)context.UserState)
        {
            ((Task)result).Wait();
        }
    }

    protected override void Cancel(AsyncCodeActivityContext context)
    {
        ((CancellationTokenSource)context.UserState).Cancel();
    }

    /// <summary>
    /// The operation executed at runtime.
    /// </summary>
    /// <param name="context">The context for this activity.</param>
    /// <param name="cancellationToken">A cancellation token for this operation.</param>
    /// <returns>A <see cref="Task"/> representing the lifetime of this operation.</returns>
    private protected abstract Task ExecuteAsync
    (
        AsyncCodeActivityContext context,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// An asynchronous task-based <see cref="AsyncCodeActivity{TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result this activity returns.</typeparam>
public abstract class AsyncTaskCodeActivity<TResult> : AsyncCodeActivity<TResult>
{
    protected sealed override IAsyncResult BeginExecute
    (
        AsyncCodeActivityContext context,
        AsyncCallback callback,
        object state
    )
    {
        var cts = new CancellationTokenSource();
        context.UserState = cts;

        var task = ExecuteAsync(context, cts.Token);

        return ApmAsyncFactory.ToBegin(task, callback, state);
    }

    protected sealed override TResult EndExecute
    (
        AsyncCodeActivityContext context,
        IAsyncResult result
    )
    {
        using ((CancellationTokenSource)context.UserState)
        {
            return ((Task<TResult>)result).Result;
        }
    }

    protected sealed override void Cancel(AsyncCodeActivityContext context)
    {
        ((CancellationTokenSource)context.UserState).Cancel();
    }

    /// <summary>
    /// The operation executed at runtime.
    /// </summary>
    /// <param name="context">The context for this activity.</param>
    /// <param name="cancellationToken">A cancellation token for this operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the lifetime and containing the result of this operation.</returns>
    private protected abstract Task<TResult> ExecuteAsync
    (
        AsyncCodeActivityContext context,
        CancellationToken cancellationToken
    );
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Interop;

namespace System.Activities;

public abstract class AsyncTaskCodeActivity : TaskCodeActivity<object>
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Browsable(false)]
    public new object Result { get; set; }

    protected abstract Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);

    private protected sealed override async Task<object> ExecuteAsyncCore(AsyncCodeActivityContext context,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken);
        return null;
    }
}

public abstract class AsyncTaskCodeActivity<TResult> : TaskCodeActivity<TResult>
{
    protected abstract Task<TResult>
        ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);

    private protected sealed override Task<TResult> ExecuteAsyncCore(AsyncCodeActivityContext context,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(context, cancellationToken);
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class TaskCodeActivity<TResult> : AsyncCodeActivity<TResult>
{
    protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback,
        object state)
    {
        var cts = new CancellationTokenSource();
        context.UserState = cts;

        var task = ExecuteAsyncCore(context, cts.Token);

        return ApmAsyncFactory.ToBegin(task, callback, state);
    }

    protected sealed override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
    {
        using ((CancellationTokenSource) context.UserState)
        {
            return ((Task<TResult>) result).Result;
        }
    }

    protected sealed override void Cancel(AsyncCodeActivityContext context)
    {
        ((CancellationTokenSource) context.UserState).Cancel();
    }

    private protected abstract Task<TResult> ExecuteAsyncCore(AsyncCodeActivityContext context,
        CancellationToken cancellationToken);
}

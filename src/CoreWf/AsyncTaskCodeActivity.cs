using System.Threading;
using System.Threading.Tasks;

namespace System.Activities
{
    public abstract class AsyncTaskCodeActivity : AsyncCodeActivity
    {
        protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var cts = new CancellationTokenSource();
            context.UserState = cts;

            var task = ExecuteAsync(context, cts.Token);

#if NET5_0_OR_GREATER
            // Generic TaskCompletionSource is available starting in .NET 5
            var tcs = new TaskCompletionSource(state);

#else
            // VoidResult allows us to simulate a void return type.
            var tcs = new TaskCompletionSource<VoidResult>(state)
#endif

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception!.InnerExceptions); // If it's faulted, there was an exception.
                }
                else if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else
                {
#if NET5_0_OR_GREATER
                    tcs.TrySetResult();
#else
                    tcs.TrySetResult(default);
#endif
                }

                callback?.Invoke(tcs.Task);
            }, cts.Token);

            return tcs.Task;
        }

        protected sealed override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task) result;
            try
            {
                task.Wait();
            }
            catch (TaskCanceledException)
            {
                if (context.IsCancellationRequested)
                {
                    context.MarkCanceled();
                }

                throw;
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    if (ex is not TaskCanceledException)
                        return;

                    if (context.IsCancellationRequested)
                        context.MarkCanceled();
                }

                throw;
            }
        }

        protected sealed override void Cancel(AsyncCodeActivityContext context)
        {
            var cts = (CancellationTokenSource)context.UserState;
            cts.Cancel();
        }

        protected abstract Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }

    public abstract class AsyncTaskCodeActivity<TResult> : AsyncCodeActivity<TResult>
    {
        protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var cts = new CancellationTokenSource();
            context.UserState = cts;
            var task = ExecuteAsync(context, cts.Token);
            var completionSource = new TaskCompletionSource<TResult>(state);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    completionSource.TrySetException(t.Exception!.InnerExceptions); // If it's faulted, there was an exception.
                }
                else if (t.IsCanceled)
                {
                    completionSource.TrySetCanceled();
                }
                else
                {
                    completionSource.TrySetResult(t.Result);
                }

                callback?.Invoke(completionSource.Task);
            }, cts.Token);

            return completionSource.Task;
        }

        protected sealed override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<TResult>) result;
            try
            {
                return task.Result;
            }
            catch (OperationCanceledException)
            {
                if (context.IsCancellationRequested)
                {
                    context.MarkCanceled();
                }

                throw;
            }
            catch (AggregateException aex)
            {
                foreach (var ex in aex.Flatten().InnerExceptions)
                {
                    if (ex is not OperationCanceledException) 
                        continue;

                    if (context.IsCancellationRequested)
                    {
                        context.MarkCanceled();
                    }
                }

                throw;
            }
        }

        protected sealed override void Cancel(AsyncCodeActivityContext context)
        {
            var cts = (CancellationTokenSource) context.UserState;
            cts.Cancel();
        }

        protected abstract Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }
}

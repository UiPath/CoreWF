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

            // VoidResult allows us to simulate a void return type.
            var tcs = new TaskCompletionSource<object>(state);

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
                    _ = tcs.TrySetResult(null);
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

        public abstract Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }

    public abstract class AsyncTaskCodeActivity<TResult> : AsyncCodeActivity<TResult>
    {
        protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var cts = new CancellationTokenSource();
            context.UserState = cts;
            var task = ExecuteAsync(context, cts.Token);
            var tcs = new TaskCompletionSource<TResult>(state);

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
                    tcs.TrySetResult(t.Result);
                }

                callback?.Invoke(tcs.Task);
            }, cts.Token);

            return tcs.Task;
        }

        protected sealed override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<TResult>) result;
            try
            {
                if (!task.IsCompleted)
                {
                    task.Wait();
                }
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

        public abstract Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }
}

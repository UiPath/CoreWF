using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Activities
{
    public abstract class AsyncTaskCodeActivity : TaskCodeActivity<object>
    {
        public abstract Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
        internal sealed override async Task<object> ExecuteAsyncCore(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            await ExecuteAsync(context, cancellationToken);
            return null;
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public new OutArgument<object> Result { get; }
    }
    public abstract class AsyncTaskCodeActivity<TResult> : TaskCodeActivity<TResult>
    {
        public abstract Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
        internal sealed override Task<TResult> ExecuteAsyncCore(AsyncCodeActivityContext context, CancellationToken cancellationToken) =>
            ExecuteAsync(context, cancellationToken);
    }

    public abstract class TaskCodeActivity<TResult> : AsyncCodeActivity<TResult>
    {
        protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var cts = new CancellationTokenSource();
            context.UserState = cts;
            var task = ExecuteAsyncCore(context, cts.Token);
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
        
        internal abstract Task<TResult> ExecuteAsyncCore(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }
}

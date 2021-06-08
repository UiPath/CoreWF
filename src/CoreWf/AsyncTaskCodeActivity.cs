using Nito.AsyncEx.Interop;
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new object Result { get; }
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

            return ApmAsyncFactory.ToBegin(task, callback, state);
        }
        protected sealed override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            using ((CancellationTokenSource)context.UserState)
            {
                return ((Task<TResult>)result).Result;
            }
        }
        protected sealed override void Cancel(AsyncCodeActivityContext context) => ((CancellationTokenSource)context.UserState).Cancel();
        internal abstract Task<TResult> ExecuteAsyncCore(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }
}
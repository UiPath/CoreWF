using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace TestObjects.CustomActivities
{
    public class AsyncTaskActivity<TResult> : AsyncTaskCodeActivity<TResult>
    {
        private readonly Task<TResult> _task;
        public AsyncTaskActivity(Task<TResult> task)
        {
            _task = task;
        }

        public AsyncTaskActivity(Func<TResult> func) : this(Task.Run(func))
        {
        }

        public override Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
            => _task;
    }
}

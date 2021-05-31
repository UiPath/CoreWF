using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace TestObjects.CustomActivities
{
    public class AsyncTaskActivity : AsyncTaskCodeActivity
    {
        private readonly Task _task;

        public AsyncTaskActivity(Task task)
        {
            _task = task;
        }

        public AsyncTaskActivity(Action action) : this(Task.Run(action))
        {
        }

        public override Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
            => _task;
    }

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

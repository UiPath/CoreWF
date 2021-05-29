using System.Threading;
using System.Threading.Tasks;

namespace System.Activities
{
    internal interface IAsyncTaskCodeActivity : IAsyncCodeActivity
    {
       Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }

    internal interface IAsyncTaskCodeActivity<TResult> : IAsyncCodeActivity
    {
        Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
    }
}

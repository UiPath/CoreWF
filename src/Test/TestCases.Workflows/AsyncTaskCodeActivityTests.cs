using Shouldly;
using System;
using System.Activities;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestCases.Activities
{
    public sealed class AsyncTaskCodeActivityTests
    {
        [Fact]
        public void ShouldReturnVoidResult()
        {
            var genericActivity = new AsyncTaskActivity<object>(_=>Task.FromResult<object>(null));

            object vr1 = null;
            object vr2 = WorkflowInvoker.Invoke<object>(genericActivity);

            Assert.Equal(vr1, vr2);
        }
        [Fact]
        public void ShouldThrow()
        {
            Activity activity = new AsyncTaskActivity(_ => Task.FromException(new InvalidOperationException("@")));
            new Action(() => WorkflowInvoker.Invoke(activity)).ShouldThrow<InvalidOperationException>().Message.ShouldBe("@");
        }
        [Fact]
        public async Task ShouldCancel()
        {
            Activity activity = new AsyncTaskActivity(token=> Task.Delay(Timeout.Infinite, token));
            var invoker = new WorkflowInvoker(activity);
            var taskCompletionSource = new TaskCompletionSource();
            InvokeCompletedEventArgs args = null;
            invoker.InvokeCompleted += (sender, localArgs) =>
            {
                args = localArgs;
                taskCompletionSource.SetResult();
            };
            invoker.InvokeAsync(invoker);
            await Task.Yield();
            invoker.CancelAsync(invoker);
            await taskCompletionSource.Task;
            args.Error.ShouldBeOfType<WorkflowApplicationAbortedException>();
        }
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldReturnConstantResult(int value)
        {
            var activity = new AsyncTaskActivity<int>(async _=>
            {
                await Task.Yield();
                return value;
            });
            var result = WorkflowInvoker.Invoke(activity);

            Assert.Equal(value, result);
        }
        [Fact]
        public void ShouldWriteCorrectString()
        {
            const string stringToWrite = "Hello, World!";

            using var memory = new MemoryStream();

            Activity activity = new AsyncTaskActivity(_ =>
            {
                using var writer = new StreamWriter(memory);
                writer.Write(stringToWrite);
                writer.Flush();
                return Task.CompletedTask;
            });

            _ = WorkflowInvoker.Invoke(activity);

            byte[] buffer = memory.ToArray();
            
            Assert.Equal(stringToWrite, Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        }
    }
    public class AsyncTaskActivity : AsyncTaskCodeActivity
    {
        private readonly Func<CancellationToken, Task> _action;
        public AsyncTaskActivity(Func<CancellationToken, Task> action) => _action = action;
        protected override Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken) => _action(cancellationToken);
    }
    public class AsyncTaskActivity<TResult> : AsyncTaskCodeActivity<TResult>
    {
        private readonly Func<CancellationToken, Task<TResult>> _action;
        public AsyncTaskActivity(Func<CancellationToken, Task<TResult>> action) => _action = action;
        protected override Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken) => _action(cancellationToken);
    }
}
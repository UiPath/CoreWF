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
            var genericActivity = new AsyncTaskActivity<object>(Task.FromResult<object>(null));

            object vr1 = null;
            object vr2 = WorkflowInvoker.Invoke<object>(genericActivity);

            Assert.Equal(vr1, vr2);
        }

        [Fact]
        public void ShouldThrow()
        {
            Activity activity = new AsyncTaskActivity(()=>Task.FromException(new InvalidOperationException("@")));
            new Action(() => WorkflowInvoker.Invoke(activity)).ShouldThrow<InvalidOperationException>().Message.ShouldBe("@");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldReturnConstantResult(int value)
        {
            var activity = new AsyncTaskActivity<int>(async()=>
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

            Activity activity = new AsyncTaskActivity(async() =>
            {
                using var writer = new StreamWriter(memory);
                writer.Write(stringToWrite);
                writer.Flush();
            });

            _ = WorkflowInvoker.Invoke(activity);

            byte[] buffer = memory.ToArray();
            
            Assert.Equal(stringToWrite, Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        }
    }
    public class AsyncTaskActivity : AsyncTaskCodeActivity
    {
        private readonly Task _task;

        public AsyncTaskActivity(Task task)
        {
            _task = task;
        }

        public AsyncTaskActivity(Func<Task> action) : this(action())
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

        public AsyncTaskActivity(Func<Task<TResult>> func) : this(func())
        {
        }

        public override Task<TResult> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
            => _task;
    }
}
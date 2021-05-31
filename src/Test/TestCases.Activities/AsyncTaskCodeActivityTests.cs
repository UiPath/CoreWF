using System;
using System.Activities;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestObjects.CustomActivities;
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

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ShouldReturnConstantResult(int value)
        {
            var activity = new AsyncTaskActivity<int>(Task.FromResult(value));
            var result = WorkflowInvoker.Invoke(activity);

            Assert.Equal(value, result);
        }

        [Fact]
        public void ShouldReturnCalculatedValue()
        {
            var activity = new AsyncTaskActivity<int>(() => 5 + 5);
            var result = WorkflowInvoker.Invoke(activity);

            Assert.Equal(10, result);
        }

        [Fact]
        public void ShouldWriteCorrectString()
        {
            const string stringToWrite = "Hello, World!";

            using var memory = new MemoryStream();

            var activity = new AsyncTaskActivity(() =>
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
}

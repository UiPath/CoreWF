using System.Activities;
using System.Threading.Tasks;
using TestObjects.CustomActivities;
using Xunit;

namespace TestCases.Activities
{
    public sealed class AsyncTaskCodeActivityTests
    {
        private static readonly AsyncTaskActivity<object> genericActivity = new(Task.FromResult<object>(null));

        [Fact]
        public void ShouldReturnVoidResult()
        {
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
    }
}

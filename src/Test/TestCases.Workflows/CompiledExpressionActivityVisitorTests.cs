using Microsoft.VisualBasic.Activities;
using System.Activities;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using Xunit;

namespace TestCases.Workflows
{
    public class CompiledExpressionActivityVisitorTests
    {
        [Fact]
        public void CompileActivity_IncludesExpressionsFromImplementation()
        {
            var seq = new Sequence();
            seq.Activities.Add(new ActivityWithImplementationExpression());
            var activity = new DynamicActivity { Implementation = () => seq };

            ActivityXamlServices.Compile(activity, new());

            var result = WorkflowInvoker.Invoke(activity);
        }
    }

    internal class ActivityWithImplementationExpression : Activity
    {
        public ActivityWithImplementationExpression()
        {
            var writeLine = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("2.ToString")) };
            Implementation = () => writeLine;
        }
    }
}

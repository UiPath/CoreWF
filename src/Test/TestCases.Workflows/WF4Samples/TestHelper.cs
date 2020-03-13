using Shouldly;
using System.Activities;
using System.Activities.XamlIntegration;
using System.IO;
using System.Text;
using System.Threading;

namespace TestCases.Workflows.WF4Samples
{
    internal static class TestHelper
    {
        internal static string InvokeWorkflow(Activity activity)
        {
            var stringBuilder = new StringBuilder();
            var consoleOutputWriter = new StringWriter(stringBuilder);
            AutoResetEvent are = new AutoResetEvent(false);
            var workflowApp = new WorkflowApplication(activity);
            workflowApp.Extensions.Add((TextWriter)consoleOutputWriter);
            workflowApp.Run();
            workflowApp.Completed = e =>
            {
                e.CompletionState.ShouldBe(ActivityInstanceState.Closed);
                are.Set();
            };
            workflowApp.Run();
            are.WaitOne();
            return stringBuilder.ToString();
        }

        internal static Activity GetActivityFromXamlResource(TestXamls xamlName)
        {
            var asm = typeof(TestHelper).Assembly;
            var xamlStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.TestXamls.{xamlName}.xaml");
            return ActivityXamlServices.Load(xamlStream);
        }
    }

    internal enum TestXamls
    {
        NonGenericForEach,
        SalaryCalculation,
    }
}

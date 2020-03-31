using Shouldly;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TestCases.Workflows.WF4Samples
{
    using StringDictionary = Dictionary<string, object>;

    internal static class TestHelper
    {
        internal static string InvokeWorkflow(Activity activity, IDictionary<string, object> inputs = null)
        {
            var consoleOutputWriter = new StringWriter();
            var invoker = new WorkflowInvoker(activity);
            invoker.Extensions.Add(consoleOutputWriter);
            invoker.Invoke(inputs ?? new StringDictionary());
            return consoleOutputWriter.ToString();
        }

        internal static Activity GetActivityFromXamlResource(TestXamls xamlName)
        {
            var asm = typeof(TestHelper).Assembly;
            var xamlStream = asm.GetManifestResourceStream($"{asm.GetName().Name}.TestXamls.{xamlName}.xaml");
            return ActivityXamlServices.Load(xamlStream, new ActivityXamlServicesSettings { CompileExpressions = true });
        }
    }

    internal enum TestXamls
    {
        NonGenericForEach,
        SalaryCalculation,
        CSharpCalculation,
    }
}

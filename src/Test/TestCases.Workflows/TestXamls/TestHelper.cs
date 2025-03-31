using Shouldly;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace TestCases.Workflows
{
    using StringDictionary = Dictionary<string, object>;

    internal static class TestHelper
    {
        internal static string InvokeWorkflow(this Activity activity, IDictionary<string, object> inputs = null)
        {
            var consoleOutputWriter = new StringWriter();
            var invoker = new WorkflowInvoker(activity);
            invoker.Extensions.Add(consoleOutputWriter);
            invoker.Invoke(inputs ?? new StringDictionary());
            return consoleOutputWriter.ToString();
        }

        internal static Activity GetActivityFromXamlResource(TestXamls xamlName, bool compileExpressions = false)
        {
            var xamlStream = GetXamlStream(xamlName);
            return ActivityXamlServices.Load(xamlStream, new ActivityXamlServicesSettings { CompileExpressions = compileExpressions });
        }

        public static Stream GetXamlStream(TestXamls xamlName)
        {
            var asm = typeof(TestHelper).Assembly;
            return asm.GetManifestResourceStream($"{asm.GetName().Name}.TestXamls.{xamlName}.xaml");
        }
    }

    public enum TestXamls
    {
        NonGenericForEach,
        SalaryCalculation,
        CSharpCalculation,
        SimpleWorkflowWithArgsAndVar,
        IfThenElseBranchWithVars,
        NestedSequencesWithVars,
        WorkflowWithReadonlyValueTypeVar,
        SpecialCharacters,
        ValueSpecialCharacterCSharp,
        ValueSpecialCharacterVb,
    }
}

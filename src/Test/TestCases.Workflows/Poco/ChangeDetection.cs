using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using TestCases.Workflows;
using TestCases.Workflows.WF4Samples;
using Xunit;
using Activity = System.Activities.Activity;
using Project = TestCases.Workflows.TestUtils.Project;
using System.Threading.Channels;
using TestCases.Apps;


namespace TestCases.Apps
{
    public class AppsNotifier
    {
        public static List<string> Changes { get; } = new List<string>();
        public static void Notify(Expression expr)
        {
            if( expr is  LambdaExpression lambda)
            {
                if (lambda.Body is MemberExpression mbExpression)
                {

                    Changes.Add(mbExpression.Member.Name);
                }
            }
        }
    }
}

namespace Poco
{
    public class ChangeDetection 
    {

        [Fact]
        public async Task DetectChanges()
        {
            var activity = TestHelper.GetActivityFromXamlResource(TestXamls.WorkflowWithReadonlyValueTypeVar);
            var compiledExpressionRoot = await GenerateAndLoadCompiledExpressionRoot(activity);
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(activity, compiledExpressionRoot);

            TestHelper.InvokeWorkflow(activity).ShouldBe(string.Empty);

            AppsNotifier.Changes.First().ShouldBe("dt");
        }

        private static async Task<ICompiledExpressionRoot> GenerateAndLoadCompiledExpressionRoot(Activity activity)
        {
            using var expressionsStringWriter = new StringWriter();
            var activityName = $"{TestXamls.WorkflowWithReadonlyValueTypeVar}_CompiledExpressionRoot";
            TextExpressionCompilerSettings settings = new()
            {
                Activity = activity,
                Language = "C#",
                ActivityName = activityName,
                RootNamespace = null,
                GenerateAsPartialClass = false,
                AlwaysGenerateSource = true,
                ForImplementation = true
            };
            new TextExpressionCompiler(settings).GenerateSource(expressionsStringWriter);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
                .Select(References.GetReference).ToArray();
            var project = new Project(assemblies);
            var compiledExpressionsClassType = await project.Compile(expressionsStringWriter.ToString(), activityName);
            return (ICompiledExpressionRoot)Activator.CreateInstance(compiledExpressionsClassType, activity);
        }
    }
}
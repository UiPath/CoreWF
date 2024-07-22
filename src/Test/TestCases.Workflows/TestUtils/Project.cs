using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace TestCases.Workflows.TestUtils
{
    internal class Project
    {
        static readonly MefHostServices HostServices = MefHostServices.Create(new[]{ "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.Features", "Microsoft.CodeAnalysis.CSharp.Features" }
            .Select(Assembly.Load));
        private readonly AdhocWorkspace _workspace = new(HostServices);
        private readonly MetadataReference[] _references;
        public Project(MetadataReference[] references) => _references = references;
        public async Task<Type> Compile(string classCode, string className)
        {
            _workspace.ClearSolution();
            CSharpCompilationOptions compilationOptions = new(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
            var scriptProjectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), className, className, LanguageNames.CSharp)
                .WithMetadataReferences(_references)
                .WithCompilationOptions(compilationOptions);
            var scriptProject = _workspace.AddProject(scriptProjectInfo);
            _workspace.AddDocument(scriptProject.Id, className, SourceText.From(classCode));
            var compilation = await _workspace.CurrentSolution.Projects.First().GetCompilationAsync();
            using var output = File.OpenWrite("Output.dll");
            var results = ScriptingAotCompiler.BuildAssembly(compilation, className, AssemblyLoadContext.Default, output);
            if (results.HasErrors)
            {
                throw new SourceExpressionException(results.ToString());
            }
            return results.ResultType;
        }
    }
}
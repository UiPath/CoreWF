using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TestCases.Workflows.WF4Samples;

namespace TestCases.Workflows.TestUtils
{
    internal class Project
    {
        static readonly MefHostServices HostServicesCS = MefHostServices.Create(new[]{ "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.CSharp.Workspaces", "Microsoft.CodeAnalysis.Features", "Microsoft.CodeAnalysis.CSharp.Features" }
            .Select(Assembly.Load));
        static readonly MefHostServices HostServicesVB = MefHostServices.Create(new[]{ "Microsoft.CodeAnalysis.Workspaces",
            "Microsoft.CodeAnalysis.VisualBasic.Workspaces", "Microsoft.CodeAnalysis.Features", "Microsoft.CodeAnalysis.VisualBasic.Features" }
            .Select(Assembly.Load));
        private readonly Dictionary<Language, AdhocWorkspace> _workspaces = new()
        {
            { Language.CSharp, new AdhocWorkspace(HostServicesCS) },
            { Language.VisualBasic, new AdhocWorkspace(HostServicesVB) }
        };
        private readonly MetadataReference[] _references;
        public Project(MetadataReference[] references) => _references = references;
        public async Task<Type> Compile(string classCode, string className, Language language)
        {
            if (_workspaces.TryGetValue(language, out var workspace) == false)
            {
                throw new NotSupportedException(nameof(language));
            }
            workspace.ClearSolution();

            CompilationOptions compilationOptions = language == Language.CSharp
                ? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
                : new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);

            var scriptProjectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), className, className, 
                language == Language.CSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic)
                .WithMetadataReferences(_references)
                .WithCompilationOptions(compilationOptions);

            var scriptProject = workspace.AddProject(scriptProjectInfo);
            workspace.AddDocument(scriptProject.Id, className, SourceText.From(classCode));
            var compilation = await workspace.CurrentSolution.Projects.First().GetCompilationAsync();
            //using var output = File.OpenWrite("Output.dll");
            var results = ScriptingAotCompiler.BuildAssembly(compilation, className, AssemblyLoadContext.Default/*, output*/);
            if (results.HasErrors)
            {
                throw new SourceExpressionException(results.ToString());
            }
            return results.ResultType;
        }
    }
}
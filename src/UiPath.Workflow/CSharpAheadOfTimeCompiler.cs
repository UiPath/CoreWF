using System.Linq;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace UiPath.Workflow
{
    class CSharpAheadOfTimeCompiler : AheadOfTimeCompiler
    {
        public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile)
        {
            var results = new TextExpressionCompilerResults();
            var scriptOptions = ScriptOptions.Default.WithReferences(classToCompile.References).WithImports(classToCompile.Imports);
            var compilation = CSharpScript.Create(classToCompile.Code, scriptOptions).GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            AddDiagnostics(diagnostics);
            if (results.HasErrors())
            {
                return results;
            }
            byte[] scriptAssemblyBytes;
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);
                AddDiagnostics(emitResult.Diagnostics);
                if (!emitResult.Success)
                {
                    return results;
                }
                scriptAssemblyBytes = stream.ToArray();
            }
            results.ResultType = Assembly.Load(scriptAssemblyBytes).GetType(compilation.ScriptClass.Name).GetNestedType(classToCompile.ClassName);
            return results;
            void AddDiagnostics(IEnumerable<Diagnostic> diagnosticsToAdd) => 
                results.AddMessages(diagnosticsToAdd.Select(diagnostic => new TextExpressionCompilerError
                    {
                        SourceLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line,
                        Number = diagnostic.Id,
                        Message = diagnostic.ToString(),
                        IsWarning = diagnostic.Severity < DiagnosticSeverity.Error,
                    }));
        }
    }
}
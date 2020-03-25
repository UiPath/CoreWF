using System;
using System.Linq;
using System.Activities.XamlIntegration;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.IO;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace UiPath.Workflow
{
    class CSharpAheadOfTimeCompiler : AheadOfTimeCompiler
    {
        public override CompilerResults Compile(CompilerParameters options, CodeCompileUnit compilationUnit)
        {
            var results = new CompilerResults(options.TempFiles);
            var code = compilationUnit.GetCSharpCode();
            var scriptOptions = ScriptOptions.Default.WithReferences(options.GetReferences());
            var script = CSharpScript.Create(code, scriptOptions);
            var compilation = script.GetCompilation();
            var diagnostics = compilation.GetDiagnostics();
            AddDiagnostics(diagnostics);
            if (results.Errors.HasErrors)
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
            results.CompiledAssembly = Assembly.Load(scriptAssemblyBytes);
            return results;
            void AddDiagnostics(IEnumerable<Diagnostic> diagnosticsToAdd) => results.Errors.AddRange(diagnosticsToAdd.Select(ToCompilerError).ToArray());
            CompilerError ToCompilerError(Diagnostic diagnostic)
            {
                var lineSpan = diagnostic.Location.GetMappedLineSpan();
                return new CompilerError
                {
                    Column = lineSpan.StartLinePosition.Character,
                    Line = lineSpan.StartLinePosition.Line,
                    FileName = lineSpan.Path,
                    ErrorNumber = diagnostic.Id,
                    ErrorText = diagnostic.Descriptor.Description.ToString(),
                    IsWarning = diagnostic.Severity < DiagnosticSeverity.Error,
                };
            }
        }
    }
}
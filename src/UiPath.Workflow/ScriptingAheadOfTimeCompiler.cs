using System.Linq;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;

namespace UiPath.Workflow
{
    public abstract class ScriptingAheadOfTimeCompiler : AheadOfTimeCompiler
    {
        protected abstract Script<object> Create(string code, ScriptOptions options);
        public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile)
        {
            var scriptOptions = ScriptOptions.Default.WithReferences(classToCompile.ReferencedAssemblies).WithImports(classToCompile.ImportedNamespaces);
            var script = Create(classToCompile.Code, scriptOptions);
            var results = Compile(script);
            if (results.HasErrors())
            {
                return results;
            }
            results.ResultType = results.ResultType.GetNestedType(classToCompile.ClassName);
            return results;
        }
        internal static TextExpressionCompilerResults Compile(Script script)
        {
            var results = new TextExpressionCompilerResults();
            var compilation = script.GetCompilation();
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
            results.ResultType = Assembly.Load(scriptAssemblyBytes).GetType(compilation.ScriptClass.Name);
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
    public class CSharpAheadOfTimeCompiler : ScriptingAheadOfTimeCompiler
    {
        protected override Script<object> Create(string code, ScriptOptions options) => CSharpScript.Create(code, options);
    }
    public class VbAheadOfTimeCompiler : ScriptingAheadOfTimeCompiler
    {
        protected override Script<object> Create(string code, ScriptOptions options) => VisualBasicScript.Create(code, options);
    }
}
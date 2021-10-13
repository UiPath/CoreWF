using System.Linq;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;

namespace System.Activities
{
    public abstract class AheadOfTimeCompiler
    {
        public abstract TextExpressionCompilerResults Compile(ClassToCompile classToCompile);
    }
    public record ClassToCompile(string ClassName, string Code, IReadOnlyCollection<Assembly> ReferencedAssemblies, IReadOnlyCollection<string> ImportedNamespaces) 
        : CompilerInput(Code, ImportedNamespaces)
    {
    }
    public abstract class ScriptingAotCompiler : AheadOfTimeCompiler
    {
        protected abstract Script<object> Create(string code, ScriptOptions options);
        public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile)
        {
            var scriptOptions = ScriptOptions.Default.WithReferences(classToCompile.ReferencedAssemblies.GetMetadataReferences()).WithImports(classToCompile.ImportedNamespaces);
            var script = Create(classToCompile.Code, scriptOptions);
            var results = BuildAssembly(script.GetCompilation());
            if (results.HasErrors)
            {
                return results;
            }
            results.ResultType = results.ResultType.GetNestedType(classToCompile.ClassName);
            return results;
        }
        internal static TextExpressionCompilerResults BuildAssembly(Compilation compilation)
        {
            var results = new TextExpressionCompilerResults();
            var diagnostics = compilation.GetDiagnostics();
            AddDiagnostics(diagnostics);
            if (results.HasErrors)
            {
                return results;
            }
            using var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);
            AddDiagnostics(emitResult.Diagnostics);
            if (!emitResult.Success)
            {
                return results;
            }
            results.ResultType = Assembly.Load(stream.GetBuffer()).GetType(compilation.ScriptClass.Name);
            return results;
            void AddDiagnostics(IEnumerable<Diagnostic> diagnosticsToAdd) =>
                results.AddMessages(diagnosticsToAdd.Select(TextExpressionCompilerError.Create));
        }
    }
    public class CSharpAotCompiler : ScriptingAotCompiler
    {
        protected override Script<object> Create(string code, ScriptOptions options) => CSharpScript.Create(code, options);
    }
    public class VbAotCompiler : ScriptingAotCompiler
    {
        protected override Script<object> Create(string code, ScriptOptions options) => VisualBasicScript.Create(code, options);
    }
}
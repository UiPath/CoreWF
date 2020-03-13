using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace System.Activities.XamlIntegration
{
    internal class TextExpressionCompilerRoslyn
    {
        private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location);
        private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);
        private static readonly MetadataReference SystemReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemLinqReference = MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location);
        private static readonly MetadataReference SystemComponentModelReference = MetadataReference.CreateFromFile(typeof(BrowsableAttribute).Assembly.Location);
        private static readonly MetadataReference SystemCodeDomReference = MetadataReference.CreateFromFile(typeof(GeneratedCodeAttribute).Assembly.Location);
        private static readonly MetadataReference SystemActivitiesReference = MetadataReference.CreateFromFile(typeof(TextExpressionCompiler).Assembly.Location);
        private static readonly MetadataReference VisualBasicReference = MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.CompilerServices.Operators).Assembly.Location);

        internal static TextExpressionCompilerResults CompileWithRoslyn(string language, CodeCompileUnit compileUnit, CodeDomProvider codeDomProvider, CompilerParameters compilerParameters, string activityFullName)
        {
            var isVisualBasic = language == "VB";
            var results = new TextExpressionCompilerResults();

            var memoryStream = new MemoryStream();
            using (var tw = new StreamWriter(memoryStream))
            {
                codeDomProvider.GenerateCodeFromCompileUnit(compileUnit, tw, new CodeGeneratorOptions());
            }

            var code = Encoding.UTF8.GetString(memoryStream.ToArray());
            var syntaxTree = isVisualBasic ? VisualBasicSyntaxTree.ParseText(code) : CSharpSyntaxTree.ParseText(code);

            // https://stackoverflow.com/questions/46421686/how-to-write-a-roslyn-analyzer-that-references-a-dotnet-standard-2-0-project
            var references = new List<MetadataReference>()
            {
                NetStandardReference,
                SystemRuntimeReference,
                SystemReference,
                SystemLinqReference,
                SystemComponentModelReference,
                SystemCodeDomReference,
                SystemActivitiesReference
            };

            if (isVisualBasic)
            {
                references.Add(VisualBasicReference);
            }

            foreach (var r in compilerParameters.ReferencedAssemblies)
            {
                references.Add(MetadataReference.CreateFromFile(r));
            }

            Compilation compilation;

            if (isVisualBasic)
            {
                compilation = VisualBasicCompilation.Create(
                    "assemblyName",
                    new[] { syntaxTree },
                    references,
                    new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
            else
            {
                compilation = CSharpCompilation.Create(
                    "assemblyName",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                {
                    results.HasErrors = true;

                    var errors = new List<TextExpressionCompilerError>();
                    foreach (var diagnostic in emitResult.Diagnostics.Where(x => x.WarningLevel <= 1))
                    {
                        errors.Add(new TextExpressionCompilerError()
                        {
                            IsWarning = diagnostic.WarningLevel > 0,
                            Message = diagnostic.GetMessage(),
                            Number = diagnostic.Id,
                            SourceLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line
                        });
                    }

                    results.SetMessages(errors, true);
                }
                else
                {
                    var assembly = Assembly.Load(dllStream.ToArray());

                    results.HasErrors = false;
                    results.ResultType = assembly.GetType(activityFullName);
                }
            }

            return results;
        }
    }
}
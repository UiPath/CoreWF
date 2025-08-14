using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Activities
{
    public sealed class VBCompilerHelper : CompilerHelper
    {
        private static int crt = 0;

        public override Compilation DefaultCompilationUnit { get; } = InitDefaultCompilationUnit();

        public override int IdentifierKind => (int)SyntaxKind.IdentifierName;

        public override StringComparer IdentifierNameComparer { get; } = StringComparer.OrdinalIgnoreCase;

        public override StringComparison IdentifierNameComparison { get; } = StringComparison.OrdinalIgnoreCase;

        public override VisualBasicParseOptions ScriptParseOptions { get; } = new VisualBasicParseOptions(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);


        public override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);

        public override string CreateExpressionCode(string[] types, string[] names, string code)
        {
            var typesStr = string.Join(CompilerHelper.Comma, types);
            var namesStr = string.Join(CompilerHelper.Comma, names);
            if (types.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
                return $"Public Shared Function CreateExpression() As Expression(Of Func(Of {typesStr}))\nReturn Function({namesStr}) ({code})\nEnd Function";

            var (myDelegate, name) = DefineDelegate(types);
            return $"{myDelegate} \n Public Shared Function CreateExpression() As Expression(Of {name}(Of {typesStr}))\nReturn Function({namesStr}) ({code})\nEnd Function";
        }

        protected override (string, string) DefineDelegateCommon(int argumentsCount)
        {
            var crtValue = Interlocked.Add(ref crt, 1);
            var part1 = new StringBuilder();
            var part2 = new StringBuilder();

            for (var i = 0; i < argumentsCount; i++)
            {
                part1.Append($" In T{i},");
                part2.Append($" ByVal arg as T{i},");
            }
            part2.Remove(part2.Length - 1, 1);

            var name = $"Func{crtValue}";
            return ($"Public Delegate Function {name}(Of {part1} Out TResult)({part2}) As TResult", name);
        }

        private static Compilation InitDefaultCompilationUnit()
        {
            VisualBasicCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                mainTypeName: null,
                globalImports: null,
                rootNamespace: "",
                optionStrict: OptionStrict.On,
                optionInfer: true,
                optionExplicit: true,
                optionCompareText: false,
                embedVbCoreRuntime: false,
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")));
            return VisualBasicCompilation.Create(Guid.NewGuid().ToString(), null, null, options);
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using ReflectionMagic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Activities
{
    public sealed class CSharpCompilerHelper : CompilerHelper
    {
        private static int crt = 0;
        private static readonly dynamic s_typeNameFormatter = GetTypeNameFormatter();
        private static readonly dynamic s_typeOptions = GetTypeOptions();

        public override Compilation DefaultCompilationUnit { get; } = InitDefaultCompilationUnit();

        public override int IdentifierKind => (int)SyntaxKind.IdentifierName;

        public override CSharpParseOptions ScriptParseOptions { get; } = new CSharpParseOptions(kind: SourceCodeKind.Script);

        public override StringComparer IdentifierNameComparer { get; } = StringComparer.Ordinal;

        public override string GetTypeName(Type type) =>
            (string)s_typeNameFormatter.FormatTypeName(type, s_typeOptions);

        public override string CreateExpressionCode(string types, string names, string code)
        {
            var arrayType = types.Split(Comma);
            if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
                return $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";

            var (myDelegate, name) = DefineDelegate(types);
            return $"{myDelegate} \n public static Expression<{name}<{types}>> CreateExpression() => ({names}) => {code};";
        }

        public override (string, string) DefineDelegate(string types)
        {
            var crtValue = Interlocked.Add(ref crt, 1);
            var arrayType = types.Split(",");
            var part1 = new StringBuilder();
            var part2 = new StringBuilder();

            for (var i = 0; i < arrayType.Length - 1; i++)
            {
                part1.Append($"in T{i}, ");
                part2.Append($" T{i} arg{i},");
            }
            part2.Remove(part2.Length - 1, 1);
            var name = $"Func{crtValue}";
            return ($"public delegate TResult {name}<{part1} out TResult>({part2});", name);
        }

        private static object GetTypeNameFormatter()
        {
            return typeof(CSharpScript)
                .Assembly
                .GetType("Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.CSharpObjectFormatter")
                .AsDynamicType()
                .s_impl
                .TypeNameFormatter;
        }

        private static Compilation InitDefaultCompilationUnit()
        {
            CSharpCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                mainTypeName: null,
                usings: null,
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: false,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            return CSharpCompilation.Create(Guid.NewGuid().ToString(), null, null, options);
        }

        private static object GetTypeOptions()
        {
            var formatterOptionsType =
                typeof(ObjectFormatter).Assembly.GetType(
                    "Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
            const int arrayBoundRadix = 0;
            const bool showNamespaces = true;
            return Activator.CreateInstance(formatterOptionsType, arrayBoundRadix, showNamespaces);
        }
    }
}

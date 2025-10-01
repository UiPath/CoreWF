using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using ReflectionMagic;
using System.Runtime.InteropServices;

namespace System.Activities
{
    public sealed class CSharpCompilerHelper : CompilerHelper
    {
        private static readonly dynamic s_typeNameFormatter = GetTypeNameFormatter();
        private static readonly dynamic s_typeOptions = GetTypeOptions();

        public override Compilation DefaultCompilationUnit { get; } = InitDefaultCompilationUnit();

        public override int IdentifierKind => (int)SyntaxKind.IdentifierName;

        public override CSharpParseOptions ScriptParseOptions { get; } = new CSharpParseOptions(kind: SourceCodeKind.Script);

        public override StringComparer IdentifierNameComparer { get; } = StringComparer.Ordinal;

        public override StringComparison IdentifierNameComparison { get; } = StringComparison.Ordinal;

        public override string GetTypeName(Type type) =>
            (string)s_typeNameFormatter.FormatTypeName(type, s_typeOptions);

        public override string CreateExpressionCode(string[] types, string[] names, string code)
        {
            var typesStr = string.Join(CompilerHelper.Comma, types);
            var namesStr = string.Join(CompilerHelper.Comma, names);
            if (types.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
                return $"public static Expression<Func<{typesStr}>> CreateExpression() => ({namesStr}) => {code};";

            var (myDelegate, name) = DefineDelegate(types);
            return $"{myDelegate} \n public static Expression<{name}<{typesStr}>> CreateExpression() => ({namesStr}) => {code};";
        }

        internal string CreateReferenceCode(string[] types, string returnType, string[] names, string code)
        {
            var strTypes = string.Join(Comma, types);
            var strNames = string.Join(Comma, names);
            return CSharpValidatorCommon.CreateReferenceCode(strTypes, returnType, strNames, code, string.Empty, 0);
        }

        internal string CreateValueCode(string[] types, string[] names, string code)
            => CSharpValidatorCommon.CreateValueCode(types, string.Join(Comma, names), code, string.Empty, 0);

        protected override (string, string) DefineDelegateCommon(int argumentsCount)
            => CSharpValidatorCommon.DefineDelegateCommon(argumentsCount);

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

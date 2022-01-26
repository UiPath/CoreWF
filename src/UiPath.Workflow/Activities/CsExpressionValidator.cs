using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using ReflectionMagic;
using System.Runtime.InteropServices;

namespace System.Activities;

/// <summary>
/// Validates C# expressions for use in fast design-time expression validation.
/// </summary>
public class CsExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<CsExpressionValidator> s_default = new();
    private static CsExpressionValidator s_instance;

    private static readonly CSharpParseOptions s_csScriptParseOptions = new(kind: SourceCodeKind.Script);

    /// <summary>
    /// Singleton instance of the default validator.
    /// </summary>
    public static CsExpressionValidator Instance 
    { 
        get { return s_instance ?? s_default.Value; } 
        set { s_instance = value; } 
    }

    private static readonly dynamic TypeOptions = GetTypeOptions();
    private static readonly dynamic TypeNameFormatter = GetTypeNameFormatter();

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    protected override Compilation GetCompilationUnit(ExpressionToCompile expressionToValidate)
    {
        if (CompilationUnit == null)
        {
            string assemblyName = Guid.NewGuid().ToString();
            CSharpCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                mainTypeName: null,
                usings: expressionToValidate.ImportedNamespaces,
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: false,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            return CSharpCompilation.Create(assemblyName, null, MetadataReferences, options);
        }
        else
        {
            // Replace imports
            var options = CompilationUnit.Options as CSharpCompilationOptions;
            return CompilationUnit.WithOptions(options.WithUsings(expressionToValidate.ImportedNamespaces));
        }
    }

    protected override string CreateValidationCode(string types, string names, string code) =>
        $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate) => 
        CSharpSyntaxTree.ParseText(expressionToValidate.Code, s_csScriptParseOptions);

    protected override string GetTypeName(Type type) => (string)TypeNameFormatter.FormatTypeName(type, TypeOptions);

    static object GetTypeOptions()
    {
        var formatterOptionsType = typeof(ObjectFormatter).Assembly.GetType("Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
        const int ArrayBoundRadix = 0;
        const bool ShowNamespaces = true;
        return Activator.CreateInstance(formatterOptionsType, new object[] { ArrayBoundRadix, ShowNamespaces });
    }

    static object GetTypeNameFormatter() =>
        typeof(CSharpScript).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.CSharpObjectFormatter")
        .AsDynamicType()
        .s_impl
        .TypeNameFormatter;
}

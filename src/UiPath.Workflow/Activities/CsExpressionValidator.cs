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

    private static readonly CSharpParseOptions s_csScriptParseOptions = new(kind: SourceCodeKind.Script);

    /// <summary>
    /// Singleton instance of the default validator.
    /// </summary>
    public static CsExpressionValidator Default { get { return s_default.Value; } }

    private static readonly dynamic TypeOptions = GetTypeOptions();
    private static readonly dynamic TypeNameFormatter = GetTypeNameFormatter();

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    protected override Compilation CreateCompilationUnit()
    {
        string assemblyName = Guid.NewGuid().ToString();
        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: false,
            xmlReferenceResolver: null,
            sourceReferenceResolver: SourceFileResolver.Default,
            concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        return CSharpCompilation.Create(assemblyName, null, MetadataReferences, options);
    }

    protected override string CreateValidationCode(string parameters, string returnType, string code) =>
         $"public static {returnType} ExpressionToValidate({parameters}) => ({code});";

    protected override string FormatParameter(string name, string type) => $"{type} {name}";

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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using System.Runtime.InteropServices;

namespace System.Activities;

/// <summary>
/// Validates VB.NET expressions for use in fast design-time expression validation.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_default = new();
    private static VbExpressionValidator s_instance;

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions = new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

    /// <summary>
    /// Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Instance
    {
        get { return s_instance ?? s_default.Value; }
        set { s_instance = value; }
    }

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    protected override Compilation GetCompilationUnit(ExpressionToCompile expressionToValidate)
    {
        var globalImports = GlobalImport.Parse(expressionToValidate.ImportedNamespaces);

        if (CompilationUnit == null)
        {
            string assemblyName = Guid.NewGuid().ToString();
            VisualBasicCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                mainTypeName: null,
                globalImports: globalImports,
                rootNamespace: "",
                optionStrict: OptionStrict.On,
                optionInfer: true,
                optionExplicit: true,
                optionCompareText: false,
                embedVbCoreRuntime: false,
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: false,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            return VisualBasicCompilation.Create(assemblyName, null, MetadataReferences, options);
        }
        else
        {
            // Replace imports
            var options = CompilationUnit.Options as VisualBasicCompilationOptions;
            return CompilationUnit.WithOptions(options.WithGlobalImports(globalImports));
        }
    }

    protected override string CreateValidationCode(string types, string names, string code) =>
        $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate) => 
        VisualBasicSyntaxTree.ParseText(expressionToValidate.Code, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}

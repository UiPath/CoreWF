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

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions = new(kind: SourceCodeKind.Script);

    /// <summary>
    /// Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Default { get { return s_default.Value; } }

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    protected override Compilation CreateCompilationUnit()
    {
        string assemblyName = Guid.NewGuid().ToString();
        VisualBasicCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
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

    protected override string CreateValidationCode(string parameters, string returnType, string code) =>
         $"Function ExpressionToValidate({parameters}) As {returnType}\nReturn ({code})\nEnd Function";

    protected override string FormatParameter(string name, string type) => $"{name} As {type}";

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate) => 
        VisualBasicSyntaxTree.ParseText(expressionToValidate.Code, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}

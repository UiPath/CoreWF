// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.VisualBasic.Activities;

namespace System.Activities;

/// <summary>
///     Validates VB.NET expressions for use in fast design-time expression validation.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_default = new();
    private static VbExpressionValidator s_instance;

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions =
        new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);
    protected override StringComparer IdentifierNameComparer => StringComparer.OrdinalIgnoreCase;

    private static readonly HashSet<Assembly> s_defaultReferencedAssemblies = new()
    {
        typeof(Collections.ICollection).Assembly,
        typeof(Enum).Assembly,
        typeof(ComponentModel.BrowsableAttribute).Assembly,
        typeof(VisualBasicValue<>).Assembly,
    };

    private Compilation DefaultCompilationUnit;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Instance
    {
        get => s_instance ?? s_default.Value;
        set => s_instance = value;
    }

    protected override int IdentifierKind => (int)SyntaxKind.IdentifierName;

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    public VbExpressionValidator() : this(null) { }

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    public VbExpressionValidator(HashSet<Assembly> referencedAssemblies)
        : base(referencedAssemblies != null
               ? new HashSet<Assembly>(s_defaultReferencedAssemblies.Union(referencedAssemblies))
               : s_defaultReferencedAssemblies)
    { }

    protected override void UpdateCompilationUnit(ExpressionContainer expressionContainer)
    {
        var globalImports = GlobalImport.Parse(expressionContainer.ExpressionToValidate.ImportedNamespaces);
        var metadataReferences = GetMetadataReferencesForExpression(expressionContainer);

        if (DefaultCompilationUnit == null)
        {
            var assemblyName = Guid.NewGuid().ToString();
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
                checkOverflow: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")));
            expressionContainer.CompilationUnit = DefaultCompilationUnit =
                VisualBasicCompilation.Create(assemblyName, null, metadataReferences, options);
        }
        else
        {
            var options = DefaultCompilationUnit.Options as VisualBasicCompilationOptions;
            var compilation = DefaultCompilationUnit.WithOptions(options!.WithGlobalImports(globalImports));
            expressionContainer.CompilationUnit = compilation.WithReferences(metadataReferences);
        }
    }

    protected override string CreateValidationCode(string types, string names, ExpressionContainer expressionContainer)
    {
        var code = expressionContainer.ExpressionToValidate.Code;
        return
            $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionContainer expressionContainer) =>
        VisualBasicSyntaxTree.ParseText("? " + expressionContainer.ExpressionToValidate.Code, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}

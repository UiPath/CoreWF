﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.VisualBasic.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Activities;

/// <summary>
///     Validates VB.NET expressions for use in fast design-time expression validation.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_default = new();
    private static VbExpressionValidator s_instance;

    private const string _referenceValidationTemplate = "Public Shared Function IsLocation() As {0}\nReturn Function({1}) as Action \nReturn Sub() {2} = CType(Nothing, {3})\nEnd Function\nEnd Function";

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions =
        new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

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

    protected override CompilerHelper CompilerHelper { get; } = new VBCompilerHelper();

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

    protected override string CreateValueCode(string types, string names, string code)
     => CompilerHelper.CreateExpressionCode(types, names, code);

    protected override string CreateReferenceCode(string types, string names, string code, string returnType)
    {
        var actionDefinition = !string.IsNullOrWhiteSpace(types) 
            ? $"Action(Of {string.Join(Comma, types)})" 
            : "Action";
        return string.Format(_referenceValidationTemplate, actionDefinition, names, code, returnType);
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionContainer expressionContainer) =>
        VisualBasicSyntaxTree.ParseText("? " + expressionContainer.ExpressionToValidate.Code, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}

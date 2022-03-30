﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CSharp.Activities;
using ReflectionMagic;

namespace System.Activities;

/// <summary>
///     Validates C# expressions for use in fast design-time expression validation.
/// </summary>
public class CsExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<CsExpressionValidator> s_default = new();
    private static CsExpressionValidator s_instance;

    private static readonly CSharpParseOptions s_csScriptParseOptions = new(kind: SourceCodeKind.Script);

    private static readonly dynamic s_typeOptions = GetTypeOptions();
    private static readonly dynamic s_typeNameFormatter = GetTypeNameFormatter();

    private static readonly HashSet<Assembly> s_defaultReferencedAssemblies = new()
    {
        typeof(Collections.ICollection).Assembly,
        typeof(ICollection<>).Assembly,
        typeof(Enum).Assembly,
        typeof(ComponentModel.BrowsableAttribute).Assembly,
        typeof(CSharpValue<>).Assembly,
    };

    private Compilation DefaultCompilationUnit;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static CsExpressionValidator Instance
    {
        get => s_instance ?? s_default.Value;
        set => s_instance = value;
    }

    protected override int IdentifierKind => (int) SyntaxKind.IdentifierName;

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    public CsExpressionValidator() : this(null) { }

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    public CsExpressionValidator(HashSet<Assembly> referencedAssemblies)
        : base(referencedAssemblies != null
               ? new HashSet<Assembly>(s_defaultReferencedAssemblies.Union(referencedAssemblies))
               : s_defaultReferencedAssemblies)
    { }

    protected override void UpdateCompilationUnit(ExpressionContainer expressionContainer)
    {
        var metadataReferences = GetMetadataReferencesForExpression(expressionContainer);

        if (DefaultCompilationUnit == null)
        {
            var assemblyName = Guid.NewGuid().ToString();
            CSharpCompilationOptions options = new(
                OutputKind.DynamicallyLinkedLibrary,
                mainTypeName: null,
                usings: expressionContainer.ExpressionToValidate.ImportedNamespaces,
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: false,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
            expressionContainer.CompilationUnit = DefaultCompilationUnit = 
                CSharpCompilation.Create(assemblyName, null, metadataReferences, options);
        }
        else
        {
            var options = DefaultCompilationUnit.Options as CSharpCompilationOptions;
            var compilation = DefaultCompilationUnit.WithOptions(
                options.WithUsings(expressionContainer.ExpressionToValidate.ImportedNamespaces));

            var missingReferences = compilation.References.Except(metadataReferences);
            expressionContainer.CompilationUnit =
                missingReferences.Any()
                ? compilation.AddReferences(missingReferences)
                : compilation;
        }
    }

    protected override string CreateValidationCode(string types, string names, string code) => 
        $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";

    protected override SyntaxTree GetSyntaxTreeForExpression(ExpressionContainer expressionContainer) => 
        CSharpSyntaxTree.ParseText(expressionContainer.ExpressionToValidate.Code, s_csScriptParseOptions);

    protected override string GetTypeName(Type type) => 
        (string) s_typeNameFormatter.FormatTypeName(type, s_typeOptions);

    private static object GetTypeOptions()
    {
        var formatterOptionsType =
            typeof(ObjectFormatter).Assembly.GetType(
                "Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
        const int arrayBoundRadix = 0;
        const bool showNamespaces = true;
        return Activator.CreateInstance(formatterOptionsType, arrayBoundRadix, showNamespaces);
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
}

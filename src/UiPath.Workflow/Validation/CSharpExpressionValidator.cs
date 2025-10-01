// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UiPath.Workflow.Validation;
using static System.Activities.CompilerHelper;

namespace System.Activities.Validation;

/// <summary>
///     Validates C# expressions for use in fast design-time expression validation.
///     ⚠️ Do not seal this class, required for customization by certain hosts.
/// </summary>
public class CSharpExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<CSharpExpressionValidator> s_instance = new(() => new());
    public override string Language => CSharpHelper.Language;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static CSharpExpressionValidator Instance { get; set; } = s_instance.Value;

    protected override CSharpCompilerHelper CompilerHelper { get; } = new CSharpCompilerHelper();

    protected override string ActivityIdentifierRegex { get; } = @"(\/\/activityId):(.*)";

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    protected CSharpExpressionValidator(HashSet<Assembly> referencedAssemblies = null)
        : base(referencedAssemblies)
    { }

    protected override Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces, ValidationSettings validationSettings = null)
    {
        var metadataReferences = GetMetadataReferencesForExpression(assemblies);

        var options = CompilerHelper.DefaultCompilationUnit.Options as CSharpCompilationOptions;

        options = options.WithUsings(namespaces);

        if (validationSettings?.MissingAssemblyResolver is Func<AssemblyName, Assembly> resolver)
        {
            options = options.WithMetadataReferenceResolver(new ExternalMetadataReferenceResolver(resolver));
        }

        var compilation = CompilerHelper.DefaultCompilationUnit.WithOptions(options).WithReferences(metadataReferences);

        return compilation;
    }

    protected override string CreateValueCode(IEnumerable<string> types, string names, string code, string activityId, int index)
        => CSharpValidatorCommon.CreateValueCode(types, names, code, activityId, index);

    protected override string CreateReferenceCode(string types, string names, string code, string activityId, string returnType, int index)
        => CSharpValidatorCommon.CreateReferenceCode(types, returnType, names, code, activityId, index);

    protected override SyntaxTree GetSyntaxTreeForExpression(string expressionText) =>
        CSharpSyntaxTree.ParseText(expressionText, CompilerHelper.ScriptParseOptions);

    protected override SyntaxTree GetSyntaxTreeForValidation(string expressionText) =>
        GetSyntaxTreeForExpression(expressionText);

    protected override string GetTypeName(Type type) => CompilerHelper.GetTypeName(type);
}
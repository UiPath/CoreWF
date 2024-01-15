// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.Activities;
using System.Collections.Generic;
using System.Reflection;
using static System.Activities.CompilerHelper;

namespace System.Activities.Validation;

/// <summary>
///     Validates C# expressions for use in fast design-time expression validation.
///     ⚠️ Do not seal this class, required for customization by certain hosts.
/// </summary>
public class CSharpExpressionValidator : RoslynExpressionValidator
{
    private const string _valueValidationTemplate = "public static System.Linq.Expressions.Expression<System.Func<{0}>> CreateExpression{1}()//activityId:{4}\n => ({2}) => {3};";
    private const string _delegateValueValidationTemplate = "{0}\npublic static System.Linq.Expressions.Expression<{1}<{2}>> CreateExpression{3}()//activityId:{6}\n => ({4}) => {5};";
    private const string _referenceValidationTemplate = "public static {0} IsLocation{1}()//activityId:{5}\n => ({2}) => {3} = default({4});";

    private static readonly Lazy<CSharpExpressionValidator> s_instance = new(() => new());
    public override string Language => CSharpHelper.Language;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static CSharpExpressionValidator Instance { get; set; } = s_instance.Value;

    protected override CSharpCompilerHelper CompilerHelper { get; } = new CSharpCompilerHelper();

    protected override string ActivityIdentifierRegex { get; } = @"(\/\/activityId):(.*)";

    protected override bool IsExpressionWellFormatted(string expressionText)
    {
        //TODO: validate the format;
        return true;
    }

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    protected CSharpExpressionValidator(HashSet<Assembly> referencedAssemblies = null)
        : base(referencedAssemblies)
    { }

    protected override Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces)
    {
        var metadataReferences = GetMetadataReferencesForExpression(assemblies);

        var options = CompilerHelper.DefaultCompilationUnit.Options as CSharpCompilationOptions;
        return CompilerHelper.DefaultCompilationUnit.WithOptions(options.WithUsings(namespaces)).WithReferences(metadataReferences);
    }

    protected override string CreateValueCode(string types, string names, string code, string activityId, int index)
    {
        var arrayType = types.Split(",");
        if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
            return string.Format(_valueValidationTemplate, types, index, names, code, activityId);

        var (myDelegate, name) = CompilerHelper.DefineDelegate(types);
        return string.Format(_delegateValueValidationTemplate, myDelegate, name, types, index, names, code, activityId);
    }

    protected override string CreateReferenceCode(string types, string names, string code, string activityId, string returnType, int index)
    {
        var actionDefinition = !string.IsNullOrWhiteSpace(types)
            ? $"Action<{string.Join(Comma, types)}>"
            : "Action";
        return string.Format(_referenceValidationTemplate, actionDefinition, index, names, code, returnType, activityId);
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expressionText) =>
        CSharpSyntaxTree.ParseText(expressionText, CompilerHelper.ScriptParseOptions);

    protected override SyntaxTree GetSyntaxTreeForValidation(string expressionText) =>
        GetSyntaxTreeForExpression(expressionText);

    protected override string GetTypeName(Type type) => CompilerHelper.GetTypeName(type);
}
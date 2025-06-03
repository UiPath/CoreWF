// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualBasic.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UiPath.Workflow.Validation;
using static System.Activities.CompilerHelper;

namespace System.Activities.Validation;
/// <summary>
///     Validates VB.NET expressions for use in fast design-time expression validation.
///     ⚠️ Do not seal this class, required for customization by certain hosts.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_instance = new(() => new());

    // This is used in case the expression does not properly close (missing quote)
    private const string _expressionEnder = "'\"";
    private const string _valueValidationTemplate = "Public Shared Function CreateExpression{0}() As System.Linq.Expressions.Expression(Of System.Func(Of {1}))'activityId:{4}\nReturn Function({2}) ({3}) {5}\nEnd Function";
    private const string _delegateValueValidationTemplate = "{0}\nPublic Shared Function CreateExpression{1}() As System.Linq.Expressions.Expression(Of {2}(Of {3}))'activityId:{6}\nReturn Function({4}) ({5}){7}\nEnd Function";
    private const string _referenceValidationTemplate = "Public Shared Function IsLocation{0}() As {1}'activityId:{5}\nReturn Function({2}) as System.Action\nReturn Sub() {3} = CType(Nothing, {4}){6}\nEnd Function\nEnd Function";

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    protected VbExpressionValidator(HashSet<Assembly> referencedAssemblies = null)
        : base(referencedAssemblies)
    { }

    public override string Language => VisualBasicHelper.Language;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Instance { get; set; } = s_instance.Value;

    protected override VBCompilerHelper CompilerHelper { get; } = new VBCompilerHelper();

    protected override string ActivityIdentifierRegex { get; } = "('activityId):(.*)";

    protected override Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces, ValidationSettings validationSettings = null)
    {
        var globalImports = GlobalImport.Parse(namespaces);
        var metadataReferences = GetMetadataReferencesForExpression(assemblies);

        var options = CompilerHelper.DefaultCompilationUnit.Options as VisualBasicCompilationOptions;
        var compilation = CompilerHelper.DefaultCompilationUnit.WithOptions(options!.WithGlobalImports(globalImports)).WithReferences(metadataReferences);

        if (validationSettings?.MissingAssemblyResolver is Func<AssemblyName, Assembly> resolver)
        {
            compilation = compilation.WithOptions(options.WithMetadataReferenceResolver(new ExternalMetadataReferenceResolver(resolver)));
        }

        return compilation;
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expressionText) =>
        VisualBasicSyntaxTree.ParseText("? " + expressionText, CompilerHelper.ScriptParseOptions);

    protected override SyntaxTree GetSyntaxTreeForValidation(string expressionText) =>
        VisualBasicSyntaxTree.ParseText(expressionText, CompilerHelper.ScriptParseOptions);

    protected override string GetTypeName(Type type) => CompilerHelper.GetTypeName(type);

    protected override string CreateValueCode(IEnumerable<string> types, string names, string code, string activityId, int index)
    {
        var serializedArgumentTypes = string.Join(Comma, types);
        if (types.Count() <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
            return string.Format(_valueValidationTemplate, index, serializedArgumentTypes, names, code, activityId, _expressionEnder);

        var (myDelegate, name) = CompilerHelper.DefineDelegate(types);
        return string.Format(_delegateValueValidationTemplate, myDelegate, index, name, serializedArgumentTypes, names, code, activityId, _expressionEnder);
    }

    protected override string CreateReferenceCode(string types, string names, string code, string activityId, string returnType, int index)
    {
        var actionDefinition = !string.IsNullOrWhiteSpace(types)
            ? $"System.Action(Of {string.Join(Comma, types)})"
            : "System.Action";
        return string.Format(_referenceValidationTemplate, index, actionDefinition, names, code, returnType, activityId, _expressionEnder);
    }
}

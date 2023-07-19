// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using Microsoft.VisualBasic.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Activities.Validation;
/// <summary>
///     Validates VB.NET expressions for use in fast design-time expression validation.
///     ⚠️ Do not seal this class, required for customization by certain hosts.
/// </summary>
public class VbExpressionValidator : RoslynExpressionValidator
{
    private static readonly Lazy<VbExpressionValidator> s_instance = new(() => new(s_defaultReferencedAssemblies));
    private static readonly CompilerHelper s_compilerHelper = new VBCompilerHelper();

    private const string _valueValidationTemplate = "Public Shared Function CreateExpression{0}() As Expression(Of Func(Of {1}))'activityId:{4}\nReturn Function({2}) ({3})'activityId:{4}\nEnd Function";
    private const string _delegateValueValidationTemplate = "{0}\nPublic Shared Function CreateExpression{1}() As Expression(Of {2}(Of {3}))'activityId:{6}\nReturn Function({4}) ({5})'activityId:{6}\nEnd Function";
    private const string _referenceValidationTemplate = "Public Shared Function IsLocation{0}() As {1}'activityId:{4}\nReturn Function({2}) as Action'activityId:{3}\nReturn Sub() {3} = CType(Nothing, {4})'activityId:{5}\nEnd Function'activityId:{4}\nEnd Function";

    private static readonly VisualBasicParseOptions s_vbScriptParseOptions =
        new(kind: SourceCodeKind.Script, languageVersion: LanguageVersion.Latest);

    private static readonly HashSet<Assembly> s_defaultReferencedAssemblies = new()
    {
        typeof(Collections.ICollection).Assembly,
        typeof(Enum).Assembly,
        typeof(ComponentModel.BrowsableAttribute).Assembly,
        typeof(VisualBasicValue<>).Assembly,
        Assembly.Load("netstandard"),
        Assembly.Load("System.Runtime")
    };

    private readonly Compilation DefaultCompilationUnit = InitDefaultCompilationUnit();

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    private VbExpressionValidator(HashSet<Assembly> referencedAssemblies)
        : base(referencedAssemblies != null
               ? new HashSet<Assembly>(s_defaultReferencedAssemblies.Union(referencedAssemblies))
               : s_defaultReferencedAssemblies)
    { }

    public override string Language => VisualBasicHelper.Language;

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static VbExpressionValidator Instance => s_instance.Value;

    protected override CompilerHelper CompilerHelper { get; } = new VBCompilerHelper();

    protected override string ActivityIdentifierRegex { get; } = "('activityId):(.*)";

    protected override Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces)
    {
        var globalImports = GlobalImport.Parse(namespaces);
        var metadataReferences = GetMetadataReferencesForExpression(assemblies);

        var options = DefaultCompilationUnit.Options as VisualBasicCompilationOptions;
        return DefaultCompilationUnit.WithOptions(options!.WithGlobalImports(globalImports)).WithReferences(metadataReferences);
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expressionText) =>
        VisualBasicSyntaxTree.ParseText("? " + expressionText, s_vbScriptParseOptions);

    protected override SyntaxTree GetSyntaxTreeForValidation(string expressionText) =>
        VisualBasicSyntaxTree.ParseText(expressionText, s_vbScriptParseOptions);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);

    private static Compilation InitDefaultCompilationUnit()
    {
        var assemblyName = Guid.NewGuid().ToString();
        VisualBasicCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
            globalImports: null,
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
        return VisualBasicCompilation.Create(assemblyName, null, null, options);
    }

    protected override string CreateValueCode(string types, string names, string code, string activityId, int index)
    {
        var arrayType = types.Split(",");
        if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
            return string.Format(_valueValidationTemplate, index, types, names, code, activityId);

        var (myDelegate, name) = s_compilerHelper.DefineDelegate(types);
        return string.Format(_delegateValueValidationTemplate, myDelegate, index, name, types, names, code, activityId);
    }

    protected override string CreateReferenceCode(string types, string names, string code, string activityId, string returnType, int index)
    {
        var actionDefinition = !string.IsNullOrWhiteSpace(types)
            ? $"Action(Of {string.Join(Comma, types)})" 
            : "Action";
        return string.Format(_referenceValidationTemplate, index, actionDefinition, names, code, returnType, activityId);
    }
}

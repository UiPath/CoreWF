// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CSharp.Activities;
using ReflectionMagic;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Activities.Validation;

/// <summary>
///     Validates C# expressions for use in fast design-time expression validation.
/// </summary>
public class DesignCSharpExpressionValidator : DesignRoslynExpressionValidator
{
    private static readonly Lazy<DesignCSharpExpressionValidator> s_default = new();
    private static DesignCSharpExpressionValidator s_instance;
    private const string _valueValidationTemplate = "public static Expression<Func<{0}>> CreateExpression{1}() => ({2}) => {3};//activityId:{4}";
    private const string _delegateValueValidationTemplate = "{0}\npublic static Expression<{1}<{2}>> CreateExpression{3}() => ({4}) => {5};//activityId:{6}";
    private const string _referenceValidationTemplate = "public static {0} IsLocation{1}() => ({2}) => {3} = default;//activityId:{4}";

    private static readonly CompilerHelper s_compilerHelper = new CSharpCompilerHelper();
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
        Assembly.Load("netstandard"),
        Assembly.Load("System.Runtime")
    };

    private readonly Compilation DefaultCompilationUnit = InitDefaultCompilationUnit();

    /// <summary>
    ///     Singleton instance of the default validator.
    /// </summary>
    public static DesignCSharpExpressionValidator Instance
    {
        get => s_instance ?? s_default.Value;
        set => s_instance = value;
    }

    protected override CompilerHelper CompilerHelper { get; } = new CSharpCompilerHelper();

    protected override string ActivityIdentifierRegex { get; } = @"(\/\/activityId):(.*)";

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    public DesignCSharpExpressionValidator() : this(null) { }

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection.
    /// </param>
    public DesignCSharpExpressionValidator(HashSet<Assembly> referencedAssemblies)
        : base(referencedAssemblies != null
               ? new HashSet<Assembly>(s_defaultReferencedAssemblies.Union(referencedAssemblies))
               : s_defaultReferencedAssemblies)
    { }

    protected override Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces)
    {
        var metadataReferences = GetMetadataReferencesForExpression(assemblies);

        var options = DefaultCompilationUnit.Options as CSharpCompilationOptions;
        return DefaultCompilationUnit.WithOptions(options.WithUsings(namespaces)).WithReferences(metadataReferences);
    }

    private static Compilation InitDefaultCompilationUnit()
    {
        var assemblyName = Guid.NewGuid().ToString();
        CSharpCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
            usings: null,
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: false,
            xmlReferenceResolver: null,
            sourceReferenceResolver: SourceFileResolver.Default,
            concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        return CSharpCompilation.Create(assemblyName, null, null, options);
    }

    protected override string CreateValueCode(string types, string names, string code, string activityId, int index)
    {
        var arrayType = types.Split(",");
        if (arrayType.Length <= 16) // .net defines Func<TResult>...Funct<T1,...T16,TResult)
            return string.Format(_valueValidationTemplate, types, index, names, code, activityId);

        var (myDelegate, name) = s_compilerHelper.DefineDelegate(types);
        return string.Format(_delegateValueValidationTemplate, myDelegate, name, types, index, names, code, activityId);
    }

    protected override string CreateReferenceCode(string types, string names, string code, string activityId, int index)
    {
        var actionDefinition = types.Any() ? $"Action<{string.Join(Comma, types)}>" : "Action";
        return string.Format(_referenceValidationTemplate, actionDefinition, index, names, code, activityId);
    }

    protected override SyntaxTree GetSyntaxTreeForExpression(string expressionText) =>
        CSharpSyntaxTree.ParseText(expressionText, s_csScriptParseOptions);

    protected override SyntaxTree GetSyntaxTreeForValidation(string expressionText) =>
        GetSyntaxTreeForExpression(expressionText);

    protected override string GetTypeName(Type type) =>
        (string)s_typeNameFormatter.FormatTypeName(type, s_typeOptions);

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

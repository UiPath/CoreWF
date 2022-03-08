using System.Activities.Expressions;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace System.Activities;

/// <summary>
///     A base class for validating text expressions using the Microsoft.CodeAnalysis (Roslyn) package.
/// </summary>
public abstract class RoslynExpressionValidator
{
    private const string Comma = ", ";
    private readonly HashSet<Assembly> _referencedAssemblies = new();
    private readonly Lazy<Dictionary<Assembly, MetadataReference>> _metadataReferenceDictionary;

    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">
    ///     Assemblies to seed the collection. Will union with
    ///     <see cref="JitCompilerHelper.DefaultReferencedAssemblies" />.
    /// </param>
    protected RoslynExpressionValidator(HashSet<Assembly> referencedAssemblies = null)
    {
        _metadataReferenceDictionary = new(GetInitialMetadataReferences);
        _referencedAssemblies.UnionWith(JitCompilerHelper.DefaultReferencedAssemblies);
        if (referencedAssemblies != null)
        {
            _referencedAssemblies.UnionWith(referencedAssemblies);
        }
    }

    /// <summary>
    ///     The kind of identifier to look for in the syntax tree as variables that need to be resolved for the expression.
    /// </summary>
    protected abstract int IdentifierKind { get; }

    /// <summary>
    ///     Current compilation unit for compiling the expression.
    /// </summary>
    protected Compilation CompilationUnit { get; set; }

    /// <summary>
    ///     Gets the MetadataReference objects for all of the referenced assemblies that this compilation unit could use.
    /// </summary>
    protected IEnumerable<MetadataReference> MetadataReferences => _metadataReferenceDictionary.Value.Values;

    /// <summary>
    ///     Gets the type name, which can be language-specific.
    /// </summary>
    /// <param name="type">typically the return type of the expression</param>
    /// <returns>type name</returns>
    protected abstract string GetTypeName(Type type);

    /// <summary>
    ///     Adds some boilerplate text to hold the expression and allow parameters and return type checking during validation
    /// </summary>
    /// <param name="types">list of parameter types in comma-separated string</param>
    /// <param name="names">list of parameter names in comma-separated string</param>
    /// <param name="code">expression code</param>
    /// <returns>expression wrapped in a method or function that returns a LambdaExpression</returns>
    protected abstract string CreateValidationCode(string types, string names, string code);

    /// <summary>
    ///     Gets the <see cref="Compilation" /> object for the current expression.
    /// </summary>
    /// <param name="expressionToValidate">current expression</param>
    /// <param name="currentActivity">current activity with the expression</param>
    /// <param name="environment">location reference environment for the expression validation</param>
    /// <returns>Compilation object</returns>
    protected abstract Compilation GetCompilationUnit(ExpressionToCompile expressionToValidate, 
        Activity currentActivity, LocationReferenceEnvironment environment);

    /// <summary>
    ///     Gets the <see cref="SyntaxTree" /> for the expression.
    /// </summary>
    /// <param name="expressionToValidate">contains the text expression</param>
    /// <returns>a syntax tree to use in the <see cref="Compilation" /></returns>
    protected abstract SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate);

    /// <summary>
    ///     Convert diagnostic messages from the compilation into ValidationError objects that can be added to the activity's
    ///     metadata.
    /// </summary>
    /// <param name="expressionToValidate">expression that was diagnosed</param>
    /// <param name="diagnostics">diagnostics returned from the compilation of an expression</param>
    /// <param name="currentActivity">current activity with the expression</param>
    /// <param name="environment">location reference environment for the expression validation</param>
    /// <returns>ValidationError objects for the current activity</returns>
    protected virtual IEnumerable<ValidationError> ProcessDiagnostics(ExpressionToCompile expressionToValidate,
        IEnumerable<TextExpressionCompilerError> diagnostics, Activity currentActivity, LocationReferenceEnvironment environment)
    {
        return from diagnostic in diagnostics
               select new ValidationError(diagnostic.Message, diagnostic.IsWarning);
    }

    /// <summary>
    ///     Validates an expression and returns any validation errors.
    /// </summary>
    /// <typeparam name="T">Expression return type</typeparam>
    /// <param name="currentActivity">activity containing the expression</param>
    /// <param name="environment">location reference environment</param>
    /// <param name="expressionText">expression text</param>
    /// <returns>validation errors</returns>
    /// <remarks>
    ///     Handles common steps for validating expressions with Roslyn. Can be reused for multiple expressions in the same
    ///     language.
    /// </remarks>
    public IEnumerable<ValidationError> Validate<T>(Activity currentActivity, LocationReferenceEnvironment environment,
        string expressionText)
    {
        EnsureReturnTypeReferenced<T>();

        JitCompilerHelper.GetAllImportReferences(currentActivity, true, out var localNamespaces,
            out var localAssemblies);
        EnsureAssembliesInCompilationUnit(localAssemblies);

        var scriptAndTypeScope = new JitCompilerHelper.ScriptAndTypeScope(environment, null);
        var expressionToValidate =
            new ExpressionToCompile(expressionText, localNamespaces, scriptAndTypeScope.FindVariable, typeof(T));

        CreateExpressionValidator(expressionToValidate, currentActivity, environment);
        ModifyPreppedCompilationUnit(expressionToValidate);
        var diagnostics = CompilationUnit.GetDiagnostics().Select(diagnostic => new TextExpressionCompilerError
        {
            SourceLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line,
            Number = diagnostic.Id,
            Message = diagnostic.ToString(),
            IsWarning = diagnostic.Severity < DiagnosticSeverity.Error
        });
        return ProcessDiagnostics(expressionToValidate, diagnostics, currentActivity, environment);
    }

    /// <summary>
    ///     Creates or gets a MetadataReference for an Assembly.
    /// </summary>
    /// <param name="asm">Assembly</param>
    /// <returns>MetadataReference or null if not found</returns>
    /// <remarks>
    ///     The default function in CoreWF first tries the non-CLS-compliant method
    ///     <see cref="System.Reflection.Metadata.AssemblyExtensions.TryGetRawMetadata"/>, which may
    ///     not work for some assemblies or in certain environments (like Blazor). On failure, the
    ///     default function will then try
    ///     <see cref="Microsoft.CodeAnalysis.AssemblyMetadata.CreateFromFile" />. If that also fails,
    ///     the function returns null and will not be cached.
    /// </remarks>
    protected virtual MetadataReference GetMetadataReference(Assembly asm)
    {
        try
        {
            return References.GetReference(asm);
        }
        catch (NotSupportedException) { }
        catch (NotImplementedException) { }

        if (!string.IsNullOrWhiteSpace(asm.Location))
        {
            try
            {
                return MetadataReference.CreateFromFile(asm.Location);
            }
            catch (IOException) { }
            catch (NotSupportedException) { }
        }

        return null;
    }

    /// <summary>
    ///     After all compilation options and syntax trees have been prepared, this method can be 
    ///     overridden to make modifications before diagnostics are retrieved.
    /// </summary>
    /// <param name="expressionToValidate">Original expression</param>
    /// <remarks>
    ///     Compilation object should have all imports, references, and compilation options set
    ///     and should have the first syntax tree set to the method with the expression. Use the
    ///     <see cref="CompilationUnit"/> property to get or set the Compilation object.
    /// </remarks>
    protected virtual void ModifyPreppedCompilationUnit(ExpressionToCompile expressionToValidate) { }

    private void CreateExpressionValidator(ExpressionToCompile expressionToValidate, Activity currentActivity, 
        LocationReferenceEnvironment environment)
    {
        CompilationUnit = GetCompilationUnit(expressionToValidate, currentActivity, environment);
        var syntaxTree = GetSyntaxTreeForExpression(expressionToValidate);
        var oldSyntaxTree = CompilationUnit!.SyntaxTrees.FirstOrDefault();
        CompilationUnit = oldSyntaxTree == null
            ? CompilationUnit!.AddSyntaxTrees(syntaxTree)
            : CompilationUnit.ReplaceSyntaxTree(oldSyntaxTree, syntaxTree);
        PrepValidation(expressionToValidate);
    }

    private void PrepValidation(ExpressionToCompile expressionToValidate)
    {
        var syntaxTree = CompilationUnit.SyntaxTrees.First();
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == IdentifierKind)
                                    .Select(n => n.ToString()).Distinct();
        var resolvedIdentifiers =
            identifiers
                .Select(name => (Name: name, Type: expressionToValidate.VariableTypeGetter(name)))
                .Where(var => var.Type != null)
                .ToArray();

        var names = string.Join(Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = string.Join(Comma,
            resolvedIdentifiers
                .Select(var => var.Type)
                .Concat(new[] {expressionToValidate.LambdaReturnType})
                .Select(GetTypeName));

        var lambdaFuncCode = CreateValidationCode(types, names, expressionToValidate.Code);
        var sourceText = SourceText.From(lambdaFuncCode);
        var newSyntaxTree = syntaxTree.WithChangedText(sourceText);
        CompilationUnit = CompilationUnit.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
    }

    private void EnsureReturnTypeReferenced<T>()
    {
        var expressionReturnType = typeof(T);

        HashSet<Type> allBaseTypes = null;
        JitCompilerHelper.EnsureTypeReferenced(expressionReturnType, ref allBaseTypes);
        List<MetadataReference> newReferences = null;
        foreach (var baseType in allBaseTypes)
        {
            var asm = baseType.Assembly;
            if (!_metadataReferenceDictionary.Value.ContainsKey(asm))
            {
                var meta = GetMetadataReference(asm);
                if (meta != null)
                {
                    _metadataReferenceDictionary.Value.Add(asm, meta);
                    newReferences ??= new List<MetadataReference>();
                    newReferences.Add(meta);
                }
            }
        }

        UpdateMetadataReferencesInCompilationUnit(newReferences);
    }

    private void EnsureAssembliesInCompilationUnit(List<AssemblyReference> localAssemblies)
    {
        List<MetadataReference> newReferences = null;
        foreach (var assemblyRef in localAssemblies)
        {
            var asm = assemblyRef.Assembly;
            if (asm == null)
            {
                assemblyRef.LoadAssembly();
                asm = assemblyRef.Assembly;
            }

            if (asm != null && !_metadataReferenceDictionary.Value.ContainsKey(asm))
            {
                var meta = GetMetadataReference(asm);
                if (meta != null)
                {
                    _metadataReferenceDictionary.Value.Add(asm, meta);
                    newReferences ??= new List<MetadataReference>();
                    newReferences.Add(meta);
                }
            }
        }

        UpdateMetadataReferencesInCompilationUnit(newReferences);
    }

    private void UpdateMetadataReferencesInCompilationUnit(IEnumerable<MetadataReference> metadataReferences)
    {
        if (metadataReferences != null && CompilationUnit != null)
        {
            CompilationUnit = CompilationUnit.AddReferences(metadataReferences);
        }
    }

    private Dictionary<Assembly, MetadataReference> GetInitialMetadataReferences()
    {
        var referenceCache = new Dictionary<Assembly, MetadataReference>();
        foreach (var referencedAssembly in _referencedAssemblies)
        {
            if (referenceCache.ContainsKey(referencedAssembly))
            {
                continue;
            }

            var mr = GetMetadataReference(referencedAssembly);
            if (mr != null)
            {
                referenceCache.Add(referencedAssembly, mr);
            }
        }

        return referenceCache;
    }
}

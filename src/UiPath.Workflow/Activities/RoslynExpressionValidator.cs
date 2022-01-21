using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Activities;

using Expressions;
using Validation;

/// <summary>
/// A base class for validating text expressions using the Microsoft.CodeAnalysis (Roslyn) package.
/// </summary>
public abstract class RoslynExpressionValidator
{
    private static readonly Dictionary<Assembly, MetadataReference> MetadataReferenceCache = new();

    /// <summary>
    /// The kind of identifier to look for in the syntax tree as variables that need to be resolved for the expression.
    /// </summary>
    protected abstract int IdentifierKind { get; }

    /// <summary>
    /// Current compilation unit for compiling the expression.
    /// </summary>
    protected Compilation CompilationUnit { get; set; }

    /// <summary>
    /// Gets the MetadataReference objects for all of the referenced assemblies that this compilation unit could use.
    /// </summary>
    protected static IEnumerable<MetadataReference> MetadataReferences => MetadataReferenceCache.Values;

    /// <summary>
    /// Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="referencedAssemblies">Assemblies to seed the collection. Will union with <see cref="JitCompilerHelper.DefaultReferencedAssemblies"/>.</param>
    protected RoslynExpressionValidator(HashSet<Assembly> referencedAssemblies = null)
    {
        referencedAssemblies ??= new HashSet<Assembly>();
        referencedAssemblies.UnionWith(JitCompilerHelper.DefaultReferencedAssemblies);
        foreach (Assembly referencedAssembly in referencedAssemblies)
        {
            MetadataReferenceCache.Add(referencedAssembly, References.GetReference(referencedAssembly));
        }
    }

    /// <summary>
    /// Gets the type name, which can be language-specific.
    /// </summary>
    /// <param name="type">typically the return type of the expression</param>
    /// <returns>type name</returns>
    protected abstract string GetTypeName(Type type);

    /// <summary>
    /// Adds some boilerplate text to hold the expression and allow parameters and return type checking during validation
    /// </summary>
    /// <param name="parameters">list of parameter names and types in comma-separated string</param>
    /// <param name="returnType">return type of expression</param>
    /// <param name="code">expression code</param>
    /// <returns>expression wrapped in a method or function</returns>
    protected abstract string CreateValidationCode(string parameters, string returnType, string code);

    /// <summary>
    /// Gets language-specific-format string for passing a parameter into a method. e.g. "int num" for C# and "num As Int" for VB.
    /// </summary>
    /// <param name="name">parameter name</param>
    /// <param name="type">parameter type</param>
    /// <returns>parameter declaration</returns>
    protected abstract string FormatParameter(string name, string type);

    /// <summary>
    /// Create the initial the <see cref="Compilation"/> object for this validator.
    /// </summary>
    protected abstract Compilation CreateCompilationUnit();

    /// <summary>
    /// Gets the <see cref="SyntaxTree"/> for the expression.
    /// </summary>
    /// <param name="expressionToValidate">contains the text expression</param>
    /// <returns>a syntax tree to use in the <see cref="Compilation"/></returns>
    protected abstract SyntaxTree GetSyntaxTreeForExpression(ExpressionToCompile expressionToValidate);

    /// <summary>
    /// Convert diagnostic messages from the compilation into ValidationError objects that can be added to the activity's metadata.
    /// </summary>
    /// <param name="diagnostics">diagnostics returned from the compilation of an expression</param>
    /// <returns>ValidationError objects for the current activity</returns>
    protected virtual IEnumerable<ValidationError> ProcessDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity >= DiagnosticSeverity.Warning)
            {
                yield return new ValidationError(diagnostic.GetMessage(), diagnostic.Severity == DiagnosticSeverity.Warning);
            }
        }
    }

    /// <summary>
    /// Validates an expression and returns any validation errors.
    /// </summary>
    /// <typeparam name="T">Expression return type</typeparam>
    /// <param name="currentActivity">activity containing the expression</param>
    /// <param name="environment">location reference environment</param>
    /// <param name="expressionText">expression text</param>
    /// <returns>validation errors</returns>
    /// <remarks>
    /// Handles common steps for validating expressions with Roslyn. Can be reused for multiple expressions in the same language.
    /// </remarks>
    public IEnumerable<ValidationError> Validate<T>(Activity currentActivity, LocationReferenceEnvironment environment, string expressionText)
    {
        EnsureReturnTypeReferenced<T>();

        JitCompilerHelper.GetAllImportReferences(currentActivity, true, out List<string> localNamespaces, out List<AssemblyReference> localAssemblies);
        EnsureAssembliesInCompilationUnit(localAssemblies);

        var scriptAndTypeScope = new JitCompilerHelper.ScriptAndTypeScope(environment, null);
        var expressionToValidate = new ExpressionToCompile(expressionText, localNamespaces, scriptAndTypeScope.FindVariable, typeof(T));

        CreateExpressionValidator(expressionToValidate);
        return ProcessDiagnostics(CompilationUnit.GetDiagnostics());
    }

    private void CreateExpressionValidator(ExpressionToCompile expressionToValidate)
    {
        CompilationUnit ??= CreateCompilationUnit();
        SyntaxTree syntaxTree = GetSyntaxTreeForExpression(expressionToValidate);
        SyntaxTree oldSyntaxTree = CompilationUnit?.SyntaxTrees.FirstOrDefault();
        CompilationUnit = oldSyntaxTree == null 
            ? CompilationUnit.AddSyntaxTrees(syntaxTree) 
            : CompilationUnit.ReplaceSyntaxTree(oldSyntaxTree, syntaxTree);

        PrepValidation(expressionToValidate);
    }

    private void PrepValidation(ExpressionToCompile expressionToValidate)
    {
        var syntaxTree = CompilationUnit.SyntaxTrees.First();
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == IdentifierKind).Select(n => n.ToString()).Distinct();
        var resolvedIdentifiers =
            identifiers
            .Select(name => (Name: name, Type: expressionToValidate.VariableTypeGetter(name)))
            .Where(var => var.Type != null)
            .ToArray();
        const string Comma = ", ";
        var parameters = string.Join(Comma, resolvedIdentifiers.Select(var => FormatParameter(var.Name, var.Type.Name)));
        var sourceText = SourceText.From(CreateValidationCode(parameters, GetTypeName(expressionToValidate.LambdaReturnType), expressionToValidate.Code));
        var newSyntaxTree = syntaxTree.WithChangedText(sourceText);
        CompilationUnit = CompilationUnit.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
    }

    private void EnsureReturnTypeReferenced<T>()
    {
        Type expressionReturnType = typeof(T);

        HashSet<Type> allBaseTypes = null;
        JitCompilerHelper.EnsureTypeReferenced(expressionReturnType, ref allBaseTypes);
        List<MetadataReference> newReferences = null;
        foreach (Type baseType in allBaseTypes)
        {
            var asm = baseType.Assembly;
            if (!MetadataReferenceCache.ContainsKey(asm))
            {
                var meta = References.GetReference(asm);
                MetadataReferenceCache.Add(asm, meta);
                newReferences ??= new();
                newReferences.Add(meta);
            }
        }

        UpdateMetadataReferencesInCompilationUnit(newReferences);
    }

    private void EnsureAssembliesInCompilationUnit(List<AssemblyReference> localAssemblies)
    {
        List<MetadataReference> newReferences = null;
        foreach (AssemblyReference assemblyRef in localAssemblies)
        {
            var asm = assemblyRef.Assembly;
            if (asm == null)
            {
                assemblyRef.LoadAssembly();
                asm = assemblyRef.Assembly;
            }

            if (asm != null && !MetadataReferenceCache.ContainsKey(asm))
            {
                var meta = References.GetReference(asm);
                MetadataReferenceCache.Add(asm, meta);
                newReferences ??= new();
                newReferences.Add(meta);

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
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace System.Activities;

/// <summary>
///     A base class for validating text expressions using the Microsoft.CodeAnalysis (Roslyn) package.
/// </summary>
public abstract class RoslynExpressionValidator
{
    private readonly IReadOnlyCollection<string> _defaultNamespaces = new string[]
    {
        "System",
        "System.Linq.Expressions"
    };

    private const string ErrorRegex = "((\\(.*\\)).*error )(.*)";
    private readonly Lazy<ConcurrentDictionary<Assembly, MetadataReference>> _metadataReferences;
    private readonly object _lockRequiredAssemblies = new();

    protected const string Comma = ", ";

    protected abstract CompilerHelper CompilerHelper { get; }
    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="seedAssemblies">
    ///     Assemblies to seed the collection. Will union with
    ///     <see cref="JitCompilerHelper.DefaultReferencedAssemblies" />.
    /// </param>
    protected RoslynExpressionValidator(HashSet<Assembly> seedAssemblies = null)
    {
        _metadataReferences = new(GetInitialMetadataReferences);

        var assembliesToReference = new HashSet<Assembly>(JitCompilerHelper.DefaultReferencedAssemblies);
        if (seedAssemblies != null)
        {
            assembliesToReference.UnionWith(seedAssemblies.Where(a => a is not null));
        }

        RequiredAssemblies = assembliesToReference;
    }

    /// <summary>
    ///     Assemblies required on the <see cref="Compilation"/> object. Use <see cref="AddRequiredAssembly(Assembly)"/>
    ///     to add more assemblies.
    /// </summary>
    protected IReadOnlySet<Assembly> RequiredAssemblies { get; private set; }

    /// <summary>
    ///     Adds an assembly to the <see cref="RequiredAssemblies"/> set.
    /// </summary>
    /// <param name="assembly">assembly</param>
    /// <remarks>
    ///     Takes a lock and replaces <see cref="RequiredAssemblies"/> with a new set. Lock is taken in case
    ///     multiple threads are adding assemblies simultaneously.
    /// </remarks>
    public void AddRequiredAssembly(Assembly assembly)
    {
        if (!RequiredAssemblies.Contains(assembly))
        {
            lock (_lockRequiredAssemblies)
            {
                if (!RequiredAssemblies.Contains(assembly))
                {
                    RequiredAssemblies = new HashSet<Assembly>(RequiredAssemblies)
                    {
                        assembly
                    };
                }
            }
        }
    }

    /// <summary>
    /// Validates the activity if the environment.IsValidating is set to true
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="activity"></param>
    /// <param name="environment"></param>
    /// <param name="expressionText"></param>
    /// <returns></returns>
    internal bool TryValidate<T>(Activity activity, CodeActivityMetadata metadata, string expressionText, bool isLocation = false)
    {
        var environment = metadata.Environment;
        if (environment.CompileExpressions)
        {
            return true;
        }
        if (!environment.IsValidating)
        {
            return false;
        }
        foreach (var validationError in Validate<T>(activity, environment, expressionText, isLocation))
        {
            activity.AddTempValidationError(validationError);
        }
        return true;
    }

    /// <summary>
    ///     Gets the MetadataReference objects for all of the referenced assemblies that expression requires.
    /// </summary>
    /// <param name="expressionContainer">expression container</param>
    /// <returns>MetadataReference objects for all required assemblies</returns>
    protected IEnumerable<MetadataReference> GetMetadataReferencesForExpression(ExpressionContainer expressionContainer) =>
        expressionContainer.RequiredAssemblies.Select(asm => TryGetMetadataReference(asm)).Where(mr => mr is not null);

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
    /// <param name="isLocation">determines if the expression is a location / reference</param>
    /// <returns>expression wrapped in a method or function that returns a LambdaExpression</returns>
    protected string CreateValidationCode(IEnumerable<string> types, string returnType, string names, string code, bool isLocation)
    {
        return isLocation
            ? CreateReferenceCode(string.Join(Comma, types), names, code, returnType)
            : CreateValueCode(string.Join(Comma, types.Concat(new[] { returnType })), names, code);
    }

    protected abstract string CreateValueCode(string types, string names, string code);

    protected abstract string CreateReferenceCode(string types, string names, string code, string returnType);

    /// <summary>
    ///     Updates the <see cref="Compilation" /> object for the expression.
    /// </summary>
    /// <param name="expressionContainer">expression container</param>
    protected abstract void UpdateCompilationUnit(ExpressionContainer expressionContainer);

    /// <summary>
    ///     Gets the <see cref="SyntaxTree" /> for the expression.
    /// </summary>
    /// <param name="expressionContainer">contains the text expression</param>
    /// <returns>a syntax tree to use in the <see cref="Compilation" /></returns>
    protected abstract SyntaxTree GetSyntaxTreeForExpression(ExpressionContainer expressionContainer);

    /// <summary>
    ///     Convert diagnostic messages from the compilation into ValidationError objects that can be added to the activity's
    ///     metadata.
    /// </summary>
    /// <param name="expressionContainer">expression container</param>
    /// <returns>ValidationError objects that will be added to current activity's metadata</returns>
    protected virtual IEnumerable<ValidationError> ProcessDiagnostics(ExpressionContainer expressionContainer)
    {
        var errors = new List<ValidationError>();
        foreach (var diagnostic in expressionContainer.Diagnostics)
        {
            var match = Regex.Match(diagnostic.Message, ErrorRegex);
            if (match.Success)
            {
                errors.Add(new ValidationError(match.Groups[3].Value, diagnostic.IsWarning));
            }
            else
            {
                errors.Add(new ValidationError(diagnostic.Message, diagnostic.IsWarning));
            }
        }
        return errors;
    }

    /// <summary>
    ///     Validates an expression and returns any validation errors.
    /// </summary>
    /// <typeparam name="TResult">Expression return type</typeparam>
    /// <param name="currentActivity">activity containing the expression</param>
    /// <param name="environment">location reference environment</param>
    /// <param name="expressionText">expression text</param>
    /// <returns>validation errors</returns>
    /// <remarks>
    ///     Handles common steps for validating expressions with Roslyn. Can be reused for multiple expressions in the same
    ///     language.
    /// </remarks>
    public virtual IEnumerable<ValidationError> Validate<TResult>(Activity currentActivity, LocationReferenceEnvironment environment,
        string expressionText, bool isLocation)
    {
        var requiredAssemblies = new HashSet<Assembly>(RequiredAssemblies);
        var resultType = typeof(TResult);
        var expressionContainer = new ExpressionContainer()
        {
            ResultType = resultType,
            CurrentActivity = currentActivity,
            Environment = environment,
            IsLocation = isLocation
        };

        JitCompilerHelper.GetAllImportReferences(currentActivity, true, out var localNamespaces, out var localAssemblies);
        requiredAssemblies.UnionWith(localAssemblies.Where(aref => aref is not null).Select(aref => aref.Assembly ?? LoadAssemblyFromReference(aref)));
        expressionContainer.RequiredAssemblies = requiredAssemblies;

        localNamespaces.AddRange(_defaultNamespaces);

        var scriptAndTypeScope = new JitCompilerHelper.ScriptAndTypeScope(environment);
        expressionContainer.ExpressionToValidate =
            new ExpressionToCompile(expressionText, localNamespaces, scriptAndTypeScope.FindVariable, resultType);

        EnsureAssembliesLoaded(expressionContainer);
        UpdateCompilationUnit(expressionContainer);
        EnsureReturnTypeReferenced(expressionContainer);

        var syntaxTree = GetSyntaxTreeForExpression(expressionContainer);
        expressionContainer.CompilationUnit = expressionContainer.CompilationUnit.AddSyntaxTrees(syntaxTree);
        PrepValidation(expressionContainer);

        ModifyPreppedCompilationUnit(expressionContainer);
        var diagnostics = expressionContainer
            .CompilationUnit
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic =>
            new TextExpressionCompilerError
            {
                SourceLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line,
                Number = diagnostic.Id,
                Message = diagnostic.ToString(),
                IsWarning = diagnostic.Severity < DiagnosticSeverity.Error
            });
        expressionContainer.Diagnostics = diagnostics;
        return ProcessDiagnostics(expressionContainer);
    }

    /// <summary>
    ///     Creates or gets a MetadataReference for an Assembly.
    /// </summary>
    /// <param name="assemblyReference">Assembly reference</param>
    /// <returns>MetadataReference or null if not found</returns>
    /// <remarks>
    ///     The default function in CoreWF first tries the non-CLS-compliant method
    ///     <see cref="Reflection.Metadata.AssemblyExtensions.TryGetRawMetadata"/>, which may
    ///     not work for some assemblies or in certain environments (like Blazor). On failure, the
    ///     default function will then try
    ///     <see cref="AssemblyMetadata.CreateFromFile" />. If that also fails,
    ///     the function returns null and will not be cached.
    /// </remarks>
    protected virtual MetadataReference GetMetadataReferenceForAssembly(Assembly assembly)
    {
        if (assembly == null)
        {
            return null;
        }

        try
        {
            return References.GetReference(assembly);
        }
        catch (NotSupportedException) { }
        catch (NotImplementedException) { }

        if (!string.IsNullOrWhiteSpace(assembly.Location))
        {
            try
            {
                return MetadataReference.CreateFromFile(assembly.Location);
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
    /// <param name="expressionContainer">expression container</param>
    /// <remarks>
    ///     Compilation object should have all imports, references, and compilation options set
    ///     and should have the first syntax tree set to the method with the expression. Use the
    ///     <see cref="ExpressionContainer.CompilationUnit"/> property to get or set the 
    ///     Compilation object.
    /// </remarks>
    protected virtual void ModifyPreppedCompilationUnit(ExpressionContainer expressionContainer) { }

    /// <summary>
    ///     If <see cref="AssemblyReference.Assembly"/> is null, loads the assembly. Default is to
    ///     call <see cref="AssemblyReference.LoadAssembly"/>.
    /// </summary>
    /// <param name="assemblyReference"></param>
    /// <returns></returns>
    protected virtual Assembly LoadAssemblyFromReference(AssemblyReference assemblyReference)
    {
        assemblyReference.LoadAssembly();
        return assemblyReference.Assembly;
    }

    private void PrepValidation(ExpressionContainer expressionContainer)
    {
        var syntaxTree = expressionContainer.CompilationUnit.SyntaxTrees.First();
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == CompilerHelper.IdentifierKind)
                                    .Select(n => n.ToString()).Distinct(CompilerHelper.IdentifierNameComparer);
        var resolvedIdentifiers =
            identifiers
                .Select(name => (Name: name, Type: expressionContainer.ExpressionToValidate.VariableTypeGetter(name)))
                .Where(var => var.Type != null)
                .ToArray();

        var names = string.Join(Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = resolvedIdentifiers.Select(var => var.Type).Select(GetTypeName);
        var returnType = GetTypeName(expressionContainer.ResultType);
        var lambdaFuncCode = CreateValidationCode(types, returnType, names, expressionContainer.ExpressionToValidate.Code, expressionContainer.IsLocation);

        var sourceText = SourceText.From(lambdaFuncCode);
        var newSyntaxTree = syntaxTree.WithChangedText(sourceText);
        expressionContainer.CompilationUnit = expressionContainer.CompilationUnit.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
    }

    private void EnsureReturnTypeReferenced(ExpressionContainer expressionContainer)
    {
        HashSet<Type> allBaseTypes = null;
        JitCompilerHelper.EnsureTypeReferenced(expressionContainer.ResultType, ref allBaseTypes);
        Lazy<List<MetadataReference>> newReferences = new();
        foreach (var baseType in allBaseTypes)
        {
            var asm = baseType.Assembly;
            if (!_metadataReferences.Value.ContainsKey(asm))
            {
                var meta = GetMetadataReferenceForAssembly(asm);
                if (meta != null)
                {
                    if (CanCache(asm))
                    {
                        _metadataReferences.Value.TryAdd(asm, meta);
                    }

                    newReferences.Value.Add(meta);
                }
            }
        }

        if (newReferences.IsValueCreated && expressionContainer.CompilationUnit != null)
        {
            expressionContainer.CompilationUnit = expressionContainer.CompilationUnit.AddReferences(newReferences.Value);
        }
    }

    private MetadataReference TryGetMetadataReference(Assembly assembly)
    {
        MetadataReference meta = null;
        if (assembly != null && !_metadataReferences.Value.TryGetValue(assembly, out meta))
        {
            meta = GetMetadataReferenceForAssembly(assembly);
            if (meta != null && CanCache(assembly))
            {
                _metadataReferences.Value.TryAdd(assembly, meta);
            }
        }

        return meta;
    }

    private bool CanCache(Assembly assembly)
        => !assembly.IsCollectible && !assembly.IsDynamic;

    private void EnsureAssembliesLoaded(ExpressionContainer expressionContainer)
    {
        foreach (var assembly in expressionContainer.RequiredAssemblies)
        {
            TryGetMetadataReference(assembly);
        }
    }

    private ConcurrentDictionary<Assembly, MetadataReference> GetInitialMetadataReferences()
    {
        var referenceCache = new ConcurrentDictionary<Assembly, MetadataReference>();
        foreach (var referencedAssembly in RequiredAssemblies)
        {
            if (referencedAssembly is null || referenceCache.ContainsKey(referencedAssembly))
            {
                continue;
            }

            var metadataReference = GetMetadataReferenceForAssembly(referencedAssembly);
            if (metadataReference != null)
            {
                referenceCache.TryAdd(referencedAssembly, metadataReference);
            }
        }

        return referenceCache;
    }
}
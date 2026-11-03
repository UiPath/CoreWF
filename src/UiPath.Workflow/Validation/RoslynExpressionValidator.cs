// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using System.Activities.Expressions;
using System.Activities.Utils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static System.Activities.JitCompilerHelper;

namespace System.Activities.Validation;

/// <summary>
///     A base class for validating text expressions using the Microsoft.CodeAnalysis (Roslyn) package.
/// </summary>
public abstract class RoslynExpressionValidator
{
    private const string ErrorRegex = "((\\(.*\\)).*error )(.*)";
    protected abstract string ActivityIdentifierRegex { get; }
    private readonly object _lockRequiredAssemblies = new();

    // The host's set of the validation in progress, see DesignTimeMetadataReferences.HostReferences. Read once per validation and
    // handed to GetCompilation this way, so the signature that hosts override stays as it is.
    private static readonly AsyncLocal<ValidationHostReferences> s_hostReferences = new();

    // With the host's set: the metadata of its names, and the resolver of the assemblies that they need, in place of the default
    // assemblies and of ValidationSettings.MissingAssemblyResolver. Null without a set.
    private protected static ValidationHostReferences HostReferences => s_hostReferences.Value;

    private protected sealed record ValidationHostReferences(IReadOnlyList<MetadataReference> Metadata, MetadataReferenceResolver MissingAssemblyResolver);

    protected abstract CompilerHelper CompilerHelper { get; }

    public abstract string Language { get; }
    /// <summary>
    ///     Initializes the MetadataReference collection.
    /// </summary>
    /// <param name="seedAssemblies">
    ///     Assemblies to seed the collection. Will union with
    ///     <see cref="JitCompilerHelper.DefaultReferencedAssemblies" />.
    /// </param>
    protected RoslynExpressionValidator(HashSet<Assembly> seedAssemblies = null)
    {
        var assembliesToReference = new HashSet<Assembly>(DefaultReferencedAssemblies);
        if (seedAssemblies != null)
        {
            assembliesToReference.UnionWith(seedAssemblies.Where(a => a is not null));
        }

        RequiredAssemblies = assembliesToReference;
    }

    /// <summary>
    ///     Assemblies required on the <see cref="Compilation"/> object. Use <see cref="AddRequiredAssembly(Assembly)"/>
    ///     to add more assemblies. A validation with the host's set, see
    ///     <see cref="DesignTimeMetadataReferences.HostReferences"/>, uses none of them: the set is the whole compile.
    /// </summary>
    protected IReadOnlySet<Assembly> RequiredAssemblies { get; private set; }

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
    ///     Gets the MetadataReference objects for all of the referenced assemblies that expression requires.
    /// </summary>
    /// <param name="assemblies">The list of assemblies</param>
    /// <returns>MetadataReference objects for all required assemblies</returns>
    protected IEnumerable<MetadataReference> GetMetadataReferencesForExpression(IReadOnlyCollection<Assembly> assemblies) =>
        assemblies.Select(asm => GetMetadataReferenceForAssembly(asm)).Where(mr => mr is not null);

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
    /// <param name="index">The index of the current expression</param>
    /// <returns>expression wrapped in a method or function that returns a LambdaExpression</returns>
    protected string CreateValidationCode(IEnumerable<string> types, string returnType, string names, string code, bool isLocation, string activityId, int index)
    {
        var typesWithReturnType = types.ToList();
        typesWithReturnType.Add(returnType);

        return isLocation
            ? CreateReferenceCode(string.Join(CompilerHelper.Comma, types), names, code, activityId, returnType, index)
            : CreateValueCode(typesWithReturnType, names, code, activityId, index);
    }

    protected abstract string CreateValueCode(IEnumerable<string> types, string names, string code, string activityId, int index);

    protected abstract string CreateReferenceCode(string types, string names, string code, string activityId, string returnType, int index);

    /// <summary>
    ///     Updates the <see cref="Compilation" /> object for the expression.
    /// </summary>
    /// <param name="assemblies">
    ///     The list of assemblies. With the host's set, see <see cref="DesignTimeMetadataReferences.HostReferences"/>, only the
    ///     references that the set's resolver did not answer; the base implementation adds the set's metadata, so an override
    ///     that does not call it compiles against its own references.
    /// </param>
    /// <param name="namespaces">The list of namespaces</param>
    protected abstract Compilation GetCompilation(IReadOnlyCollection<Assembly> assemblies, IReadOnlyCollection<string> namespaces, ValidationSettings validationSettings = null);

    /// <summary>
    ///     Gets the <see cref="SyntaxTree" /> for the expression.
    /// </summary>
    /// <param name="expressionText">The expression text</param>
    /// <returns>a syntax tree to use in the <see cref="Compilation" /></returns>
    protected abstract SyntaxTree GetSyntaxTreeForExpression(string expressionText);

    protected abstract SyntaxTree GetSyntaxTreeForValidation(string expressionText);

    /// <summary>
    ///     Convert diagnostic messages from the compilation into ValidationError objects that can be added to the activity's
    ///     metadata.
    /// </summary>
    /// <param name="expressionContainer">expression container</param>
    /// <returns>ValidationError objects that will be added to current activity's metadata</returns>
    private IEnumerable<ValidationError> ProcessDiagnostics(ImmutableArray<Diagnostic> diagnostics, string text, ValidationScope validationScope)
    {
        var errors = new List<(ValidationError, Diagnostic)>();
        if (diagnostics.Any())
        {
            var textLines = text.Split('\n');
            foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var match = Regex.Match(diagnostic.ToString(), ErrorRegex);
                ValidationError error;
                if (match.Success)
                {
                    var diagnosticLineNumber = diagnostic.Location.GetMappedLineSpan().StartLinePosition.Line;
                    var activity = GetErrorActivity(textLines, diagnosticLineNumber, validationScope);
                    error = new ValidationError(match.Groups[3].Value, false, activity);
                }
                else
                {
                    error = new ValidationError(diagnostic.ToString(), false);
                }
                errors.Add((error, diagnostic));
            }
        }
        return CurateErrors(errors, text);
    }

    /// <summary>
    /// Required for errors customization by certain hosts.
    /// </summary>
    /// <param name="originalErrors"></param>
    /// <returns></returns>
    protected virtual IEnumerable<ValidationError> CurateErrors(IEnumerable<(ValidationError error, Diagnostic)> originalErrors, string text)
    {
        return originalErrors.Select(item => item.error);
    }

    private Activity GetErrorActivity(string[] textLines, int diagnosticLineNumber, ValidationScope validationScope)
    {
        if (diagnosticLineNumber < 0)
            return null;

        var lineText = textLines[diagnosticLineNumber];
        var lineMatch = Regex.Match(lineText, ActivityIdentifierRegex);

        if (lineMatch.Success)
        {
            return validationScope.GetExpression(lineMatch.Groups[2].Value).Activity;
        }
        else
        {
            return GetErrorActivity(textLines, diagnosticLineNumber - 1, validationScope);
        }
    }

    internal IList<ValidationError> Validate(Activity currentActivity, ValidationScope validationScope, ValidationSettings validationSettings)
    {
        if (validationScope is null)
        {
            return Array.Empty<ValidationError>();
        }

        GetAllImportReferences(currentActivity, true, out var localNamespaces, out var localAssemblies);
        var references = localAssemblies.Where(aref => aref is not null);
        var hostReferences = DesignTimeMetadataReferences.GetHostReferences(references);
        HashSet<Assembly> requiredAssemblies;
        ValidationHostReferences validationHostReferences = null;
        if (hostReferences == null)
        {
            requiredAssemblies = new HashSet<Assembly>(RequiredAssemblies);
            requiredAssemblies.UnionWith(references.Select(aref => aref.Assembly ?? LoadAssemblyFromReference(aref)));
        }
        else
        {
            // As in the JIT compile: no default assembly, the set's metadata, and a load only for a name that the set does not answer.
            requiredAssemblies = new HashSet<Assembly>();
            var hostMetadata = new List<MetadataReference>();
            foreach (var reference in hostReferences.References)
            {
                if (MetadataReferenceUtils.GetDesignTimeMetadataReference(reference, hostReferences.Resolver) is { } metadata)
                {
                    hostMetadata.Add(metadata);
                }
                else if ((reference.Assembly ?? LoadAssemblyFromReference(reference)) is { } assembly)
                {
                    requiredAssemblies.Add(assembly);
                }
            }

            validationHostReferences = new ValidationHostReferences(hostMetadata, hostReferences.MissingAssemblyResolver);
        }

        localNamespaces.AddRange(CompilerHelper.DefaultNamespaces);

        Compilation compilation;
        var outerHostReferences = s_hostReferences.Value;
        s_hostReferences.Value = validationHostReferences;
        try
        {
            compilation = GetCompilation(requiredAssemblies, localNamespaces, validationSettings);
        }
        finally
        {
            s_hostReferences.Value = outerHostReferences;
        }
        var expressionsTextBuilder = new StringBuilder();
        int index = 0;
        foreach (var expressionToValidate in validationScope.GetAllExpressions())
        {
            AddExpressionToValidate(expressionToValidate, expressionsTextBuilder, index++);
        }

        compilation = compilation.AddSyntaxTrees(GetSyntaxTreeForValidation(expressionsTextBuilder.ToString()));

        var diagnostics = compilation
            .GetDiagnostics();
        var errors = ProcessDiagnostics(diagnostics, expressionsTextBuilder.ToString(), validationScope).ToList();
        validationScope.Clear();
        return errors;
    }

    /// <summary>
    ///     Creates or gets a MetadataReference for an Assembly.
    /// </summary>
    /// <param name="assembly">The assembly</param>
    /// <returns>MetadataReference or null if not found</returns>
    protected virtual MetadataReference GetMetadataReferenceForAssembly(Assembly assembly)
    => MetadataReferenceUtils.GetMetadataReferenceForAssembly(assembly);

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

    private void AddExpressionToValidate(ExpressionToValidate expressionToValidate, StringBuilder expressionBuilder, int index)
    {
        var syntaxTree = GetSyntaxTreeForExpression(expressionToValidate.ExpressionText);
        var identifiers = syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == CompilerHelper.IdentifierKind)
                                    .Select(n => n.ToString()).Distinct(CompilerHelper.IdentifierNameComparer);
        var resolvedIdentifiers =
            identifiers
                .Select(name => (Name: name, Type: new ScriptAndTypeScope(expressionToValidate.Environment).FindVariable(name, CompilerHelper.IdentifierNameComparison)))
                .Where(var => var.Type != null)
                .ToArray();

        var names = string.Join(CompilerHelper.Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = resolvedIdentifiers.Select(var => var.Type).Select(GetTypeName);
        var returnType = GetTypeName(expressionToValidate.ResultType);
        var lambdaFuncCode = CreateValidationCode(types, returnType, names, expressionToValidate.ExpressionText, expressionToValidate.IsLocation, expressionToValidate.Activity.Id, index);
        expressionBuilder.AppendLine(lambdaFuncCode);
    }
}
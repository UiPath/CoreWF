// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using ReflectionMagic;
using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Activities;

public abstract class JustInTimeCompiler
{
    public abstract LambdaExpression CompileExpression(ExpressionToCompile compilerRequest);

    /// <summary>
    /// Get a <see cref="Compilation"/> object for the given expression that can be used for validation diagnostics.
    /// </summary>
    /// <param name="expressionToCompile">A record containing the expression, type, and namespace information</param>
    /// <returns>A <see cref="Compilation"/> object for the given expression</returns>
    public abstract Compilation GetExpressionValidator(ExpressionToCompile expressionToCompile);
}
public record CompilerInput(string Code, IReadOnlyCollection<string> ImportedNamespaces)
{
}
public record ExpressionToCompile(string Code, IReadOnlyCollection<string> ImportedNamespaces, Func<string, Type> VariableTypeGetter, Type LambdaReturnType)
    : CompilerInput(Code, ImportedNamespaces)
{
}
public abstract class ScriptingJitCompiler : JustInTimeCompiler
{
    /// <summary>
    /// A <see cref="Compilation"/> object that can be reused with new syntax trees.
    /// </summary>
    protected Compilation CompilationUnit { get; set; }

    protected MetadataReference[] MetadataReferences { get; set; }
    protected ScriptingJitCompiler(HashSet<Assembly> referencedAssemblies) => MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
    protected abstract int IdentifierKind { get; }
    protected abstract string CreateExpressionCode(string types, string names, string code);
    protected abstract string GetTypeName(Type type);
    protected abstract Script<object> Create(string code, ScriptOptions options);
    
    /// <summary>
    /// Initialize the <see cref="CompilationUnit"/> for this JIT compiler.
    /// </summary>
    protected abstract void InitValidatorCompilationUnit();

    /// <summary>
    /// Compiles the passed in expression into a <see cref="LambdaExpression"/> ready for execution.
    /// </summary>
    /// <param name="expressionToCompile">The expression to compile</param>
    /// <returns>A <see cref="LambdaExpression"/> representing the passed in expression text</returns>
    public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
    {
        var options = ScriptOptions.Default
            .WithReferences(MetadataReferences)
            .WithImports(expressionToCompile.ImportedNamespaces)
            .WithOptimizationLevel(OptimizationLevel.Release);
        var untypedExpressionScript = Create(expressionToCompile.Code, options);
        var compilation = untypedExpressionScript.GetCompilation();
        var finalCompilation = PrepCompilation(compilation, expressionToCompile);
        var results = ScriptingAotCompiler.BuildAssembly(finalCompilation);
        if (results.HasErrors)
        {
            throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerErrorSpecificExpression(expressionToCompile.Code, results), results.CompilerMessages));
        }
        return (LambdaExpression)results.ResultType.GetMethod("CreateExpression").Invoke(null, null);
    }
    public IEnumerable<string> GetIdentifiers(SyntaxTree syntaxTree) =>
        syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == IdentifierKind).Select(n => n.ToString()).Distinct().ToArray();

    /// <remarks>
    /// This is a default implementation that does not attempt to initialize the <see cref="CompilationUnit"/>. That needs to be done
    /// prior to calling this method.
    /// </remarks>
    public override Compilation GetExpressionValidator(ExpressionToCompile expressionToCompile)
    {
        Runtime.Fx.Assert(CompilationUnit != null, "CompilationUnit not initialized. Likely no language support for validating expressions.");
        return PrepCompilation(CompilationUnit, expressionToCompile);
    }

    /// <remarks>
    /// If a <see cref="Compilation"/> object has already been created, it will have the expression as the first syntax tree in the list.
    /// This method gets the syntax tree, makes modifications to it, and replaces it in the Compilation object. The same process is done
    /// whether the Compilation object was manually created or is from a <see cref="Script"/> object.
    /// </remarks>
    private Compilation PrepCompilation(Compilation compilation, ExpressionToCompile expressionToCompile)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var identifiers = GetIdentifiers(syntaxTree);
        var resolvedIdentifiers =
            identifiers
            .Select(name => (Name: name, Type: expressionToCompile.VariableTypeGetter(name)))
            .Where(var => var.Type != null)
            .ToArray();
        const string Comma = ", ";
        var names = string.Join(Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = string.Join(Comma,
            resolvedIdentifiers
            .Select(var => var.Type)
            .Concat(new[] { expressionToCompile.LambdaReturnType })
            .Select(GetTypeName));
        return compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(
            CreateExpressionCode(types, names, expressionToCompile.Code))));
    }
}
public static class References
{
    public unsafe static MetadataReference GetReference(Assembly assembly)
    {
        if (!assembly.TryGetRawMetadata(out var blob, out var length))
        {
            throw new NotSupportedException();
        }
        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
        return assemblyMetadata.GetReference();
    }
    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) => assemblies.Select(GetReference);
}
public class VbJitCompiler : ScriptingJitCompiler
{
    private readonly VisualBasicParseOptions _parseOptions = new(kind: SourceCodeKind.Script);

    public VbJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }
    protected override Script<object> Create(string code, ScriptOptions options) => VisualBasicScript.Create("? " + code, options);
    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
    protected override string CreateExpressionCode(string types, string names, string code) =>
         $"Public Shared Function CreateExpression() As Expression(Of Func(Of {types}))\nReturn Function({names}) ({code})\nEnd Function";
    protected override int IdentifierKind => (int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierName;
    
    /// <summary>
    /// Creates a <see cref="VisualBasicCompilation"/> object using options borrowed from <see cref="VisualBasicScriptCompiler"/>.
    /// </summary>
    protected override void InitValidatorCompilationUnit()
    {
        string assemblyName = Guid.NewGuid().ToString();
        VisualBasicCompilationOptions options = new(
            OutputKind.DynamicallyLinkedLibrary,
            mainTypeName: null,
            rootNamespace: "",
            optionStrict: OptionStrict.On,
            optionInfer: true,
            optionExplicit: true,
            optionCompareText: false,
            embedVbCoreRuntime: false,
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: false,
            xmlReferenceResolver: null,
            sourceReferenceResolver: SourceFileResolver.Default,
            concurrentBuild: !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")),
            assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        CompilationUnit = VisualBasicCompilation.Create(assemblyName, null, MetadataReferences, options);        
    }

    /// <remarks>
    /// Parses the syntax tree for VB. This approach may also work for C# so could be refactored to be in the base class.
    /// </remarks>
    public override Compilation GetExpressionValidator(ExpressionToCompile expressionToCompile)
    {
        var syntaxTree = VisualBasicSyntaxTree.ParseText(expressionToCompile.Code, _parseOptions);
        var oldSyntaxTree = CompilationUnit?.SyntaxTrees.FirstOrDefault();

        if (oldSyntaxTree == null)
        {
            InitValidatorCompilationUnit();
            CompilationUnit = CompilationUnit.AddSyntaxTrees(syntaxTree);
        }
        else
        {
            CompilationUnit = CompilationUnit.ReplaceSyntaxTree(oldSyntaxTree, syntaxTree);
        }

        base.GetExpressionValidator(expressionToCompile);
        return CompilationUnit;
    }
}
public class CSharpJitCompiler : ScriptingJitCompiler
{
    private static readonly dynamic TypeOptions = GetTypeOptions();
    private static readonly dynamic TypeNameFormatter = GetTypeNameFormatter();
    public CSharpJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }
    protected override Script<object> Create(string code, ScriptOptions options) => CSharpScript.Create(code, options);
    protected override string GetTypeName(Type type) => (string)TypeNameFormatter.FormatTypeName(type, TypeOptions);
    protected override string CreateExpressionCode(string types, string names, string code) =>
         $"public static Expression<Func<{types}>> CreateExpression() => ({names}) => {code};";
    protected override int IdentifierKind => (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierName;
    protected override void InitValidatorCompilationUnit()
    {
        throw new NotImplementedException();
    }
    static object GetTypeOptions()
    {
        var formatterOptionsType = typeof(ObjectFormatter).Assembly.GetType("Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
        const int ArrayBoundRadix = 0;
        const bool ShowNamespaces = true;
        return Activator.CreateInstance(formatterOptionsType, new object[] { ArrayBoundRadix, ShowNamespaces });
    }
    static object GetTypeNameFormatter() =>
        typeof(CSharpScript).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.CSharpObjectFormatter")
        .AsDynamicType()
        .s_impl
        .TypeNameFormatter;
}

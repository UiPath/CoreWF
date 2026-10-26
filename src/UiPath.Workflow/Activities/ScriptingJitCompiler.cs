// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;
using ReflectionMagic;

namespace System.Activities;

public abstract class JustInTimeCompiler
{
    public abstract LambdaExpression CompileExpression(ExpressionToCompile compilerRequest);

    /// <summary>
    /// True when the compiler adds <see cref="ExpressionToCompile.MetadataReferences"/> to the compile.
    /// When false, the caller loads those assemblies and gives them to the compiler.
    /// </summary>
    public virtual bool SupportsMetadataReferences => false;
}

public record CompilerInput(string Code, IReadOnlyCollection<string> ImportedNamespaces) { }

public record ExpressionToCompile(string Code, IReadOnlyCollection<string> ImportedNamespaces,
    Func<string, StringComparison, Type> VariableTypeGetter, Type LambdaReturnType)
    : CompilerInput(Code, ImportedNamespaces)
{
    /// <summary>
    /// The metadata of the referenced assemblies that the compile does not load. See <see cref="DesignTimeMetadataReferences"/>.
    /// </summary>
    public IReadOnlyCollection<MetadataReference> MetadataReferences { get; init; } = Array.Empty<MetadataReference>();

    /// <summary>
    /// True when the assemblies of the compiler and <see cref="MetadataReferences"/> are the whole reference set of the compile, see
    /// <see cref="DesignTimeMetadataReferences.HostReferencesAreComplete"/>: the compiler then adds no assembly of the host, not even
    /// for a reference that a referenced assembly misses.
    /// </summary>
    public bool HostReferencesAreComplete { get; init; }
}

public abstract class ScriptingJitCompiler : JustInTimeCompiler
{
    protected ScriptingJitCompiler(HashSet<Assembly> referencedAssemblies)
    {
        MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
    }

    protected IReadOnlyCollection<MetadataReference> MetadataReferences { get; init; }
    public override bool SupportsMetadataReferences => true;
    protected abstract string GetTypeName(Type type);
    protected abstract Script<object> Create(string code, ScriptOptions options);
    protected abstract CompilerHelper CompilerHelper { get; }

    public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
    {
        var references = MetadataReferences.Concat(expressionToCompile.MetadataReferences).ToArray();
        var options = ScriptOptions.Default
                                   .WithReferences(references)
                                   .WithImports(expressionToCompile.ImportedNamespaces)
                                   .WithOptimizationLevel(OptimizationLevel.Release);
        if (expressionToCompile.HostReferencesAreComplete)
        {
            options = options.WithMetadataResolver(NoMissingAssemblyResolver.Instance);
        }

        var untypedExpressionScript = Create(expressionToCompile.Code, options);
        var compilation = expressionToCompile.HostReferencesAreComplete
            ? GetCompilationWithTheReferencesOnly(untypedExpressionScript, references)
            : untypedExpressionScript.GetCompilation();
        var syntaxTree = compilation.SyntaxTrees.First();
        var identifiers = GetIdentifiers(syntaxTree);
        var resolvedIdentifiers =
            identifiers
                .Select(name => (Name: name, Type: expressionToCompile.VariableTypeGetter(name, CompilerHelper.IdentifierNameComparison)))
                .Where(var => var.Type != null)
                .ToArray();
        var names = resolvedIdentifiers.Select(var => var.Name).ToArray();
        var types = resolvedIdentifiers.Select(var => var.Type).Concat(new[] { expressionToCompile.LambdaReturnType }).Select(GetTypeName).ToArray();
        var finalCompilation = compilation.ReplaceSyntaxTree(syntaxTree, syntaxTree.WithChangedText(SourceText.From(
            CompilerHelper.CreateExpressionCode(types, names, expressionToCompile.Code))));

        var collectibleAlc = new AssemblyLoadContext("ScriptingJit" + Guid.NewGuid(), true);
        collectibleAlc.Resolving += CollectibleAlc_Resolving;
        using var scope = collectibleAlc.EnterContextualReflection();

        var results = TryBuild();
        if (results.HasErrors)
        {
            var errorResults = new TextExpressionCompilerResults
            {
                ResultType = results.ResultType,
            };
            errorResults.AddMessages(results.CompilerMessages.Where(m => !m.IsWarning));
            throw FxTrace.Exception.AsError(new SourceExpressionException(
                SR.CompilerErrorSpecificExpression(expressionToCompile.Code, errorResults), errorResults.CompilerMessages));
        }

        return (LambdaExpression)results.ResultType.GetMethod("CreateExpression")!.Invoke(null, null);

        TextExpressionCompilerResults TryBuild()
        {
            try
            {
                return ScriptingAotCompiler.BuildAssembly(finalCompilation, compilation.ScriptClass.Name, collectibleAlc);
            }
            catch(Exception ex)
            {
                throw FxTrace.Exception.AsError(new SourceExpressionException(SR.CompilerError, ex));
            }

        }
    }

    // The compilation of a script whose references are complete: it references the given assemblies and no other, and takes the result
    // type of the submission from the core library among them. The scripts of this repository give their compilation, they never run.
    protected virtual Compilation GetCompilationWithTheReferencesOnly(Script<object> script, IEnumerable<MetadataReference> references)
        => script.GetCompilation();

    // The resolver of a script takes a reference that a referenced assembly misses from the framework of the host. A complete set has
    // nothing to add: the reference stays missing, and the compile reports a type from it as such.
    private sealed class NoMissingAssemblyResolver : MetadataReferenceResolver
    {
        public static readonly MetadataReferenceResolver Instance = new NoMissingAssemblyResolver();

        public override bool ResolveMissingAssemblies => false;

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
            => ImmutableArray<PortableExecutableReference>.Empty;

        public override bool Equals(object other) => ReferenceEquals(this, other);

        public override int GetHashCode() => typeof(NoMissingAssemblyResolver).GetHashCode();
    }

    private Assembly CollectibleAlc_Resolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
        // The assembly may have been loaded in a separate AssemblyLoadContext and should be available in the current AppDomain.
        return AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.FullName == assemblyName.FullName);
    }

    public IEnumerable<string> GetIdentifiers(SyntaxTree syntaxTree)
    {
        return syntaxTree.GetRoot().DescendantNodesAndSelf().Where(n => n.RawKind == CompilerHelper.IdentifierKind)
                         .Select(n => n.ToString()).Distinct(CompilerHelper.IdentifierNameComparer).ToArray();
    }
}

public static class References
{
    public static unsafe MetadataReference GetReference(Assembly assembly)
    {
        if (!assembly.TryGetRawMetadata(out var blob, out var length))
        {
            throw new NotSupportedException();
        }

        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
        return assemblyMetadata.GetReference();
    }

    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies)
    {
        return assemblies.Select(GetReference);
    }
}

public class VbJitCompiler : ScriptingJitCompiler
{
    public VbJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }

    protected override CompilerHelper CompilerHelper { get; } = new VBCompilerHelper();

    protected override Script<object> Create(string code, ScriptOptions options) =>
        VisualBasicScript.Create("? " + code, options);

    protected override string GetTypeName(Type type) => VisualBasicObjectFormatter.FormatTypeName(type);
}

public class CSharpJitCompiler : ScriptingJitCompiler
{
    private static readonly dynamic s_typeOptions = GetTypeOptions();
    private static readonly dynamic s_typeNameFormatter = GetTypeNameFormatter();

    public CSharpJitCompiler(HashSet<Assembly> referencedAssemblies) : base(referencedAssemblies) { }

    protected override CompilerHelper CompilerHelper { get; } = new CSharpCompilerHelper();

    protected override Script<object> Create(string code, ScriptOptions options) => CSharpScript.Create(code, options);

    // The C# script adds the core library of the host and binds the result type of the submission to it, which a compile against the
    // reference pack of another framework cannot do. The script gives the options and the parsed code; with no result type given, the
    // submission takes System.Object from the core library of the compile. The Visual Basic script of this repository does both by itself.
    protected override Compilation GetCompilationWithTheReferencesOnly(Script<object> script, IEnumerable<MetadataReference> references)
    {
        var scriptCompilation = (CSharpCompilation)script.GetCompilation();
        return CSharpCompilation.CreateScriptCompilation(scriptCompilation.AssemblyName, scriptCompilation.SyntaxTrees.Single(), references, scriptCompilation.Options);
    }

    protected override string GetTypeName(Type type) => (string)s_typeNameFormatter.FormatTypeName(type, s_typeOptions);

    private static object GetTypeOptions()
    {
        var formatterOptionsType =
            typeof(ObjectFormatter).Assembly.GetType(
                "Microsoft.CodeAnalysis.Scripting.Hosting.CommonTypeNameFormatterOptions");
        const int arrayBoundRadix = 0;
        const bool showNamespaces = true;
        return Activator.CreateInstance(formatterOptionsType!, arrayBoundRadix, showNamespaces);
    }

    private static object GetTypeNameFormatter()
    {
        return typeof(CSharpScript).Assembly
                                   .GetType("Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.CSharpObjectFormatter")
                                   .AsDynamicType()
                                   .s_impl
                                   .TypeNameFormatter;
    }
}

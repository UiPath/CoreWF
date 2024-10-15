// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.ExpressionParser;
using System.Activities.Internals;
using System.Activities.XamlIntegration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Collections;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
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
}

public record CompilerInput(string Code, IReadOnlyCollection<string> ImportedNamespaces) { }

public record ExpressionToCompile(string Code, IReadOnlyCollection<string> ImportedNamespaces,
    Func<string, StringComparison, Type> VariableTypeGetter, Type LambdaReturnType)
    : CompilerInput(Code, ImportedNamespaces)
{ }

public sealed class CachedMetadataReferenceResolver : MetadataReferenceResolver
{
    public static CachedMetadataReferenceResolver Default = new CachedMetadataReferenceResolver(ScriptMetadataResolver.Default);

    ScriptMetadataResolver _resolver;

    private class ResolveCacheKey
    {
        private string _reference;
        private string _baseFilePath;
        private MetadataReferenceProperties _properties;

        private readonly int _hashCode;

        public ResolveCacheKey(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            _reference = reference;
            _baseFilePath = baseFilePath;
            _properties = properties;

            _hashCode = reference?.GetHashCode() ?? 0;
            _hashCode = CombineHashCodes(_hashCode, _baseFilePath?.GetHashCode() ?? 0);
            _hashCode = CombineHashCodes(_hashCode, properties.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj is not ResolveCacheKey rtcKey || _hashCode != rtcKey._hashCode)
            {
                return false;
            }

            return _reference == rtcKey._reference &&
                _baseFilePath == rtcKey._baseFilePath &&
                _properties.Equals(rtcKey._properties);
        }

        public override int GetHashCode() => _hashCode;
    }
    ConcurrentDictionary<ResolveCacheKey, ImmutableArray<PortableExecutableReference>> _resolveCache = new ConcurrentDictionary<ResolveCacheKey, ImmutableArray<PortableExecutableReference>>();

    private class ResolveMissingCacheKey
    {
        private MetadataReference _definition;
        private AssemblyIdentity _referenceIdentity;

        private readonly int _hashCode;

        public ResolveMissingCacheKey(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            _definition = definition;
            this._referenceIdentity = referenceIdentity;

            _hashCode = definition.GetHashCode();
            _hashCode = CombineHashCodes(_hashCode, _referenceIdentity.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj is not ResolveMissingCacheKey rtcKey || _hashCode != rtcKey._hashCode)
            {
                return false;
            }

            return _definition.Equals(rtcKey._definition) &&
                _referenceIdentity.Equals(rtcKey._referenceIdentity);
        }

        public override int GetHashCode() => _hashCode;
    }
    ConcurrentDictionary<ResolveMissingCacheKey, PortableExecutableReference> _resolveMissingCache = new ConcurrentDictionary<ResolveMissingCacheKey, PortableExecutableReference>();

    public CachedMetadataReferenceResolver(ScriptMetadataResolver resolver)
    {
        _resolver = resolver;
    }

    public override bool Equals(object other)
    {
        return ReferenceEquals(this, other) ||
            other != null && other is CachedMetadataReferenceResolver &&
            Equals(_resolver, ((CachedMetadataReferenceResolver)other)._resolver) &&
            Equals(_resolveCache, ((CachedMetadataReferenceResolver)other)._resolveCache) &&
            Equals(_resolveMissingCache, ((CachedMetadataReferenceResolver)other)._resolveMissingCache);
    }

    public override int GetHashCode()
    {
        return CombineHashCodes(_resolver.GetHashCode(),
            CombineHashCodes(_resolveCache.GetHashCode(),
            _resolveMissingCache.GetHashCode()));
    }
    private static int CombineHashCodes(int h1, int h2) => ((h1 << 5) + h1) ^ h2;

    public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
    {
        ImmutableArray<PortableExecutableReference> ret;

        var cacheKey = new ResolveCacheKey(reference, baseFilePath, properties);

        if (!_resolveCache.TryGetValue(cacheKey, out ret))
        {
            ret = _resolver.ResolveReference(reference, baseFilePath, properties);
            _resolveCache.TryAdd(cacheKey, ret);
        }

        return ret;
    }

    public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

    public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
    {
        PortableExecutableReference ret;

        var cacheKey = new ResolveMissingCacheKey(definition, referenceIdentity);

        if (!_resolveMissingCache.TryGetValue(cacheKey, out ret))
        {
            ret = _resolver.ResolveMissingAssembly(definition, referenceIdentity);
            _resolveMissingCache.TryAdd(cacheKey, ret);
        }

        return ret;
    }
}

public abstract class ScriptingJitCompiler : JustInTimeCompiler
{
    protected ScriptingJitCompiler(HashSet<Assembly> referencedAssemblies)
    {
        MetadataReferences = referencedAssemblies.GetMetadataReferences().ToArray();
    }

    protected IReadOnlyCollection<MetadataReference> MetadataReferences { get; init; }
    protected abstract string GetTypeName(Type type);
    protected abstract Script<object> Create(string code, ScriptOptions options);
    protected abstract CompilerHelper CompilerHelper { get; }

    public override LambdaExpression CompileExpression(ExpressionToCompile expressionToCompile)
    {
        var options = ScriptOptions.Default
                                   .WithReferences(MetadataReferences)
                                   .WithImports(expressionToCompile.ImportedNamespaces)
                                   .WithOptimizationLevel(OptimizationLevel.Release)
                                   .WithMetadataResolver(CachedMetadataReferenceResolver.Default);
        var untypedExpressionScript = Create(expressionToCompile.Code, options);
        var compilation = untypedExpressionScript.GetCompilation();
        var syntaxTree = compilation.SyntaxTrees.First();
        var identifiers = GetIdentifiers(syntaxTree);
        var resolvedIdentifiers =
            identifiers
                .Select(name => (Name: name, Type: expressionToCompile.VariableTypeGetter(name, CompilerHelper.IdentifierNameComparison)))
                .Where(var => var.Type != null)
                .ToArray();
        var names = string.Join(CompilerHelper.Comma, resolvedIdentifiers.Select(var => var.Name));
        var types = string.Join(CompilerHelper.Comma,
            resolvedIdentifiers
                .Select(var => var.Type)
                .Concat(new[] { expressionToCompile.LambdaReturnType })
                .Select(GetTypeName));
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



    private Assembly CollectibleAlc_Resolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(a => a.FullName == assemblyName.FullName);
        if (assembly != null)
            return assembly;

        return loadContext.LoadFromAssemblyName(assemblyName);
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

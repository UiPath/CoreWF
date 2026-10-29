using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Xunit;

namespace TestCases.Workflows;

// A designer can know the file of an assembly that it does not want to load yet: the compile needs the metadata only.
public class DesignTimeMetadataReferencesTests
{
    private readonly string _name = "DesignTime.NotLoadable" + Guid.NewGuid().ToString("N");

    // The net5 reference pack: the target framework of a project is older than the framework of the host, and its pack declares the
    // framework types that the loaded core library of the host defines.
    private static readonly Lazy<IReadOnlyDictionary<string, MetadataReference>> s_referencePack = new(() =>
    {
        var folder = typeof(DesignTimeMetadataReferencesTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "Net5ReferencePack").Value;
        return Directory.GetFiles(folder, "*.dll").ToDictionary(path => Path.GetFileNameWithoutExtension(path), path => (MetadataReference)MetadataReference.CreateFromFile(path));
    });

    [Fact]
    public async Task CSharp_NameOnlyReferenceThatTheResolverKnows_CompilesWithoutALoad()
    {
        var path = EmitAssemblyNothingCanLoadByName();
        DesignTimeMetadataReferences.Resolver = name => name.Name == _name ? MetadataReference.CreateFromFile(path) : null;
        try
        {
            var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

            result.SourceExpressionException.ShouldBeNull();
            IsLoaded().ShouldBeFalse();
        }
        finally
        {
            DesignTimeMetadataReferences.Resolver = null;
        }
    }

    [Fact]
    public async Task VisualBasic_NameOnlyReferenceThatTheResolverKnows_CompilesWithoutALoad()
    {
        var path = EmitAssemblyNothingCanLoadByName();
        DesignTimeMetadataReferences.Resolver = name => name.Name == _name ? MetadataReference.CreateFromFile(path) : null;
        try
        {
            var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

            result.SourceExpressionException.ShouldBeNull();
            IsLoaded().ShouldBeFalse();
        }
        finally
        {
            DesignTimeMetadataReferences.Resolver = null;
        }
    }

    // With no resolver, a name-only reference still needs a load, and nothing can load this one.
    [Fact]
    public async Task CSharp_NameOnlyReferenceWithNoResolver_DoesNotCompile()
    {
        EmitAssemblyNothingCanLoadByName();

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldNotBeNull();
    }

    // A resolver that fails does not fail the compile: the reference loads by its name, as with no resolver.
    [Fact]
    public async Task CSharp_ResolverThatThrows_LoadsTheAssemblyAsBefore()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        DesignTimeMetadataReferences.Resolver = _ => throw new InvalidOperationException("The resolver failed.");

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // The synchronous compile runs the code that it builds, so only a reference that the expression does not use stays unloaded.
    [Fact]
    public void CSharp_Sync_NameOnlyReferenceThatTheResolverKnows_CompilesWithoutALoad()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_NameOnlyReferenceThatTheResolverKnows_CompilesWithoutALoad()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void CSharp_Sync_TypeThatTheExpressionUses_LoadsWhenTheExpressionIsCreated()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void VisualBasic_Sync_TypeThatTheExpressionUses_LoadsWhenTheExpressionIsCreated()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void CSharp_Sync_ACompileWithoutTheReference_DoesNotGetTheResultOfACompileWithIt()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withReferenceError, out _);
        withReferenceError.ShouldBeNull();

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var withoutReferenceError, out _);

        withoutReferenceError.ShouldNotBeNull();
    }

    // A new metadata object means new metadata, also with the same assembly name, for example a rebuilt package.
    [Fact]
    public void CSharp_Sync_NewMetadataWithTheSameName_DoesNotGetTheResultOfTheOldMetadata()
    {
        var withValue = MetadataReference.CreateFromFile(EmitAssemblyNothingCanLoadByName());
        var withoutValue = MetadataReference.CreateFromFile(EmitAssemblyNothingCanLoadByName("namespace DesignTimeNamespace { public static class Marker { } }"));
        var current = withValue;
        DesignTimeMetadataReferences.Resolver = name => name.Name == _name ? current : null;
        try
        {
            CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withValueError, out _);
            withValueError.ShouldBeNull();
            current = withoutValue;

            CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withoutValueError, out _);

            withoutValueError.ShouldNotBeNull();
        }
        finally
        {
            DesignTimeMetadataReferences.Resolver = null;
        }
    }

    // A compiler that does not declare metadata support gets the assembly loaded, as before.
    [Fact]
    public void VisualBasic_Sync_CompilerWithoutMetadataSupport_GetsTheLoadedAssembly()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        var compilerFactory = VisualBasicSettings.Default.CompilerFactory;
        VisualBasicSettings.Default.CompilerFactory = references => new CompilerWithoutMetadataSupport(references);
        try
        {
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

            compileError.ShouldBeNull();
            IsLoaded().ShouldBeTrue();
        }
        finally
        {
            VisualBasicSettings.Default.CompilerFactory = compilerFactory;
        }
    }

    // The compiler cache keeps 10 compilers, so the compiles after the 10th remove the oldest ones from the cache.
    [Fact]
    public void VisualBasic_Sync_CompilerWithoutMetadataSupport_ManyCompiles_KeepTheCompilerCacheUsable()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        var compilerFactory = VisualBasicSettings.Default.CompilerFactory;
        VisualBasicSettings.Default.CompilerFactory = references => new CompilerWithoutMetadataSupport(references);
        try
        {
            for (var i = 0; i < 15; i++)
            {
                VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), $"Marker.Value + {i}", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);
                compileError.ShouldBeNull();
            }

            DesignTimeMetadataReferences.Resolver = null;
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "1 + 1", ["System"], Array.Empty<AssemblyReference>(), null, out _, out var otherCompileError, out _);
            otherCompileError.ShouldBeNull();
        }
        finally
        {
            VisualBasicSettings.Default.CompilerFactory = compilerFactory;
        }
    }

    [Fact]
    public void CSharp_Sync_ResolverThatThrows_LoadsTheAssemblyAsBefore()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        DesignTimeMetadataReferences.Resolver = _ => throw new InvalidOperationException("The resolver failed.");

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // The host holds the whole reference set as metadata: the reference pack of the target framework of the project, its packages and
    // its own assemblies. It declares the set complete, so the compile adds no loaded assembly of the host, and the framework types
    // come from the pack alone.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    // Without the declaration, the loaded core library of the host sits next to the pack that declares the same types, and Visual Basic
    // finds every one of them ambiguous. The C# script hides the duplicates of the core library, so its compile mixes the two.
    [Fact]
    public void VisualBasic_Sync_PackNextToTheLoadedHost_DoesNotCompile()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: false);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    // A framework type comes from the pack alone, and the expression that the compile creates carries the type of the host.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileAFrameworkTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        CSharpDesignerHelper.CreatePrecompiledValue(null, "new List<string>()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out var returnType, out var compileError, out _);

        compileError.ShouldBeNull();
        returnType.ShouldBe(typeof(List<string>));
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileAFrameworkTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(null, "New List(Of String)()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out var returnType, out var compileError, out _);

        compileError.ShouldBeNull();
        returnType.ShouldBe(typeof(List<string>));
    }

    // The assembly of the target type is not added either: here it is the loaded core library of the host, which cannot sit next to the pack.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileToATargetTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        var activity = CSharpDesignerHelper.CreatePrecompiledValue(typeof(List<string>), "new List<string>()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        activity.ShouldBeOfType<CSharpValue<List<string>>>();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileToATargetTypeFromThePack_GiveTheCompilerNoLoadedAssembly()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);
        var compilerFactory = VisualBasicSettings.Default.CompilerFactory;
        HashSet<Assembly> loadedReferences = null;
        VisualBasicSettings.Default.CompilerFactory = references => new VbJitCompiler(loadedReferences = references);
        try
        {
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(List<string>), "New List(Of String)()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

            compileError.ShouldBeNull();
            loadedReferences.ShouldBeEmpty();
        }
        finally
        {
            VisualBasicSettings.Default.CompilerFactory = compilerFactory;
        }
    }

    // A member that the host has and the pack does not: the compile sees the pack, not the host.
    [Fact]
    public void CSharp_Sync_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "System.Environment.ProcessPath", [], Array.Empty<AssemblyReference>(), null, out _, out var hostCompileError, out _);
        hostCompileError.ShouldBeNull();
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(string), "System.Environment.ProcessPath", [], Array.Empty<AssemblyReference>(), null, out _, out var hostCompileError, out _);
        hostCompileError.ShouldBeNull();
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    // The code that the compile runs binds the package by name, so the package loads when the expression is created, as with the loaded set.
    [Fact]
    public void CSharp_Sync_CompleteReferences_LoadTheAssemblyOfAMethodWhenTheExpressionIsCreated()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_LoadTheAssemblyOfAMethodWhenTheExpressionIsCreated()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // A compiler without metadata support still gets the loaded set of before: the design time assembly, the defaults and the target type.
    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompilerWithoutMetadataSupport_GetsTheLoadedSetAsBefore()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);
        var compilerFactory = VisualBasicSettings.Default.CompilerFactory;
        VisualBasicSettings.Default.CompilerFactory = references => new CompilerWithoutMetadataSupport(references);
        try
        {
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

            compileError.ShouldBeNull();
            IsLoaded().ShouldBeTrue();
        }
        finally
        {
            VisualBasicSettings.Default.CompilerFactory = compilerFactory;
        }
    }

    // A set that fails counts as none: the compile adds the loaded set of the host, as with no set.
    [Fact]
    public void CSharp_Sync_HostReferencesThatThrow_CompileAsBefore()
    {
        using var designTime = UseResolverAndLoaderFor(EmitAssemblyNothingCanLoadByName());
        DesignTimeMetadataReferences.HostReferences = () => throw new InvalidOperationException("The set failed.");
        try
        {
            CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

            compileError.ShouldBeNull();
        }
        finally
        {
            DesignTimeMetadataReferences.HostReferences = null;
        }
    }

    // One compile reads one state of the host: the set's resolver gives the metadata, also when Resolver already answers for a
    // newer state, here a copy of the package without the constant.
    [Fact]
    public void CSharp_Sync_CompleteReferences_TakeTheMetadataOfTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);
        UseNewerStateWithoutTheConstant();

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
    }

    [Fact]
    public async Task VisualBasic_CompleteReferences_TakeTheMetadataOfTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);
        UseNewerStateWithoutTheConstant();

        var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
    }

    // The same loaded assemblies with and without a set are two compiles: the complete one gets no help from the host, also when
    // the other compile of the same text is cached. IWebProxy is in System.Net.Primitives, which the set lacks.
    [Fact]
    public void CSharp_Sync_SameAssembliesWithAndWithoutASet_DoNotShareACachedCompile()
    {
        const string expression = "System.Net.Http.HttpClient.DefaultProxy == null";
        var assemblies = HostDefaults().Append(typeof(System.Net.Http.HttpClient).Assembly).Distinct().ToList();
        var references = assemblies.Select(assembly => new AssemblyReference { Assembly = assembly }).ToArray();
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), expression, [], references, null, out _, out var partialError, out _);
        partialError.ShouldBeNull();

        var set = new DesignTimeReferenceSet(assemblies.Select(assembly => assembly.GetName()).ToList(), null);
        DesignTimeMetadataReferences.HostReferences = () => set;
        try
        {
            CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), expression, [], references, null, out _, out var completeError, out _);

            completeError.ShouldNotBeNull();
        }
        finally
        {
            DesignTimeMetadataReferences.HostReferences = null;
        }
    }

    // A caller that passes only some references, or none, still compiles against the host's set.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CallerWithNoReferences_CompilesAgainstTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CallerWithNoReferences_CompilesAgainstTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    // The asynchronous compile takes the same set.
    [Fact]
    public async Task CSharp_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public async Task VisualBasic_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public async Task CSharp_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack(), hostReferencesAreComplete: true);

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldNotBeNull();
    }

    private sealed class CompilerWithoutMetadataSupport(HashSet<Assembly> references) : JustInTimeCompiler
    {
        private readonly VbJitCompiler _compiler = new(references);

        public override LambdaExpression CompileExpression(ExpressionToCompile compilerRequest)
            => _compiler.CompileExpression(compilerRequest with { MetadataReferences = [] });
    }

    private AssemblyReference NameOnlyReference() => new() { AssemblyName = new AssemblyName(_name) };

    // The host loads a design time assembly only on first use, into its own context.
    private IDisposable UseResolverAndLoaderFor(string path)
    {
        DesignTimeMetadataReferences.Resolver = name => name.Name == _name ? MetadataReference.CreateFromFile(path) : null;
        var context = new AssemblyLoadContext("activities");
        ResolveEventHandler load = (_, args) => new AssemblyName(args.Name).Name == _name ? context.LoadFromAssemblyPath(path) : null;
        AppDomain.CurrentDomain.AssemblyResolve += load;
        return new Restore(() =>
        {
            AppDomain.CurrentDomain.AssemblyResolve -= load;
            DesignTimeMetadataReferences.Resolver = null;
        });
    }

    // The host holds the reference pack and the design time assembly as metadata, loads the design time assembly on first use, and
    // gives them as its whole reference set or gives no set.
    private IDisposable UseReferencePackFor(string path, bool hostReferencesAreComplete)
    {
        var loader = UseResolverAndLoaderFor(path);
        var designTimeMetadata = MetadataReference.CreateFromFile(path);
        var pack = s_referencePack.Value;
        Func<AssemblyName, MetadataReference> resolver = name => name.Name == _name ? designTimeMetadata : pack.GetValueOrDefault(name.Name);
        DesignTimeMetadataReferences.Resolver = resolver;
        var set = new DesignTimeReferenceSet(PackAndNameOnlyReferences().Select(reference => reference.AssemblyName).ToList(), resolver);
        DesignTimeMetadataReferences.HostReferences = hostReferencesAreComplete ? () => set : null;
        return new Restore(() =>
        {
            DesignTimeMetadataReferences.HostReferences = null;
            loader.Dispose();
        });
    }

    // Resolver answers for a newer state of the host than the set: the package no longer has Marker.
    private void UseNewerStateWithoutTheConstant()
    {
        var newer = MetadataReference.CreateFromFile(EmitAssemblyNothingCanLoadByName("namespace DesignTimeNamespace { public static class Other { } }", s_referencePack.Value["System.Runtime"]));
        var pack = s_referencePack.Value;
        DesignTimeMetadataReferences.Resolver = name => name.Name == _name ? newer : pack.GetValueOrDefault(name.Name);
    }

    // The assemblies that a compile with no set adds by itself.
    private static IEnumerable<Assembly> HostDefaults()
        => (IEnumerable<Assembly>)typeof(DesignTimeMetadataReferences).Assembly.GetType("System.Activities.JitCompilerHelper", throwOnError: true).GetField("DefaultReferencedAssemblies").GetValue(null);

    // The references of a project: the whole reference pack and the design time assembly, all by name.
    private IEnumerable<AssemblyReference> PackAndNameOnlyReferences()
        => s_referencePack.Value.Keys.Select(name => new AssemblyReference { AssemblyName = new AssemblyName(name) }).Append(NameOnlyReference());

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    private bool IsLoaded() => AssemblyLoadContext.All.SelectMany(context => context.Assemblies).Any(assembly => assembly.GetName().Name == _name);

    // A package of a project: built against the reference pack of its target framework, not against the core library of the host.
    private string EmitAssemblyForThePack() => EmitAssemblyNothingCanLoadByName(coreLibrary: s_referencePack.Value["System.Runtime"]);

    private string EmitAssemblyNothingCanLoadByName(
        string source = "namespace DesignTimeNamespace { public static class Marker { public const int Value = 42; public static int Get() => Value; } }",
        MetadataReference coreLibrary = null)
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(DesignTimeMetadataReferencesTests), Guid.NewGuid().ToString("N"))).FullName;
        var path = Path.Combine(directory, _name + ".dll");
        var compilation = CSharpCompilation.Create(_name,
            [CSharpSyntaxTree.ParseText(source)],
            [coreLibrary ?? MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.Emit(path).Success.ShouldBeTrue();
        return path;
    }
}

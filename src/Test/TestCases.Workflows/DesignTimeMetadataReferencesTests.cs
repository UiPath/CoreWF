using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
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

    // The metadata of the assemblies that a compile with no set adds by itself, one object per assembly.
    private static readonly Lazy<IReadOnlyDictionary<string, MetadataReference>> s_hostDefaults = new(() =>
        HostDefaults().ToDictionary(assembly => assembly.GetName().Name, assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location), StringComparer.OrdinalIgnoreCase));

    [Fact]
    public async Task CSharp_NameOnlyReferenceThatTheSetKnows_CompilesWithoutALoad()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public async Task VisualBasic_NameOnlyReferenceThatTheSetKnows_CompilesWithoutALoad()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    // With no set, a name-only reference still needs a load, and nothing can load this one.
    [Fact]
    public async Task CSharp_NameOnlyReferenceWithNoSet_DoesNotCompile()
    {
        EmitAssemblyNothingCanLoadByName();

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldNotBeNull();
    }

    // A resolver that fails does not fail the compile: the reference loads by its name, as with no resolver.
    [Fact]
    public async Task CSharp_ResolverThatThrows_LoadsTheAssemblyAsBefore()
    {
        using var loader = UseLoaderFor(EmitAssemblyNothingCanLoadByName());
        using var set = UseSet(HostSetNames(), _ => throw new InvalidOperationException("The resolver failed."));

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [new AssemblyName(_name)], null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // The synchronous compile runs the code that it builds, so only a reference that the expression does not use stays unloaded.
    [Fact]
    public void CSharp_Sync_NameOnlyReferenceThatTheSetKnows_CompilesWithoutALoad()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_NameOnlyReferenceThatTheSetKnows_CompilesWithoutALoad()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void CSharp_Sync_TypeThatTheExpressionUses_LoadsWhenTheExpressionIsCreated()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void VisualBasic_Sync_TypeThatTheExpressionUses_LoadsWhenTheExpressionIsCreated()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void CSharp_Sync_ACompileWithoutTheReference_DoesNotGetTheResultOfACompileWithIt()
    {
        using (UseHostSetFor(EmitAssemblyNothingCanLoadByName()))
        {
            CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withReferenceError, out _);
            withReferenceError.ShouldBeNull();
        }

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
        using var set = UseSet(HostSetNames(), name => name.Name == _name ? current : s_hostDefaults.Value.GetValueOrDefault(name.Name));
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withValueError, out _);
        withValueError.ShouldBeNull();
        current = withoutValue;

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var withoutValueError, out _);

        withoutValueError.ShouldNotBeNull();
    }

    // A compiler that does not declare metadata support gets the assembly loaded, as before.
    [Fact]
    public void VisualBasic_Sync_CompilerWithoutMetadataSupport_GetsTheLoadedAssembly()
    {
        using var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());
        using var compiler = UseCompiler(references => new CompilerWithoutMetadataSupport(references));

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // The compiler cache keeps 10 compilers, so the compiles after the 10th remove the oldest ones from the cache.
    [Fact]
    public void VisualBasic_Sync_CompilerWithoutMetadataSupport_ManyCompiles_KeepTheCompilerCacheUsable()
    {
        var designTime = UseHostSetFor(EmitAssemblyNothingCanLoadByName());
        using var compiler = UseCompiler(references => new CompilerWithoutMetadataSupport(references));
        for (var i = 0; i < 15; i++)
        {
            VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), $"Marker.Value + {i}", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);
            compileError.ShouldBeNull();
        }

        designTime.Dispose();
        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "1 + 1", ["System"], Array.Empty<AssemblyReference>(), null, out _, out var otherCompileError, out _);
        otherCompileError.ShouldBeNull();
    }

    [Fact]
    public void CSharp_Sync_ResolverThatThrows_LoadsTheAssemblyAsBefore()
    {
        using var loader = UseLoaderFor(EmitAssemblyNothingCanLoadByName());
        using var set = UseSet(HostSetNames(), _ => throw new InvalidOperationException("The resolver failed."));

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // The host holds the whole reference set as metadata: the reference pack of the target framework of the project, its packages and
    // its own assemblies. The compile adds no loaded assembly of the host, and the framework types come from the pack alone.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    // A framework type comes from the pack alone, and the expression that the compile creates carries the type of the host.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileAFrameworkTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        CSharpDesignerHelper.CreatePrecompiledValue(null, "new List<string>()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out var returnType, out var compileError, out _);

        compileError.ShouldBeNull();
        returnType.ShouldBe(typeof(List<string>));
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileAFrameworkTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(null, "New List(Of String)()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out var returnType, out var compileError, out _);

        compileError.ShouldBeNull();
        returnType.ShouldBe(typeof(List<string>));
    }

    // The assembly of the target type is not added either: here it is the loaded core library of the host, which cannot sit next to the pack.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CompileToATargetTypeFromThePack()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        var activity = CSharpDesignerHelper.CreatePrecompiledValue(typeof(List<string>), "new List<string>()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        activity.ShouldBeOfType<CSharpValue<List<string>>>();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompileToATargetTypeFromThePack_GiveTheCompilerNoLoadedAssembly()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());
        HashSet<Assembly> loadedReferences = null;
        using var compiler = UseCompiler(references => new VbJitCompiler(loadedReferences = references));

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(List<string>), "New List(Of String)()", ["System.Collections.Generic"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        loadedReferences.ShouldBeEmpty();
    }

    // A member that the host has and the pack does not: the compile sees the pack, not the host.
    [Fact]
    public void CSharp_Sync_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "System.Environment.ProcessPath", [], Array.Empty<AssemblyReference>(), null, out _, out var hostCompileError, out _);
        hostCompileError.ShouldBeNull();
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(string), "System.Environment.ProcessPath", [], Array.Empty<AssemblyReference>(), null, out _, out var hostCompileError, out _);
        hostCompileError.ShouldBeNull();
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    // The code that the compile runs binds the package by name, so the package loads when the expression is created, as with the loaded set.
    [Fact]
    public void CSharp_Sync_CompleteReferences_LoadTheAssemblyOfAMethodWhenTheExpressionIsCreated()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_LoadTheAssemblyOfAMethodWhenTheExpressionIsCreated()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Get()", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // A compiler without metadata support still gets the loaded set of before: the design time assembly, the defaults and the target type.
    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CompilerWithoutMetadataSupport_GetsTheLoadedSetAsBefore()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());
        using var compiler = UseCompiler(references => new CompilerWithoutMetadataSupport(references));

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeTrue();
    }

    // A set that fails counts as none: the compile adds the loaded set of the host, as with no set.
    [Fact]
    public void CSharp_Sync_HostReferencesThatThrow_CompileAsBefore()
    {
        using var loader = UseLoaderFor(EmitAssemblyNothingCanLoadByName());
        using var set = UseHostReferences(() => throw new InvalidOperationException("The set failed."));

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], [NameOnlyReference()], null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
    }

    // Both parts of a set are required: a set without a resolver counts as none, so the compile gets the help of the host as with no
    // set. IWebProxy is in System.Net.Primitives, which the names lack and the script of a compile with no set finds in the host.
    [Fact]
    public void CSharp_Sync_SetWithoutAResolver_CompilesAsWithNoSet()
    {
        var assemblies = HostDefaults().Append(typeof(System.Net.Http.HttpClient).Assembly).Distinct().ToList();
        using var set = UseSet(assemblies.Select(assembly => assembly.GetName()), resolver: null);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), "System.Net.Http.HttpClient.DefaultProxy == null", [], assemblies.Select(assembly => new AssemblyReference { Assembly = assembly }).ToArray(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
    }

    // The same loaded assemblies with and without a set are two compiles: the complete one gets no help from the host, also when the
    // other compile of the same text is cached. The set's resolver knows no System.Net.Primitives, so IWebProxy stays missing.
    [Fact]
    public void CSharp_Sync_SameAssembliesWithAndWithoutASet_DoNotShareACachedCompile()
    {
        const string expression = "System.Net.Http.HttpClient.DefaultProxy == null";
        var assemblies = HostDefaults().Append(typeof(System.Net.Http.HttpClient).Assembly).Distinct().ToList();
        var references = assemblies.Select(assembly => new AssemblyReference { Assembly = assembly }).ToArray();
        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), expression, [], references, null, out _, out var partialError, out _);
        partialError.ShouldBeNull();
        using var set = UseSet(assemblies.Select(assembly => assembly.GetName()), _ => null);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), expression, [], references, null, out _, out var completeError, out _);

        completeError.ShouldNotBeNull();
    }

    // A caller that passes only some references, or none, still compiles against the host's set.
    [Fact]
    public void CSharp_Sync_CompleteReferences_CallerWithNoReferences_CompilesAgainstTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_CallerWithNoReferences_CompilesAgainstTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    // The names give the assemblies that an expression names; the resolver gives the assemblies that those need. Here the package
    // returns a type of a dependency that the names lack.
    [Fact]
    public void CSharp_Sync_CompleteReferences_ADependencyThatTheNamesLack_ComesFromTheResolver()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: true);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), "Marker.Make() == null", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_ADependencyThatTheNamesLack_ComesFromTheResolver()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: true);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(bool), "Marker.Make() Is Nothing", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
    }

    [Fact]
    public async Task CSharp_CompleteReferences_ADependencyThatTheNamesLack_ComesFromTheResolver()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: true);

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(bool), "Marker.Make() == null", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
    }

    [Fact]
    public async Task VisualBasic_CompleteReferences_ADependencyThatTheNamesLack_ComesFromTheResolver()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: true);

        var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(bool), "Marker.Make() Is Nothing", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
    }

    // A dependency that the resolver does not know stays missing: the compile takes none from the host.
    [Fact]
    public void CSharp_Sync_CompleteReferences_ADependencyThatTheResolverLacks_StaysMissing()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: false);

        CSharpDesignerHelper.CreatePrecompiledValue(typeof(bool), "Marker.Make() == null", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    [Fact]
    public void VisualBasic_Sync_CompleteReferences_ADependencyThatTheResolverLacks_StaysMissing()
    {
        using var designTime = UsePackageWithADependency(resolverKnowsTheDependency: false);

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(bool), "Marker.Make() Is Nothing", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldNotBeNull();
    }

    // A compiler outside this library gets the set's resolver with the request, for the assemblies that the referenced ones need.
    [Fact]
    public void VisualBasic_Sync_CompleteReferences_GiveTheCompilerTheResolverOfTheSet()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());
        var requests = new List<ExpressionToCompile>();
        using var compiler = UseCompiler(references => new RecordingCompiler(references, requests));

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        var request = requests.ShouldHaveSingleItem();
        request.HostReferencesAreComplete.ShouldBeTrue();
        request.MetadataResolver.ShouldNotBeNull();
        request.MetadataResolver.ResolveMissingAssemblies.ShouldBeTrue();
    }

    [Fact]
    public void VisualBasic_Sync_NoSet_GiveTheCompilerNoResolver()
    {
        var requests = new List<ExpressionToCompile>();
        using var compiler = UseCompiler(references => new RecordingCompiler(references, requests));

        VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(typeof(int), "1 + 1", ["System"], Array.Empty<AssemblyReference>(), null, out _, out var compileError, out _);

        compileError.ShouldBeNull();
        var request = requests.ShouldHaveSingleItem();
        request.HostReferencesAreComplete.ShouldBeFalse();
        request.MetadataResolver.ShouldBeNull();
    }

    // The asynchronous compile takes the same set.
    [Fact]
    public async Task CSharp_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public async Task VisualBasic_CompleteReferences_CompileAConstantAgainstThePackWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(typeof(int), "Marker.Value", ["DesignTimeNamespace"], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldBeNull();
        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public async Task CSharp_CompleteReferences_DoNotCompileAMemberOfTheHostAlone()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(typeof(string), "System.Environment.ProcessPath", [], PackAndNameOnlyReferences(), null);

        result.SourceExpressionException.ShouldNotBeNull();
    }

    // The validation takes the same set, so the designer and the validation agree on an expression.
    [Fact]
    public void VisualBasic_Validation_CompleteReferences_RejectAMemberOfTheHostAlone()
    {
        var package = EmitAssemblyForThePack();
        using (UseLoaderFor(package))
        {
            Validate(new VisualBasicValue<string>("System.Environment.ProcessPath")).Errors.ShouldBeEmpty();
        }

        using var designTime = UseReferencePackFor(package);

        Validate(new VisualBasicValue<string>("System.Environment.ProcessPath")).Errors.ShouldHaveSingleItem().Message.ShouldContain("ProcessPath");
    }

    [Fact]
    public void CSharp_Validation_CompleteReferences_RejectAMemberOfTheHostAlone()
    {
        var package = EmitAssemblyForThePack();
        using (UseLoaderFor(package))
        {
            Validate(new CSharpValue<string>("System.Environment.ProcessPath")).Errors.ShouldBeEmpty();
        }

        using var designTime = UseReferencePackFor(package);

        Validate(new CSharpValue<string>("System.Environment.ProcessPath")).Errors.ShouldHaveSingleItem().Message.ShouldContain("ProcessPath");
    }

    [Fact]
    public void VisualBasic_Validation_CompleteReferences_ReadThePackageWithoutALoad()
    {
        using var designTime = UseReferencePackFor(EmitAssemblyForThePack());

        Validate(new VisualBasicValue<string>("Marker.Value.ToString()")).Errors.ShouldBeEmpty();

        IsLoaded().ShouldBeFalse();
    }

    [Fact]
    public void CSharp_Validation_CompleteReferences_ADependencyThatTheNamesLack_ComesFromTheResolver()
    {
        using (UsePackageWithADependency(resolverKnowsTheDependency: true))
        {
            Validate(new CSharpValue<string>("(Marker.Make() == null).ToString()")).Errors.ShouldBeEmpty();
        }

        using (UsePackageWithADependency(resolverKnowsTheDependency: false))
        {
            Validate(new CSharpValue<string>("(Marker.Make() == null).ToString()")).Errors.ShouldNotBeEmpty();
        }
    }

    private ValidationResults Validate(Activity<string> expression)
    {
        var workflow = new Sequence { Activities = { new WriteLine { Text = new InArgument<string>(expression) } } };
        TextExpression.SetReferencesForImplementation(workflow, [new AssemblyReference { AssemblyName = new AssemblyName(_name) }]);
        TextExpression.SetNamespacesForImplementation(workflow, ["DesignTimeNamespace"]);
        var settings = new ValidationSettings
        {
            SkipExpressionCompilation = false,
            SkipImplementationChildren = false,
            SkipValidatingRootConfiguration = false,
            ForceExpressionCache = false
        };

        return ActivityValidationServices.Validate(workflow, settings);
    }

    private sealed class CompilerWithoutMetadataSupport(HashSet<Assembly> references) : JustInTimeCompiler
    {
        private readonly VbJitCompiler _compiler = new(references);

        public override LambdaExpression CompileExpression(ExpressionToCompile compilerRequest)
            => _compiler.CompileExpression(compilerRequest with { MetadataReferences = [] });
    }

    private sealed class RecordingCompiler(HashSet<Assembly> references, List<ExpressionToCompile> requests) : VbJitCompiler(references)
    {
        public override LambdaExpression CompileExpression(ExpressionToCompile compilerRequest)
        {
            requests.Add(compilerRequest);
            return base.CompileExpression(compilerRequest);
        }
    }

    private AssemblyReference NameOnlyReference() => new() { AssemblyName = new AssemblyName(_name) };

    // The host loads a design time assembly only on first use, into its own context.
    private IDisposable UseLoaderFor(params string[] paths)
    {
        var context = new AssemblyLoadContext("activities");
        var byName = paths.ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        ResolveEventHandler load = (_, args) => byName.TryGetValue(new AssemblyName(args.Name).Name, out var path) ? context.LoadFromAssemblyPath(path) : null;
        AppDomain.CurrentDomain.AssemblyResolve += load;
        return new Restore(() => AppDomain.CurrentDomain.AssemblyResolve -= load);
    }

    private static IDisposable UseHostReferences(Func<DesignTimeReferenceSet> hostReferences)
    {
        DesignTimeMetadataReferences.HostReferences = hostReferences;
        return new Restore(() => DesignTimeMetadataReferences.HostReferences = null);
    }

    private static IDisposable UseSet(IEnumerable<AssemblyName> names, Func<AssemblyName, MetadataReference> resolver)
    {
        var set = new DesignTimeReferenceSet(names.ToList(), resolver);
        return UseHostReferences(() => set);
    }

    private static IDisposable UseCompiler(Func<HashSet<Assembly>, JustInTimeCompiler> compilerFactory)
    {
        var previous = VisualBasicSettings.Default.CompilerFactory;
        VisualBasicSettings.Default.CompilerFactory = compilerFactory;
        return new Restore(() => VisualBasicSettings.Default.CompilerFactory = previous);
    }

    // The set of a host that runs on the framework of the project: its default assemblies and the design time assembly, as metadata.
    private IDisposable UseHostSetFor(string path)
    {
        var loader = UseLoaderFor(path);
        var designTimeMetadata = MetadataReference.CreateFromFile(path);
        var set = UseSet(HostSetNames(), name => name.Name == _name ? designTimeMetadata : s_hostDefaults.Value.GetValueOrDefault(name.Name));
        return new Restore(() =>
        {
            set.Dispose();
            loader.Dispose();
        });
    }

    private IEnumerable<AssemblyName> HostSetNames() => s_hostDefaults.Value.Keys.Select(name => new AssemblyName(name)).Append(new AssemblyName(_name));

    // The host holds the reference pack, the workflow runtime and the design time assembly as metadata, gives them as its whole
    // reference set, and loads the design time assembly on first use.
    private IDisposable UseReferencePackFor(string path, params string[] dependencies)
    {
        var loader = UseLoaderFor([path, .. dependencies]);
        var metadata = new[] { path }.Concat(dependencies).ToDictionary(file => Path.GetFileNameWithoutExtension(file), file => (MetadataReference)MetadataReference.CreateFromFile(file), StringComparer.OrdinalIgnoreCase);
        var workflowRuntime = typeof(Activity).Assembly.GetName();
        metadata[workflowRuntime.Name] = s_hostDefaults.Value[workflowRuntime.Name];
        var pack = s_referencePack.Value;
        var set = UseSet(PackAndNameOnlyReferences().Select(reference => reference.AssemblyName).Append(workflowRuntime), name => metadata.GetValueOrDefault(name.Name) ?? pack.GetValueOrDefault(name.Name));
        return new Restore(() =>
        {
            set.Dispose();
            loader.Dispose();
        });
    }

    // The package returns a type of its dependency; the names hold the pack and the package, and the resolver may know the dependency.
    private IDisposable UsePackageWithADependency(bool resolverKnowsTheDependency)
    {
        var dependencyName = _name + ".Dependency";
        var dependency = EmitAssembly(dependencyName, "namespace DependencyNamespace { public sealed class Thing { } }", s_referencePack.Value["System.Runtime"]);
        var package = EmitAssembly(_name,
            "namespace DesignTimeNamespace { public static class Marker { public static DependencyNamespace.Thing Make() => null; } }",
            s_referencePack.Value["System.Runtime"], MetadataReference.CreateFromFile(dependency));
        return resolverKnowsTheDependency ? UseReferencePackFor(package, dependency) : UseReferencePackFor(package);
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
        => EmitAssembly(_name, source, coreLibrary ?? MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

    private static string EmitAssembly(string name, string source, params MetadataReference[] references)
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(DesignTimeMetadataReferencesTests), Guid.NewGuid().ToString("N"))).FullName;
        var path = Path.Combine(directory, name + ".dll");
        var compilation = CSharpCompilation.Create(name,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.Emit(path).Success.ShouldBeTrue();
        return path;
    }
}

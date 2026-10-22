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

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }

    private bool IsLoaded() => AssemblyLoadContext.All.SelectMany(context => context.Assemblies).Any(assembly => assembly.GetName().Name == _name);

    private string EmitAssemblyNothingCanLoadByName(
        string source = "namespace DesignTimeNamespace { public static class Marker { public const int Value = 42; public static int Get() => Value; } }")
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(DesignTimeMetadataReferencesTests), Guid.NewGuid().ToString("N"))).FullName;
        var path = Path.Combine(directory, _name + ".dll");
        var compilation = CSharpCompilation.Create(_name,
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.Emit(path).Success.ShouldBeTrue();
        return path;
    }
}

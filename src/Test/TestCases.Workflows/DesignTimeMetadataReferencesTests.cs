using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.IO;
using System.Linq;
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

    private string EmitAssemblyNothingCanLoadByName()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(DesignTimeMetadataReferencesTests), Guid.NewGuid().ToString("N"))).FullName;
        var path = Path.Combine(directory, _name + ".dll");
        var compilation = CSharpCompilation.Create(_name,
            [CSharpSyntaxTree.ParseText("namespace DesignTimeNamespace { public static class Marker { public const int Value = 42; } }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        compilation.Emit(path).Success.ShouldBeTrue();
        return path;
    }
}

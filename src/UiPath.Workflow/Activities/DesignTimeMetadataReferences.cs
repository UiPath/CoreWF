using Microsoft.CodeAnalysis;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Activities.Utils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace System.Activities;

/// <summary>
/// The reference set that a host gives to its design time expression compiles.
/// </summary>
public static class DesignTimeMetadataReferences
{
    /// <summary>
    /// Gives the whole reference set of a design time compile, or null for none. Called once per compile, on the caller's
    /// execution context.
    /// <para>
    /// With a set, the JIT compile, the designer compile and the validation reference the set only, never the host's framework,
    /// so a host can compile against the reference pack of another framework. A caller's reference outside the set counts only
    /// when the resolver answers it; else it is dropped. Workflows often reference System.Private.* assemblies, so a set for a
    /// reference pack should answer them, for example with facades that forward to the pack.
    /// </para>
    /// </summary>
    public static Func<DesignTimeReferenceSet> HostReferences { get; set; }

    // Null when the host gives no set, or a set that throws or misses a part.
    internal static HostCompileReferences GetHostReferences(IEnumerable<AssemblyReference> callerReferences)
    {
        DesignTimeReferenceSet set;
        try
        {
            set = HostReferences?.Invoke();
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            ExceptionTrace.TraceUnhandledException(e);
            return null;
        }

        if (set == null)
        {
            return null;
        }

        if (set.Names == null || set.Resolver == null)
        {
            ExceptionTrace.AsWarning(new ArgumentException(
                $"{nameof(HostReferences)} gave a set without {(set.Names == null ? nameof(set.Names) : nameof(set.Resolver))}; the compile uses no set."));
            return null;
        }

        var references = new List<AssemblyReference>(set.Names.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hostName in set.Names)
        {
            if (hostName?.Name != null && names.Add(hostName.Name))
            {
                references.Add(new AssemblyReference { AssemblyName = hostName });
            }
        }

        // Never load a caller's reference outside the set: it can be an assembly of the host's framework, such as
        // System.Private.CoreLib, which would add a second core library.
        foreach (var reference in callerReferences)
        {
            var assemblyName = reference?.AssemblyName ?? reference?.Assembly?.GetName();
            if (assemblyName?.Name != null && !names.Contains(assemblyName.Name)
                && MetadataReferenceUtils.Resolve(set.Resolver, assemblyName) != null)
            {
                names.Add(assemblyName.Name);
                references.Add(new AssemblyReference { AssemblyName = assemblyName });
            }
        }

        return new HostCompileReferences(references, set.Resolver);
    }
}

/// <summary>
/// The whole reference set of one design time compile, from one state of the host. Both parts are required.
/// </summary>
/// <param name="Names">
/// The assemblies of the compile. They must include the core library and every assembly that an expression can name.
/// </param>
/// <param name="Resolver">
/// The metadata of an assembly by name, or null for none. It is asked for the names, for caller references outside the set and for
/// assemblies that a reference depends on; the last must be a <see cref="PortableExecutableReference"/>. A name of the set without
/// an answer loads by name. Must not load assemblies. Compiled expressions are cached by the returned objects, so return the same
/// object while the metadata, including that of its dependencies, does not change.
/// </param>
public sealed record DesignTimeReferenceSet(IReadOnlyCollection<AssemblyName> Names, Func<AssemblyName, MetadataReference> Resolver);

// The references of one compile with the host's set.
internal sealed class HostCompileReferences
{
    public HostCompileReferences(IReadOnlyList<AssemblyReference> references, Func<AssemblyName, MetadataReference> resolver)
    {
        References = references;
        Resolver = resolver;
        MissingAssemblyResolver = new HostMissingAssemblyResolver(resolver);
    }

    public IReadOnlyList<AssemblyReference> References { get; }

    public Func<AssemblyName, MetadataReference> Resolver { get; }

    public MetadataReferenceResolver MissingAssemblyResolver { get; }
}

// Resolves a missing dependency from the set's resolver only, never from the host's framework.
internal sealed class HostMissingAssemblyResolver : MetadataReferenceResolver
{
    private readonly Func<AssemblyName, MetadataReference> _resolver;

    public HostMissingAssemblyResolver(Func<AssemblyName, MetadataReference> resolver) => _resolver = resolver;

    public override bool ResolveMissingAssemblies => true;

    public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
    {
        if (referenceIdentity?.Name == null)
        {
            return null;
        }

        var assemblyName = new AssemblyName { Name = referenceIdentity.Name, Version = referenceIdentity.Version, CultureName = referenceIdentity.CultureName };
        if (!referenceIdentity.PublicKeyToken.IsDefaultOrEmpty)
        {
            assemblyName.SetPublicKeyToken(referenceIdentity.PublicKeyToken.ToArray());
        }

        return MetadataReferenceUtils.Resolve(_resolver, assemblyName) as PortableExecutableReference;
    }

    public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        => ImmutableArray<PortableExecutableReference>.Empty;

    public override bool Equals(object other) => other is HostMissingAssemblyResolver resolver && resolver._resolver == _resolver;

    public override int GetHashCode() => _resolver.GetHashCode();
}

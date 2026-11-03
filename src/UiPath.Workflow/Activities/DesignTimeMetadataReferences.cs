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
    /// Gives the whole reference set of a design time compile, or null when the host has none. Evaluated once per compile, on the
    /// execution context of the caller of the compile, so a host can scope its answer with its own <see cref="Threading.AsyncLocal{T}"/>.
    /// <para>
    /// With no set, the compile references the caller's references, loaded, plus the default assemblies, which are the loaded core
    /// library and framework of the host, and the assembly of the target type.
    /// </para>
    /// <para>
    /// With a set, every design time compile starts from its names: the JIT compile, the designer compile and the validation. The
    /// compile adds no assembly of its own, so a host can compile against the reference pack of another framework. A caller's
    /// reference whose name the set lacks counts only when the set's resolver answers it, also when the reference holds a loaded
    /// assembly; else the compile drops it. The workflows of a host name implementation assemblies of the host's framework, such as
    /// System.Private.CoreLib, System.Private.Uri and System.Private.Xml, so a set that answers them, for example with facades
    /// that forward to the reference pack, keeps the types of those references. The set's resolver gives the metadata of every
    /// name, and of every assembly that a referenced assembly needs and the names lack; the compile never takes one from the
    /// framework of the host. A name of the set that the resolver does not answer loads by its name; a dependency that it does not
    /// answer stays missing. A compiler that does not support metadata references gets the loaded assemblies: the names, the
    /// default assemblies and the assembly of the target type.
    /// </para>
    /// </summary>
    public static Func<DesignTimeReferenceSet> HostReferences { get; set; }

    // The references of one compile from the host's set. Null when the host gives no set; a set that fails, or that misses a part,
    // counts as none, so the compile adds its own assemblies as with no set.
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

        // A caller's reference that the names lack counts only when the set's resolver answers it, also when it holds a loaded assembly.
        // The compile never loads one for it: the default import references of VisualBasicSettings and the workflows of a host name
        // the framework of the host, such as System.Private.CoreLib, which would bring a second core library next to the set's.
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
/// The whole reference set of one design time compile, read from one state of the host. Both parts are required.
/// </summary>
/// <param name="Names">
/// The assemblies of the compile, by name. They must include the core library, such as the System.Runtime of a reference pack, and
/// every assembly that an expression can name; the resolver gives the assemblies that those need.
/// </param>
/// <param name="Resolver">
/// The metadata of an assembly by its name, for the state that gave <paramref name="Names"/>. The compile asks it for every name, for
/// every caller's reference that the names lack, and for every assembly that a referenced assembly misses; that last answer must be a
/// <see cref="PortableExecutableReference"/>, and any other kind counts as none. Give the same metadata object while the metadata does
/// not change: the compiled expressions are cached by the metadata objects of the names and of the caller's references. The cache does
/// not see the assemblies that a referenced assembly misses, so give a name new metadata when an assembly that it needs changes. It
/// must not load an assembly, and a null result means the host has none.
/// </param>
public sealed record DesignTimeReferenceSet(IReadOnlyCollection<AssemblyName> Names, Func<AssemblyName, MetadataReference> Resolver);

// The references of one compile from the host's set: its names, then the caller's references of the names it lacks, with the set's
// resolver for their metadata and for an assembly that one of them misses.
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

    // Answers the assemblies that a referenced assembly needs and the set does not name, from the set's resolver only.
    public MetadataReferenceResolver MissingAssemblyResolver { get; }
}

// The resolver of a compile against the host's set: a reference that a referenced assembly misses comes from the set's resolver, never
// from the framework of the host, and stays missing when the set has none.
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

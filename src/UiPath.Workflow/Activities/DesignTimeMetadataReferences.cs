using Microsoft.CodeAnalysis;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.Reflection;

namespace System.Activities;

/// <summary>
/// The metadata of the assemblies that a design time expression compile references by name only.
/// </summary>
public static class DesignTimeMetadataReferences
{
    /// <summary>
    /// Gives the metadata of an assembly that the compile references by name, so that the compile does not load it.
    /// With no resolver, or a null result, the compile loads the assembly by its name.
    /// Give the same object while the metadata does not change: the compiled expressions are cached by this object, so a new
    /// object compiles the expression again.
    /// </summary>
    public static Func<AssemblyName, MetadataReference> Resolver { get; set; }

    /// <summary>
    /// Gives the whole reference set of a design time compile by name, or null when the host has none. With a set, every design
    /// time compile references it, whatever references its caller passes: a caller's reference of a name in the set is replaced
    /// by the set's, and the compile adds no assembly of its own, neither its default assemblies, which are the loaded core
    /// library and framework of the host, nor the assembly of the target type. A host can then compile against the reference
    /// pack of another framework, without the implementation assemblies of its own framework next to it. <see cref="Resolver"/>
    /// gives the metadata of each name; a name it does not answer loads, as any reference by name. A compiler that does not
    /// support metadata references still gets the loaded set as before. Evaluated once per compile.
    /// </summary>
    public static Func<IReadOnlyCollection<AssemblyName>> HostReferences { get; set; }

    // The references of one compile: the host's set, then the caller's references of the names it lacks. Null when the host gives
    // no set; a set that fails counts as none, so the compile adds its own assemblies as before.
    internal static List<AssemblyReference> GetHostReferences(IEnumerable<AssemblyReference> callerReferences)
    {
        IReadOnlyCollection<AssemblyName> hostNames;
        try
        {
            hostNames = HostReferences?.Invoke();
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

        if (hostNames == null)
        {
            return null;
        }

        var references = new List<AssemblyReference>(hostNames.Count);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var hostName in hostNames)
        {
            if (hostName?.Name != null && names.Add(hostName.Name))
            {
                references.Add(new AssemblyReference { AssemblyName = hostName });
            }
        }

        foreach (var reference in callerReferences)
        {
            var name = reference?.AssemblyName?.Name ?? reference?.Assembly?.GetName().Name;
            if (name != null && !names.Contains(name))
            {
                references.Add(reference);
            }
        }

        return references;
    }
}

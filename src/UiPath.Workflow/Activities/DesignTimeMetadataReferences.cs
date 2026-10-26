using Microsoft.CodeAnalysis;
using System.Activities.Internals;
using System.Activities.Runtime;
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
    /// Tells whether the metadata that <see cref="Resolver"/> gives for the referenced names is the whole reference set of the
    /// compile. When it returns true, the design time compile adds no assembly of its own: neither its default assemblies, which
    /// are the loaded core library and framework of the host, nor the assembly of the target type. A host can then compile
    /// against the reference pack of another framework, without the implementation assemblies of its own framework next to it.
    /// A compiler that does not support metadata references still gets the loaded set as before. Evaluated once per compile.
    /// </summary>
    public static Func<bool> HostReferencesAreComplete { get; set; }

    // A check that fails counts as not complete, so the compile adds its own assemblies as before.
    internal static bool AreHostReferencesComplete()
    {
        try
        {
            return HostReferencesAreComplete?.Invoke() == true;
        }
        catch (Exception e)
        {
            if (Fx.IsFatal(e))
            {
                throw;
            }

            ExceptionTrace.TraceUnhandledException(e);
            return false;
        }
    }
}

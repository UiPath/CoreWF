using Microsoft.CodeAnalysis;
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
}

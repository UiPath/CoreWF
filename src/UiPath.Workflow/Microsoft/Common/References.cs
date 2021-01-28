namespace Microsoft.Common
{
    using Microsoft.CodeAnalysis;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;

    static class References
    {
        unsafe static MetadataReference GetReference(Assembly assembly)
        {
            if (!assembly.TryGetRawMetadata(out var blob, out var length))
            {
                throw new NotSupportedException();
            }
            var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
            var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
            return assemblyMetadata.GetReference();
        }
        public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<Assembly> assemblies) =>
            assemblies.Select(GetReference);
    }
}
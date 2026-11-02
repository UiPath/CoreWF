using Microsoft.CodeAnalysis;
using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.Activities.Utils
{
    internal static class MetadataReferenceUtils
    {
        private static readonly Lazy<ConcurrentDictionary<Assembly, MetadataReference>> _metadataReferences = new();

        /// <summary>
        /// Retrieves the metadata reference and caches it for faster future retrieval
        /// Will not cache if assembly is dynamic or collectible.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The MetadataReference for the given assembly</returns>
        internal static MetadataReference GetMetadataReferenceForAssembly(Assembly assembly)
        {
            MetadataReference meta = null;
            if (assembly != null && !_metadataReferences.Value.TryGetValue(assembly, out meta))
            {
                meta = GetMetadataReference(assembly);
                if (meta != null && CanCache(assembly))
                {
                    _metadataReferences.Value.TryAdd(assembly, meta);
                }
            }

            return meta;
        }

        internal static IReadOnlyCollection<MetadataReference> GetMetadataReferences(IEnumerable<AssemblyReference> assemblyReferences, Func<AssemblyName, MetadataReference> resolver = null)
        {
            var result = new List<MetadataReference>();
            foreach (var assemblyReference in assemblyReferences.OfType<AssemblyReference>())
            {
                var reference = GetDesignTimeMetadataReference(assemblyReference, resolver)
                    ?? GetMetadataReferenceForAssembly(assemblyReference.Assembly ?? LoadAssemblyFromReference(assemblyReference));
                if (reference is not null)
                {
                    result.Add(reference);
                }
            }
            return result;
        }

        /// <summary>
        ///     The metadata that the resolver of the compile's set gives for a reference with no assembly, so the reference needs
        ///     no load. See <see cref="DesignTimeMetadataReferences.HostReferences"/>.
        /// </summary>
        /// <remarks>With no resolver, or one that fails, the reference loads by its name.</remarks>
        internal static MetadataReference GetDesignTimeMetadataReference(AssemblyReference assemblyReference, Func<AssemblyName, MetadataReference> resolver)
        {
            if (assemblyReference.Assembly is not null || assemblyReference.AssemblyName is null)
            {
                return null;
            }

            return Resolve(resolver, assemblyReference.AssemblyName);
        }

        // A resolver that fails gives no metadata.
        internal static MetadataReference Resolve(Func<AssemblyName, MetadataReference> resolver, AssemblyName assemblyName)
        {
            try
            {
                return resolver?.Invoke(assemblyName);
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
        }

        internal static IReadOnlyCollection<MetadataReference> GetMetadataReferences(IEnumerable<Assembly> assemblies)
        {
            var result = new List<MetadataReference>();
            foreach (var assembly in assemblies)
            {
                var reference = GetMetadataReferenceForAssembly(assembly);
                if (reference is not null)
                {
                    result.Add(reference);
                }
            }
            return result;
        }

        private static bool CanCache(Assembly assembly)
            => !assembly.IsCollectible && !assembly.IsDynamic;

        private static MetadataReference GetMetadataReference(Assembly assembly)
        {
            if (assembly == null)
            {
                return null;
            }

            try
            {
                return References.GetReference(assembly);
            }
            catch (NotSupportedException) { }
            catch (NotImplementedException) { }

            if (!string.IsNullOrWhiteSpace(assembly.Location))
            {
                try
                {
                    return MetadataReference.CreateFromFile(assembly.Location);
                }
                catch (IOException) { }
                catch (NotSupportedException) { }
            }

            return null;
        }

        /// <summary>
        ///     If <see cref="AssemblyReference.Assembly"/> is null, loads the assembly. Default is to
        ///     call <see cref="AssemblyReference.LoadAssembly"/>.
        /// </summary>
        /// <param name="assemblyReference"></param>
        /// <returns></returns>
        private static Assembly LoadAssemblyFromReference(AssemblyReference assemblyReference)
        {
            assemblyReference.LoadAssembly();
            return assemblyReference.Assembly;
        }
    }
}

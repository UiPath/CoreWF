using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace UiPath.Workflow.Validation
{
    internal sealed class ExternalMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly Func<AssemblyName, Assembly> _resolver;

        private static readonly ConcurrentDictionary<string, PortableExecutableReference> _referenceCache = new();

        public override bool ResolveMissingAssemblies => true;

        public ExternalMetadataReferenceResolver(Func<AssemblyName, Assembly> resolver)
        {
            _resolver = resolver;
        }

        public override PortableExecutableReference? ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            try
            {
                if (referenceIdentity is null || _resolver is null)
                    return null;

                var assemblyName = new AssemblyName
                {
                    Name = referenceIdentity.Name,
                    Version = referenceIdentity.Version,
                    CultureName = referenceIdentity.CultureName,
                };

                assemblyName.SetPublicKeyToken(referenceIdentity.PublicKeyToken.ToArray());

                var assembly = _resolver.Invoke(assemblyName);

                if (string.IsNullOrEmpty(assembly?.Location))
                    return null;

                if (_referenceCache.TryGetValue(assembly.Location, out var cachedReference))
                {
                    return cachedReference;
                }

                var reference = MetadataReference.CreateFromFile(assembly.Location);
                _referenceCache.TryAdd(assembly.Location, reference);

                return reference;
            }
            catch
            {
                // In case of any exception, return null
                return null;
            }
        }

        public static void ClearReferenceCache() => _referenceCache.Clear();

        public override ImmutableArray<PortableExecutableReference> ResolveReference(
            string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        public override bool Equals(object? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => GetType().GetHashCode();
    }
}

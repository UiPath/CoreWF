using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace UiPath.Workflow.Validation
{
    internal sealed class ExternalMetadataReferenceResolver : MetadataReferenceResolver
    {
        private readonly Func<AssemblyName, Assembly> _resolver;

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

                return MetadataReference.CreateFromFile(assembly.Location);
            }
            catch
            {
                // In case of any exception, return null
                return null;
            }
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(
            string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            return ImmutableArray<PortableExecutableReference>.Empty;
        }

        public override bool Equals(object? other) => ReferenceEquals(this, other);
        public override int GetHashCode() => GetType().GetHashCode();
    }
}

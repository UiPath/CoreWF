// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Markup;
using System.Xaml;
using System.Xml.Linq;

namespace Microsoft.VisualBasic.Activities.XamlIntegration;

internal static class VisualBasicExpressionConverter
{
    private static readonly Regex s_assemblyQualifiedNamespaceRegex = new(
        "clr-namespace:(?<namespace>[^;]*);assembly=(?<assembly>.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static VisualBasicSettings CollectXmlNamespacesAndAssemblies(ITypeDescriptorContext context)
    {
        // access XamlSchemaContext.ReferenceAssemblies 
        // for the Compiled Xaml scenario
        IList<Assembly> xsCtxReferenceAssemblies = null;
        if (context.GetService(typeof(IXamlSchemaContextProvider)) is IXamlSchemaContextProvider xamlSchemaContextProvider && xamlSchemaContextProvider.SchemaContext != null)
        {
            xsCtxReferenceAssemblies = xamlSchemaContextProvider.SchemaContext.ReferenceAssemblies;
            if (xsCtxReferenceAssemblies is {Count: 0})
            {
                xsCtxReferenceAssemblies = null;
            }
        }

        VisualBasicSettings settings = null;
        var namespaceResolver = (IXamlNamespaceResolver) context.GetService(typeof(IXamlNamespaceResolver));

        if (namespaceResolver == null)
        {
            return null;
        }

        lock (AssemblyCache.XmlnsMappingsLockObject)
        {
            // Fetch xmlnsMappings for the prefixes returned by the namespaceResolver service

            foreach (var prefix in namespaceResolver.GetNamespacePrefixes())
            {
                WrapCachedMapping(prefix, out var mapping);
                if (mapping.IsEmpty)
                {
                    continue;
                }

                settings ??= new VisualBasicSettings();

                foreach (var importReference in mapping.ImportReferences)
                {
                    if (xsCtxReferenceAssemblies != null)
                    {
                        // this is "compiled Xaml" 
                        VisualBasicImportReference newImportReference;

                        if (importReference.EarlyBoundAssembly != null)
                        {
                            if (xsCtxReferenceAssemblies.Contains(importReference.EarlyBoundAssembly))
                            {
                                newImportReference = importReference.Clone();
                                newImportReference.EarlyBoundAssembly = importReference.EarlyBoundAssembly;
                                settings.ImportReferences.Add(newImportReference);
                            }

                            continue;
                        }

                        foreach (var t in xsCtxReferenceAssemblies)
                        {
                            var xsCtxAssemblyName =
                                AssemblyReference.GetFastAssemblyName(t);
                            if (importReference.AssemblySatisfiesReference(xsCtxAssemblyName))
                            {
                                // bind this assembly early to the importReference
                                // so later AssemblyName resolution can be skipped
                                newImportReference = importReference.Clone();
                                newImportReference.EarlyBoundAssembly = t;
                                settings.ImportReferences.Add(newImportReference);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // this is "loose Xaml"
                        var newImportReference = importReference.Clone();
                        if (importReference.EarlyBoundAssembly != null)
                            // VBImportReference.Clone() method deliberately doesn't copy 
                            // its EarlyBoundAssembly to the cloned instance.
                            // we need to explicitly copy the original's EarlyBoundAssembly
                        {
                            newImportReference.EarlyBoundAssembly = importReference.EarlyBoundAssembly;
                        }

                        settings.ImportReferences.Add(newImportReference);
                    }
                }
            }
        }

        return settings;
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical = "Critical because we are accessing critical member AssemblyCache.XmlnsMappings.",
        Safe =
            "Safe because we prevent partial trusted code from manipulating the cache directly by creating a read-only wrapper around the cached XmlnsMapping.")]
    [SecuritySafeCritical]
    private static void WrapCachedMapping(NamespaceDeclaration prefix, out ReadOnlyXmlnsMapping readOnlyMapping)
    {
        var xmlns = XNamespace.Get(prefix.Namespace);

        if (!AssemblyCache.XmlnsMappings.TryGetValue(xmlns, out var mapping))
        {
            // Match a namespace of the form "clr-namespace:<namespace-name>;assembly=<assembly-name>"

            var match = s_assemblyQualifiedNamespaceRegex.Match(prefix.Namespace);

            if (match.Success)
            {
                mapping.ImportReferences = new HashSet<VisualBasicImportReference>();
                mapping.ImportReferences.Add(
                    new VisualBasicImportReference
                    {
                        Assembly = match.Groups["assembly"].Value,
                        Import = match.Groups["namespace"].Value,
                        Xmlns = xmlns
                    });
            }
            else
            {
                mapping.ImportReferences = new HashSet<VisualBasicImportReference>();
            }

            AssemblyCache.XmlnsMappings[xmlns] = mapping;
        }

        // ReadOnlyXmlnsMapping constructor tolerates an empty mapping being passed in.
        readOnlyMapping = new ReadOnlyXmlnsMapping(mapping);
    }

    /// <summary>
    ///     Static class used to cache assembly metadata.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 XmlnsMappings for static assemblies are not GC'd. In v4.0 we can assume that all static assemblies
    ///                 containing XmlnsDefinition attributes are non-collectible. The CLR will provide no public mechanism
    ///                 for unloading a static assembly or specifying that a static assembly is collectible. While there
    ///                 may be some small number of assemblies identified by the CLR as collectible, none will contain
    ///                 XmlnsDefinition attributes. Should the CLR provide a public mechanism for unloading a static assembly
    ///                 or specifying that a static assembly is collectible, we should revisit this decision based on scenarios
    ///                 that flow from these mechanisms.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 XmlnsMappings for dynamic assemblies are not created. This is because the hosted Visual Basic compiler
    ///                 does not support dynamic assembly references. Should support for dynamic assembly references be
    ///                 added to the Visual Basic compiler, we should strip away Assembly.IsDynamic checks from this class and
    ///                 update the code ensure that VisualBasicImportReference instances are removed in a timely manner.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    private static class AssemblyCache
    {
        private static bool s_initialized;

        // This is here so that obtaining the lock is not required to be SecurityCritical.
        public static readonly object XmlnsMappingsLockObject = new();

        [Fx.Tag.SecurityNoteAttribute(Critical =
            "Critical because we are storing assembly references and if we allowed PT access, they could mess with that.")]
        [SecurityCritical]
        private static Dictionary<XNamespace, XmlnsMapping> s_xmlnsMappings;

        public static Dictionary<XNamespace, XmlnsMapping> XmlnsMappings
        {
            [Fx.Tag.SecurityNoteAttribute(Critical =
                "Critical because providing access to the critical xmlnsMappings dictionary.")]
            [SecurityCritical]
            get
            {
                EnsureInitialized();
                return s_xmlnsMappings;
            }
        }

        [Fx.Tag.SecurityNoteAttribute(Critical =
            "Critical because we are accessing critical member xmlnsMappings and CacheLoadedAssembly. Only called from CLR.")]
        [SecurityCritical]
        private static void OnAssemblyLoaded(object sender, AssemblyLoadEventArgs args)
        {
            var assembly = args.LoadedAssembly;

            if (assembly.IsDefined(typeof(XmlnsDefinitionAttribute), false) && !assembly.IsDynamic)
            {
                lock (XmlnsMappingsLockObject)
                {
                    CacheLoadedAssembly(assembly);
                }
            }
        }

        [Fx.Tag.SecurityNoteAttribute(Critical =
            "Critical because we are accessing AppDomain.AssemblyLoaded and we are accessing critical member xmlnsMappings.")]
        [SecurityCritical]
        private static void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            if (s_xmlnsMappings == null)
            {
                Interlocked.CompareExchange(ref s_xmlnsMappings,
                    new Dictionary<XNamespace, XmlnsMapping>(new XNamespaceEqualityComparer()),
                    null);
            }

            lock (XmlnsMappingsLockObject)
            {
                if (s_initialized)
                {
                    return;
                }

                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    if (assembly.IsDefined(typeof(XmlnsDefinitionAttribute), false) && !assembly.IsDynamic)
                    {
                        CacheLoadedAssembly(assembly);
                    }
                }

                s_initialized = true;
            }
        }

        [Fx.Tag.SecurityNoteAttribute(Critical = "Critical because we are accessing critical member xmlnsMappings.")]
        [SecurityCritical]
        private static void CacheLoadedAssembly(Assembly assembly)
        {
            // this VBImportReference is only used as an entry to the xmlnsMappings cache
            // and is never meant to be Xaml serialized.
            // those VBImportReferences that are to be Xaml serialized are created by Clone() method.
            var attributes =
                (XmlnsDefinitionAttribute[]) assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), false);
            var assemblyName = assembly.FullName;

            foreach (var t in attributes)
            {
                var xmlns = XNamespace.Get(t.XmlNamespace);

                if (!s_xmlnsMappings.TryGetValue(xmlns, out var mapping))
                {
                    mapping.ImportReferences = new HashSet<VisualBasicImportReference>();
                    s_xmlnsMappings[xmlns] = mapping;
                }

                var newImportReference = new VisualBasicImportReference
                {
                    Assembly = assemblyName,
                    Import = t.ClrNamespace,
                    Xmlns = xmlns,
                    // this leads to the short-cut, skipping the normal assembly resolution routine
                    // early binding the assembly
                    EarlyBoundAssembly = assembly
                };
                mapping.ImportReferences.Add(newImportReference);
            }
        }

        private class XNamespaceEqualityComparer : IEqualityComparer<XNamespace>
        {
            bool IEqualityComparer<XNamespace>.Equals(XNamespace x, XNamespace y) => x == y;

            int IEqualityComparer<XNamespace>.GetHashCode(XNamespace x) => x.GetHashCode();
        }
    }

    /// <summary>
    ///     Struct used to cache XML Namespace mappings.
    /// </summary>
    private struct XmlnsMapping
    {
        public HashSet<VisualBasicImportReference> ImportReferences;

        public bool IsEmpty => ImportReferences == null || ImportReferences.Count == 0;
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical =
            "Critical because we are accessing a XmlnsMapping that is stored in the XmlnsMappings cache, which is SecurityCritical.",
        Safe = "Safe because we are wrapping the XmlnsMapping and not allowing unsafe code to modify it.")]
    [SecuritySafeCritical]
    private struct ReadOnlyXmlnsMapping
    {
        private XmlnsMapping _wrappedMapping;

        internal ReadOnlyXmlnsMapping(XmlnsMapping mapping)
        {
            _wrappedMapping = mapping;
        }

        internal bool IsEmpty => _wrappedMapping.IsEmpty;

        internal IEnumerable<ReadOnlyVisualBasicImportReference> ImportReferences
        {
            get
            {
                foreach (var wrappedReference in _wrappedMapping.ImportReferences)
                {
                    yield return new ReadOnlyVisualBasicImportReference(wrappedReference);
                }
            }
        }
    }

    [Fx.Tag.SecurityNoteAttribute(
        Critical =
            "Critical because we are accessing a VisualBasicImportReference that is stored in the XmlnsMappings cache, which is SecurityCritical.",
        Safe =
            "Safe because we are wrapping the VisualBasicImportReference and not allowing unsafe code to modify it.")]
    [SecuritySafeCritical]
    private readonly struct ReadOnlyVisualBasicImportReference
    {
        private readonly VisualBasicImportReference _wrappedReference;

        internal ReadOnlyVisualBasicImportReference(VisualBasicImportReference referenceToWrap)
        {
            _wrappedReference = referenceToWrap;
        }

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //internal string Assembly
        //{
        //    get
        //    {
        //        return this.wrappedReference.Assembly;
        //    }
        //}

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //internal string Import
        //{
        //    get
        //    {
        //        return this.wrappedReference.Import;
        //    }
        //}

        internal Assembly EarlyBoundAssembly => _wrappedReference.EarlyBoundAssembly;

        internal VisualBasicImportReference Clone()
        {
            return _wrappedReference.Clone();
        }

        // this code is borrowed from XamlSchemaContext
        internal bool AssemblySatisfiesReference(AssemblyName assemblyName)
        {
            if (_wrappedReference.AssemblyName.Name != assemblyName.Name)
            {
                return false;
            }

            if (_wrappedReference.AssemblyName.Version != null &&
                !_wrappedReference.AssemblyName.Version.Equals(assemblyName.Version))
            {
                return false;
            }

            if (_wrappedReference.AssemblyName.CultureInfo != null &&
                !_wrappedReference.AssemblyName.CultureInfo.Equals(assemblyName.CultureInfo))
            {
                return false;
            }

            var requiredToken = _wrappedReference.AssemblyName.GetPublicKeyToken();
            if (requiredToken != null)
            {
                var actualToken = assemblyName.GetPublicKeyToken();
                if (!AssemblyNameEqualityComparer.IsSameKeyToken(requiredToken, actualToken))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _wrappedReference.GetHashCode();
        }

        // If this is ever needed, uncomment this. It is commented out now to avoid FxCop violation because it is not called.
        //public bool Equals(VisualBasicImportReference other)
        //{
        //    return this.wrappedReference.Equals(other);
        //}
    }
}

// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Portable.Xaml;
    using System.Activities.Internals;

    public class SerializableFuncDeferringLoader : XamlDeferringLoader
    {
        private const string xmlPrefix = "xml";

        public override object Load(XamlReader xamlReader, IServiceProvider context)
        {
            FuncFactory factory = FuncFactory.CreateFactory(xamlReader, context);
            if (context.GetService(typeof(IXamlNamespaceResolver)) is IXamlNamespaceResolver nsResolver)
            {
                factory.ParentNamespaces = nsResolver.GetNamespacePrefixes().ToList();
            }
            return factory.GetFunc();
        }

        public override XamlReader Save(object value, IServiceProvider serviceProvider)
        {
            FuncFactory factory = GetFactory(value as Delegate);
            if (factory == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SavingFuncToXamlNotSupported));
            }
            XamlReader result = factory.Nodes.GetReader();
            if (factory.ParentNamespaces != null)
            {
                result = InsertNamespaces(result, factory.ParentNamespaces);
            }
            return result;
        }

        private static FuncFactory GetFactory(Delegate func)
        {
            return (func != null) ? func.Target as FuncFactory : null;
        }

        // We don't know what namespaces are actually used inside convertible values, so any namespaces
        // that were in the parent scope on load need to be regurgitated on save, unless the prefixes were
        // shadowed in the child scope.
        // This can potentially cause namespace bloat, but the alternative is emitting unloadable XAML.
        private static XamlReader InsertNamespaces(XamlReader reader, IEnumerable<NamespaceDeclaration> parentNamespaces)
        {
            XamlNodeQueue namespaceNodes = new XamlNodeQueue(reader.SchemaContext);
            HashSet<string> childPrefixes = new HashSet<string>();
            while (reader.Read() && reader.NodeType == XamlNodeType.NamespaceDeclaration)
            {
                childPrefixes.Add(reader.Namespace.Prefix);
                namespaceNodes.Writer.WriteNode(reader);
            }
            foreach (NamespaceDeclaration parentNamespace in parentNamespaces)
            {
                if (!childPrefixes.Contains(parentNamespace.Prefix) &&
                    !IsXmlNamespace(parentNamespace))
                {
                    namespaceNodes.Writer.WriteNamespace(parentNamespace);
                }
            }
            if (!reader.IsEof)
            {
                namespaceNodes.Writer.WriteNode(reader);
            }
            return new ConcatenatingXamlReader(namespaceNodes.Reader, reader);
        }

        // We need to special-case the XML namespace because it is always in scope,
        // but can't actually be written to XML.
        private static bool IsXmlNamespace(NamespaceDeclaration namespaceDecl)
        {
            return namespaceDecl.Prefix == xmlPrefix && namespaceDecl.Namespace == XamlLanguage.Xml1998Namespace;
        }
    }
}



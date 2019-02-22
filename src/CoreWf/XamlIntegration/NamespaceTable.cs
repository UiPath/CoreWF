// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System.Collections.Generic;
    using Portable.Xaml;

    internal class NamespaceTable : IXamlNamespaceResolver
    {
        private List<NamespaceDeclaration> tempNamespaceList;
        private readonly Stack<List<NamespaceDeclaration>> namespaceStack;
        private Dictionary<string, NamespaceDeclaration> namespacesCache;

        public NamespaceTable()
        {
            this.tempNamespaceList = new List<NamespaceDeclaration>();
            this.namespaceStack = new Stack<List<NamespaceDeclaration>>();
        }

        public string GetNamespace(string prefix)
        {
            if (this.namespacesCache == null)
            {
                ConstructNamespaceCache();
            }

            if (this.namespacesCache.TryGetValue(prefix, out NamespaceDeclaration result))
            {
                return result.Namespace;
            }
            else
            {
                return null;
            }
        }

        public void ManageNamespace(XamlReader reader)
        {
            switch (reader.NodeType)
            {
                case XamlNodeType.NamespaceDeclaration:
                    AddNamespace(reader.Namespace);
                    break;
                case XamlNodeType.StartObject:
                case XamlNodeType.StartMember:
                case XamlNodeType.GetObject:
                    EnterScope();
                    break;
                case XamlNodeType.EndMember:
                case XamlNodeType.EndObject:
                    ExitScope();
                    break;
            }
        }

        public void AddNamespace(NamespaceDeclaration xamlNamespace)
        {
            this.tempNamespaceList.Add(xamlNamespace);
            this.namespacesCache = null;
        }

        public void EnterScope()
        {
            if (this.tempNamespaceList != null)
            {
                this.namespaceStack.Push(this.tempNamespaceList);
                this.tempNamespaceList = new List<NamespaceDeclaration>();
            }
        }

        public void ExitScope()
        {
            List<NamespaceDeclaration> namespaceList = this.namespaceStack.Pop();
            if (namespaceList.Count != 0)
            {
                this.namespacesCache = null;
            }
        }

        public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes()
        {
            if (this.namespacesCache == null)
            {
                ConstructNamespaceCache();
            }

            return this.namespacesCache.Values;
        }

        private void ConstructNamespaceCache()
        {
            Dictionary<string, NamespaceDeclaration> localNamespaces = new Dictionary<string, NamespaceDeclaration>();
            if (this.tempNamespaceList != null && this.tempNamespaceList.Count > 0)
            {
                foreach (NamespaceDeclaration tempNamespace in tempNamespaceList)
                {
                    if (!localNamespaces.ContainsKey(tempNamespace.Prefix))
                    {
                        localNamespaces.Add(tempNamespace.Prefix, tempNamespace);
                    }
                }
            }
            foreach (List<NamespaceDeclaration> currentNamespaces in this.namespaceStack)
            {
                foreach (NamespaceDeclaration currentNamespace in currentNamespaces)
                {
                    if (!localNamespaces.ContainsKey(currentNamespace.Prefix))
                    {
                        localNamespaces.Add(currentNamespace.Prefix, currentNamespace);
                    }
                }
            }
            this.namespacesCache = localNamespaces;
        }
    }
}

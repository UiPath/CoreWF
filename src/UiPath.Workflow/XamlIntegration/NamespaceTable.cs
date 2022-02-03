// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Xaml;

namespace System.Activities.XamlIntegration;

internal class NamespaceTable : IXamlNamespaceResolver
{
    private readonly Stack<List<NamespaceDeclaration>> _namespaceStack;
    private Dictionary<string, NamespaceDeclaration> _namespacesCache;
    private List<NamespaceDeclaration> _tempNamespaceList;

    public NamespaceTable()
    {
        _tempNamespaceList = new List<NamespaceDeclaration>();
        _namespaceStack = new Stack<List<NamespaceDeclaration>>();
    }

    public string GetNamespace(string prefix)
    {
        if (_namespacesCache == null)
        {
            ConstructNamespaceCache();
        }

        return _namespacesCache.TryGetValue(prefix, out var result) ? result.Namespace : null;
    }

    public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes()
    {
        if (_namespacesCache == null)
        {
            ConstructNamespaceCache();
        }

        return _namespacesCache.Values;
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
        _tempNamespaceList.Add(xamlNamespace);
        _namespacesCache = null;
    }

    public void EnterScope()
    {
        if (_tempNamespaceList != null)
        {
            _namespaceStack.Push(_tempNamespaceList);
            _tempNamespaceList = new List<NamespaceDeclaration>();
        }
    }

    public void ExitScope()
    {
        var namespaceList = _namespaceStack.Pop();
        if (namespaceList.Count != 0)
        {
            _namespacesCache = null;
        }
    }

    private void ConstructNamespaceCache()
    {
        var localNamespaces = new Dictionary<string, NamespaceDeclaration>();
        if (_tempNamespaceList is {Count: > 0})
        {
            foreach (var tempNamespace in _tempNamespaceList.Where(tempNamespace =>
                         !localNamespaces.ContainsKey(tempNamespace.Prefix)))
            {
                localNamespaces.Add(tempNamespace.Prefix, tempNamespace);
            }
        }

        foreach (var currentNamespaces in _namespaceStack)
        foreach (var currentNamespace in currentNamespaces.Where(currentNamespace =>
                     !localNamespaces.ContainsKey(currentNamespace.Prefix)))
        {
            localNamespaces.Add(currentNamespace.Prefix, currentNamespace);
        }

        _namespacesCache = localNamespaces;
    }
}

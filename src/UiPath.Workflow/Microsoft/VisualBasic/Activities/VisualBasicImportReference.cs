// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities.Expressions;
using System.Globalization;
using System.Reflection;
using System.Xaml;
using System.Xml.Linq;

namespace Microsoft.VisualBasic.Activities;

public class VisualBasicImportReference : IEquatable<VisualBasicImportReference>
{
    private static readonly AssemblyNameEqualityComparer s_equalityComparer = new();
    private string _assemblyNameString;
    private int _hashCode;
    private string _import;

    public string Assembly
    {
        get => _assemblyNameString;

        set
        {
            if (value == null)
            {
                AssemblyName = null;
                _assemblyNameString = null;
            }
            else
            {
                // FileLoadException thrown from this ctor indicates invalid assembly name
                AssemblyName = new AssemblyName(value);
                _assemblyNameString = AssemblyName.FullName;
            }

            EarlyBoundAssembly = null;
        }
    }

    public string Import
    {
        get => _import;
        set
        {
            if (value != null)
            {
                _import = value.Trim();
                _hashCode = _import.ToUpperInvariant().GetHashCode();
            }
            else
            {
                _import = null;
                _hashCode = 0;
            }

            EarlyBoundAssembly = null;
        }
    }

    internal AssemblyName AssemblyName { get; private set; }

    internal XNamespace Xmlns { get; set; }

    // for the short-cut assembly resolution
    // from VBImportReference.AssemblyName ==> System.Reflection.Assembly
    // this is an internal state that implies the context in which a VB assembly resolution is progressing
    // once VB extracted this Assembly object to pass onto the compiler, 
    // it must explicitly set this property back to null.
    // Clone() will also explicitly set this property of the new to null to prevent users from inadvertently 
    // creating a copy of VBImportReference that might not resolve to the assembly of his or her intent.
    internal Assembly EarlyBoundAssembly { get; set; }

    public bool Equals(VisualBasicImportReference other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (EarlyBoundAssembly != other.EarlyBoundAssembly)
        {
            return false;
        }

        // VB does case insensitive comparisons for imports
        if (string.Compare(Import, other.Import, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        // now compare the assemblies
        if (AssemblyName == null && other.AssemblyName == null)
        {
            return true;
        }

        if (AssemblyName == null && other.AssemblyName != null)
        {
            return false;
        }

        if (AssemblyName != null && other.AssemblyName == null)
        {
            return false;
        }

        return s_equalityComparer.Equals(AssemblyName, other.AssemblyName);
    }

    internal VisualBasicImportReference Clone()
    {
        var toReturn = (VisualBasicImportReference) MemberwiseClone();
        toReturn.EarlyBoundAssembly = null;
        // Also make a clone of the AssemblyName.
        toReturn.AssemblyName = (AssemblyName) AssemblyName.Clone();
        return toReturn;
    }


    public override int GetHashCode()
    {
        return _hashCode;
    }

    internal void GenerateXamlNamespace(INamespacePrefixLookup namespaceLookup)
    {
        // promote reference to xmlns declaration
        string xamlNamespace = null;
        if (Xmlns != null && !string.IsNullOrEmpty(Xmlns.NamespaceName))
        {
            xamlNamespace = Xmlns.NamespaceName;
        }
        else
        {
            xamlNamespace = string.Format(CultureInfo.InvariantCulture, "clr-namespace:{0};assembly={1}", Import,
                Assembly);
        }

        // we don't need the return value since we just want to register the namespace/assembly pair
        namespaceLookup.LookupPrefix(xamlNamespace);
    }
}

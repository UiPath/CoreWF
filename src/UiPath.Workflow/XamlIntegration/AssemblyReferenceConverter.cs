// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xaml;

namespace System.Activities.XamlIntegration;

public class AssemblyReferenceConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is not string stringValue)
        {
            return base.ConvertFrom(context, culture, value);
        }

        var result = new AssemblyReference
        {
            AssemblyName = new AssemblyName(stringValue)
        };

        var schemaContext = GetSchemaContext(context);
        if (schemaContext is {ReferenceAssemblies: {Count: > 0}})
        {
            var assembly = ResolveAssembly(result.AssemblyName, schemaContext.ReferenceAssemblies);
            if (assembly != null)
            {
                result.Assembly = assembly;
            }
            else
                // SchemaContext.ReferenceAssemblies is an exclusive list.
                // Disallow referencing assemblies that are not included in the list.
            {
                result = null;
            }
        }

        return result;

    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
        Type destinationType)
    {
        if (destinationType != typeof(string) || value is not AssemblyReference reference)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        if (reference.AssemblyName != null)
        {
            return reference.AssemblyName.ToString();
        }

        if (reference.Assembly == null)
        {
            return null;
        }

        var schemaContext = GetSchemaContext(context);
        if (schemaContext == null || schemaContext.FullyQualifyAssemblyNamesInClrNamespaces)
        {
            return reference.Assembly.FullName;
        }

        var assemblyName = AssemblyReference.GetFastAssemblyName(reference.Assembly);
        return assemblyName.Name;
    }

    private static XamlSchemaContext GetSchemaContext(ITypeDescriptorContext context)
    {
        return context.GetService(typeof(IXamlSchemaContextProvider)) is IXamlSchemaContextProvider provider
            ? provider.SchemaContext
            : null;
    }

    private static Assembly ResolveAssembly(AssemblyName assemblyReference, IEnumerable<Assembly> assemblies)
    {
        return (from assembly in assemblies
                let assemblyName = AssemblyReference.GetFastAssemblyName(assembly)
                where AssemblyReference.AssemblySatisfiesReference(assemblyName, assemblyReference)
                select assembly).FirstOrDefault();
    }
}

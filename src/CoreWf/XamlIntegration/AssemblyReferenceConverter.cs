// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using CoreWf.Expressions;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;
    using Portable.Xaml;

    public class AssemblyReferenceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                AssemblyReference result = new AssemblyReference
                {
                    AssemblyName = new AssemblyName(stringValue)
                };

                XamlSchemaContext schemaContext = GetSchemaContext(context);
                if (schemaContext != null &&
                    schemaContext.ReferenceAssemblies != null &&
                    schemaContext.ReferenceAssemblies.Count > 0)
                {
                    Assembly assembly = ResolveAssembly(result.AssemblyName, schemaContext.ReferenceAssemblies);
                    if (assembly != null)
                    {
                        result.Assembly = assembly;
                    }
                    else
                    {
                        // SchemaContext.ReferenceAssemblies is an exclusive list.
                        // Disallow referencing assemblies that are not included in the list.
                        result = null;
                    }
                }

                return result;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            AssemblyReference reference = value as AssemblyReference;
            if (destinationType == typeof(string) && reference != null)
            {
                if (reference.AssemblyName != null)
                {
                    return reference.AssemblyName.ToString();
                }
                else if (reference.Assembly != null)
                {
                    XamlSchemaContext schemaContext = GetSchemaContext(context);
                    if (schemaContext == null || schemaContext.FullyQualifyAssemblyNamesInClrNamespaces)
                    {
                        return reference.Assembly.FullName;
                    }
                    else
                    {
                        AssemblyName assemblyName = AssemblyReference.GetFastAssemblyName(reference.Assembly);
                        return assemblyName.Name;
                    }
                }
                else
                {
                    return null;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        private static XamlSchemaContext GetSchemaContext(ITypeDescriptorContext context)
        {
            return context.GetService(typeof(IXamlSchemaContextProvider)) is IXamlSchemaContextProvider provider ? provider.SchemaContext : null;
        }

        private static Assembly ResolveAssembly(AssemblyName assemblyReference, IEnumerable<Assembly> assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                AssemblyName assemblyName = AssemblyReference.GetFastAssemblyName(assembly);
                if (AssemblyReference.AssemblySatisfiesReference(assemblyName, assemblyReference))
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}

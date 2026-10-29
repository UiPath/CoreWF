// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Activities
{
    internal abstract class ExpressionCompiler
    {
        public Compilation Compile(string expressionText, bool isLocation, Type returnType, IReadOnlyCollection<string> namespaces, IReadOnlyCollection<AssemblyReference> referencedAssemblies, LocationReferenceEnvironment environment)
        {
            var syntaxTree = GetSyntaxTreeForExpression(expressionText, isLocation, returnType, environment);
            // A host that gives its reference set gets no default assembly, see DesignTimeMetadataReferences.HostReferences.
            var assemblies = (IReadOnlyCollection<AssemblyReference>)DesignTimeMetadataReferences.GetHostReferences(referencedAssemblies)
                ?? JitCompilerHelper.DefaultReferencedAssemblies.Select(a => (AssemblyReference)a).Union(referencedAssemblies).ToList();
            return GetCompilation(assemblies, namespaces).AddSyntaxTrees(syntaxTree);
        }

        public abstract Type GetReturnType(Compilation compilation);

        protected static Assembly GetAssemblyForType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayTypeSymbol)
            {
                return AssemblyReference.GetAssembly(new AssemblyName(GetAssemblyForType(arrayTypeSymbol.ElementType).GetName().Name));
            }
            else
            {
                return AssemblyReference.GetAssembly(new AssemblyName(type.ContainingAssembly.Name));
            }
        }

        protected static Type GetSystemType(ITypeSymbol typeSymbol, Assembly assembly)
        {
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                return GetSystemType(arrayTypeSymbol.ElementType, assembly).MakeArrayType();
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsGenericType)
                {
                    return assembly.GetType($"{namedTypeSymbol.ContainingNamespace.ToDisplayString()}.{namedTypeSymbol.MetadataName}").MakeGenericType(namedTypeSymbol.TypeArguments.Select(t => GetSystemType(t, GetAssemblyForType(t))).ToArray());
                }

                if (namedTypeSymbol.IsAnonymousType)
                {
                    return typeof(object);
                }
            }

            return assembly.GetType($"{typeSymbol.ContainingNamespace.ToDisplayString()}.{typeSymbol.MetadataName}");
        }

        protected abstract Compilation GetCompilation(IReadOnlyCollection<AssemblyReference> assemblies, IReadOnlyCollection<string> namespaces);

        protected abstract SyntaxTree GetSyntaxTreeForExpression(string expression, bool isLocation, Type returnType, LocationReferenceEnvironment environment);
    }
}
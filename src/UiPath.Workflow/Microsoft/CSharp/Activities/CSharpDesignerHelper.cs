// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CSharp.Activities
{
    internal class CSharpHelper : JitCompilerHelper<CSharpHelper>
    {
        public CSharpHelper(string expressionText) : base(expressionText) { }
        public CSharpHelper(string expressionText, HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }

        protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => new CSharpJitCompiler(references);

        public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            List<string> localNamespaces;
            List<AssemblyReference> localAssemblies;
            GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity, false, out localNamespaces, out localAssemblies);
            var helper = new CSharpHelper(expressionText);
            HashSet<AssemblyName> localReferenceAssemblies = new HashSet<AssemblyName>();
            HashSet<string> localImports = new HashSet<string>(localNamespaces);
            foreach (AssemblyReference assemblyReference in localAssemblies)
            {
                if (assemblyReference.Assembly != null)
                {
                    // directly add the Assembly to the list
                    // so that we don't have to go through 
                    // the assembly resolution process
                    if (helper.referencedAssemblies == null)
                    {
                        helper.referencedAssemblies = new HashSet<Assembly>();
                    }
                    helper.referencedAssemblies.Add(assemblyReference.Assembly);
                }
                else if (assemblyReference.AssemblyName != null)
                {
                    localReferenceAssemblies.Add(assemblyReference.AssemblyName);
                }
            }
            helper.Initialize(localReferenceAssemblies, localImports);
            return helper.Compile<T>(publicAccessor, isLocationExpression);
        }
        internal static string Language => "C#";
    }

    class CSharpExpressionFactory<T> : ExpressionFactory
    {
        public override Activity CreateReference(string expressionText) => new CSharpReference<T>(expressionText);
        public override Activity CreateValue(string expressionText) => new CSharpValue<T>(expressionText);
    }
    class CSharpDesignerHelperImpl : DesignerHelperImpl
    {
        public override Type ExpressionFactoryType => typeof(CSharpExpressionFactory<>);
        public override string Language => CSharpHelper.Language;
        public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyName> references, HashSet<string> namespaces) =>
            new CSharpHelper(expressionText, references, namespaces);
    }
    public static class CSharpDesignerHelper
    {
        static readonly DesignerHelperImpl Impl = new CSharpDesignerHelperImpl();
        public static Activity RecompileValue(ActivityWithResult rValue, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.RecompileValue(rValue, out returnType, out compileError, out vbSettings);
        public static Activity RecompileReference(ActivityWithResult lValue, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.RecompileReference(lValue, out returnType, out compileError, out vbSettings);
        public static Activity CreatePrecompiledValue(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.CreatePrecompiledValue(targetType, expressionText, namespaces, referencedAssemblies, environment, out returnType, out compileError, out vbSettings);
        public static Activity CreatePrecompiledReference(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies, environment, out returnType, out compileError, out vbSettings);
    }
}
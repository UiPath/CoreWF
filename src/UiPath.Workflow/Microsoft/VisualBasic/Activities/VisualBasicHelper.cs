// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.VisualBasic.Activities
{
    class VisualBasicHelper : CompilerHelper
    {
        public VisualBasicHelper(string expressionText, HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }
        VisualBasicHelper(string expressionText) : base(expressionText) { }

        protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => VisualBasicSettings.CreateCompiler(references);

        internal static string Language
        {
            get
            {
                return "VB";
            }
        }

        public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            List<string> localNamespaces;
            List<AssemblyReference> localAssemblies;
            GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity,
                false, out localNamespaces, out localAssemblies);

            var helper = new VisualBasicHelper(expressionText);
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
    }
}
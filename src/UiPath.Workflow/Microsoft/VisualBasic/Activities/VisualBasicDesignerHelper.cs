﻿// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.VisualBasic.Activities
{
    class VisualBasicHelper : JitCompilerHelper<VisualBasicHelper>
    {
        public VisualBasicHelper(string expressionText, HashSet<AssemblyName> refAssemNames, HashSet<string> namespaceImportsNames) : base(expressionText, refAssemNames, namespaceImportsNames) { }
        VisualBasicHelper(string expressionText) : base(expressionText) { }
        protected override JustInTimeCompiler CreateCompiler(HashSet<Assembly> references) => VisualBasicSettings.CreateCompiler(references);
        internal static string Language => "VB";
        public static Expression<Func<ActivityContext, T>> Compile<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor, bool isLocationExpression)
        {
            var helper = InitializeHelper(expressionText, publicAccessor);
            return helper.Compile<T>(publicAccessor, isLocationExpression);
        }
        
        /// <summary>
        /// Validates the expression to get the diagnostic errors. Does not do a full compilation.
        /// </summary>
        /// <typeparam name="T">Type of the value expected to return from the expression</typeparam>
        /// <param name="expressionText">Expression text</param>
        /// <param name="publicAccessor">Contains information on the environment where the expression is located</param>
        /// <returns>An enumerable of all warning or error diagnostic messages</returns>
        public static IEnumerable<ValidationError> Validate<T>(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            var helper = InitializeHelper(expressionText, publicAccessor);
            return helper.Validate<T>(publicAccessor.ActivityMetadata.Environment);
        }

        /// <remarks>
        /// This code is used by both <see cref="Compile{T}(string, CodeActivityPublicEnvironmentAccessor, bool)"/> and 
        /// <see cref="Validate{T}(string, CodeActivityPublicEnvironmentAccessor)"/>.
        /// </remarks>
        private static VisualBasicHelper InitializeHelper(string expressionText, CodeActivityPublicEnvironmentAccessor publicAccessor)
        {
            GetAllImportReferences(publicAccessor.ActivityMetadata.CurrentActivity, false, out List<string> localNamespaces, out List<AssemblyReference> localAssemblies);
            var helper = new VisualBasicHelper(expressionText);
            HashSet<AssemblyName> localReferenceAssemblies = new();
            HashSet<string> localImports = new(localNamespaces);
            foreach (AssemblyReference assemblyReference in localAssemblies)
            {
                if (assemblyReference.Assembly != null)
                {
                    // directly add the Assembly to the list
                    // so that we don't have to go through 
                    // the assembly resolution process
                    helper.referencedAssemblies ??= new HashSet<Assembly>();
                    helper.referencedAssemblies.Add(assemblyReference.Assembly);
                }
                else if (assemblyReference.AssemblyName != null)
                {
                    localReferenceAssemblies.Add(assemblyReference.AssemblyName);
                }
            }
            helper.Initialize(localReferenceAssemblies, localImports);
            return helper;
        }
    }
    class VisualBasicDesignerHelperImpl : DesignerHelperImpl
    {
        public override Type ExpressionFactoryType => typeof(VisualBasicExpressionFactory<>);
        public override string Language => VisualBasicHelper.Language;
        public override JitCompilerHelper CreateJitCompilerHelper(string expressionText, HashSet<AssemblyName> references, HashSet<string> namespaces) =>
            new VisualBasicHelper(expressionText, references, namespaces);

    }
    public static class VisualBasicDesignerHelper
    {
        static readonly DesignerHelperImpl Impl = new VisualBasicDesignerHelperImpl();
        // Returns the additional constraint for visual basic which enforces variable name shadowing for 
        // projects targeting 4.0 for backward compatibility. 
        public static Constraint NameShadowingConstraint { get; } = new VisualBasicNameShadowingConstraint();
        // Recompile the VBValue passed in, with its current LocationReferenceEnvironment context
        // in a weakly-typed manner (the argument VBValue's type argument is ignored)
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity RecompileVisualBasicValue(ActivityWithResult visualBasicValue, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.RecompileValue(visualBasicValue, out returnType, out compileError, out vbSettings);
        // Recompile the VBReference passed in, with its current LocationReferenceEnvironment context
        // in a weakly-typed manner (the argument VBReference's type argument is ignored)
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity RecompileVisualBasicReference(ActivityWithResult visualBasicReference, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.RecompileReference(visualBasicReference, out returnType, out compileError, out vbSettings);
        // create a pre-compiled VBValueExpression, and also provides expressin type back to the caller.
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidOutParameters,
        //    Justification = "Design has been approved")]
        public static Activity CreatePrecompiledVisualBasicValue(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.CreatePrecompiledValue(targetType, expressionText, namespaces, referencedAssemblies, environment, out returnType, out compileError, out vbSettings);
        public static Activity CreatePrecompiledVisualBasicReference(Type targetType, string expressionText, IEnumerable<string> namespaces, IEnumerable<string> referencedAssemblies,
            LocationReferenceEnvironment environment, out Type returnType, out SourceExpressionException compileError, out VisualBasicSettings vbSettings) =>
            Impl.CreatePrecompiledReference(targetType, expressionText, namespaces, referencedAssemblies, environment, out returnType, out compileError, out vbSettings);
        public static Activity CreatePrecompiledValue(Type targetType, string expressionText, Activity parent, out Type returnType, out SourceExpressionException compileError) =>
            Impl.CreatePrecompiledValue(targetType, expressionText, parent, out returnType, out compileError);
        public static Activity CreatePrecompiledReference(Type targetType, string expressionText, Activity parent, out Type returnType, out SourceExpressionException compileError) =>
            Impl.CreatePrecompiledReference(targetType, expressionText, parent, out returnType, out compileError);
    }
    class VisualBasicExpressionFactory<T> : ExpressionFactory
    {
        public override Activity CreateReference(string expressionText) => new VisualBasicReference<T>(expressionText);
        public override Activity CreateValue(string expressionText) => new VisualBasicValue<T>(expressionText);
    }
}